using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly AppDbContext _context;

    public WorkflowsController(IWorkflowService workflowService, AppDbContext context)
    {
        _workflowService = workflowService;
        _context = context;
    }

    /// <summary>
    /// Start a new AI screening workflow for an application.
    /// </summary>
    [HttpPost("recruitment-screening")]
    [Authorize(Roles = "SystemAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> StartScreeningWorkflow(
        [FromBody] StartScreeningWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var companyId = await GetCompanyIdAsync(cancellationToken);

        var serviceRequest = new StartScreeningRequest
        {
            ApplicationId = request.ApplicationId,
            JobId = request.JobId,
        };

        var result = await _workflowService.StartScreeningWorkflowAsync(
            serviceRequest, userId, companyId, cancellationToken,
            Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase));

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            if (result.ErrorCode == "CONFLICT") return Conflict(result.Error);
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Get a workflow by ID with all details.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWorkflow(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessWorkflowAsync(id, cancellationToken)) return Forbid();
        var result = await _workflowService.GetWorkflowAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Get all workflows for the user's company.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWorkflows(CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        if (companyId == Guid.Empty && !User.IsInRole("SystemAdmin")) return Forbid();
        var result = await _workflowService.GetWorkflowsByCompanyAsync(companyId, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Approve a workflow that is awaiting approval.
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "SystemAdmin,HiringManager,Recruiter")]
    public async Task<IActionResult> ApproveWorkflow(
        Guid id,
        [FromBody] WorkflowDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessWorkflowAsync(id, cancellationToken)) return Forbid();
        var userId = GetUserId();
        var result = await _workflowService.ApproveWorkflowAsync(
            id, userId, request.Comments, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Reject a workflow that is awaiting approval.
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "SystemAdmin,HiringManager,Recruiter")]
    public async Task<IActionResult> RejectWorkflow(
        Guid id,
        [FromBody] WorkflowDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessWorkflowAsync(id, cancellationToken)) return Forbid();
        var userId = GetUserId();
        var result = await _workflowService.RejectWorkflowAsync(
            id, userId, request.Comments, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Request revision of a workflow that is awaiting approval.
    /// </summary>
    [HttpPost("{id}/revise")]
    [Authorize(Roles = "SystemAdmin,HiringManager,Recruiter")]
    public async Task<IActionResult> RequestRevision(
        Guid id,
        [FromBody] WorkflowDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessWorkflowAsync(id, cancellationToken)) return Forbid();
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(request.Comments))
            return BadRequest("Comments are required when requesting a revision.");

        var result = await _workflowService.RequestRevisionAsync(
            id, userId, request.Comments, cancellationToken,
            Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase));
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Internal callback for the AI service to push results back.
    /// </summary>
    [HttpPost("callback")]
    [Authorize(Roles = "SystemAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> AiCallback(
        [FromBody] AiWorkflowCallbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessWorkflowAsync(request.WorkflowId, cancellationToken)) return Forbid();
        var result = await _workflowService.UpdateWorkflowFromAiAsync(
            request, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "CONFLICT") return Conflict(result.Error);
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private async Task<bool> CanAccessWorkflowAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        var result = await _workflowService.GetWorkflowAsync(workflowId, cancellationToken);
        if (!result.IsSuccess) return false;
        if (User.IsInRole("SystemAdmin")) return true;
        var companyId = await GetCompanyIdAsync(cancellationToken);
        return companyId != Guid.Empty && result.Data!.CompanyId == companyId;
    }

    private async Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(claim, out var id)) return id;
        var userId = GetUserId();
        if (userId == Guid.Empty) return Guid.Empty;
        return await _context.CompanyMemberships.Where(m => m.UserId == userId)
            .OrderBy(m => m.CreatedAt).Select(m => m.CompanyId).FirstOrDefaultAsync(cancellationToken);
    }
}

public class StartScreeningWorkflowRequest
{
    public Guid ApplicationId { get; set; }
    public Guid JobId { get; set; }
}

public class WorkflowDecisionRequest
{
    public string? Comments { get; set; }
}
