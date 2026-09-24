namespace TalentFlow.Application.DTOs.Applications;

/// <summary>
/// Deterministic candidate scoring model.
/// Scores are calculated by backend business logic, NOT by LLM.
/// </summary>
public class CandidateScoreResult
{
    public Guid ApplicationId { get; set; }
    public Guid CandidateProfileId { get; set; }
    public Guid JobId { get; set; }

    // Score breakdown (out of 100)
    public int MandatorySkillsScore { get; set; }      // max 40
    public int PreferredSkillsScore { get; set; }       // max 20
    public int ExperienceScore { get; set; }            // max 25
    public int EducationScore { get; set; }             // max 10
    public int CertificationScore { get; set; }         // max 5
    public int TotalScore { get; set; }                 // max 100

    // Skill details
    public List<SkillMatch> MandatorySkillMatches { get; set; } = new();
    public List<SkillMatch> PreferredSkillMatches { get; set; } = new();

    // Experience
    public decimal TotalYearsExperience { get; set; }
    public int MinimumRequiredYears { get; set; }
    public bool MeetsExperienceRequirement { get; set; }

    // Classification
    public string Recommendation { get; set; } = string.Empty; // EligibleForInterview, ManualReview, NotRecommended
    public string RecommendationReason { get; set; } = string.Empty;
}

public class SkillMatch
{
    public string SkillName { get; set; } = string.Empty;
    public bool IsMatched { get; set; }
    public string? CandidateProficiency { get; set; }
    public int CandidateYearsExperience { get; set; }
    public int Weight { get; set; }
    public int ScoreAwarded { get; set; }
}
