using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Applications;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobRepository _jobRepository;

    public ApplicationService(IApplicationRepository applicationRepository, IJobRepository jobRepository)
    {
        _applicationRepository = applicationRepository;
        _jobRepository = jobRepository;
    }

    public async Task<Result<ApplicationResponse>> CreateApplicationAsync(Guid jobId, CreateApplicationRequest request, Guid candidateProfileId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return Result<ApplicationResponse>.NotFound("Job not found");

        if (job.Status != JobStatus.Published)
            return Result<ApplicationResponse>.Failure("Job is not currently accepting applications");

        if (job.ApplicationDeadline.HasValue && job.ApplicationDeadline.Value < DateTime.UtcNow)
            return Result<ApplicationResponse>.Failure("The application deadline for this job has passed.");

        var hasApplied = await _applicationRepository.HasAppliedAsync(jobId, candidateProfileId, cancellationToken);
        if (hasApplied)
            return Result<ApplicationResponse>.Conflict("You have already applied for this job");

        var application = new Domain.Entities.Application
        {
            JobId = jobId,
            CandidateProfileId = candidateProfileId,
            CoverLetter = request.CoverLetter,
            ResumeDocumentId = request.ResumeDocumentId,
            Status = ApplicationStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        application.History.Add(new ApplicationHistory
        {
            FromStatus = ApplicationStatus.Submitted, // Actually, it's starting at Submitted
            ToStatus = ApplicationStatus.Submitted,
            Notes = "Application submitted"
        });

        await _applicationRepository.AddAsync(application, cancellationToken);

        // Normally we might trigger an event to score the AI here, but for sprint 1 we'll mock it or leave it null

        return Result<ApplicationResponse>.Success(MapToResponse(application));
    }

    public async Task<Result<ApplicationResponse>> GetApplicationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetApplicationWithDetailsAsync(id, cancellationToken);
        if (application == null)
            return Result<ApplicationResponse>.NotFound("Application not found");

        return Result<ApplicationResponse>.Success(MapToResponse(application));
    }

    public async Task<Result<PagedResult<ApplicationResponse>>> GetApplicationsAsync(PaginationParams paginationParams, Guid? jobId = null, Guid? candidateProfileId = null, CancellationToken cancellationToken = default)
    {
        var result = await _applicationRepository.GetApplicationsAsync(paginationParams, jobId, candidateProfileId, null, cancellationToken);
        
        var response = new PagedResult<ApplicationResponse>(
            result.Items.Select(MapToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize
        );

        return Result<PagedResult<ApplicationResponse>>.Success(response);
    }

    public async Task<Result> WithdrawApplicationAsync(Guid id, Guid candidateProfileId, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(id, cancellationToken);
        if (application == null)
            return Result.NotFound("Application not found");

        if (application.CandidateProfileId != candidateProfileId)
            return Result.Forbidden();

        if (application.Status == ApplicationStatus.Rejected || application.Status == ApplicationStatus.Hired || application.Status == ApplicationStatus.Withdrawn)
            return Result.Failure("Cannot withdraw an application in its current state");

        var oldStatus = application.Status;
        application.Status = ApplicationStatus.Withdrawn;
        
        application.History.Add(new ApplicationHistory
        {
            FromStatus = oldStatus,
            ToStatus = ApplicationStatus.Withdrawn,
            Notes = "Withdrawn by candidate"
        });

        await _applicationRepository.UpdateAsync(application, cancellationToken);
        return Result.Success();
    }

    private static ApplicationResponse MapToResponse(Domain.Entities.Application application)
    {
        return new ApplicationResponse
        {
            Id = application.Id,
            JobId = application.JobId,
            JobTitle = application.Job?.Title ?? string.Empty,
            CompanyName = application.Job?.Company?.Name ?? string.Empty,
            CandidateProfileId = application.CandidateProfileId,
            CandidateName = application.CandidateProfile?.User?.FirstName + " " + application.CandidateProfile?.User?.LastName,
            Status = application.Status.ToString(),
            SubmittedAt = application.SubmittedAt,
            CoverLetter = application.CoverLetter,
            AiScore = application.AiScore,
            AiRecommendation = application.AiRecommendation,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt
        };
    }
}
