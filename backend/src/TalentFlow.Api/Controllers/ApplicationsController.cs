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
    private readonly ICandidateProfileService _candidateProfileService;

    public ApplicationsController(IApplicationService applicationService, ICandidateProfileService candidateProfileService)
    {
        _applicationService = applicationService;
        _candidateProfileService = candidateProfileService;
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
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "CONFLICT") return Conflict(result.Error);
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetApplication), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        var result = await _applicationService.GetApplicationByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> GetApplications(
        [FromQuery] PaginationParams paginationParams,
        [FromQuery] Guid? jobId,
        [FromQuery] Guid? candidateProfileId)
    {
        var result = await _applicationService.GetApplicationsAsync(paginationParams, jobId, candidateProfileId);
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
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
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
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "System";
        var result = await _applicationService.UpdateApplicationStatusAsync(id, request.Status, userEmail, request.Notes);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "InvalidStateTransition") return BadRequest(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetApplicationHistory(Guid id)
    {
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
        var docs = await _applicationService.GetApplicationDocumentsAsync(id);
        if (!docs.IsSuccess)
            return NotFound(docs.Error);

        return Ok(docs.Data);
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
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }
}
