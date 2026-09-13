using TalentFlow.Application.Common;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using Application = TalentFlow.Domain.Entities.Application;

namespace TalentFlow.Application.Interfaces.Repositories;

public interface IApplicationRepository : IRepository<Domain.Entities.Application>
{
    Task<bool> HasAppliedAsync(Guid jobId, Guid candidateProfileId, CancellationToken cancellationToken = default);

    Task<PagedResult<Domain.Entities.Application>> GetApplicationsAsync(
        PaginationParams paginationParams,
        Guid? jobId = null,
        Guid? candidateProfileId = null,
        ApplicationStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Domain.Entities.Application?> GetApplicationWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
