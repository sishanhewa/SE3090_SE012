using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Employees;
using TalentFlow.Application.Interfaces.Services;

namespace TalentFlow.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId}/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> CreateEmployee(Guid companyId, [FromBody] CreateEmployeeRequest request)
    {
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
        var result = await _employeeService.GetEmployeesByCompanyAsync(companyId, paginationParams);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,CompanyAdmin,Recruiter,HiringManager")]
    public async Task<IActionResult> UpdateEmployee(Guid companyId, Guid id, [FromBody] UpdateEmployeeRequest request)
    {
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
        // Note: The implementation logic for this should ideally be in IEmployeeService.
        // For now, it returns a 501 Not Implemented or routes to a temporary placeholder.
        return StatusCode(501, "Endpoint is pending full implementation. Please use CreateEmployee for manual creation.");
    }
}
