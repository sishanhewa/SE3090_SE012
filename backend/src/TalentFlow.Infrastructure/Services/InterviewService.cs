using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Interviews;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Services;

public class InterviewService : IInterviewService
{
    private readonly IInterviewRepository _interviewRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IGoogleCalendarService _calendarService;
    private readonly ILogger<InterviewService> _logger;
    private readonly AppDbContext? _context;
    private readonly IConfiguration? _configuration;

    public InterviewService(
        IInterviewRepository interviewRepository,
        IApplicationRepository applicationRepository,
        IGoogleCalendarService calendarService,
        ILogger<InterviewService> logger,
        AppDbContext? context = null,
        IConfiguration? configuration = null)
    {
        _interviewRepository = interviewRepository;
        _applicationRepository = applicationRepository;
        _calendarService = calendarService;
        _logger = logger;
        _context = context;
        _configuration = configuration;
    }

    public async Task<Result<InterviewResponse>> ScheduleInterviewAsync(CreateInterviewRequest request, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetApplicationWithDetailsAsync(request.ApplicationId, cancellationToken);
        if (application == null)
            return Result<InterviewResponse>.NotFound("Application not found.");

        if (application.Status != Domain.Enums.ApplicationStatus.Shortlisted && application.Status != Domain.Enums.ApplicationStatus.Interview)
            return Result<InterviewResponse>.Failure("Interviews can only be scheduled for applications in 'Shortlisted' or 'Interview' status.", "InvalidStateTransition");
        if (request.ScheduledAt.ToUniversalTime() <= DateTime.UtcNow.AddMinutes(30) ||
            request.DurationMinutes < 15 || request.DurationMinutes > 480)
            return Result<InterviewResponse>.Failure("Choose a future interview slot and a duration of 15 to 480 minutes.");
        if (application.Interviews.Any(i => i.Status != InterviewStatus.Cancelled))
            return Result<InterviewResponse>.Conflict("An active interview already exists for this application.");
        if (application.CandidateProfile?.User != null &&
            await _interviewRepository.HasConflictAsync(application.CandidateProfile.UserId,
                request.ScheduledAt, request.DurationMinutes, cancellationToken: cancellationToken))
            return Result<InterviewResponse>.Conflict("The candidate already has an interview at this time.");

        var calendarId = _configuration?["GoogleCalendar:CalendarId"];
        var calendarAvailable = false;
        if (!string.IsNullOrWhiteSpace(calendarId))
        {
            var busy = await _calendarService.GetBusySlotsAsync(calendarId, request.ScheduledAt,
                request.ScheduledAt.AddMinutes(request.DurationMinutes), cancellationToken);
            calendarAvailable = busy.IsSuccess &&
                !string.IsNullOrWhiteSpace(application.CandidateProfile?.User?.Email);
            if (busy.IsSuccess && busy.Data?.Any(slot =>
                slot.Start < request.ScheduledAt.AddMinutes(request.DurationMinutes) &&
                slot.End > request.ScheduledAt) == true)
                return Result<InterviewResponse>.Conflict("The interview calendar is busy at this time.");
        }

        var interview = new Interview
        {
            ApplicationId = request.ApplicationId,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            MeetingUrl = request.MeetingUrl,
            Location = request.Location,
            Notes = request.Notes,
            Status = InterviewStatus.Proposed
        };

        await _interviewRepository.AddAsync(interview, cancellationToken);

        // Calendar failures leave a visible proposal that staff can retry explicitly.
        try
        {
            var calendarResult = calendarAvailable
                ? await _calendarService.CreateInterviewEventAsync(BuildCalendarRequest(interview, application), cancellationToken)
                : Result<CalendarEventResult>.Failure("Calendar availability is not verified.");

            if (calendarResult.IsSuccess && calendarResult.Data != null)
            {
                interview.CalendarEventId = calendarResult.Data.EventId;
                interview.Status = InterviewStatus.Scheduled;
                if (string.IsNullOrEmpty(interview.MeetingUrl) && !string.IsNullOrEmpty(calendarResult.Data.MeetLink))
                {
                    interview.MeetingUrl = calendarResult.Data.MeetLink;
                }
                await _interviewRepository.UpdateAsync(interview, cancellationToken);
                application.Status = ApplicationStatus.Interview;
                application.History.Add(new ApplicationHistory
                {
                    FromStatus = ApplicationStatus.Shortlisted,
                    ToStatus = ApplicationStatus.Interview,
                    ChangedBy = "System",
                    Notes = "Interview invitation sent through Google Calendar."
                });
                await _applicationRepository.UpdateAsync(application, cancellationToken);
            }
            else _logger.LogWarning("Interview {InterviewId} remains proposed: {Error}", interview.Id, calendarResult.Error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create calendar event for interview {InterviewId}, continuing without it.", interview.Id);
        }

        return Result<InterviewResponse>.Success(MapToResponse(interview));
    }

    public async Task<Result<PagedResult<InterviewResponse>>> GetInterviewsByCompanyAsync(
        Guid companyId, PaginationParams paginationParams, CancellationToken cancellationToken = default)
    {
        if (_context == null) return Result<PagedResult<InterviewResponse>>.Failure("Interview list is unavailable.");
        var query = _context.Interviews.AsNoTracking()
            .Include(i => i.Application).ThenInclude(a => a.Job)
            .Include(i => i.Feedback).ThenInclude(f => f.Reviewer)
            .Where(i => companyId == Guid.Empty || i.Application.Job.CompanyId == companyId)
            .OrderByDescending(i => i.ScheduledAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize).ToListAsync(cancellationToken);
        return Result<PagedResult<InterviewResponse>>.Success(new PagedResult<InterviewResponse>(
            items.Select(MapToResponse).ToList(), total, paginationParams.Page, paginationParams.PageSize));
    }

    public async Task<Result<InterviewResponse>> GetInterviewByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var interview = await _interviewRepository.GetByIdAsync(id, cancellationToken);
        if (interview == null)
            return Result<InterviewResponse>.NotFound("Interview not found.");

        return Result<InterviewResponse>.Success(MapToResponse(interview));
    }

    public async Task<Result<PagedResult<InterviewResponse>>> GetInterviewsByApplicationAsync(Guid applicationId, PaginationParams paginationParams, CancellationToken cancellationToken = default)
    {
        var interviews = await _interviewRepository.FindAsync(i => i.ApplicationId == applicationId, cancellationToken);

        // Simple paginated retrieval for sprint 1
        var pagedItems = interviews
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(MapToResponse)
            .ToList();

        var response = new PagedResult<InterviewResponse>(
            pagedItems,
            interviews.Count,
            paginationParams.Page,
            paginationParams.PageSize
        );

        return Result<PagedResult<InterviewResponse>>.Success(response);
    }

    public async Task<Result<InterviewResponse>> UpdateInterviewAsync(Guid id, UpdateInterviewRequest request, CancellationToken cancellationToken = default)
    {
        var interview = await _interviewRepository.GetByIdAsync(id, cancellationToken);
        if (interview == null)
            return Result<InterviewResponse>.NotFound("Interview not found.");

        if (interview.Status is InterviewStatus.Completed or InterviewStatus.Cancelled or InterviewStatus.NoShow)
            return Result<InterviewResponse>.Failure("This interview can no longer be changed.");
        var newStart = request.ScheduledAt ?? interview.ScheduledAt;
        var newDuration = request.DurationMinutes ?? interview.DurationMinutes;
        if (newStart.ToUniversalTime() <= DateTime.UtcNow.AddMinutes(30) || newDuration < 15 || newDuration > 480)
            return Result<InterviewResponse>.Failure("Choose a future interview slot and valid duration.");
        if (request.ScheduledAt.HasValue)
        {
            var application = await _applicationRepository.GetApplicationWithDetailsAsync(interview.ApplicationId, cancellationToken);
            if (application?.CandidateProfile?.User != null &&
                await _interviewRepository.HasConflictAsync(application.CandidateProfile.UserId,
                    newStart, newDuration, interview.Id, cancellationToken))
                return Result<InterviewResponse>.Conflict("The candidate already has an interview at this time.");
        }
        if (interview.CalendarEventId != null && (request.ScheduledAt.HasValue || request.DurationMinutes.HasValue))
        {
            var application = await _applicationRepository.GetApplicationWithDetailsAsync(interview.ApplicationId, cancellationToken);
            if (application == null) return Result<InterviewResponse>.NotFound("Application not found.");
            var replacement = new Interview { ScheduledAt = newStart, DurationMinutes = newDuration,
                Location = request.Location ?? interview.Location, MeetingUrl = request.MeetingUrl ?? interview.MeetingUrl,
                Notes = request.Notes ?? interview.Notes };
            var updated = await _calendarService.UpdateInterviewEventAsync(interview.CalendarEventId,
                BuildCalendarRequest(replacement, application), cancellationToken);
            if (!updated.IsSuccess) return Result<InterviewResponse>.Failure(updated.Error ?? "Calendar update failed.");
        }
        interview.ScheduledAt = newStart;
        interview.DurationMinutes = newDuration;
        if (request.MeetingUrl != null) interview.MeetingUrl = request.MeetingUrl;
        if (request.Location != null) interview.Location = request.Location;
        if (request.Notes != null) interview.Notes = request.Notes;

        await _interviewRepository.UpdateAsync(interview, cancellationToken);

        return Result<InterviewResponse>.Success(MapToResponse(interview));
    }

    public async Task<Result> CancelInterviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var interview = await _interviewRepository.GetByIdAsync(id, cancellationToken);
        if (interview == null)
            return Result.NotFound("Interview not found.");

        if (interview.Status == InterviewStatus.Completed || interview.Status == InterviewStatus.Cancelled)
            return Result.Failure("Interview cannot be cancelled in its current state.", "InvalidStateTransition");

        // Keep the interview active when an existing invitation cannot be cancelled.
        if (!string.IsNullOrEmpty(interview.CalendarEventId))
        {
            var deleted = await _calendarService.DeleteInterviewEventAsync(interview.CalendarEventId, cancellationToken);
            if (!deleted.IsSuccess) return Result.Failure(deleted.Error ?? "Calendar cancellation failed.");
        }

        interview.Status = InterviewStatus.Cancelled;
        await _interviewRepository.UpdateAsync(interview, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateStatusAsync(Guid id, InterviewStatus status, CancellationToken cancellationToken = default)
    {
        var interview = await _interviewRepository.GetByIdAsync(id, cancellationToken);
        if (interview == null)
            return Result.NotFound("Interview not found.");

        if (interview.Status == status) return Result.Success();
        var allowed = interview.Status switch
        {
            InterviewStatus.Proposed => status == InterviewStatus.Scheduled,
            InterviewStatus.Scheduled => status is InterviewStatus.InProgress or InterviewStatus.Completed or InterviewStatus.NoShow,
            InterviewStatus.InProgress => status is InterviewStatus.Completed or InterviewStatus.NoShow,
            _ => false
        };
        if (!allowed) return Result.Failure("Invalid interview status change.", "InvalidStateTransition");
        if (status == InterviewStatus.Scheduled && interview.Status == InterviewStatus.Proposed)
        {
            var application = await _applicationRepository.GetApplicationWithDetailsAsync(interview.ApplicationId, cancellationToken);
            if (application == null) return Result.NotFound("Application not found.");
            if (string.IsNullOrWhiteSpace(application.CandidateProfile?.User?.Email))
                return Result.Failure("Candidate email is required for a Calendar invitation.");
            var calendarId = _configuration?["GoogleCalendar:CalendarId"];
            if (string.IsNullOrWhiteSpace(calendarId))
                return Result.Failure("Google Calendar is not configured.");
            var busy = await _calendarService.GetBusySlotsAsync(calendarId, interview.ScheduledAt,
                interview.ScheduledAt.AddMinutes(interview.DurationMinutes), cancellationToken);
            if (!busy.IsSuccess) return Result.Failure(busy.Error ?? "Calendar availability could not be verified.");
            if (busy.Data?.Any(slot => slot.Start < interview.ScheduledAt.AddMinutes(interview.DurationMinutes) &&
                slot.End > interview.ScheduledAt) == true)
                return Result.Failure("The interview calendar is busy at this time.");
            var invited = await _calendarService.CreateInterviewEventAsync(
                BuildCalendarRequest(interview, application), cancellationToken);
            if (!invited.IsSuccess || invited.Data == null)
                return Result.Failure(invited.Error ?? "Calendar invitation failed.");
            interview.CalendarEventId = invited.Data.EventId;
            interview.MeetingUrl ??= invited.Data.MeetLink;
            application.Status = ApplicationStatus.Interview;
            application.History.Add(new ApplicationHistory { FromStatus = ApplicationStatus.Shortlisted,
                ToStatus = ApplicationStatus.Interview, ChangedBy = "System",
                Notes = "Interview invitation sent through Google Calendar." });
            await _applicationRepository.UpdateAsync(application, cancellationToken);
        }
        interview.Status = status;
        await _interviewRepository.UpdateAsync(interview, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> AddFeedbackAsync(Guid id, Guid reviewerId, AddInterviewFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var interview = await _interviewRepository.GetInterviewWithDetailsAsync(id, cancellationToken);
        if (interview == null)
            return Result.NotFound("Interview not found.");
        if (interview.Status is not (InterviewStatus.Scheduled or InterviewStatus.InProgress or InterviewStatus.Completed))
            return Result.Failure("Feedback can only be added to a scheduled or completed interview.");
        if (request.TechnicalScore is < 1 or > 10 || request.CommunicationScore is < 1 or > 10 ||
            request.ExperienceScore is < 1 or > 10 ||
            request.Recommendation is not ("StrongHire" or "Hire" or "NoHire" or "StrongNoHire"))
            return Result.Failure("Use scores from 1 to 10 and a valid recommendation.");

        var existingFeedback = interview.Feedback.FirstOrDefault(f => f.ReviewerId == reviewerId);
        if (existingFeedback != null)
        {
            existingFeedback.TechnicalScore = request.TechnicalScore;
            existingFeedback.CommunicationScore = request.CommunicationScore;
            existingFeedback.ExperienceScore = request.ExperienceScore;
            existingFeedback.OverallScore = (request.TechnicalScore + request.CommunicationScore + request.ExperienceScore) / 3;
            existingFeedback.Comments = request.Comments;
            existingFeedback.Recommendation = request.Recommendation;
        }
        else
        {
            interview.Feedback.Add(new InterviewFeedback
            {
                ReviewerId = reviewerId,
                TechnicalScore = request.TechnicalScore,
                CommunicationScore = request.CommunicationScore,
                ExperienceScore = request.ExperienceScore,
                OverallScore = (request.TechnicalScore + request.CommunicationScore + request.ExperienceScore) / 3,
                Comments = request.Comments,
                Recommendation = request.Recommendation
            });
        }

        await _interviewRepository.UpdateAsync(interview, cancellationToken);
        return Result.Success();
    }

    private static InterviewResponse MapToResponse(Interview interview)
    {
        return new InterviewResponse
        {
            Id = interview.Id,
            ApplicationId = interview.ApplicationId,
            ScheduledAt = interview.ScheduledAt,
            DurationMinutes = interview.DurationMinutes,
            Status = interview.Status.ToString(),
            MeetingUrl = interview.MeetingUrl,
            Location = interview.Location,
            Notes = interview.Notes,
            CalendarInvitationSent = !string.IsNullOrEmpty(interview.CalendarEventId) &&
                !interview.CalendarEventId.StartsWith("mock-") && !interview.CalendarEventId.StartsWith("failed-"),
            CalendarEventId = interview.CalendarEventId,
            CreatedAt = interview.CreatedAt,
            UpdatedAt = interview.UpdatedAt,
            Feedbacks = interview.Feedback.Select(f => new InterviewFeedbackResponse
            {
                Id = f.Id,
                ReviewerName = f.Reviewer?.FirstName + " " + f.Reviewer?.LastName,
                OverallScore = f.OverallScore,
                Recommendation = f.Recommendation,
                Comments = f.Comments,
                CreatedAt = f.CreatedAt
            }).ToList()
        };
    }

    private static CalendarEventRequest BuildCalendarRequest(Interview interview, TalentFlow.Domain.Entities.Application application)
    {
        var candidate = application.CandidateProfile?.User;
        return new CalendarEventRequest
        {
            EventId = $"tf{interview.Id:N}",
            Title = $"Interview: {candidate?.FirstName ?? "Candidate"} — {application.Job?.Title ?? "Position"}",
            Description = $"Interview for {application.Job?.Title ?? "the position"}.\n\n{interview.Notes}",
            StartTime = interview.ScheduledAt,
            EndTime = interview.ScheduledAt.AddMinutes(interview.DurationMinutes),
            Location = interview.Location,
            MeetingUrl = interview.MeetingUrl,
            AttendeeEmails = candidate?.Email is string email ? new List<string> { email } : new List<string>()
        };
    }
}
