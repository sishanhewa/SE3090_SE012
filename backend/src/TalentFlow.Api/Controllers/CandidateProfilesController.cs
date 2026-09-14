using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentFlow.Application.DTOs.CandidateProfiles;
using TalentFlow.Application.Interfaces.Services;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CandidateProfilesController : ControllerBase
{
    private readonly ICandidateProfileService _profileService;

    public CandidateProfilesController(ICandidateProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpPost]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateCandidateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var result = await _profileService.CreateProfileAsync(userGuid, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "CONFLICT") return Conflict(result.Error);
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetMyProfile), new { }, result.Data);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var result = await _profileService.GetProfileByUserIdAsync(userGuid);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateCandidateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var result = await _profileService.UpdateProfileAsync(userGuid, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }
}
