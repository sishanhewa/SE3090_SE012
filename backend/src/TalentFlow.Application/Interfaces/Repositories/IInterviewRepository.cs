using TalentFlow.Application.Common;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Application.Interfaces.Repositories;

public interface IInterviewRepository : IRepository<Interview>
{
    Task<PagedResult<Interview>> GetInterviewsAsync(
        PaginationParams paginationParams,
        Guid? applicationId = null,
        InterviewStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Interview?> GetInterviewWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check for overlapping interviews for a candidate or interviewer.
    /// </summary>
    Task<bool> HasConflictAsync(
        Guid userId,
        DateTime scheduledAt,
        int durationMinutes,
        Guid? excludeInterviewId = null,
        CancellationToken cancellationToken = default);
}
