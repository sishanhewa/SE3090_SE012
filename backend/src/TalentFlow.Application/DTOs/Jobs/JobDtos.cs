using System.ComponentModel.DataAnnotations;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Application.DTOs.Jobs;

public class CreateJobRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public int MinimumExperience { get; set; }

    [Range(1, int.MaxValue)]
    public int VacancyCount { get; set; } = 1;

    public DateTime? ApplicationDeadline { get; set; }

    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    public List<CreateJobRequirementRequest> Requirements { get; set; } = new();
    public List<CreateJobSkillRequest> SkillRequirements { get; set; } = new();
}

public class CreateJobRequirementRequest
{
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsMandatory { get; set; } = true;
    public int Weight { get; set; } = 10;
}

public class CreateJobSkillRequest
{
    [Required]
    public Guid SkillId { get; set; }

    public bool IsMandatory { get; set; } = true;
    public int Weight { get; set; } = 10;
}

public class UpdateJobRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(5000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public int? MinimumExperience { get; set; }
    public int? VacancyCount { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
}

public class JobResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? EmploymentType { get; set; }
    public string? Location { get; set; }
    public int MinimumExperience { get; set; }
    public int VacancyCount { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int ApplicationCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
