using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Jobs;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Application.Interfaces.Repositories;

public interface IJobRepository : IRepository<Job>
{
    Task<PagedResult<Job>> GetJobsAsync(
        JobSearchParams searchParams,
        Guid? companyId = null,
        CancellationToken cancellationToken = default);

    Task<Job?> GetJobWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
