using System;
using System.ComponentModel.DataAnnotations;

namespace TalentFlow.Application.DTOs.Employees;

public class CreateEmployeeRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    [Required]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    public string Position { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    public Guid? ApplicationId { get; set; }
}

public class UpdateEmployeeRequest
{
    public Guid? DepartmentId { get; set; }
    public string? Position { get; set; }
    public string? Status { get; set; } // Onboarding, Active, Terminated
}

public class EmployeeResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid DepartmentId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
