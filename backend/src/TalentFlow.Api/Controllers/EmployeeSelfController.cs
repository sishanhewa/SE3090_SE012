using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Services;

namespace TalentFlow.Api.Controllers;

/// <summary>
/// Separate controller for employee self-service endpoints (no companyId in route).
/// </summary>
[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeeSelfController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeSelfController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Get the current user's employee profile.
    /// Used by the Flutter candidate/employee app.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var result = await _employeeService.GetEmployeeByUserIdAsync(userGuid);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get onboarding tasks for a specific employee.
    /// </summary>
    [HttpGet("{id}/onboarding")]
    public async Task<IActionResult> GetOnboardingTasks(Guid id)
    {
        var result = await _employeeService.GetOnboardingTasksAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }
}
