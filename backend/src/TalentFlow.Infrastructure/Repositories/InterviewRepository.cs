using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Repositories;

public class InterviewRepository : Repository<Interview>, IInterviewRepository
{
    public InterviewRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Interview>> GetInterviewsAsync(
        PaginationParams paginationParams,
        Guid? applicationId = null,
        InterviewStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(i => i.Application)
                .ThenInclude(a => a.CandidateProfile)
                    .ThenInclude(cp => cp.User)
            .Include(i => i.PanelMembers)
                .ThenInclude(pm => pm.User)
            .AsQueryable();

        if (applicationId.HasValue)
            query = query.Where(i => i.ApplicationId == applicationId.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        query = query.OrderByDescending(i => i.ScheduledAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Interview>(items, totalCount, paginationParams.Page, paginationParams.PageSize);
    }

    public async Task<Interview?> GetInterviewWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Application)
                .ThenInclude(a => a.CandidateProfile)
                    .ThenInclude(cp => cp.User)
            .Include(i => i.PanelMembers)
                .ThenInclude(pm => pm.User)
            .Include(i => i.Feedback)
                .ThenInclude(f => f.Reviewer)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<bool> HasConflictAsync(
        Guid userId,
        DateTime scheduledAt,
        int durationMinutes,
        Guid? excludeInterviewId = null,
        CancellationToken cancellationToken = default)
    {
        var endTime = scheduledAt.AddMinutes(durationMinutes);

        var query = DbSet
            .Include(i => i.PanelMembers)
            .Where(i => i.Status != InterviewStatus.Cancelled)
            .Where(i => i.ScheduledAt < endTime && i.ScheduledAt.AddMinutes(i.DurationMinutes) > scheduledAt);

        if (excludeInterviewId.HasValue)
            query = query.Where(i => i.Id != excludeInterviewId.Value);

        // Check if the user is involved in any overlapping interview
        // (either as a candidate through the application or as a panel member)
        return await query.AnyAsync(i =>
            i.PanelMembers.Any(pm => pm.UserId == userId) ||
            i.Application.CandidateProfile.UserId == userId,
            cancellationToken);
    }
}
