using System.Collections.Generic;
using TalentFlow.Domain.Common;

namespace TalentFlow.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<CompanyMembership> Memberships { get; set; } = new List<CompanyMembership>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
