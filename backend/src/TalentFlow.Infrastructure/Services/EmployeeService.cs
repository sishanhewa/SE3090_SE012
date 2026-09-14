using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Employees;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
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
}
