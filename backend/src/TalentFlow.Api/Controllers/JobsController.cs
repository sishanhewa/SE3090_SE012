using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Jobs;
using TalentFlow.Application.Interfaces.Services;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId}/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Coordinator")]
    public async Task<IActionResult> CreateJob(Guid companyId, [FromBody] CreateJobRequest request)
    {
        var result = await _jobService.CreateJobAsync(request, companyId);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetJob), new { companyId = companyId, id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    [AllowAnonymous] // Assuming jobs can be viewed publicly by candidates
    public async Task<IActionResult> GetJob(Guid companyId, Guid id)
    {
        var result = await _jobService.GetJobByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        // Optional: Ensure job belongs to the companyId in route, though GetJobByIdAsync doesn't check it directly
        // We trust the ID lookup for now.
        if (result.Data!.CompanyId != companyId)
            return NotFound();

        return Ok(result.Data);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetJobs(Guid companyId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _jobService.GetJobsAsync(paginationParams, companyId);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Coordinator")]
    public async Task<IActionResult> UpdateJob(Guid companyId, Guid id, [FromBody] UpdateJobRequest request)
    {
        var result = await _jobService.UpdateJobAsync(id, request, companyId);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "Forbidden") return Forbid();
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpPost("{id}/publish")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Coordinator")]
    public async Task<IActionResult> PublishJob(Guid companyId, Guid id)
    {
        var result = await _jobService.PublishJobAsync(id, companyId);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "Forbidden") return Forbid();
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    [HttpPost("{id}/close")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Coordinator")]
    public async Task<IActionResult> CloseJob(Guid companyId, Guid id)
    {
        var result = await _jobService.CloseJobAsync(id, companyId);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "Forbidden") return Forbid();
            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
