using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowsController : ControllerBase
{
    // For Sprint 1, this just stubs out the endpoint for Sprint 3.
    // Real implementation will call the AI service.

    [HttpPost("recruitment-screening")]
    [Authorize(Roles = "SystemAdmin,Recruiter,HiringManager")]
    public IActionResult StartScreeningWorkflow([FromBody] StartWorkflowRequest request)
    {
        // TODO: Validate request, store workflow state in DB, call Python AI service
        
        // Placeholder return
        return Accepted(new { WorkflowId = Guid.NewGuid(), Status = "Planning" });
    }

    [HttpGet("{id}")]
    public IActionResult GetWorkflow(Guid id)
    {
        // TODO: Retrieve from WorkflowExecution table
        return Ok(new { WorkflowId = id, Status = "Planning" });
    }
}

public class StartWorkflowRequest
{
    public Guid ApplicationId { get; set; }
    public Guid JobId { get; set; }
}
