using TalentFlow.Application.Common;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Application.Interfaces.Repositories;

public interface IJobRepository : IRepository<Job>
{
    Task<PagedResult<Job>> GetJobsAsync(
        PaginationParams paginationParams,
        Guid? companyId = null,
        JobStatus? status = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<Job?> GetJobWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
