using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Employees;

namespace TalentFlow.Application.Interfaces.Services;

/// <summary>
/// Service for managing onboarding templates and employee onboarding tasks.
/// </summary>
public interface IOnboardingService
{
    Task<Result<OnboardingTemplateResponse>> CreateTemplateAsync(
        Guid companyId, CreateOnboardingTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<List<OnboardingTemplateResponse>>> GetTemplatesByCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default);

    Task<Result<List<EmployeeOnboardingTaskResponse>>> GetEmployeeOnboardingTasksAsync(
        Guid employeeId, CancellationToken cancellationToken = default);

    Task<Result<EmployeeOnboardingTaskResponse>> CompleteOnboardingTaskAsync(
        Guid taskId, CompleteOnboardingTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<List<EmployeeOnboardingTaskResponse>>> AssignOnboardingAsync(
        Guid employeeId, Guid templateId,
        CancellationToken cancellationToken = default);
}
