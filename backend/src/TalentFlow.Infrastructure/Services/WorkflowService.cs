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
using TalentFlow.Application.DTOs.Interviews;
using System.Text.Json;

namespace TalentFlow.Infrastructure.Services;

/// <summary>
/// Manages AI workflow execution lifecycle including creation,
/// status tracking, and human approval flow.
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly AppDbContext _context;
    private readonly IAgentServiceClient _agentServiceClient;
    private readonly IInterviewService? _interviewService;
    private readonly ISchedulingService? _schedulingService;

    public WorkflowService(AppDbContext context, IAgentServiceClient agentServiceClient,
        IInterviewService? interviewService = null, ISchedulingService? schedulingService = null)
    {
        _context = context;
        _agentServiceClient = agentServiceClient;
        _interviewService = interviewService;
        _schedulingService = schedulingService;
    }

    public async Task<Result<WorkflowExecutionResponse>> StartScreeningWorkflowAsync(
        StartScreeningRequest request, Guid initiatedById, Guid companyId,
        CancellationToken cancellationToken = default, string? authToken = null)
    {
        // Validate application exists and is in correct state
        var application = await _context.Applications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application == null)
            return Result<WorkflowExecutionResponse>.NotFound("Application not found.");

        if (application.JobId != request.JobId ||
            (companyId != Guid.Empty && application.Job.CompanyId != companyId))
            return Result<WorkflowExecutionResponse>.Forbidden();

        if (!await _context.Users.AnyAsync(u => u.Id == initiatedById, cancellationToken))
            return Result<WorkflowExecutionResponse>.Failure("Your session is no longer valid. Please sign in again.");

        if (!application.ResumeDocumentId.HasValue)
            return Result<WorkflowExecutionResponse>.Failure("Attach a CV to this application before running AI screening.");

        if (application.Status != ApplicationStatus.Submitted &&
            application.Status != ApplicationStatus.Screening)
            return Result<WorkflowExecutionResponse>.Failure(
                "Application must be in Submitted or Screening status to start screening.");

        if (await _context.WorkflowExecutions.AnyAsync(w =>
                w.RelatedEntityId == application.Id &&
                (w.Status == WorkflowStatus.Planning || w.Status == WorkflowStatus.InProgress ||
                 w.Status == WorkflowStatus.AwaitingApproval), cancellationToken))
            return Result<WorkflowExecutionResponse>.Conflict("Screening is already running or awaiting review.");

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
        var previousStatus = application.Status;
        application.Status = ApplicationStatus.Screening;
        _context.ApplicationHistory.Add(new ApplicationHistory
        {
            ApplicationId = application.Id,
            FromStatus = previousStatus,
            ToStatus = ApplicationStatus.Screening,
            ChangedBy = "System",
            Notes = "AI screening workflow initiated."
        });

        _context.WorkflowExecutions.Add(workflow);
        await _context.SaveChangesAsync(cancellationToken);

        // Trigger AI Agent Service
        var started = await _agentServiceClient.StartScreeningWorkflowAsync(
            workflow.Id.ToString(),
            application.Id.ToString(),
            application.JobId.ToString(),
            initiatedById.ToString(),
            application.Job.CompanyId.ToString(), authToken
        );

        if (!started)
        {
            workflow.Status = WorkflowStatus.Failed;
            workflow.ErrorDetails = "The AI service could not start screening. Please retry.";
            workflow.CompletedAt = DateTime.UtcNow;
            application.Status = previousStatus;
            _context.ApplicationHistory.Add(new ApplicationHistory
            {
                ApplicationId = application.Id,
                FromStatus = ApplicationStatus.Screening,
                ToStatus = previousStatus,
                ChangedBy = "System",
                Notes = workflow.ErrorDetails
            });
            await _context.SaveChangesAsync(cancellationToken);
            return Result<WorkflowExecutionResponse>.Failure(workflow.ErrorDetails);
        }

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
            .Where(w => companyId == Guid.Empty || w.CompanyId == companyId)
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

        if (workflow.ValidationResults.Any(v => !v.Passed) && string.IsNullOrWhiteSpace(comments))
            return Result<WorkflowExecutionResponse>.Failure(
                "Add a review note before overriding failed CV validation checks.");

        workflow.Status = WorkflowStatus.Approved;
        workflow.CompletedAt = DateTime.UtcNow;
        _context.WorkflowApprovals.Add(new WorkflowApproval
        {
            WorkflowExecutionId = workflowId,
            RequestedAction = "Review CV screening and shortlist candidate",
            Decision = "Shortlisted",
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
                if (oldStatus != ApplicationStatus.Screening)
                    return Result<WorkflowExecutionResponse>.Failure("Application is no longer in screening.");
                application.Status = ApplicationStatus.Shortlisted;
                _context.ApplicationHistory.Add(new ApplicationHistory
                {
                    ApplicationId = application.Id,
                    FromStatus = oldStatus,
                    ToStatus = ApplicationStatus.Shortlisted,
                    ChangedBy = decidedById.ToString(),
                    Notes = "Screening reviewed and candidate shortlisted."
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Scheduling is a separate, recoverable side effect. The human shortlist decision
        // remains recorded even when Calendar credentials or a valid slot are unavailable.
        if (workflow.RelatedEntityId is Guid applicationId && _interviewService != null &&
            !await _context.Interviews.AnyAsync(i => i.ApplicationId == applicationId &&
                i.Status != InterviewStatus.Cancelled, cancellationToken))
        {
            var proposedSlots = GetFutureSlots(workflow.FinalResult);
            var application = await _context.Applications.FindAsync(new object[] { applicationId }, cancellationToken);
            if (application != null && _schedulingService != null)
            {
                var available = await _schedulingService.GetAvailableSlotsAsync(
                    application.CandidateProfileId, new List<Guid>(), DateTime.UtcNow.AddDays(1),
                    DateTime.UtcNow.AddDays(14), cancellationToken: cancellationToken);
                if (available.IsSuccess && available.Data != null)
                    proposedSlots.AddRange(available.Data.Select(s => s.StartTime));
            }
            var allocated = false;
            foreach (var slot in proposedSlots.Distinct().Where(s => s > DateTime.UtcNow.AddMinutes(30)))
            {
                var scheduled = await _interviewService.ScheduleInterviewAsync(new CreateInterviewRequest
                {
                    ApplicationId = applicationId,
                    ScheduledAt = slot,
                    DurationMinutes = 60,
                    Notes = "Automatically allocated after human shortlist decision."
                }, cancellationToken);
                if (scheduled.IsSuccess)
                {
                    allocated = true;
                    if (scheduled.Data?.CalendarInvitationSent == false)
                        workflow.ErrorDetails = "Interview slot allocated; Google Calendar invitation is pending. Retry from Interviews after configuring Calendar.";
                    break;
                }
                workflow.ErrorDetails = scheduled.Error;
            }
            if (!allocated)
                workflow.ErrorDetails = "Candidate shortlisted; no verified slot could be allocated. Schedule an interview manually.";
            await _context.SaveChangesAsync(cancellationToken);
        }

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

        if (string.IsNullOrWhiteSpace(comments))
            return Result<WorkflowExecutionResponse>.Failure("A reason is required to reject the application.");

        if (workflow.RelatedEntityType == "Application" && workflow.RelatedEntityId is Guid applicationId)
        {
            var application = await _context.Applications.FindAsync(new object[] { applicationId }, cancellationToken);
            if (application == null || application.Status != ApplicationStatus.Screening)
                return Result<WorkflowExecutionResponse>.Failure("Application is no longer in screening.");
            application.Status = ApplicationStatus.Rejected;
            _context.ApplicationHistory.Add(new ApplicationHistory
            {
                ApplicationId = applicationId,
                FromStatus = ApplicationStatus.Screening,
                ToStatus = ApplicationStatus.Rejected,
                ChangedBy = decidedById.ToString(),
                Notes = comments
            });
        }

        workflow.Status = WorkflowStatus.Rejected;
        workflow.CompletedAt = DateTime.UtcNow;
        _context.WorkflowApprovals.Add(new WorkflowApproval
        {
            WorkflowExecutionId = workflowId,
            RequestedAction = "Review CV screening and reject candidate",
            Decision = "RejectedCandidate",
            DecidedById = decidedById,
            DecidedAt = DateTime.UtcNow,
            Comments = comments
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result<WorkflowExecutionResponse>.Success(MapToResponse(workflow));
    }

    public async Task<Result<WorkflowExecutionResponse>> RequestRevisionAsync(
        Guid workflowId, Guid decidedById, string comments,
        CancellationToken cancellationToken = default, string? authToken = null)
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
            Decision = "RevisionRequested",
            DecidedById = decidedById,
            DecidedAt = DateTime.UtcNow,
            Comments = comments
        });

        await _context.SaveChangesAsync(cancellationToken);
        if (!workflow.RelatedEntityId.HasValue)
            return Result<WorkflowExecutionResponse>.Failure("The linked application is missing.");
        var application = await _context.Applications.FirstOrDefaultAsync(
            a => a.Id == workflow.RelatedEntityId.Value, cancellationToken);
        if (application == null)
            return Result<WorkflowExecutionResponse>.NotFound("Application not found.");
        return await StartScreeningWorkflowAsync(
            new StartScreeningRequest { ApplicationId = application.Id, JobId = application.JobId },
            decidedById, workflow.CompanyId ?? Guid.Empty, cancellationToken, authToken);
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
        if (workflow.Status != WorkflowStatus.Planning && workflow.Status != WorkflowStatus.InProgress)
            return Result<WorkflowExecutionResponse>.Conflict("Workflow result has already been recorded.");
        if (!Enum.TryParse<WorkflowStatus>(request.Status, true, out var newStatus) ||
            (newStatus != WorkflowStatus.AwaitingApproval && newStatus != WorkflowStatus.Failed))
            return Result<WorkflowExecutionResponse>.Failure("Invalid AI workflow status.");
        if (newStatus == WorkflowStatus.AwaitingApproval && string.IsNullOrWhiteSpace(request.FinalResult))
            return Result<WorkflowExecutionResponse>.Failure("AI result is missing.");
        workflow.Status = newStatus;

        workflow.FinalResult = request.FinalResult;
        workflow.Plan = request.Plan;
        workflow.ErrorDetails = request.ErrorDetails;

        if (!string.IsNullOrWhiteSpace(request.FinalResult))
        {
            try
            {
                using var resultDocument = System.Text.Json.JsonDocument.Parse(request.FinalResult);
                if (workflow.RelatedEntityType == "Application" && workflow.RelatedEntityId is Guid applicationId &&
                    resultDocument.RootElement.TryGetProperty("screening_recommendation", out var recommendation))
                {
                    var value = recommendation.GetString();
                    if (value is "Shortlist" or "DoNotShortlist" or "NeedsReview")
                    {
                        var application = await _context.Applications.FindAsync(new object[] { applicationId }, cancellationToken);
                        if (application != null) application.AiRecommendation = value;
                    }
                }
                if (resultDocument.RootElement.TryGetProperty("validation", out var validation) &&
                    validation.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var passed = validation.TryGetProperty("is_valid", out var isValid) && isValid.GetBoolean();
                    _context.WorkflowValidationResults.Add(new WorkflowValidationResult
                    {
                        WorkflowExecutionId = workflow.Id,
                        ValidationType = "CV screening checks",
                        Passed = passed,
                        Errors = validation.TryGetProperty("errors", out var errors) ? errors.GetRawText() : "[]",
                        Warnings = validation.TryGetProperty("warnings", out var warnings) ? warnings.GetRawText() : "[]"
                    });
                }
            }
            catch (System.Text.Json.JsonException)
            {
                workflow.Status = WorkflowStatus.Failed;
                workflow.ErrorDetails = "AI service returned invalid result data.";
            }
        }

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

    private static List<DateTime> GetFutureSlots(string? finalResult)
    {
        var result = new List<DateTime>();
        if (string.IsNullOrWhiteSpace(finalResult)) return result;
        try
        {
            using var document = JsonDocument.Parse(finalResult);
            if (!document.RootElement.TryGetProperty("interview_proposal", out var proposal) ||
                !proposal.TryGetProperty("proposed_slots", out var slots) ||
                slots.ValueKind != JsonValueKind.Array) return result;
            foreach (var slot in slots.EnumerateArray())
            {
                if (slot.TryGetProperty("start_time", out var value) &&
                    DateTimeOffset.TryParse(value.GetString(), out var parsed) &&
                    parsed.UtcDateTime > DateTime.UtcNow.AddHours(1))
                {
                    result.Add(parsed.UtcDateTime);
                }
            }
        }
        catch (JsonException) { }
        return result;
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
