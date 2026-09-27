using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Repositories;

public class OfferRepository : Repository<Offer>, IOfferRepository
{
    public OfferRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Offer>> GetOffersAsync(
        PaginationParams paginationParams,
        Guid? applicationId = null,
        OfferStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(o => o.Application)
                .ThenInclude(a => a.CandidateProfile)
                    .ThenInclude(cp => cp.User)
            .Include(o => o.Application)
                .ThenInclude(a => a.Job)
            .AsQueryable();

        if (applicationId.HasValue)
            query = query.Where(o => o.ApplicationId == applicationId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        query = query.OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Offer>(items, totalCount, paginationParams.Page, paginationParams.PageSize);
    }

    public async Task<Offer?> GetOfferWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(o => o.Application)
                .ThenInclude(a => a.CandidateProfile)
                    .ThenInclude(cp => cp.User)
            .Include(o => o.Application)
                .ThenInclude(a => a.Job)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
}
