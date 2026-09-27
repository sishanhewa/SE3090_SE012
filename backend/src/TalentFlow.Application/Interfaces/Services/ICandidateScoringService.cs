using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Applications;

namespace TalentFlow.Application.Interfaces.Services;

/// <summary>
/// Deterministic candidate scoring service.
/// Calculates scores based on configured weights and business rules.
/// </summary>
public interface ICandidateScoringService
{
    /// <summary>
    /// Calculate deterministic score for a candidate against a job.
    /// </summary>
    Task<Result<CandidateScoreResult>> CalculateScoreAsync(
        Guid applicationId, CancellationToken cancellationToken = default);
}
