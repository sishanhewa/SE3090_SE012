using TalentFlow.Application.Common;

namespace TalentFlow.Application.Interfaces.Services;

/// <summary>
/// Manages AI workflow executions — creation, status tracking, and approval.
/// </summary>
public interface IWorkflowService
{
    Task<Result<WorkflowExecutionResponse>> StartScreeningWorkflowAsync(
        StartScreeningRequest request, Guid initiatedById, Guid companyId,
        CancellationToken cancellationToken = default);

    Task<Result<WorkflowExecutionResponse>> GetWorkflowAsync(
        Guid workflowId, CancellationToken cancellationToken = default);

    Task<Result<List<WorkflowExecutionResponse>>> GetWorkflowsByCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default);

    Task<Result<WorkflowExecutionResponse>> ApproveWorkflowAsync(
        Guid workflowId, Guid decidedById, string? comments,
        CancellationToken cancellationToken = default);

    Task<Result<WorkflowExecutionResponse>> RejectWorkflowAsync(
        Guid workflowId, Guid decidedById, string? comments,
        CancellationToken cancellationToken = default);

    Task<Result<WorkflowExecutionResponse>> RequestRevisionAsync(
        Guid workflowId, Guid decidedById, string comments,
        CancellationToken cancellationToken = default);

    Task<Result<WorkflowExecutionResponse>> UpdateWorkflowFromAiAsync(
        AiWorkflowCallbackRequest request,
        CancellationToken cancellationToken = default);
}

public class AiWorkflowCallbackRequest
{
    public Guid WorkflowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FinalResult { get; set; }
    public string? ErrorDetails { get; set; }
    public List<AiAgentStepRequest> AgentSteps { get; set; } = new();
}

public class AiAgentStepRequest
{
    public string AgentName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Input { get; set; }
    public string? Output { get; set; }
    public List<AiToolCallRequest> ToolCalls { get; set; } = new();
}

public class AiToolCallRequest
{
    public string ToolName { get; set; } = string.Empty;
    public string? Input { get; set; }
    public string? Output { get; set; }
    public bool Validated { get; set; }
    public int DurationMs { get; set; }
}


public class StartScreeningRequest
{
    public Guid ApplicationId { get; set; }
    public Guid JobId { get; set; }
}

public class WorkflowExecutionResponse
{
    public Guid Id { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Plan { get; set; }
    public string? FinalResult { get; set; }
    public string? ErrorDetails { get; set; }
    public Guid InitiatedById { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<AgentStepResponse> AgentSteps { get; set; } = new();
    public List<WorkflowApprovalResponse> Approvals { get; set; } = new();
    public List<ValidationResultResponse> ValidationResults { get; set; } = new();
}

public class AgentStepResponse
{
    public Guid Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Input { get; set; }
    public string? Output { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorDetails { get; set; }
    public List<ToolCallResponse> ToolCalls { get; set; } = new();
}

public class ToolCallResponse
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string? Input { get; set; }
    public string? Output { get; set; }
    public bool Validated { get; set; }
    public int DurationMs { get; set; }
}

public class WorkflowApprovalResponse
{
    public Guid Id { get; set; }
    public string RequestedAction { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? Decision { get; set; }
    public Guid? DecidedById { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Comments { get; set; }
}

public class ValidationResultResponse
{
    public Guid Id { get; set; }
    public string ValidationType { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Errors { get; set; }
    public string? Warnings { get; set; }
}
