using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Entities;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Repositories;

public class CompanyRepository : Repository<Company>, ICompanyRepository
{
    public CompanyRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Company?> GetCompanyWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Departments)
            .Include(c => c.Memberships)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> IsUserMemberOfCompanyAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await Context.CompanyMemberships
            .AnyAsync(cm => cm.UserId == userId && cm.CompanyId == companyId && cm.IsActive, cancellationToken);
    }

    public async Task<CompanyMembership?> GetMembershipAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await Context.CompanyMemberships
            .FirstOrDefaultAsync(cm => cm.UserId == userId && cm.CompanyId == companyId && cm.IsActive, cancellationToken);
    }
}
