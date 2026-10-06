using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessChat([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
    {
        // Enforce the company ID
        var claim = User.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(claim, out var companyId))
        {
            request.CompanyId = companyId;
        }

        // Get Authorization token from request header to pass to python service
        var authHeader = Request.Headers["Authorization"].ToString();
        var token = authHeader.StartsWith("Bearer ") ? authHeader.Substring("Bearer ".Length).Trim() : string.Empty;

        var result = await _chatService.ProcessChatAsync(request, token, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(500, result.Error);
        }

        return Ok(result.Data);
    }
}
