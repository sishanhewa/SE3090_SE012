using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Applications;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly ICandidateProfileService _candidateProfileService;
    private readonly AppDbContext _context;

    public ApplicationsController(IApplicationService applicationService, ICandidateProfileService candidateProfileService, AppDbContext context)
    {
        _applicationService = applicationService;
        _candidateProfileService = candidateProfileService;
        _context = context;
    }

    [HttpPost("jobs/{jobId}")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Apply(Guid jobId, [FromBody] CreateApplicationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var profileResult = await _candidateProfileService.GetProfileByUserIdAsync(userGuid);
        if (!profileResult.IsSuccess)
            return BadRequest("Candidate profile must be created before applying.");

        var result = await _applicationService.CreateApplicationAsync(jobId, request, profileResult.Data!.Id);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "CONFLICT") return Conflict(result.Error);
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetApplication), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        if (!await CanAccessApplicationAsync(id)) return Forbid();
        var result = await _applicationService.GetApplicationByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);
        if (User.IsInRole("Candidate"))
        {
            result.Data!.AiScore = null;
            result.Data.AiRecommendation = null;
        }

        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> GetApplications(
        [FromQuery] PaginationParams paginationParams,
        [FromQuery] Guid? jobId,
        [FromQuery] Guid? candidateProfileId)
    {
        Guid? companyId = null;
        if (!User.IsInRole("SystemAdmin"))
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var staffUserId)) return Unauthorized();
            var parsedCompanyId = await GetCompanyIdAsync(staffUserId);
            if (parsedCompanyId == Guid.Empty) return Forbid();
            companyId = parsedCompanyId;
        }
        var result = await _applicationService.GetApplicationsAsync(paginationParams, jobId, candidateProfileId, companyId: companyId);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetMyApplications([FromQuery] PaginationParams paginationParams)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var profileResult = await _candidateProfileService.GetProfileByUserIdAsync(userGuid);
        if (!profileResult.IsSuccess)
            return Ok(new { Items = new object[] {}, TotalCount = 0, Page = 1, PageSize = 10 }); // No profile means no apps

        var result = await _applicationService.GetMyApplicationsAsync(profileResult.Data!.Id, paginationParams);
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        foreach (var application in result.Data!.Items)
        {
            application.AiScore = null;
            application.AiRecommendation = null;
        }

        return Ok(result.Data);
    }

    [HttpPost("{id}/withdraw")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var profileResult = await _candidateProfileService.GetProfileByUserIdAsync(userGuid);
        if (!profileResult.IsSuccess)
            return BadRequest("Candidate profile not found.");

        var result = await _applicationService.WithdrawApplicationAsync(id, profileResult.Data!.Id);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            if (result.ErrorCode == "CONFLICT") return Conflict(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    public class UpdateStatusRequest
    {
        public TalentFlow.Domain.Enums.ApplicationStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        if (!await CanAccessApplicationAsync(id)) return Forbid();
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "System";
        var result = await _applicationService.UpdateApplicationStatusAsync(id, request.Status, userEmail, request.Notes);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "InvalidStateTransition") return BadRequest(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetApplicationHistory(Guid id)
    {
        if (!await CanAccessApplicationAsync(id)) return Forbid();
        var result = await _applicationService.GetApplicationByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        // Fetch history from DB directly
        var history = await _applicationService.GetApplicationHistoryAsync(id);
        if (!history.IsSuccess)
            return BadRequest(history.Error);

        return Ok(history.Data);
    }

    [HttpGet("{id}/documents")]
    public async Task<IActionResult> GetApplicationDocuments(Guid id)
    {
        if (!await CanAccessApplicationAsync(id)) return Forbid();
        var docs = await _applicationService.GetApplicationDocumentsAsync(id);
        if (!docs.IsSuccess)
            return NotFound(docs.Error);

        return Ok(docs.Data);
    }

    [HttpGet("{id}/resume")]
    public async Task<IActionResult> DownloadResume(Guid id)
    {
        if (!await CanAccessApplicationAsync(id)) return Forbid();
        var result = await _applicationService.GetResumeAsync(id);
        if (!result.IsSuccess) return NotFound(result.Error);
        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpGet("{id}/documents/{documentId}")]
    public async Task<IActionResult> DownloadDocument(Guid id, Guid documentId)
    {
        if (!await CanAccessApplicationAsync(id)) return Forbid();
        var result = await _applicationService.GetDocumentAsync(id, documentId);
        if (!result.IsSuccess) return NotFound(result.Error);
        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("{id}/documents")]
    [Authorize(Roles = "Candidate")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<IActionResult> UploadDocument(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var result = await _applicationService.UploadDocumentAsync(id, userGuid, file.OpenReadStream(), file.FileName, file.Length);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            if (result.ErrorCode == "CONFLICT") return Conflict(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    private async Task<bool> CanAccessApplicationAsync(Guid applicationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid)) return false;

        var access = await _context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => new { OwnerId = a.CandidateProfile.UserId, CompanyId = a.Job.CompanyId })
            .FirstOrDefaultAsync();
        if (access == null) return false;
        if (User.IsInRole("SystemAdmin")) return true;
        if (User.IsInRole("Candidate")) return access.OwnerId == userGuid;
        if (!(User.IsInRole("CompanyAdmin") || User.IsInRole("Recruiter") || User.IsInRole("HiringManager"))) return false;
        return await GetCompanyIdAsync(userGuid) == access.CompanyId;
    }

    private async Task<Guid> GetCompanyIdAsync(Guid userId)
    {
        if (Guid.TryParse(User.FindFirstValue("CompanyId"), out var companyId)) return companyId;
        return await _context.CompanyMemberships.Where(m => m.UserId == userId)
            .OrderBy(m => m.CreatedAt).Select(m => m.CompanyId).FirstOrDefaultAsync();
    }
}
