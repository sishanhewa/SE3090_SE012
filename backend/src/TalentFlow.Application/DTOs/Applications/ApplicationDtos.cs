using System.ComponentModel.DataAnnotations;

namespace TalentFlow.Application.DTOs.Applications;

public class CreateApplicationRequest
{
    [MaxLength(5000)]
    public string? CoverLetter { get; set; }

    public Guid? ResumeDocumentId { get; set; }
}

public class ApplicationResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid CandidateProfileId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? CoverLetter { get; set; }
    public decimal? AiScore { get; set; }
    public string? AiRecommendation { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
