using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;
using ApplicationEntity = TalentFlow.Domain.Entities.Application;

namespace TalentFlow.Infrastructure.Repositories;

public class ApplicationRepository : Repository<ApplicationEntity>, IApplicationRepository
{
    public ApplicationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> HasAppliedAsync(Guid jobId, Guid candidateProfileId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            a => a.JobId == jobId && a.CandidateProfileId == candidateProfileId,
            cancellationToken);
    }

    public async Task<PagedResult<ApplicationEntity>> GetApplicationsAsync(
        PaginationParams paginationParams,
        Guid? jobId = null,
        Guid? candidateProfileId = null,
        ApplicationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(a => a.Job)
                .ThenInclude(j => j.Company)
            .Include(a => a.CandidateProfile)
                .ThenInclude(cp => cp.User)
            .AsQueryable();

        if (jobId.HasValue)
            query = query.Where(a => a.JobId == jobId.Value);

        if (candidateProfileId.HasValue)
            query = query.Where(a => a.CandidateProfileId == candidateProfileId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        query = query.OrderByDescending(a => a.SubmittedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ApplicationEntity>(items, totalCount, paginationParams.Page, paginationParams.PageSize);
    }

    public async Task<ApplicationEntity?> GetApplicationWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.Job)
                .ThenInclude(j => j.Company)
            .Include(a => a.CandidateProfile)
                .ThenInclude(cp => cp.User)
            .Include(a => a.History)
            .Include(a => a.Interviews)
            .Include(a => a.Offer)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
