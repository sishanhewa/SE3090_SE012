using System;
using System.Collections.Generic;
using TalentFlow.Domain.Common;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Domain.Entities;

public class Application : BaseEntity
{
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;

    public Guid CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string? CoverLetter { get; set; }
    public Guid? ResumeDocumentId { get; set; }

    // AI Assessment
    public decimal? AiScore { get; set; }
    public string? AiRecommendation { get; set; }

    // Navigation
    public ICollection<ApplicationHistory> History { get; set; } = new List<ApplicationHistory>();
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    public Offer? Offer { get; set; }
}

public class ApplicationHistory : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public ApplicationStatus FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }
    public string? ChangedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
