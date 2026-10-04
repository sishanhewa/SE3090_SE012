using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Employees;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly AppDbContext _context;

    public EmployeeService(IEmployeeRepository employeeRepository, AppDbContext context)
    {
        _employeeRepository = employeeRepository;
        _context = context;
    }

    public async Task<Result<EmployeeResponse>> CreateEmployeeAsync(Guid companyId, CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = new Employee
        {
            UserId = request.UserId,
            CompanyId = companyId,
            DepartmentId = request.DepartmentId,
            EmployeeNumber = request.EmployeeNumber,
            Position = request.Position,
            StartDate = request.StartDate,
            ApplicationId = request.ApplicationId,
            Status = EmployeeStatus.Onboarding
        };

        employee.StatusHistory.Add(new EmployeeStatusHistory
        {
            FromStatus = EmployeeStatus.Onboarding, // Starting status
            ToStatus = EmployeeStatus.Onboarding,
            Notes = "Employee onboarded."
        });

        await _employeeRepository.AddAsync(employee, cancellationToken);

        return Result<EmployeeResponse>.Success(MapToResponse(employee));
    }

    public async Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetEmployeeWithDetailsAsync(id, cancellationToken);
        if (employee == null)
            return Result<EmployeeResponse>.NotFound("Employee not found.");

        return Result<EmployeeResponse>.Success(MapToResponse(employee));
    }

    public async Task<Result<PagedResult<EmployeeResponse>>> GetEmployeesByCompanyAsync(Guid companyId, PaginationParams paginationParams, CancellationToken cancellationToken = default)
    {
        var result = await _employeeRepository.GetEmployeesAsync(paginationParams, companyId, null, null, cancellationToken);

        var response = new PagedResult<EmployeeResponse>(
            result.Items.Select(MapToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize
        );

        return Result<PagedResult<EmployeeResponse>>.Success(response);
    }

    public async Task<Result<EmployeeResponse>> UpdateEmployeeAsync(Guid id, Guid companyId, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);
        if (employee == null)
            return Result<EmployeeResponse>.NotFound("Employee not found.");

        if (employee.CompanyId != companyId)
            return Result<EmployeeResponse>.Forbidden();

        if (request.DepartmentId.HasValue) employee.DepartmentId = request.DepartmentId.Value;
        if (request.Position != null) employee.Position = request.Position;
        
        if (request.Status != null && Enum.TryParse<EmployeeStatus>(request.Status, true, out var newStatus))
        {
            if (employee.Status != newStatus)
            {
                employee.StatusHistory.Add(new EmployeeStatusHistory
                {
                    FromStatus = employee.Status,
                    ToStatus = newStatus,
                    Notes = "Status updated via API."
                });
                employee.Status = newStatus;
            }
        }

        await _employeeRepository.UpdateAsync(employee, cancellationToken);

        return Result<EmployeeResponse>.Success(MapToResponse(employee));
    }

    private static EmployeeResponse MapToResponse(Employee employee)
    {
        return new EmployeeResponse
        {
            Id = employee.Id,
            UserId = employee.UserId,
            Name = employee.User?.FirstName + " " + employee.User?.LastName,
            CompanyId = employee.CompanyId,
            DepartmentId = employee.DepartmentId,
            EmployeeNumber = employee.EmployeeNumber,
            Position = employee.Position,
            StartDate = employee.StartDate,
            Status = employee.Status.ToString(),
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }
    public async Task<Result<EmployeeResponse>> CreateEmployeeFromHireAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        // Fetch the application with all related data
        var application = await _context.Applications
            .Include(a => a.Job)
                .ThenInclude(j => j.Company)
            .Include(a => a.CandidateProfile)
                .ThenInclude(cp => cp.User)
            .Include(a => a.Offer)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
            return Result<EmployeeResponse>.NotFound("Application not found.");

        // Validate application status is Hired
        if (application.Status != ApplicationStatus.Hired)
            return Result<EmployeeResponse>.Failure(
                $"Application must be in 'Hired' status to create an employee. Current status: {application.Status}");

        // Validate offer exists and is accepted
        if (application.Offer == null || application.Offer.Status != OfferStatus.Accepted)
            return Result<EmployeeResponse>.Failure(
                "Application must have an accepted offer before creating an employee.");

        // Check employee doesn't already exist for this application
        var existingEmployee = await _context.Employees
            .AnyAsync(e => e.ApplicationId == applicationId, cancellationToken);

        if (existingEmployee)
            return Result<EmployeeResponse>.Conflict("An employee has already been created from this application.");

        // Generate unique employee number
        var employeeNumber = await GenerateEmployeeNumberAsync(application.Job.CompanyId, cancellationToken);

        // Create the employee
        var employee = new Employee
        {
            UserId = application.CandidateProfile.UserId,
            CompanyId = application.Job.CompanyId,
            DepartmentId = application.Job.DepartmentId,
            EmployeeNumber = employeeNumber,
            Position = application.Offer.Position,
            StartDate = application.Offer.StartDate,
            Status = EmployeeStatus.Onboarding,
            ApplicationId = applicationId
        };

        employee.StatusHistory.Add(new EmployeeStatusHistory
        {
            FromStatus = EmployeeStatus.Onboarding,
            ToStatus = EmployeeStatus.Onboarding,
            ChangedBy = "System",
            Notes = $"Employee created from accepted offer for '{application.Job.Title}' position.",
            ChangedAt = DateTime.UtcNow
        });

        await _employeeRepository.AddAsync(employee, cancellationToken);

        // Auto-assign onboarding template if one exists for the company
        await AutoAssignOnboardingAsync(employee.Id, application.Job.CompanyId, cancellationToken);

        // Reload with details for response
        var savedEmployee = await _employeeRepository.GetEmployeeWithDetailsAsync(employee.Id, cancellationToken);
        return Result<EmployeeResponse>.Success(MapToResponse(savedEmployee ?? employee));
    }

    private async Task<string> GenerateEmployeeNumberAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var count = await _context.Employees
            .Where(e => e.CompanyId == companyId)
            .CountAsync(cancellationToken);

        var number = $"EMP-{count + 1:D4}";

        // Ensure uniqueness
        while (await _employeeRepository.EmployeeNumberExistsAsync(number, cancellationToken))
        {
            count++;
            number = $"EMP-{count + 1:D4}";
        }

        return number;
    }

    private async Task AutoAssignOnboardingAsync(Guid employeeId, Guid companyId, CancellationToken cancellationToken)
    {
        // Find the first onboarding template for the company
        var template = await _context.OnboardingTemplates
            .Include(t => t.Tasks)
            .Where(t => t.CompanyId == companyId)
            .OrderBy(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (template == null || !template.Tasks.Any())
            return;

        var employeeTasks = template.Tasks.Select(t => new EmployeeOnboardingTask
        {
            EmployeeId = employeeId,
            OnboardingTaskId = t.Id,
            IsCompleted = false
        }).ToList();

        _context.EmployeeOnboardingTasks.AddRange(employeeTasks);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<EmployeeResponse>> GetEmployeeByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByUserIdAsync(userId, cancellationToken);
        if (employee == null)
            return Result<EmployeeResponse>.NotFound("Employee profile not found.");

        // Reload with full details
        var detailed = await _employeeRepository.GetEmployeeWithDetailsAsync(employee.Id, cancellationToken);
        return Result<EmployeeResponse>.Success(MapToResponse(detailed ?? employee));
    }

    public async Task<Result<List<OnboardingTaskResponse>>> GetOnboardingTasksAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var tasks = await _context.EmployeeOnboardingTasks
            .Include(et => et.OnboardingTask)
            .Where(et => et.EmployeeId == employeeId)
            .OrderBy(et => et.OnboardingTask.SortOrder)
            .Select(et => new OnboardingTaskResponse
            {
                Id = et.Id,
                Title = et.OnboardingTask.Title,
                Description = et.OnboardingTask.Description,
                SortOrder = et.OnboardingTask.SortOrder,
                IsMandatory = et.OnboardingTask.IsMandatory
            })
            .ToListAsync(cancellationToken);

        return Result<List<OnboardingTaskResponse>>.Success(tasks);
    }
}