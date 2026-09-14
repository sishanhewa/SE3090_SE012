using System;
using System.ComponentModel.DataAnnotations;

namespace TalentFlow.Application.DTOs.Interviews;

public class CreateInterviewRequest
{
    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    [Range(15, 480)]
    public int DurationMinutes { get; set; } = 60;

    public string? MeetingUrl { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class UpdateInterviewRequest
{
    public DateTime? ScheduledAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? MeetingUrl { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class InterviewResponse
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MeetingUrl { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
