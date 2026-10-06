using TalentFlow.Application.Common;

namespace TalentFlow.Application.Interfaces.Services;

/// <summary>
/// Service interface for Google Calendar integration.
/// Used for creating, updating, and deleting interview calendar events.
/// </summary>
public interface IGoogleCalendarService
{
    /// <summary>
    /// Create a calendar event for an interview.
    /// </summary>
    Task<Result<CalendarEventResult>> CreateInterviewEventAsync(
        CalendarEventRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing calendar event.
    /// </summary>
    Task<Result<CalendarEventResult>> UpdateInterviewEventAsync(
        string eventId, CalendarEventRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a calendar event.
    /// </summary>
    Task<Result> DeleteInterviewEventAsync(
        string eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check availability of a user in a time range via Google Calendar.
    /// </summary>
    Task<Result<List<CalendarBusySlot>>> GetBusySlotsAsync(
        string email, DateTime rangeStart, DateTime rangeEnd,
        CancellationToken cancellationToken = default);
}

public class CalendarEventRequest
{
    public string? EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    public List<string> AttendeeEmails { get; set; } = new();
}

public class CalendarEventResult
{
    public string EventId { get; set; } = string.Empty;
    public string? HtmlLink { get; set; }
    public string? MeetLink { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class CalendarBusySlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
