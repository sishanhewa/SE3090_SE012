using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Employees;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Services;

/// <summary>
/// Manages onboarding templates and employee onboarding task assignments.
/// </summary>
public class OnboardingService : IOnboardingService
{
    private readonly AppDbContext _context;

    public OnboardingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OnboardingTemplateResponse>> CreateTemplateAsync(
        Guid companyId, CreateOnboardingTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = new OnboardingTemplate
        {
            Name = request.Name,
            Description = request.Description,
            CompanyId = companyId
        };

        foreach (var taskReq in request.Tasks)
        {
            template.Tasks.Add(new OnboardingTask
            {
                Title = taskReq.Title,
                Description = taskReq.Description,
                SortOrder = taskReq.SortOrder,
                IsMandatory = taskReq.IsMandatory
            });
        }

        _context.OnboardingTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<OnboardingTemplateResponse>.Success(MapTemplateToResponse(template));
    }

    public async Task<Result<List<OnboardingTemplateResponse>>> GetTemplatesByCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        var templates = await _context.OnboardingTemplates
            .Include(t => t.Tasks)
            .Where(t => t.CompanyId == companyId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var response = templates.Select(MapTemplateToResponse).ToList();
        return Result<List<OnboardingTemplateResponse>>.Success(response);
    }

    public async Task<Result<List<EmployeeOnboardingTaskResponse>>> GetEmployeeOnboardingTasksAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        var tasks = await _context.EmployeeOnboardingTasks
            .Include(t => t.OnboardingTask)
            .Where(t => t.EmployeeId == employeeId)
            .OrderBy(t => t.OnboardingTask.SortOrder)
            .ToListAsync(cancellationToken);

        return Result<List<EmployeeOnboardingTaskResponse>>.Success(
            tasks.Select(MapTaskToResponse).ToList());
    }

    public async Task<Result<EmployeeOnboardingTaskResponse>> CompleteOnboardingTaskAsync(
        Guid taskId, CompleteOnboardingTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _context.EmployeeOnboardingTasks
            .Include(t => t.OnboardingTask)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null)
            return Result<EmployeeOnboardingTaskResponse>.NotFound("Onboarding task not found.");

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;
        task.Notes = request.Notes;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<EmployeeOnboardingTaskResponse>.Success(MapTaskToResponse(task));
    }

    public async Task<Result<List<EmployeeOnboardingTaskResponse>>> AssignOnboardingAsync(
        Guid employeeId, Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.OnboardingTemplates
            .Include(t => t.Tasks)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
            return Result<List<EmployeeOnboardingTaskResponse>>.NotFound("Template not found.");

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (employee == null)
            return Result<List<EmployeeOnboardingTaskResponse>>.NotFound("Employee not found.");

        var employeeTasks = template.Tasks.Select(t => new EmployeeOnboardingTask
        {
            EmployeeId = employeeId,
            OnboardingTaskId = t.Id,
            IsCompleted = false
        }).ToList();

        _context.EmployeeOnboardingTasks.AddRange(employeeTasks);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        var savedTasks = await _context.EmployeeOnboardingTasks
            .Include(t => t.OnboardingTask)
            .Where(t => t.EmployeeId == employeeId)
            .OrderBy(t => t.OnboardingTask.SortOrder)
            .ToListAsync(cancellationToken);

        return Result<List<EmployeeOnboardingTaskResponse>>.Success(
            savedTasks.Select(MapTaskToResponse).ToList());
    }

    private static OnboardingTemplateResponse MapTemplateToResponse(OnboardingTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        Description = template.Description,
        CompanyId = template.CompanyId,
        Tasks = template.Tasks.Select(t => new OnboardingTaskResponse
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            SortOrder = t.SortOrder,
            IsMandatory = t.IsMandatory
        }).OrderBy(t => t.SortOrder).ToList()
    };

    private static EmployeeOnboardingTaskResponse MapTaskToResponse(EmployeeOnboardingTask task) => new()
    {
        Id = task.Id,
        OnboardingTaskId = task.OnboardingTaskId,
        TaskTitle = task.OnboardingTask.Title,
        TaskDescription = task.OnboardingTask.Description,
        IsMandatory = task.OnboardingTask.IsMandatory,
        IsCompleted = task.IsCompleted,
        CompletedAt = task.CompletedAt,
        Notes = task.Notes
    };
}
