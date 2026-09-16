using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Common;
using TalentFlow.Application.DTOs.Jobs;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Infrastructure.Services;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;

    public JobService(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<Result<JobResponse>> CreateJobAsync(CreateJobRequest request, Guid companyId, CancellationToken cancellationToken = default)
    {
        var job = new Job
        {
            Title = request.Title,
            Description = request.Description,
            EmploymentType = request.EmploymentType,
            Location = request.Location,
            MinimumExperience = request.MinimumExperience,
            VacancyCount = request.VacancyCount,
            ApplicationDeadline = request.ApplicationDeadline,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            CompanyId = companyId,
            DepartmentId = request.DepartmentId,
            Status = JobStatus.Draft
        };

        // Add requirements
        if (request.Requirements != null)
        {
            foreach (var req in request.Requirements)
            {
                job.Requirements.Add(new JobRequirement { Description = req.Description, IsMandatory = req.IsMandatory, Weight = req.Weight });
            }
        }

        await _jobRepository.AddAsync(job, cancellationToken);

        return Result<JobResponse>.Success(MapToResponse(job));
    }

    public async Task<Result<JobResponse>> GetJobByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null)
            return Result<JobResponse>.NotFound("Job not found");

        return Result<JobResponse>.Success(MapToResponse(job));
    }

    public async Task<Result<PagedResult<JobResponse>>> GetJobsAsync(JobSearchParams searchParams, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var result = await _jobRepository.GetJobsAsync(searchParams, companyId, cancellationToken);
        
        var response = new PagedResult<JobResponse>(
            result.Items.Select(MapToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize
        );

        return Result<PagedResult<JobResponse>>.Success(response);
    }

    public async Task<Result<JobResponse>> UpdateJobAsync(Guid id, UpdateJobRequest request, Guid companyId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null)
            return Result<JobResponse>.NotFound("Job not found");

        if (job.CompanyId != companyId)
            return Result<JobResponse>.Forbidden();

        if (request.Title != null) job.Title = request.Title;
        if (request.Description != null) job.Description = request.Description;
        if (request.EmploymentType != null) job.EmploymentType = request.EmploymentType;
        if (request.Location != null) job.Location = request.Location;
        if (request.MinimumExperience.HasValue) job.MinimumExperience = request.MinimumExperience.Value;
        if (request.VacancyCount.HasValue) job.VacancyCount = request.VacancyCount.Value;
        if (request.ApplicationDeadline.HasValue) job.ApplicationDeadline = request.ApplicationDeadline.Value;
        if (request.SalaryMin.HasValue) job.SalaryMin = request.SalaryMin.Value;
        if (request.SalaryMax.HasValue) job.SalaryMax = request.SalaryMax.Value;

        await _jobRepository.UpdateAsync(job, cancellationToken);

        return Result<JobResponse>.Success(MapToResponse(job));
    }

    public async Task<Result> PublishJobAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null)
            return Result.NotFound("Job not found");

        if (job.CompanyId != companyId)
            return Result.Forbidden();

        if (job.Status != JobStatus.Draft && job.Status != JobStatus.Closed)
            return Result.Failure("Job is already published", "InvalidStateTransition");

        // Business rules validation
        if (string.IsNullOrWhiteSpace(job.Title))
            return Result.Failure("Job title is required to publish.", "ValidationFailed");
        
        if (string.IsNullOrWhiteSpace(job.Description))
            return Result.Failure("Job description is required to publish.", "ValidationFailed");

        // Assuming Department is populated or checking DepartmentId if Department navigation is null
        if (job.DepartmentId == Guid.Empty)
            return Result.Failure("Department is required to publish.", "ValidationFailed");

        if (job.VacancyCount <= 0)
            return Result.Failure("Vacancy count must be greater than zero.", "ValidationFailed");

        if (!job.ApplicationDeadline.HasValue || job.ApplicationDeadline.Value <= DateTime.UtcNow)
            return Result.Failure("Application deadline must be in the future.", "ValidationFailed");

        job.Status = JobStatus.Published;
        await _jobRepository.UpdateAsync(job, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CloseJobAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null)
            return Result.NotFound("Job not found");

        if (job.CompanyId != companyId)
            return Result.Forbidden();

        if (job.Status != JobStatus.Published)
            return Result.Failure("Only published jobs can be closed", "InvalidStateTransition");

        job.Status = JobStatus.Closed;
        await _jobRepository.UpdateAsync(job, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ArchiveJobAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null)
            return Result.NotFound("Job not found");

        if (job.CompanyId != companyId)
            return Result.Forbidden();

        if (job.Status != JobStatus.Closed)
            return Result.Failure("Only closed jobs can be archived", "InvalidStateTransition");

        job.Status = JobStatus.Archived;
        await _jobRepository.UpdateAsync(job, cancellationToken);

        return Result.Success();
    }

    private static JobResponse MapToResponse(Job job)
    {
        return new JobResponse
        {
            Id = job.Id,
            Title = job.Title,
            Description = job.Description,
            EmploymentType = job.EmploymentType,
            Location = job.Location,
            MinimumExperience = job.MinimumExperience,
            VacancyCount = job.VacancyCount,
            ApplicationDeadline = job.ApplicationDeadline,
            Status = job.Status.ToString(),
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            CompanyId = job.CompanyId,
            DepartmentId = job.DepartmentId,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };
    }
}
