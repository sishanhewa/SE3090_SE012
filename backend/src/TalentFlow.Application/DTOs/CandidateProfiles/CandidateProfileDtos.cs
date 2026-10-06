using System;
using System.ComponentModel.DataAnnotations;

namespace TalentFlow.Application.DTOs.CandidateProfiles;

public class CreateCandidateProfileRequest
{
    public string? Summary { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
}

public class UpdateCandidateProfileRequest : CreateCandidateProfileRequest
{
}

public class CandidateProfileResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Summary { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? ProfileImageUrl { get; set; }

    public System.Collections.Generic.IList<CandidateSkillDto> Skills { get; set; } = new System.Collections.Generic.List<CandidateSkillDto>();
    public System.Collections.Generic.IList<CandidateExperienceDto> Experience { get; set; } = new System.Collections.Generic.List<CandidateExperienceDto>();
    public System.Collections.Generic.IList<CandidateEducationDto> Education { get; set; } = new System.Collections.Generic.List<CandidateEducationDto>();
}

public class CandidateSkillDto
{
    public Guid Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string? ProficiencyLevel { get; set; }
}

public class CandidateExperienceDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public class CandidateEducationDto
{
    public Guid Id { get; set; }
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
