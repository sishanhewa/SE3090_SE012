using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Employee>> GetEmployeesAsync(
        PaginationParams paginationParams,
        Guid? companyId = null,
        Guid? departmentId = null,
        EmployeeStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(e => e.User)
            .Include(e => e.Company)
            .Include(e => e.Department)
            .AsQueryable();

        if (companyId.HasValue)
            query = query.Where(e => e.CompanyId == companyId.Value);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(paginationParams.Search))
        {
            var search = paginationParams.Search.ToLower();
            query = query.Where(e =>
                e.User.FirstName.ToLower().Contains(search) ||
                e.User.LastName.ToLower().Contains(search) ||
                e.EmployeeNumber.ToLower().Contains(search));
        }

        query = query.OrderBy(e => e.User.LastName).ThenBy(e => e.User.FirstName);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Employee>(items, totalCount, paginationParams.Page, paginationParams.PageSize);
    }

    public async Task<Employee?> GetEmployeeWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.User)
            .Include(e => e.Company)
            .Include(e => e.Department)
            .Include(e => e.StatusHistory)
            .Include(e => e.OnboardingTasks)
                .ThenInclude(ot => ot.OnboardingTask)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> EmployeeNumberExistsAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.EmployeeNumber == employeeNumber, cancellationToken);
    }

    public async Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.Company)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
    }
}
