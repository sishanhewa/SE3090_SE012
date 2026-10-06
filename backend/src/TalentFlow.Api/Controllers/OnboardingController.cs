using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.DTOs.Employees;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;
    private readonly AppDbContext _context;

    public OnboardingController(IOnboardingService onboardingService, AppDbContext context)
    {
        _onboardingService = onboardingService;
        _context = context;
    }

    [HttpGet("/api/companies/{companyId}/onboarding/templates")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> GetTemplates(Guid companyId)
    {
        if (!await CanAccessCompanyAsync(companyId)) return Forbid();
        var result = await _onboardingService.GetTemplatesByCompanyAsync(companyId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("/api/companies/{companyId}/onboarding/templates")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> CreateTemplate(Guid companyId, [FromBody] CreateOnboardingTemplateRequest request)
    {
        if (!await CanAccessCompanyAsync(companyId)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Tasks.Count == 0 ||
            request.Tasks.Any(t => string.IsNullOrWhiteSpace(t.Title))) return BadRequest("Add a name and at least one task.");
        var result = await _onboardingService.CreateTemplateAsync(companyId, request);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("/api/employees/{employeeId}/onboarding/assign/{templateId}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> AssignTemplate(Guid employeeId, Guid templateId)
    {
        if (!await CanAccessEmployeeAsync(employeeId)) return Forbid();
        var result = await _onboardingService.AssignOnboardingAsync(employeeId, templateId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("/api/employees/{employeeId}/onboarding")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager,Employee,Candidate")]
    public async Task<IActionResult> GetEmployeeOnboardingTasks(Guid employeeId)
    {
        if (!await CanAccessEmployeeAsync(employeeId)) return Forbid();
        var result = await _onboardingService.GetEmployeeOnboardingTasksAsync(employeeId);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    [HttpPatch("tasks/{taskId}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager,Employee,Candidate")]
    public async Task<IActionResult> CompleteOnboardingTask(Guid taskId, [FromBody] CompleteOnboardingTaskRequest request)
    {
        var employeeId = await _context.EmployeeOnboardingTasks.Where(t => t.Id == taskId)
            .Select(t => t.EmployeeId).FirstOrDefaultAsync();
        if (employeeId == Guid.Empty || !await CanAccessEmployeeAsync(employeeId)) return Forbid();
        var result = await _onboardingService.CompleteOnboardingTaskAsync(taskId, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }
        return Ok(result.Data);
    }

    private async Task<bool> CanAccessCompanyAsync(Guid companyId)
    {
        if (User.IsInRole("SystemAdmin")) return true;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return false;
        return await _context.CompanyMemberships.AnyAsync(m => m.CompanyId == companyId && m.UserId == userId);
    }

    private async Task<bool> CanAccessEmployeeAsync(Guid employeeId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee == null) return false;
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) && employee.UserId == userId)
            return true;
        return await CanAccessCompanyAsync(employee.CompanyId);
    }
}
