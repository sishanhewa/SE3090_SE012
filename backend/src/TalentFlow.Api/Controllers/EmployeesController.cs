using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Employees;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId}/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly AppDbContext _context;

    public EmployeesController(IEmployeeService employeeService, AppDbContext context)
    {
        _employeeService = employeeService;
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> CreateEmployee(Guid companyId, [FromBody] CreateEmployeeRequest request)
    {
        if (!await CanAccessCompanyAsync(companyId)) return Forbid();
        var result = await _employeeService.CreateEmployeeAsync(companyId, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetEmployee), new { companyId = companyId, id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager,Employee")]
    public async Task<IActionResult> GetEmployee(Guid companyId, Guid id)
    {
        if (!await CanAccessCompanyAsync(companyId)) return Forbid();
        var result = await _employeeService.GetEmployeeByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        if (result.Data!.CompanyId != companyId)
            return NotFound();

        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> GetEmployees(Guid companyId, [FromQuery] PaginationParams paginationParams)
    {
        if (!await CanAccessCompanyAsync(companyId)) return Forbid();
        var result = await _employeeService.GetEmployeesByCompanyAsync(companyId, paginationParams);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> UpdateEmployee(Guid companyId, Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        if (!await CanAccessCompanyAsync(companyId)) return Forbid();
        var result = await _employeeService.UpdateEmployeeAsync(id, companyId, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(result.Error);
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpPost("from-hire/{applicationId}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,HiringManager,Recruiter")]
    public async Task<IActionResult> CreateEmployeeFromHire(Guid applicationId)
    {
        var companyId = await _context.Applications.Where(a => a.Id == applicationId)
            .Select(a => a.Job.CompanyId).FirstOrDefaultAsync();
        if (!Guid.TryParse(RouteData.Values["companyId"]?.ToString(), out var routeCompanyId) ||
            companyId == Guid.Empty || routeCompanyId != companyId ||
            !await CanAccessCompanyAsync(companyId)) return Forbid();
        var result = await _employeeService.CreateEmployeeFromHireAsync(applicationId);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Data);
    }

    private async Task<bool> CanAccessCompanyAsync(Guid companyId)
    {
        if (User.IsInRole("SystemAdmin")) return true;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return false;
        return await _context.CompanyMemberships.AnyAsync(m => m.CompanyId == companyId && m.UserId == userId);
    }
}
