using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentFlow.Application.DTOs.Employees;
using TalentFlow.Application.Interfaces.Services;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpGet("/api/employees/{employeeId}/onboarding")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager,Employee")]
    public async Task<IActionResult> GetEmployeeOnboardingTasks(Guid employeeId)
    {
        var result = await _onboardingService.GetEmployeeOnboardingTasksAsync(employeeId);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    [HttpPatch("tasks/{taskId}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Employee")]
    public async Task<IActionResult> CompleteOnboardingTask(Guid taskId, [FromBody] CompleteOnboardingTaskRequest request)
    {
        var result = await _onboardingService.CompleteOnboardingTaskAsync(taskId, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }
}
