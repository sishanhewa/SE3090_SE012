using TalentFlow.Application.Common;

using TalentFlow.Application.DTOs.Employees;

namespace TalentFlow.Application.Interfaces.Services;

public interface IEmployeeService
{
    Task<Result<EmployeeResponse>> CreateEmployeeAsync(Guid companyId, CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<EmployeeResponse>>> GetEmployeesByCompanyAsync(Guid companyId, PaginationParams paginationParams, CancellationToken cancellationToken = default);
    Task<Result<EmployeeResponse>> UpdateEmployeeAsync(Guid id, Guid companyId, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeResponse>> CreateEmployeeFromHireAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<Result<EmployeeResponse>> GetEmployeeByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<List<OnboardingTaskResponse>>> GetOnboardingTasksAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
