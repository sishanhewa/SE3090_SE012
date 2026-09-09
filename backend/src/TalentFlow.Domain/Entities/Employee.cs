using System;
using System.Collections.Generic;
using TalentFlow.Domain.Common;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Domain.Entities;

public class Employee : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public string EmployeeNumber { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Onboarding;

    // If created from a hire
    public Guid? ApplicationId { get; set; }

    // Navigation
    public ICollection<EmployeeStatusHistory> StatusHistory { get; set; } = new List<EmployeeStatusHistory>();
    public ICollection<EmployeeOnboardingTask> OnboardingTasks { get; set; } = new List<EmployeeOnboardingTask>();
}

public class EmployeeStatusHistory : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public EmployeeStatus FromStatus { get; set; }
    public EmployeeStatus ToStatus { get; set; }
    public string? ChangedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public class OnboardingTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public ICollection<OnboardingTask> Tasks { get; set; } = new List<OnboardingTask>();
}

public class OnboardingTask : BaseEntity
{
    public Guid OnboardingTemplateId { get; set; }
    public OnboardingTemplate OnboardingTemplate { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsMandatory { get; set; } = true;
}

public class EmployeeOnboardingTask : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid OnboardingTaskId { get; set; }
    public OnboardingTask OnboardingTask { get; set; } = null!;

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}
