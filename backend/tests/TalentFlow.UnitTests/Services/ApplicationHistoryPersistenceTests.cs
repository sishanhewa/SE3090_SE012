using Microsoft.EntityFrameworkCore;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;
using TalentFlow.Infrastructure.Repositories;
using Xunit;
using ApplicationEntity = TalentFlow.Domain.Entities.Application;

namespace TalentFlow.UnitTests.Services;

public class ApplicationHistoryPersistenceTests
{
    [Fact]
    public async Task UpdateAsync_InsertsNewHistoryAndPersistsWithdrawal()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AppDbContext(options);
        var application = new ApplicationEntity { Status = ApplicationStatus.Submitted };
        context.Applications.Add(application);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new ApplicationRepository(context);
        var stored = (await repository.GetByIdAsync(application.Id))!;
        stored.Status = ApplicationStatus.Withdrawn;
        stored.History.Add(new ApplicationHistory {
            FromStatus = ApplicationStatus.Submitted,
            ToStatus = ApplicationStatus.Withdrawn,
            Notes = "Regression verification"
        });
        await repository.UpdateAsync(stored);
        context.ChangeTracker.Clear();
        Assert.Equal(ApplicationStatus.Withdrawn, (await repository.GetByIdAsync(application.Id))!.Status);
        Assert.Equal(1, await context.ApplicationHistory.CountAsync(h => h.ApplicationId == application.Id));
    }
}
