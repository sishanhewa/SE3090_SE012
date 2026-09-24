using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Services;

/// <summary>
/// Detects scheduling conflicts and finds available interview slots.
/// </summary>
public class SchedulingService : ISchedulingService
{
    private readonly AppDbContext _context;

    public SchedulingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> HasCandidateConflictAsync(
        Guid candidateProfileId, DateTime proposedStart, int durationMinutes,
        Guid? excludeInterviewId = null, CancellationToken cancellationToken = default)
    {
        var proposedEnd = proposedStart.AddMinutes(durationMinutes);

        var conflicting = await _context.Interviews
            .Include(i => i.Application)
            .Where(i => i.Application.CandidateProfileId == candidateProfileId)
            .Where(i => i.Status != InterviewStatus.Cancelled)
            .Where(i => excludeInterviewId == null || i.Id != excludeInterviewId.Value)
            .Where(i => i.ScheduledAt < proposedEnd &&
                        i.ScheduledAt.AddMinutes(i.DurationMinutes) > proposedStart)
            .AnyAsync(cancellationToken);

        return Result<bool>.Success(conflicting);
    }

    public async Task<Result<bool>> HasInterviewerConflictAsync(
        Guid interviewerId, DateTime proposedStart, int durationMinutes,
        Guid? excludeInterviewId = null, CancellationToken cancellationToken = default)
    {
        var proposedEnd = proposedStart.AddMinutes(durationMinutes);

        var conflicting = await _context.InterviewPanelMembers
            .Include(pm => pm.Interview)
            .Where(pm => pm.UserId == interviewerId)
            .Where(pm => pm.Interview.Status != InterviewStatus.Cancelled)
            .Where(pm => excludeInterviewId == null || pm.InterviewId != excludeInterviewId.Value)
            .Where(pm => pm.Interview.ScheduledAt < proposedEnd &&
                         pm.Interview.ScheduledAt.AddMinutes(pm.Interview.DurationMinutes) > proposedStart)
            .AnyAsync(cancellationToken);

        return Result<bool>.Success(conflicting);
    }

    public async Task<Result<List<AvailableSlot>>> GetAvailableSlotsAsync(
        Guid candidateProfileId, List<Guid> interviewerIds,
        DateTime rangeStart, DateTime rangeEnd,
        int durationMinutes = 60, CancellationToken cancellationToken = default)
    {
        var slots = new List<AvailableSlot>();

        // Generate candidate slots in business hours (9 AM - 5 PM)
        var current = rangeStart.Date.AddHours(9);
        while (current.AddMinutes(durationMinutes) <= rangeEnd)
        {
            // Skip weekends
            if (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
            {
                current = current.AddDays(1).Date.AddHours(9);
                continue;
            }

            // Skip outside business hours
            if (current.Hour < 9 || current.Hour >= 17)
            {
                current = current.AddDays(1).Date.AddHours(9);
                continue;
            }

            // Check candidate availability
            var candidateConflict = await HasCandidateConflictAsync(
                candidateProfileId, current, durationMinutes,
                cancellationToken: cancellationToken);

            if (!candidateConflict.Data)
            {
                // Check which interviewers are available
                var availableInterviewers = new List<Guid>();
                foreach (var interviewerId in interviewerIds)
                {
                    var conflict = await HasInterviewerConflictAsync(
                        interviewerId, current, durationMinutes,
                        cancellationToken: cancellationToken);

                    if (!conflict.Data)
                        availableInterviewers.Add(interviewerId);
                }

                if (availableInterviewers.Any())
                {
                    slots.Add(new AvailableSlot
                    {
                        StartTime = current,
                        EndTime = current.AddMinutes(durationMinutes),
                        AllInterviewersAvailable = availableInterviewers.Count == interviewerIds.Count,
                        AvailableInterviewerIds = availableInterviewers
                    });
                }
            }

            current = current.AddMinutes(60); // Step by 1 hour
        }

        return Result<List<AvailableSlot>>.Success(slots.Take(10).ToList());
    }
}
