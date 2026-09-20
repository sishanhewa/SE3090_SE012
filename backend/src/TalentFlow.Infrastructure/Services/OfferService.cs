using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Offers;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Infrastructure.Services;

public class OfferService : IOfferService
{
    private readonly IOfferRepository _offerRepository;
    private readonly IApplicationRepository _applicationRepository;

    public OfferService(IOfferRepository offerRepository, IApplicationRepository applicationRepository)
    {
        _offerRepository = offerRepository;
        _applicationRepository = applicationRepository;
    }

    public async Task<Result<OfferResponse>> CreateOfferAsync(CreateOfferRequest request, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application == null)
            return Result<OfferResponse>.NotFound("Application not found.");

        if (application.Status != ApplicationStatus.Interview && application.Status != ApplicationStatus.Shortlisted && application.Status != ApplicationStatus.Offered)
            return Result<OfferResponse>.Failure("Offers can only be created for applications that are in advanced stages.");

        var offer = new Offer
        {
            ApplicationId = request.ApplicationId,
            Position = request.Position,
            Salary = request.Salary,
            EmploymentType = request.EmploymentType,
            StartDate = request.StartDate,
            ExpiryDate = request.ExpiryDate,
            AdditionalTerms = request.AdditionalTerms,
            Status = OfferStatus.Draft
        };

        await _offerRepository.AddAsync(offer, cancellationToken);

        // Update application status
        application.Status = ApplicationStatus.Offered;
        await _applicationRepository.UpdateAsync(application, cancellationToken);

        return Result<OfferResponse>.Success(MapToResponse(offer));
    }

    public async Task<Result<OfferResponse>> GetOfferByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetOfferWithDetailsAsync(id, cancellationToken);
        if (offer == null)
            return Result<OfferResponse>.NotFound("Offer not found.");

        return Result<OfferResponse>.Success(MapToResponse(offer));
    }

    public async Task<Result<PagedResult<OfferResponse>>> GetOffersAsync(PaginationParams paginationParams, Guid? applicationId = null, CancellationToken cancellationToken = default)
    {
        var pagedOffers = await _offerRepository.GetOffersAsync(paginationParams, applicationId, null, cancellationToken);

        var response = new PagedResult<OfferResponse>(
            pagedOffers.Items.Select(MapToResponse).ToList(),
            pagedOffers.TotalCount,
            pagedOffers.Page,
            pagedOffers.PageSize
        );

        return Result<PagedResult<OfferResponse>>.Success(response);
    }

    public async Task<Result<OfferResponse>> UpdateOfferAsync(Guid id, UpdateOfferRequest request, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetByIdAsync(id, cancellationToken);
        if (offer == null)
            return Result<OfferResponse>.NotFound("Offer not found.");

        if (offer.Status != OfferStatus.Draft && offer.Status != OfferStatus.Sent)
            return Result<OfferResponse>.Failure("Only Draft or Sent offers can be updated.");

        if (request.Position != null) offer.Position = request.Position;
        if (request.Salary.HasValue) offer.Salary = request.Salary.Value;
        if (request.EmploymentType != null) offer.EmploymentType = request.EmploymentType;
        if (request.StartDate.HasValue) offer.StartDate = request.StartDate.Value;
        if (request.ExpiryDate.HasValue) offer.ExpiryDate = request.ExpiryDate.Value;
        if (request.AdditionalTerms != null) offer.AdditionalTerms = request.AdditionalTerms;

        await _offerRepository.UpdateAsync(offer, cancellationToken);

        return Result<OfferResponse>.Success(MapToResponse(offer));
    }

    public async Task<Result> UpdateStatusAsync(Guid id, OfferStatus status, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetOfferWithDetailsAsync(id, cancellationToken);
        if (offer == null)
            return Result.NotFound("Offer not found.");

        offer.Status = status;
        await _offerRepository.UpdateAsync(offer, cancellationToken);

        // If offer accepted, update application to Hired
        if (status == OfferStatus.Accepted && offer.Application != null)
        {
            offer.Application.Status = ApplicationStatus.Hired;
            await _applicationRepository.UpdateAsync(offer.Application, cancellationToken);
        }
        else if (status == OfferStatus.Rejected && offer.Application != null)
        {
            // Usually we wouldn't necessarily reject the app entirely, but just for flow
            offer.Application.Status = ApplicationStatus.Rejected;
            await _applicationRepository.UpdateAsync(offer.Application, cancellationToken);
        }

        return Result.Success();
    }

    private static OfferResponse MapToResponse(Offer offer)
    {
        return new OfferResponse
        {
            Id = offer.Id,
            ApplicationId = offer.ApplicationId,
            JobTitle = offer.Application?.Job?.Title ?? string.Empty,
            CandidateName = offer.Application?.CandidateProfile?.User != null 
                ? $"{offer.Application.CandidateProfile.User.FirstName} {offer.Application.CandidateProfile.User.LastName}" 
                : string.Empty,
            Position = offer.Position,
            Salary = offer.Salary,
            EmploymentType = offer.EmploymentType,
            StartDate = offer.StartDate,
            ExpiryDate = offer.ExpiryDate,
            Status = offer.Status.ToString(),
            AdditionalTerms = offer.AdditionalTerms,
            CreatedAt = offer.CreatedAt
        };
    }
}
