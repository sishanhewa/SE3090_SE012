using TalentFlow.Domain.Entities;

namespace TalentFlow.Application.Interfaces.Repositories;

public interface ICompanyRepository : IRepository<Company>
{
    Task<Company?> GetCompanyWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsUserMemberOfCompanyAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default);

    Task<CompanyMembership?> GetMembershipAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default);
}
