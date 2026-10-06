using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly TimeZoneInfo _businessTimeZone;

    public SchedulingService(AppDbContext context, IConfiguration? configuration = null)
    {
        _context = context;
        var zoneId = configuration?["Scheduling:TimeZoneId"] ?? "UTC";
        try { _businessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(zoneId); }
        catch (TimeZoneNotFoundException) { _businessTimeZone = TimeZoneInfo.Utc; }
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
        if (durationMinutes is < 15 or > 480 || rangeEnd <= rangeStart)
            return Result<List<AvailableSlot>>.Failure("Invalid interview time range or duration.");

        // Generate business-hour slots in the configured company time zone, then persist UTC.
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(rangeStart.ToUniversalTime(), _businessTimeZone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(rangeEnd.ToUniversalTime(), _businessTimeZone);
        var currentLocal = localStart.Date.AddHours(9);
        while (currentLocal.AddMinutes(durationMinutes) <= localEnd)
        {
            if (_businessTimeZone.IsInvalidTime(currentLocal))
            {
                currentLocal = currentLocal.AddHours(1);
                continue;
            }
            var current = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(currentLocal, DateTimeKind.Unspecified), _businessTimeZone);
            if (current <= DateTime.UtcNow.AddMinutes(30) || current < rangeStart ||
                current.AddMinutes(durationMinutes) > rangeEnd)
            {
                currentLocal = currentLocal.AddHours(1);
                continue;
            }
            // Skip weekends
            if (currentLocal.DayOfWeek == DayOfWeek.Saturday || currentLocal.DayOfWeek == DayOfWeek.Sunday)
            {
                currentLocal = currentLocal.AddDays(1).Date.AddHours(9);
                continue;
            }

            // Skip outside business hours
            if (currentLocal.TimeOfDay < TimeSpan.FromHours(9) ||
                currentLocal.AddMinutes(durationMinutes).Date != currentLocal.Date ||
                currentLocal.AddMinutes(durationMinutes).TimeOfDay > TimeSpan.FromHours(17))
            {
                currentLocal = currentLocal.AddDays(1).Date.AddHours(9);
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

                if (interviewerIds.Count == 0 || availableInterviewers.Count == interviewerIds.Count)
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

            currentLocal = currentLocal.AddHours(1);
        }

        return Result<List<AvailableSlot>>.Success(slots.Take(10).ToList());
    }
}
