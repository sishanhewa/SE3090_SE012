using System;
using System.Collections.Generic;
using TalentFlow.Domain.Common;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Domain.Entities;

/// <summary>
/// Persists Agentic AI workflow execution state.
/// </summary>
public class WorkflowExecution : BaseEntity
{
    public string Objective { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Planning;
    public string? Plan { get; set; }  // JSON structured plan
    public string? FinalResult { get; set; } // JSON final result
    public string? ErrorDetails { get; set; }

    public Guid InitiatedById { get; set; }
    public ApplicationUser InitiatedBy { get; set; } = null!;

    public Guid? CompanyId { get; set; }
    public Guid? RelatedEntityId { get; set; } // e.g., ApplicationId
    public string? RelatedEntityType { get; set; } // e.g., "Application"

    public DateTime? CompletedAt { get; set; }

    // Navigation
    public ICollection<AgentStep> AgentSteps { get; set; } = new List<AgentStep>();
    public ICollection<WorkflowValidationResult> ValidationResults { get; set; } = new List<WorkflowValidationResult>();
    public ICollection<WorkflowApproval> Approvals { get; set; } = new List<WorkflowApproval>();
}

public class AgentStep : BaseEntity
{
    public Guid WorkflowExecutionId { get; set; }
    public WorkflowExecution WorkflowExecution { get; set; } = null!;

    public string AgentName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string? Input { get; set; } // JSON
    public string? Output { get; set; } // JSON
    public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorDetails { get; set; }

    // Navigation
    public ICollection<ToolCall> ToolCalls { get; set; } = new List<ToolCall>();
}

public class ToolCall : BaseEntity
{
    public Guid AgentStepId { get; set; }
    public AgentStep AgentStep { get; set; } = null!;

    public string ToolName { get; set; } = string.Empty;
    public string? Input { get; set; } // JSON
    public string? Output { get; set; } // JSON
    public bool Validated { get; set; }
    public int DurationMs { get; set; }
}

public class WorkflowValidationResult : BaseEntity
{
    public Guid WorkflowExecutionId { get; set; }
    public WorkflowExecution WorkflowExecution { get; set; } = null!;

    public string ValidationType { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Errors { get; set; } // JSON
    public string? Warnings { get; set; } // JSON
}

public class WorkflowApproval : BaseEntity
{
    public Guid WorkflowExecutionId { get; set; }
    public WorkflowExecution WorkflowExecution { get; set; } = null!;

    public string RequestedAction { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string? Decision { get; set; } // Approved, Rejected, RevisionRequested
    public Guid? DecidedById { get; set; }
    public ApplicationUser? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Comments { get; set; }
}
