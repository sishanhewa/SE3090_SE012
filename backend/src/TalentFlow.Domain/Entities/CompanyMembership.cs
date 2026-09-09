using System;
using TalentFlow.Domain.Common;

namespace TalentFlow.Domain.Entities;

/// <summary>
/// Links users to companies with specific roles.
/// </summary>
public class CompanyMembership : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Role { get; set; } = string.Empty; // Recruiter, HiringManager, etc.
    public bool IsActive { get; set; } = true;
}
