using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Repositories;

public class JobRepository : Repository<Job>, IJobRepository
{
    public JobRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Job>> GetJobsAsync(
        PaginationParams paginationParams,
        Guid? companyId = null,
        JobStatus? status = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(j => j.Company)
            .Include(j => j.Department)
            .AsQueryable();

        if (companyId.HasValue)
            query = query.Where(j => j.CompanyId == companyId.Value);

        if (status.HasValue)
            query = query.Where(j => j.Status == status.Value);

        if (departmentId.HasValue)
            query = query.Where(j => j.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(paginationParams.Search))
        {
            var search = paginationParams.Search.ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(search) ||
                j.Description.ToLower().Contains(search));
        }

        // Sorting
        query = paginationParams.SortBy?.ToLower() switch
        {
            "title" => paginationParams.SortDescending ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
            "deadline" => paginationParams.SortDescending ? query.OrderByDescending(j => j.ApplicationDeadline) : query.OrderBy(j => j.ApplicationDeadline),
            "created" => paginationParams.SortDescending ? query.OrderByDescending(j => j.CreatedAt) : query.OrderBy(j => j.CreatedAt),
            _ => query.OrderByDescending(j => j.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Job>(items, totalCount, paginationParams.Page, paginationParams.PageSize);
    }

    public async Task<Job?> GetJobWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(j => j.Company)
            .Include(j => j.Department)
            .Include(j => j.Requirements)
            .Include(j => j.SkillRequirements)
                .ThenInclude(sr => sr.Skill)
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }
}
