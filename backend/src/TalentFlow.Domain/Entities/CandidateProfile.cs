using System;
using System.Collections.Generic;
using TalentFlow.Domain.Common;

namespace TalentFlow.Domain.Entities;

public class CandidateProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? Summary { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? ProfileImageUrl { get; set; }

    // Navigation
    public ICollection<CandidateSkill> Skills { get; set; } = new List<CandidateSkill>();
    public ICollection<CandidateEducation> Education { get; set; } = new List<CandidateEducation>();
    public ICollection<CandidateExperience> Experience { get; set; } = new List<CandidateExperience>();
    public ICollection<CandidateDocument> Documents { get; set; } = new List<CandidateDocument>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}

public class CandidateSkill : BaseEntity
{
    public Guid CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;

    public int YearsOfExperience { get; set; }
    public string? ProficiencyLevel { get; set; } // Beginner, Intermediate, Advanced, Expert
}

public class CandidateEducation : BaseEntity
{
    public Guid CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Grade { get; set; }
}

public class CandidateExperience : BaseEntity
{
    public Guid CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public class CandidateDocument : BaseEntity
{
    public Guid CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // CV, Certificate, Portfolio
    public long FileSizeBytes { get; set; }
}
