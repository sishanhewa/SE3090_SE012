using System;
using System.ComponentModel.DataAnnotations;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Application.DTOs.Offers;

public class CreateOfferRequest
{
    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public string Position { get; set; } = string.Empty;

    [Required]
    public decimal Salary { get; set; }

    public string? EmploymentType { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    public string? AdditionalTerms { get; set; }
}

public class UpdateOfferRequest
{
    public string? Position { get; set; }
    public decimal? Salary { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? AdditionalTerms { get; set; }
}

public class OfferResponse
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdditionalTerms { get; set; }
    public DateTime CreatedAt { get; set; }
}
