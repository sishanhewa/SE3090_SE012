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
        TalentFlow.Application.DTOs.Jobs.JobSearchParams searchParams,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(j => j.Company)
            .Include(j => j.Department)
            .AsQueryable();

        if (companyId.HasValue)
            query = query.Where(j => j.CompanyId == companyId.Value);

        if (searchParams.Status.HasValue)
            query = query.Where(j => j.Status == searchParams.Status.Value);

        if (!string.IsNullOrWhiteSpace(searchParams.Department))
        {
            var dept = searchParams.Department.ToLower();
            query = query.Where(j => j.Department.Name.ToLower().Contains(dept));
        }

        if (!string.IsNullOrWhiteSpace(searchParams.EmploymentType))
        {
            var empType = searchParams.EmploymentType.ToLower();
            query = query.Where(j => j.EmploymentType.ToLower().Contains(empType));
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Search))
        {
            var search = searchParams.Search.ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(search) ||
                j.Description.ToLower().Contains(search));
        }

        // Sorting
        query = (searchParams.SortBy ?? searchParams.SortBy)?.ToLower() switch
        {
            "title" => searchParams.SortDescending ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
            "deadline" => searchParams.SortDescending ? query.OrderByDescending(j => j.ApplicationDeadline) : query.OrderBy(j => j.ApplicationDeadline),
            "created" => searchParams.SortDescending ? query.OrderByDescending(j => j.CreatedAt) : query.OrderBy(j => j.CreatedAt),
            _ => query.OrderByDescending(j => j.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Job>(items, totalCount, searchParams.Page, searchParams.PageSize);
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
