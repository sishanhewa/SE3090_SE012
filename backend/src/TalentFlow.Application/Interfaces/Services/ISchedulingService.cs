using TalentFlow.Application.Common;

namespace TalentFlow.Application.Interfaces.Services;

/// <summary>
/// Handles interview scheduling conflict detection.
/// </summary>
public interface ISchedulingService
{
    /// <summary>
    /// Check if a candidate has overlapping interviews at the proposed time.
    /// </summary>
    Task<Result<bool>> HasCandidateConflictAsync(
        Guid candidateProfileId, DateTime proposedStart, int durationMinutes,
        Guid? excludeInterviewId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an interviewer has overlapping interviews at the proposed time.
    /// </summary>
    Task<Result<bool>> HasInterviewerConflictAsync(
        Guid interviewerId, DateTime proposedStart, int durationMinutes,
        Guid? excludeInterviewId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available time slots for a candidate-interviewer pair within a date range.
    /// </summary>
    Task<Result<List<AvailableSlot>>> GetAvailableSlotsAsync(
        Guid candidateProfileId, List<Guid> interviewerIds,
        DateTime rangeStart, DateTime rangeEnd,
        int durationMinutes = 60, CancellationToken cancellationToken = default);
}

public class AvailableSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool AllInterviewersAvailable { get; set; }
    public List<Guid> AvailableInterviewerIds { get; set; } = new();
}
