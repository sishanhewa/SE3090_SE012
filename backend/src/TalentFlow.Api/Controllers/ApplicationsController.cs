using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Applications;
using TalentFlow.Application.Interfaces.Services;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpPost("jobs/{jobId}")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Apply(Guid jobId, [FromBody] CreateApplicationRequest request)
    {
        // Mock candidate profile ID resolution (in real app, fetched via user context)
        // Assume the logged-in user is a candidate and we have their Profile ID in claims,
        // or we fetch it via CandidateProfileService.
        // For sprint 1, let's just pass a dummy Guid or parse from claims if available.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        // Warning: This implies the candidateProfileId is same as userId for mocking purposes,
        // or we need CandidateProfileService to get the profile. Let's use userGuid as placeholder.
        // We will fix this once CandidateProfileService is integrated.
        Guid candidateProfileId = userGuid; // TODO: Fetch actual profile ID

        var result = await _applicationService.CreateApplicationAsync(jobId, request, candidateProfileId);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "CONFLICT") return Conflict(result.Error);
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetApplication), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        var result = await _applicationService.GetApplicationByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        // TODO: Ensure the user requesting is either the candidate, or an authorized company user
        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetApplications(
        [FromQuery] PaginationParams paginationParams,
        [FromQuery] Guid? jobId,
        [FromQuery] Guid? candidateProfileId)
    {
        // TODO: Enforce permissions (Candidate can only query their own, Company user can query their jobs)
        var result = await _applicationService.GetApplicationsAsync(paginationParams, jobId, candidateProfileId);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpPost("{id}/withdraw")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        Guid candidateProfileId = userGuid; // TODO: Fetch actual profile ID

        var result = await _applicationService.WithdrawApplicationAsync(id, candidateProfileId);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
