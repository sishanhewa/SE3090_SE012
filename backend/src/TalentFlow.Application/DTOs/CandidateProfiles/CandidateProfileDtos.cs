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
}
