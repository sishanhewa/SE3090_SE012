using System;
using System.Collections.Generic;
using TalentFlow.Domain.Common;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Domain.Entities;

public class Job : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? EmploymentType { get; set; } // FullTime, PartTime, Contract
    public string? Location { get; set; }
    public int MinimumExperience { get; set; }
    public int VacancyCount { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Draft;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    // Navigation
    public ICollection<JobRequirement> Requirements { get; set; } = new List<JobRequirement>();
    public ICollection<JobSkillRequirement> SkillRequirements { get; set; } = new List<JobSkillRequirement>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
