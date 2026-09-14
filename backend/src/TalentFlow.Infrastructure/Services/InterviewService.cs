using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Interviews;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Infrastructure.Services;

public class InterviewService : IInterviewService
{
    private readonly IInterviewRepository _interviewRepository;
    private readonly IApplicationRepository _applicationRepository;

    public InterviewService(IInterviewRepository interviewRepository, IApplicationRepository applicationRepository)
    {
        _interviewRepository = interviewRepository;
        _applicationRepository = applicationRepository;
    }

    public async Task<Result<InterviewResponse>> ScheduleInterviewAsync(CreateInterviewRequest request, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application == null)
            return Result<InterviewResponse>.NotFound("Application not found.");

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

        return Result<InterviewResponse>.Success(MapToResponse(interview));
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

        if (request.ScheduledAt.HasValue) interview.ScheduledAt = request.ScheduledAt.Value;
        if (request.DurationMinutes.HasValue) interview.DurationMinutes = request.DurationMinutes.Value;
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

        interview.Status = InterviewStatus.Cancelled;
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
            CreatedAt = interview.CreatedAt,
            UpdatedAt = interview.UpdatedAt
        };
    }
}
