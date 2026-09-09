using System;
using System.Collections.Generic;
using TalentFlow.Domain.Common;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Domain.Entities;

public class Interview : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public InterviewStatus Status { get; set; } = InterviewStatus.Proposed;
    public string? MeetingUrl { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public string? CalendarEventId { get; set; }

    // Navigation
    public ICollection<InterviewPanelMember> PanelMembers { get; set; } = new List<InterviewPanelMember>();
    public ICollection<InterviewFeedback> Feedback { get; set; } = new List<InterviewFeedback>();
}

public class InterviewPanelMember : BaseEntity
{
    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Role { get; set; } = string.Empty; // Lead, Panelist, Observer
}

public class InterviewFeedback : BaseEntity
{
    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    public Guid ReviewerId { get; set; }
    public ApplicationUser Reviewer { get; set; } = null!;

    public int TechnicalScore { get; set; }
    public int CommunicationScore { get; set; }
    public int ExperienceScore { get; set; }
    public int OverallScore { get; set; }
    public string? Comments { get; set; }
    public string? Recommendation { get; set; } // StrongHire, Hire, NoHire, StrongNoHire
}

public class HiringDecision : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public string Decision { get; set; } = string.Empty; // ProceedToOffer, Reject, Hold
    public string? Reason { get; set; }

    public Guid DecidedById { get; set; }
    public ApplicationUser DecidedBy { get; set; } = null!;
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}

public class Offer : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public string Position { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public OfferStatus Status { get; set; } = OfferStatus.Draft;
    public string? AdditionalTerms { get; set; }

    public Guid? ApprovedById { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
