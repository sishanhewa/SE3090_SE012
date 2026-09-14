using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Common;
using TalentFlow.Application.DTOs.Companies;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;

namespace TalentFlow.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;

    public CompanyService(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result<CompanyResponse>> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = new Company
        {
            Name = request.Name,
            Description = request.Description,
            Website = request.Website,
            Industry = request.Industry,
            Address = request.Address,
            IsActive = true
        };

        await _companyRepository.AddAsync(company, cancellationToken);

        return Result<CompanyResponse>.Success(MapToResponse(company));
    }

    public async Task<Result<CompanyResponse>> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(id, cancellationToken);
        if (company == null)
            return Result<CompanyResponse>.NotFound("Company not found");

        return Result<CompanyResponse>.Success(MapToResponse(company));
    }

    public async Task<Result<PagedResult<CompanyResponse>>> GetCompaniesAsync(PaginationParams paginationParams, CancellationToken cancellationToken = default)
    {
        var all = await _companyRepository.GetAllAsync(cancellationToken);
        
        var response = new PagedResult<CompanyResponse>(
            all.Select(MapToResponse).ToList(),
            all.Count,
            paginationParams.Page,
            paginationParams.PageSize
        );

        return Result<PagedResult<CompanyResponse>>.Success(response);
    }

    public async Task<Result<CompanyResponse>> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(id, cancellationToken);
        if (company == null)
            return Result<CompanyResponse>.NotFound("Company not found");

        company.Name = request.Name;
        company.Description = request.Description;
        company.Website = request.Website;
        company.Industry = request.Industry;
        company.Address = request.Address;

        await _companyRepository.UpdateAsync(company, cancellationToken);

        return Result<CompanyResponse>.Success(MapToResponse(company));
    }

    private static CompanyResponse MapToResponse(Company company)
    {
        return new CompanyResponse
        {
            Id = company.Id,
            Name = company.Name,
            Description = company.Description,
            Website = company.Website,
            LogoUrl = company.LogoUrl,
            Address = company.Address,
            Industry = company.Industry,
            IsActive = company.IsActive,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt
        };
    }
}
