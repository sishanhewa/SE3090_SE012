using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentFlow.Application.Interfaces.Services;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowsController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
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
        var companyId = GetCompanyId();

        var serviceRequest = new StartScreeningRequest
        {
            ApplicationId = request.ApplicationId,
            JobId = request.JobId,
        };

        var result = await _workflowService.StartScreeningWorkflowAsync(
            serviceRequest, userId, companyId, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "Forbidden") return Forbid();
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
        var result = await _workflowService.GetWorkflowAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "Forbidden") return Forbid();
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
        var companyId = GetCompanyId();
        var result = await _workflowService.GetWorkflowsByCompanyAsync(companyId, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "Forbidden") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Approve a workflow that is awaiting approval.
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "SystemAdmin,HiringManager")]
    public async Task<IActionResult> ApproveWorkflow(
        Guid id,
        [FromBody] WorkflowDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _workflowService.ApproveWorkflowAsync(
            id, userId, request.Comments, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "Forbidden") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Reject a workflow that is awaiting approval.
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "SystemAdmin,HiringManager")]
    public async Task<IActionResult> RejectWorkflow(
        Guid id,
        [FromBody] WorkflowDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _workflowService.RejectWorkflowAsync(
            id, userId, request.Comments, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "Forbidden") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Request revision of a workflow that is awaiting approval.
    /// </summary>
    [HttpPost("{id}/revise")]
    [Authorize(Roles = "SystemAdmin,HiringManager")]
    public async Task<IActionResult> RequestRevision(
        Guid id,
        [FromBody] WorkflowDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(request.Comments))
            return BadRequest("Comments are required when requesting a revision.");

        var result = await _workflowService.RequestRevisionAsync(
            id, userId, request.Comments, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "Forbidden") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private Guid GetCompanyId()
    {
        var claim = User.FindFirst("CompanyId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
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
