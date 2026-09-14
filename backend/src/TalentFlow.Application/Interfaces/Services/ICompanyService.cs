using TalentFlow.Application.Common;

using TalentFlow.Application.DTOs.Companies;

namespace TalentFlow.Application.Interfaces.Services;

public interface ICompanyService
{
    Task<Result<CompanyResponse>> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<Result<CompanyResponse>> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<CompanyResponse>>> GetCompaniesAsync(PaginationParams paginationParams, CancellationToken cancellationToken = default);
    Task<Result<CompanyResponse>> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default);
}
