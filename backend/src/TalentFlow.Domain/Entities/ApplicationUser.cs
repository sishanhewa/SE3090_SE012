using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace TalentFlow.Domain.Entities;

/// <summary>
/// Extends ASP.NET Identity User with application-specific fields.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CandidateProfile? CandidateProfile { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<CompanyMembership> CompanyMemberships { get; set; } = new List<CompanyMembership>();
}
