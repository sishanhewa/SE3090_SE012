using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Applications;

namespace TalentFlow.Application.Interfaces.Services;

public interface IApplicationService
{
    Task<Result<ApplicationResponse>> CreateApplicationAsync(Guid jobId, CreateApplicationRequest request, Guid candidateProfileId, CancellationToken cancellationToken = default);
    Task<Result<ApplicationResponse>> GetApplicationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ApplicationResponse>>> GetApplicationsAsync(PaginationParams paginationParams, Guid? jobId = null, Guid? candidateProfileId = null, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ApplicationResponse>>> GetMyApplicationsAsync(Guid candidateProfileId, PaginationParams paginationParams, CancellationToken cancellationToken = default);
    Task<Result> WithdrawApplicationAsync(Guid id, Guid candidateProfileId, CancellationToken cancellationToken = default);
    Task<Result> UpdateApplicationStatusAsync(Guid id, TalentFlow.Domain.Enums.ApplicationStatus newStatus, string changedBy, string? notes = null, CancellationToken cancellationToken = default);
    // Task<Result> UploadDocumentAsync(Guid applicationId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default); // Placeholder for document upload
}
