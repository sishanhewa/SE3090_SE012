using System;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Application.Interfaces.Repositories;

public interface IOfferRepository : IRepository<Offer>
{
    Task<PagedResult<Offer>> GetOffersAsync(
        PaginationParams paginationParams,
        Guid? applicationId = null,
        OfferStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Offer?> GetOfferWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
