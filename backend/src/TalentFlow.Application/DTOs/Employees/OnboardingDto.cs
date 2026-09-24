namespace TalentFlow.Application.DTOs.Employees;

public class OnboardingTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CompanyId { get; set; }
    public List<OnboardingTaskResponse> Tasks { get; set; } = new();
}

public class OnboardingTaskResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsMandatory { get; set; }
}

public class EmployeeOnboardingTaskResponse
{
    public Guid Id { get; set; }
    public Guid OnboardingTaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string? TaskDescription { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateOnboardingTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CreateOnboardingTaskRequest> Tasks { get; set; } = new();
}

public class CreateOnboardingTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsMandatory { get; set; } = true;
}

public class CompleteOnboardingTaskRequest
{
    public string? Notes { get; set; }
}
