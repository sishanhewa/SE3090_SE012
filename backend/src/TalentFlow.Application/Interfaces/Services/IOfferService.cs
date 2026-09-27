using System;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Offers;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Application.Interfaces.Services;

public interface IOfferService
{
    Task<Result<OfferResponse>> CreateOfferAsync(CreateOfferRequest request, CancellationToken cancellationToken = default);
    Task<Result<OfferResponse>> GetOfferByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<OfferResponse>>> GetOffersAsync(PaginationParams paginationParams, Guid? applicationId = null, CancellationToken cancellationToken = default);
    Task<Result<OfferResponse>> UpdateOfferAsync(Guid id, UpdateOfferRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateStatusAsync(Guid id, OfferStatus status, CancellationToken cancellationToken = default);
}
