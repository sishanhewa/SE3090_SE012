using TalentFlow.Application.Common;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Application.Interfaces.Repositories;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<PagedResult<Employee>> GetEmployeesAsync(
        PaginationParams paginationParams,
        Guid? companyId = null,
        Guid? departmentId = null,
        EmployeeStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Employee?> GetEmployeeWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> EmployeeNumberExistsAsync(string employeeNumber, CancellationToken cancellationToken = default);

    Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
