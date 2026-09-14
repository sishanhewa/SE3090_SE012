using TalentFlow.Application.Common;

using TalentFlow.Application.DTOs.Interviews;

namespace TalentFlow.Application.Interfaces.Services;

public interface IInterviewService
{
    Task<Result<InterviewResponse>> ScheduleInterviewAsync(CreateInterviewRequest request, CancellationToken cancellationToken = default);
    Task<Result<InterviewResponse>> GetInterviewByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<InterviewResponse>>> GetInterviewsByApplicationAsync(Guid applicationId, PaginationParams paginationParams, CancellationToken cancellationToken = default);
    Task<Result<InterviewResponse>> UpdateInterviewAsync(Guid id, UpdateInterviewRequest request, CancellationToken cancellationToken = default);
    Task<Result> CancelInterviewAsync(Guid id, CancellationToken cancellationToken = default);
}
