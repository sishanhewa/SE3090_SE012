using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Jobs;

namespace TalentFlow.Application.Interfaces.Services;

public interface IJobService
{
    Task<Result<JobResponse>> CreateJobAsync(CreateJobRequest request, Guid companyId, CancellationToken cancellationToken = default);
    Task<Result<JobResponse>> GetJobByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<JobResponse>>> GetJobsAsync(JobSearchParams searchParams, Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<Result<JobResponse>> UpdateJobAsync(Guid id, UpdateJobRequest request, Guid companyId, CancellationToken cancellationToken = default);
    Task<Result> PublishJobAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<Result> CloseJobAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<Result> ArchiveJobAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}
