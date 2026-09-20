using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Offers;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OffersController : ControllerBase
{
    private readonly IOfferService _offerService;

    public OffersController(IOfferService offerService)
    {
        _offerService = offerService;
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferRequest request)
    {
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
        var result = await _offerService.GetOfferByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> GetOffers([FromQuery] PaginationParams paginationParams, [FromQuery] Guid? applicationId)
    {
        var result = await _offerService.GetOffersAsync(paginationParams, applicationId);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> UpdateOffer(Guid id, [FromBody] UpdateOfferRequest request)
    {
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
        var result = await _offerService.UpdateStatusAsync(id, request.Status);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NotFound") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
