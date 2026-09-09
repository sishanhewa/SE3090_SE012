using System;
using System.Collections.Generic;
using TalentFlow.Domain.Common;

namespace TalentFlow.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // Navigation
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
