using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Offers;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OffersController : ControllerBase
{
    private readonly IOfferService _offerService;
    private readonly AppDbContext _context;

    public OffersController(IOfferService offerService, AppDbContext context)
    {
        _offerService = offerService;
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferRequest request)
    {
        if (!await CanAccessApplicationAsync(request.ApplicationId)) return Forbid();
        var result = await _offerService.CreateOfferAsync(request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetOffer), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOffer(Guid id)
    {
        if (!await CanAccessOfferAsync(id)) return Forbid();
        var result = await _offerService.GetOfferByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);
        if (User.IsInRole("Candidate") && result.Data!.Status is not ("Sent" or "Accepted" or "Rejected"))
            return Forbid();

        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager,Candidate")]
    public async Task<IActionResult> GetOffers([FromQuery] PaginationParams paginationParams, [FromQuery] Guid? applicationId)
    {
        if (User.IsInRole("Candidate") && !applicationId.HasValue) return Forbid();
        if (applicationId.HasValue && !await CanAccessApplicationAsync(applicationId.Value)) return Forbid();
        if (User.IsInRole("Candidate") && applicationId.HasValue &&
            !await _context.Offers.AnyAsync(o => o.ApplicationId == applicationId.Value &&
                (o.Status == OfferStatus.Sent || o.Status == OfferStatus.Accepted ||
                 o.Status == OfferStatus.Rejected))) return Forbid();
        var companyId = await GetCompanyIdAsync();
        if (companyId == Guid.Empty && !User.IsInRole("SystemAdmin") && !User.IsInRole("Candidate")) return Forbid();
        var result = await _offerService.GetOffersAsync(paginationParams, applicationId,
            User.IsInRole("SystemAdmin") || User.IsInRole("Candidate") ? null : companyId);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> UpdateOffer(Guid id, [FromBody] UpdateOfferRequest request)
    {
        if (!await CanAccessOfferAsync(id)) return Forbid();
        var result = await _offerService.UpdateOfferAsync(id, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    public class UpdateOfferStatusRequest
    {
        public OfferStatus Status { get; set; }
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOfferStatusRequest request)
    {
        if (!await CanAccessOfferAsync(id)) return Forbid();
        if (User.IsInRole("Candidate") && request.Status is not (OfferStatus.Accepted or OfferStatus.Rejected))
            return Forbid();
        if (request.Status == OfferStatus.Approved && !User.IsInRole("SystemAdmin") &&
            !User.IsInRole("CompanyAdmin") && !User.IsInRole("HiringManager"))
            return Forbid();
        if (!User.IsInRole("Candidate") && !User.IsInRole("SystemAdmin") &&
            !User.IsInRole("CompanyAdmin") && !User.IsInRole("Recruiter") && !User.IsInRole("HiringManager"))
            return Forbid();
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Unauthorized();
        var actorLabel = User.FindFirstValue(ClaimTypes.Email) ?? actorId.ToString();
        var result = await _offerService.UpdateStatusAsync(id, request.Status, actorId, actorLabel);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    [HttpPost("{id}/notify-hired")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> ResendHiredNotification(Guid id)
    {
        if (!await CanAccessOfferAsync(id)) return Forbid();
        var result = await _offerService.ResendHiredNotificationAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        if (Guid.TryParse(User.FindFirst("CompanyId")?.Value, out var companyId)) return companyId;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Guid.Empty;
        return await _context.CompanyMemberships.Where(m => m.UserId == userId)
            .Select(m => m.CompanyId).FirstOrDefaultAsync();
    }

    private async Task<bool> CanAccessOfferAsync(Guid offerId)
    {
        var applicationId = await _context.Offers.Where(o => o.Id == offerId)
            .Select(o => o.ApplicationId).FirstOrDefaultAsync();
        return applicationId != Guid.Empty && await CanAccessApplicationAsync(applicationId);
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
}
