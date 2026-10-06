using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.DTOs.CandidateProfiles;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CandidateProfilesController : ControllerBase
{
    private readonly ICandidateProfileService _profileService;
    private readonly AppDbContext _context;

    public CandidateProfilesController(ICandidateProfileService profileService, AppDbContext context)
    {
        _profileService = profileService;
        _context = context;
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfileById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid)) return Unauthorized();
        if (!User.IsInRole("SystemAdmin"))
        {
            var companyId = Guid.TryParse(User.FindFirstValue("CompanyId"), out var parsed)
                ? parsed
                : await _context.CompanyMemberships.Where(m => m.UserId == userGuid)
                    .OrderBy(m => m.CreatedAt).Select(m => m.CompanyId).FirstOrDefaultAsync();
            var isStaff = User.IsInRole("CompanyAdmin") || User.IsInRole("Recruiter") || User.IsInRole("HiringManager");
            var authorized = await _context.CandidateProfiles.AnyAsync(p => p.Id == id &&
                (p.UserId == userGuid ||
                 (isStaff && companyId != Guid.Empty && p.Applications.Any(a => a.Job.CompanyId == companyId))));
            if (!authorized) return Forbid();
        }
        var result = await _profileService.GetProfileByIdAsync(id);
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
