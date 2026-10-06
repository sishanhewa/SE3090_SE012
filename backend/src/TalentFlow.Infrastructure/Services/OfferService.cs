using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Offers;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Services;

public class OfferService : IOfferService
{
    private readonly IOfferRepository _offerRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly AppDbContext _context;
    private readonly IEmployeeService _employeeService;
    private readonly CandidateNotificationService _notifications;
    private readonly ILogger<OfferService> _logger;

    public OfferService(IOfferRepository offerRepository, IApplicationRepository applicationRepository,
        AppDbContext context, IEmployeeService employeeService,
        CandidateNotificationService notifications, ILogger<OfferService> logger)
    {
        _offerRepository = offerRepository;
        _applicationRepository = applicationRepository;
        _context = context;
        _employeeService = employeeService;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<Result<OfferResponse>> CreateOfferAsync(CreateOfferRequest request, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application == null)
            return Result<OfferResponse>.NotFound("Application not found.");

        if (application.Status != ApplicationStatus.Interview)
            return Result<OfferResponse>.Failure("Complete the interview before drafting an offer.");
        if (await _context.Offers.AnyAsync(o => o.ApplicationId == request.ApplicationId, cancellationToken))
            return Result<OfferResponse>.Conflict("An offer already exists for this application.");
        if (!await _context.Interviews.Include(i => i.Feedback).AnyAsync(i =>
                i.ApplicationId == request.ApplicationId && i.Status == InterviewStatus.Completed &&
                i.Feedback.Any(), cancellationToken))
            return Result<OfferResponse>.Failure("A completed interview with feedback is required before an offer.");
        if (request.Salary <= 0 || request.StartDate.Date <= DateTime.UtcNow.Date ||
            request.ExpiryDate.Date < DateTime.UtcNow.Date ||
            request.StartDate.Date <= request.ExpiryDate.Date)
            return Result<OfferResponse>.Failure("Enter a positive salary and future offer dates.");

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

        return Result<OfferResponse>.Success(MapToResponse(offer));
    }

    public async Task<Result<OfferResponse>> GetOfferByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetOfferWithDetailsAsync(id, cancellationToken);
        if (offer == null)
            return Result<OfferResponse>.NotFound("Offer not found.");

        return Result<OfferResponse>.Success(MapToResponse(offer));
    }

    public async Task<Result<PagedResult<OfferResponse>>> GetOffersAsync(PaginationParams paginationParams, Guid? applicationId = null, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Offers.AsNoTracking().Include(o => o.Application)
            .ThenInclude(a => a.Job).Include(o => o.Application)
            .ThenInclude(a => a.CandidateProfile).ThenInclude(cp => cp.User).AsQueryable();
        if (applicationId.HasValue) query = query.Where(o => o.ApplicationId == applicationId.Value);
        if (companyId.HasValue) query = query.Where(o => o.Application.Job.CompanyId == companyId.Value);
        var count = await query.CountAsync(cancellationToken);
        var offers = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize).ToListAsync(cancellationToken);
        var response = new PagedResult<OfferResponse>(offers.Select(MapToResponse).ToList(),
            count, paginationParams.Page, paginationParams.PageSize);

        return Result<PagedResult<OfferResponse>>.Success(response);
    }

    public async Task<Result<OfferResponse>> UpdateOfferAsync(Guid id, UpdateOfferRequest request, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetByIdAsync(id, cancellationToken);
        if (offer == null)
            return Result<OfferResponse>.NotFound("Offer not found.");

        if (offer.Status != OfferStatus.Draft)
            return Result<OfferResponse>.Failure("Only draft offers can be changed; published terms are locked.");

        var salary = request.Salary ?? offer.Salary;
        var startDate = request.StartDate ?? offer.StartDate;
        var expiryDate = request.ExpiryDate ?? offer.ExpiryDate;
        if (salary <= 0 || startDate.Date <= DateTime.UtcNow.Date ||
            expiryDate.Date < DateTime.UtcNow.Date || startDate.Date <= expiryDate.Date)
            return Result<OfferResponse>.Failure("Enter a positive salary and future offer dates.");

        if (request.Position != null) offer.Position = request.Position;
        if (request.Salary.HasValue) offer.Salary = request.Salary.Value;
        if (request.EmploymentType != null) offer.EmploymentType = request.EmploymentType;
        if (request.StartDate.HasValue) offer.StartDate = request.StartDate.Value;
        if (request.ExpiryDate.HasValue) offer.ExpiryDate = request.ExpiryDate.Value;
        if (request.AdditionalTerms != null) offer.AdditionalTerms = request.AdditionalTerms;

        await _offerRepository.UpdateAsync(offer, cancellationToken);

        return Result<OfferResponse>.Success(MapToResponse(offer));
    }

    public async Task<Result> UpdateStatusAsync(Guid id, OfferStatus status, Guid actorId, string actorLabel, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetOfferWithDetailsAsync(id, cancellationToken);
        if (offer == null)
            return Result.NotFound("Offer not found.");

        if (offer.Status == status)
        {
            if (status == OfferStatus.Accepted &&
                !await _context.Employees.AnyAsync(e => e.ApplicationId == offer.ApplicationId, cancellationToken))
            {
                var resumed = await _employeeService.CreateEmployeeFromHireAsync(offer.ApplicationId, cancellationToken);
                return resumed.IsSuccess || resumed.ErrorCode == "CONFLICT"
                    ? Result.Success() : Result.Failure(resumed.Error ?? "Onboarding setup failed.");
            }
            return Result.Success();
        }
        var valid = offer.Status switch
        {
            OfferStatus.Draft => status is OfferStatus.PendingApproval or OfferStatus.Withdrawn,
            OfferStatus.PendingApproval => status is OfferStatus.Approved or OfferStatus.Draft or OfferStatus.Withdrawn,
            OfferStatus.Approved => status is OfferStatus.Sent or OfferStatus.Withdrawn,
            OfferStatus.Sent => status is OfferStatus.Accepted or OfferStatus.Rejected or OfferStatus.Expired or OfferStatus.Withdrawn,
            _ => false
        };
        if (!valid) return Result.Failure($"Cannot change offer from {offer.Status} to {status}.", "InvalidStateTransition");
        if (status == OfferStatus.Sent && offer.ExpiryDate < DateTime.UtcNow)
            return Result.Failure("This offer has expired.");
        if (status == OfferStatus.Accepted && offer.ExpiryDate < DateTime.UtcNow)
            return Result.Failure("This offer has expired and cannot be accepted.");

        var application = offer.Application!;
        if (status == OfferStatus.Accepted && application.CandidateProfile != null &&
            await _context.Employees.AnyAsync(e => e.UserId == application.CandidateProfile.UserId &&
                e.ApplicationId != application.Id, cancellationToken))
            return Result.Failure("This candidate already has an employee record from another application.");
        await using var transaction = status == OfferStatus.Accepted && _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
        offer.Status = status;
        if (status == OfferStatus.Approved)
        {
            offer.ApprovedById = actorId;
            offer.ApprovedAt = DateTime.UtcNow;
        }
        if (status == OfferStatus.Sent)
        {
            application.Status = ApplicationStatus.Offered;
            application.History.Add(new ApplicationHistory { FromStatus = ApplicationStatus.Interview,
                ToStatus = ApplicationStatus.Offered, ChangedBy = actorLabel, Notes = "Offer published to candidate portal." });
        }
        else if (status == OfferStatus.Accepted)
        {
            application.Status = ApplicationStatus.Hired;
            application.History.Add(new ApplicationHistory { FromStatus = ApplicationStatus.Offered,
                ToStatus = ApplicationStatus.Hired, ChangedBy = actorLabel, Notes = "Candidate accepted the offer." });
        }
        else if (status == OfferStatus.Rejected)
        {
            application.Status = ApplicationStatus.Rejected;
            application.History.Add(new ApplicationHistory { FromStatus = ApplicationStatus.Offered,
                ToStatus = ApplicationStatus.Rejected, ChangedBy = actorLabel, Notes = "Candidate declined the offer." });
        }
        else if (status is OfferStatus.Withdrawn or OfferStatus.Expired &&
                 application.Status == ApplicationStatus.Offered)
        {
            application.Status = ApplicationStatus.Interview;
            application.History.Add(new ApplicationHistory { FromStatus = ApplicationStatus.Offered,
                ToStatus = ApplicationStatus.Interview, ChangedBy = actorLabel, Notes = $"Offer {status.ToString().ToLowerInvariant()}." });
        }

        await _offerRepository.UpdateAsync(offer, cancellationToken);
        if (status is OfferStatus.Sent or OfferStatus.Accepted or OfferStatus.Rejected or OfferStatus.Withdrawn or OfferStatus.Expired)
            await _applicationRepository.UpdateAsync(application, cancellationToken);

        if (status == OfferStatus.Accepted)
        {
            var employee = await _employeeService.CreateEmployeeFromHireAsync(application.Id, cancellationToken);
            if (!employee.IsSuccess && employee.ErrorCode != "CONFLICT")
                return Result.Failure($"Onboarding setup failed; the offer remains unchanged: {employee.Error}");
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
            var candidate = application.CandidateProfile?.User;
            if (!string.IsNullOrWhiteSpace(candidate?.Email))
            {
                var message = await _notifications.SendHiredAsync(candidate.Email,
                    candidate.FirstName, offer.Position, offer.StartDate, cancellationToken);
                application.History.Add(new ApplicationHistory { FromStatus = ApplicationStatus.Hired,
                    ToStatus = ApplicationStatus.Hired, ChangedBy = "System",
                    Notes = message.IsSuccess ? "Hiring notification email sent to candidate." : message.Error });
                await _applicationRepository.UpdateAsync(application, cancellationToken);
                if (!message.IsSuccess) _logger.LogWarning("Hiring email pending for application {ApplicationId}: {Reason}",
                    application.Id, message.Error);
            }
            else
            {
                application.History.Add(new ApplicationHistory { FromStatus = ApplicationStatus.Hired,
                    ToStatus = ApplicationStatus.Hired, ChangedBy = "System",
                    Notes = "Hiring notification pending: candidate email is missing." });
                await _applicationRepository.UpdateAsync(application, cancellationToken);
            }
        }
        return Result.Success();
    }

    public async Task<Result> ResendHiredNotificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetOfferWithDetailsAsync(id, cancellationToken);
        if (offer == null) return Result.NotFound("Offer not found.");
        if (offer.Status != OfferStatus.Accepted || offer.Application?.Status != ApplicationStatus.Hired)
            return Result.Failure("The candidate must be hired before sending a hiring notification.");
        var candidate = offer.Application.CandidateProfile?.User;
        if (string.IsNullOrWhiteSpace(candidate?.Email)) return Result.Failure("Candidate email is missing.");
        var sent = await _notifications.SendHiredAsync(candidate.Email, candidate.FirstName,
            offer.Position, offer.StartDate, cancellationToken);
        offer.Application.History.Add(new ApplicationHistory { FromStatus = ApplicationStatus.Hired,
            ToStatus = ApplicationStatus.Hired, ChangedBy = "Hiring team",
            Notes = sent.IsSuccess ? "Hiring notification email sent to candidate." : sent.Error });
        await _applicationRepository.UpdateAsync(offer.Application, cancellationToken);
        return sent;
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
