using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;
using TalentFlow.Infrastructure.AI;

namespace TalentFlow.Infrastructure.Services;

/// <summary>
/// Manages AI workflow execution lifecycle including creation,
/// status tracking, and human approval flow.
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly AppDbContext _context;
    private readonly IAgentServiceClient _agentServiceClient;

    public WorkflowService(AppDbContext context, IAgentServiceClient agentServiceClient)
    {
        _context = context;
        _agentServiceClient = agentServiceClient;
    }

    public async Task<Result<WorkflowExecutionResponse>> StartScreeningWorkflowAsync(
        StartScreeningRequest request, Guid initiatedById, Guid companyId,
        CancellationToken cancellationToken = default)
    {
        // Validate application exists and is in correct state
        var application = await _context.Applications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application == null)
            return Result<WorkflowExecutionResponse>.NotFound("Application not found.");

        if (application.Status != ApplicationStatus.Submitted &&
            application.Status != ApplicationStatus.Screening)
            return Result<WorkflowExecutionResponse>.Failure(
                "Application must be in Submitted or Screening status to start screening.");

        // Create workflow execution
        var workflow = new WorkflowExecution
        {
            Objective = $"Evaluate application {application.Id} for {application.Job.Title} " +
                        "and determine whether the candidate should proceed to interview.",
            Status = WorkflowStatus.Planning,
            InitiatedById = initiatedById,
            CompanyId = companyId,
            RelatedEntityId = request.ApplicationId,
            RelatedEntityType = "Application"
        };

        // Update application status to Screening
        application.Status = ApplicationStatus.Screening;
        _context.ApplicationHistory.Add(new ApplicationHistory
        {
            ApplicationId = application.Id,
            FromStatus = ApplicationStatus.Submitted,
            ToStatus = ApplicationStatus.Screening,
            ChangedBy = "System",
            Notes = "AI screening workflow initiated."
        });

        _context.WorkflowExecutions.Add(workflow);
        await _context.SaveChangesAsync(cancellationToken);

        // Trigger AI Agent Service
        _ = _agentServiceClient.StartScreeningWorkflowAsync(
            workflow.Id.ToString(),
            application.Id.ToString(),
            application.JobId.ToString(),
            initiatedById.ToString(),
            companyId.ToString()
        );

        return Result<WorkflowExecutionResponse>.Success(MapToResponse(workflow));
    }

    public async Task<Result<WorkflowExecutionResponse>> GetWorkflowAsync(
        Guid workflowId, CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowWithDetailsAsync(workflowId, cancellationToken);
        if (workflow == null)
            return Result<WorkflowExecutionResponse>.NotFound("Workflow not found.");

        return Result<WorkflowExecutionResponse>.Success(MapToResponse(workflow));
    }

    public async Task<Result<List<WorkflowExecutionResponse>>> GetWorkflowsByCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        var workflows = await _context.WorkflowExecutions
            .Include(w => w.AgentSteps)
                .ThenInclude(s => s.ToolCalls)
            .Include(w => w.Approvals)
            .Include(w => w.ValidationResults)
            .Where(w => w.CompanyId == companyId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<List<WorkflowExecutionResponse>>.Success(
            workflows.Select(MapToResponse).ToList());
    }

    public async Task<Result<WorkflowExecutionResponse>> ApproveWorkflowAsync(
        Guid workflowId, Guid decidedById, string? comments,
        CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowWithDetailsAsync(workflowId, cancellationToken);
        if (workflow == null)
            return Result<WorkflowExecutionResponse>.NotFound("Workflow not found.");

        if (workflow.Status != WorkflowStatus.AwaitingApproval)
            return Result<WorkflowExecutionResponse>.Failure(
                "Workflow is not awaiting approval.");

        workflow.Status = WorkflowStatus.Approved;
        _context.WorkflowApprovals.Add(new WorkflowApproval
        {
            WorkflowExecutionId = workflowId,
            RequestedAction = "Approve screening recommendation",
            Decision = "Approved",
            DecidedById = decidedById,
            DecidedAt = DateTime.UtcNow,
            Comments = comments
        });

        if (workflow.RelatedEntityType == "Application" && workflow.RelatedEntityId.HasValue)
        {
            var application = await _context.Applications.FindAsync(new object[] { workflow.RelatedEntityId.Value }, cancellationToken);
            if (application != null)
            {
                var oldStatus = application.Status;
                application.Status = ApplicationStatus.Interview;
                _context.ApplicationHistory.Add(new ApplicationHistory
                {
                    ApplicationId = application.Id,
                    FromStatus = oldStatus,
                    ToStatus = ApplicationStatus.Interview,
                    ChangedBy = "System",
                    Notes = "Screening approved. Proceeding to interview."
                });

                if (!string.IsNullOrEmpty(workflow.FinalResult))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(workflow.FinalResult);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("interview_proposal", out var ip) && 
                            ip.TryGetProperty("proposed_slots", out var slots) && 
                            slots.GetArrayLength() > 0)
                        {
                            var firstSlot = slots[0];
                            if (firstSlot.TryGetProperty("start_time", out var startTimeProp) && 
                                DateTime.TryParse(startTimeProp.GetString(), out var startTime))
                            {
                                _context.Interviews.Add(new Interview
                                {
                                    ApplicationId = application.Id,
                                    ScheduledAt = startTime.ToUniversalTime(),
                                    DurationMinutes = 60,
                                    Status = InterviewStatus.Proposed,
                                    Notes = "Proposed by Agentic AI based on calendar availability"
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Ignore JSON parsing errors silently
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<WorkflowExecutionResponse>.Success(MapToResponse(workflow));
    }

    public async Task<Result<WorkflowExecutionResponse>> RejectWorkflowAsync(

        Guid workflowId, Guid decidedById, string? comments,
        CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowWithDetailsAsync(workflowId, cancellationToken);
        if (workflow == null)
            return Result<WorkflowExecutionResponse>.NotFound("Workflow not found.");

        if (workflow.Status != WorkflowStatus.AwaitingApproval)
            return Result<WorkflowExecutionResponse>.Failure(
                "Workflow is not awaiting approval.");

        workflow.Status = WorkflowStatus.Rejected;
        workflow.CompletedAt = DateTime.UtcNow;
        _context.WorkflowApprovals.Add(new WorkflowApproval
        {
            WorkflowExecutionId = workflowId,
            RequestedAction = "Approve screening recommendation",
            Decision = "Rejected",
            DecidedById = decidedById,
            DecidedAt = DateTime.UtcNow,
            Comments = comments
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result<WorkflowExecutionResponse>.Success(MapToResponse(workflow));
    }

    public async Task<Result<WorkflowExecutionResponse>> RequestRevisionAsync(
        Guid workflowId, Guid decidedById, string comments,
        CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowWithDetailsAsync(workflowId, cancellationToken);
        if (workflow == null)
            return Result<WorkflowExecutionResponse>.NotFound("Workflow not found.");

        if (workflow.Status != WorkflowStatus.AwaitingApproval)
            return Result<WorkflowExecutionResponse>.Failure(
                "Workflow is not awaiting approval.");

        workflow.Status = WorkflowStatus.Planning; // Re-run
        _context.WorkflowApprovals.Add(new WorkflowApproval
        {
            WorkflowExecutionId = workflowId,
            RequestedAction = "Approve screening recommendation",
            Decision = "RevisionRequested",
            DecidedById = decidedById,
            DecidedAt = DateTime.UtcNow,
            Comments = comments
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result<WorkflowExecutionResponse>.Success(MapToResponse(workflow));
    }

    public async Task<Result<WorkflowExecutionResponse>> UpdateWorkflowFromAiAsync(
        AiWorkflowCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _context.WorkflowExecutions
            .Include(w => w.AgentSteps)
            .FirstOrDefaultAsync(w => w.Id == request.WorkflowId, cancellationToken);

        if (workflow == null)
            return Result<WorkflowExecutionResponse>.NotFound("Workflow not found.");

        // Update status
        if (Enum.TryParse<WorkflowStatus>(request.Status, true, out var newStatus))
            workflow.Status = newStatus;

        workflow.FinalResult = request.FinalResult;
        workflow.ErrorDetails = request.ErrorDetails;

        if (newStatus == WorkflowStatus.AwaitingApproval ||
            newStatus == WorkflowStatus.Completed ||
            newStatus == WorkflowStatus.Failed)
        {
            if (newStatus != WorkflowStatus.AwaitingApproval)
                workflow.CompletedAt = DateTime.UtcNow;
        }

        // Add agent steps
        foreach (var step in request.AgentSteps)
        {
            var agentStep = new AgentStep
            {
                WorkflowExecutionId = workflow.Id,
                AgentName = step.AgentName,
                StepOrder = step.StepOrder,
                Status = step.Status,
                Input = step.Input,
                Output = step.Output,
                StartedAt = DateTime.UtcNow.AddSeconds(-1),
                CompletedAt = DateTime.UtcNow,
            };

            foreach (var tc in step.ToolCalls)
            {
                agentStep.ToolCalls.Add(new ToolCall
                {
                    ToolName = tc.ToolName,
                    Input = tc.Input,
                    Output = tc.Output,
                    Validated = tc.Validated,
                    DurationMs = tc.DurationMs,
                });
            }

            _context.AgentSteps.Add(agentStep);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Reload with full details for response
        var updated = await GetWorkflowWithDetailsAsync(workflow.Id, cancellationToken);
        return Result<WorkflowExecutionResponse>.Success(MapToResponse(updated!));
    }

    private async Task<WorkflowExecution?> GetWorkflowWithDetailsAsync(
        Guid workflowId, CancellationToken cancellationToken)
    {
        return await _context.WorkflowExecutions
            .Include(w => w.AgentSteps)
                .ThenInclude(s => s.ToolCalls)
            .Include(w => w.Approvals)
            .Include(w => w.ValidationResults)
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);
    }

    private static WorkflowExecutionResponse MapToResponse(WorkflowExecution w) => new()
    {
        Id = w.Id,
        Objective = w.Objective,
        Status = w.Status.ToString(),
        Plan = w.Plan,
        FinalResult = w.FinalResult,
        ErrorDetails = w.ErrorDetails,
        InitiatedById = w.InitiatedById,
        CompanyId = w.CompanyId,
        CreatedAt = w.CreatedAt,
        CompletedAt = w.CompletedAt,
        AgentSteps = w.AgentSteps.OrderBy(s => s.StepOrder).Select(s => new AgentStepResponse
        {
            Id = s.Id,
            AgentName = s.AgentName,
            StepOrder = s.StepOrder,
            Status = s.Status,
            Input = s.Input,
            Output = s.Output,
            StartedAt = s.StartedAt,
            CompletedAt = s.CompletedAt,
            ErrorDetails = s.ErrorDetails,
            ToolCalls = s.ToolCalls.Select(tc => new ToolCallResponse
            {
                Id = tc.Id,
                ToolName = tc.ToolName,
                Input = tc.Input,
                Output = tc.Output,
                Validated = tc.Validated,
                DurationMs = tc.DurationMs
            }).ToList()
        }).ToList(),
        Approvals = w.Approvals.OrderByDescending(a => a.RequestedAt).Select(a => new WorkflowApprovalResponse
        {
            Id = a.Id,
            RequestedAction = a.RequestedAction,
            RequestedAt = a.RequestedAt,
            Decision = a.Decision,
            DecidedById = a.DecidedById,
            DecidedAt = a.DecidedAt,
            Comments = a.Comments
        }).ToList(),
        ValidationResults = w.ValidationResults.Select(vr => new ValidationResultResponse
        {
            Id = vr.Id,
            ValidationType = vr.ValidationType,
            Passed = vr.Passed,
            Errors = vr.Errors,
            Warnings = vr.Warnings
        }).ToList()
    };
}
