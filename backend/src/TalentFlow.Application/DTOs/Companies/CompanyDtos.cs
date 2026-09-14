using System;

namespace TalentFlow.Application.DTOs.Companies;

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Address { get; set; }
}

public class UpdateCompanyRequest : CreateCompanyRequest
{
}

public class CompanyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
