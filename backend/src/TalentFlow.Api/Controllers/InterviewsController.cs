using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Interviews;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterviewsController : ControllerBase
{
    private readonly IInterviewService _interviewService;
    private readonly AppDbContext _context;

    public InterviewsController(IInterviewService interviewService, AppDbContext context)
    {
        _interviewService = interviewService;
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> GetInterviews([FromQuery] PaginationParams paginationParams)
    {
        var companyId = await GetCompanyIdAsync();
        if (companyId == Guid.Empty && !User.IsInRole("SystemAdmin")) return Forbid();
        var result = await _interviewService.GetInterviewsByCompanyAsync(companyId, paginationParams);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> ScheduleInterview([FromBody] CreateInterviewRequest request)
    {
        if (!await CanAccessApplicationAsync(request.ApplicationId)) return Forbid();
        var result = await _interviewService.ScheduleInterviewAsync(request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetInterview), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInterview(Guid id)
    {
        if (!await CanAccessInterviewAsync(id)) return Forbid();
        var result = await _interviewService.GetInterviewByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    [HttpGet("application/{applicationId}")]
    public async Task<IActionResult> GetInterviewsByApplication(Guid applicationId, [FromQuery] PaginationParams paginationParams)
    {
        if (!await CanAccessApplicationAsync(applicationId)) return Forbid();
        var result = await _interviewService.GetInterviewsByApplicationAsync(applicationId, paginationParams);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> UpdateInterview(Guid id, [FromBody] UpdateInterviewRequest request)
    {
        if (!await CanAccessInterviewAsync(id)) return Forbid();
        var result = await _interviewService.UpdateInterviewAsync(id, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> CancelInterview(Guid id)
    {
        if (!await CanAccessInterviewAsync(id)) return Forbid();
        var result = await _interviewService.CancelInterviewAsync(id);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "InvalidStateTransition") return BadRequest(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    public class UpdateInterviewStatusRequest
    {
        public TalentFlow.Domain.Enums.InterviewStatus Status { get; set; }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateInterviewStatusRequest request)
    {
        if (!await CanAccessInterviewAsync(id)) return Forbid();
        var result = await _interviewService.UpdateStatusAsync(id, request.Status);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    [HttpPost("{id}/feedback")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")] // Evaluators
    public async Task<IActionResult> AddFeedback(Guid id, [FromBody] AddInterviewFeedbackRequest request)
    {
        if (!await CanAccessInterviewAsync(id)) return Forbid();
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var result = await _interviewService.AddFeedbackAsync(id, userGuid, request);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        if (Guid.TryParse(User.FindFirst("CompanyId")?.Value, out var companyId)) return companyId;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Guid.Empty;
        return await _context.CompanyMemberships.Where(m => m.UserId == userId)
            .Select(m => m.CompanyId).FirstOrDefaultAsync();
    }

    private async Task<bool> CanAccessApplicationAsync(Guid applicationId)
    {
        var application = await _context.Applications.Include(a => a.Job)
            .Include(a => a.CandidateProfile).FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application == null) return false;
        if (User.IsInRole("SystemAdmin")) return true;
        if (User.IsInRole("Candidate"))
            return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) &&
                application.CandidateProfile.UserId == userId;
        return application.Job.CompanyId == await GetCompanyIdAsync();
    }

    private async Task<bool> CanAccessInterviewAsync(Guid interviewId)
    {
        var applicationId = await _context.Interviews.Where(i => i.Id == interviewId)
            .Select(i => i.ApplicationId).FirstOrDefaultAsync();
        return applicationId != Guid.Empty && await CanAccessApplicationAsync(applicationId);
    }
}
