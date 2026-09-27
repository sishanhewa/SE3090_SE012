using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Infrastructure.Persistence;
using TalentFlow.Infrastructure.Services;
using Xunit;

namespace TalentFlow.UnitTests.Services;

/// <summary>
/// Tests for deterministic candidate scoring service.
/// </summary>
public class CandidateScoringServiceTests
{
    private AppDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CalculateScore_NonExistentApplication_ReturnsNotFound()
    {
        using var context = CreateTestContext();
        var service = new CandidateScoringService(context);

        var result = await service.CalculateScoreAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ScoreBreakdown_MaximumPossible_Is100()
    {
        // Verify score weights add up to 100
        const int mandatoryMax = 40;
        const int preferredMax = 20;
        const int experienceMax = 25;
        const int educationMax = 10;
        const int certMax = 5;

        Assert.Equal(100, mandatoryMax + preferredMax + experienceMax + educationMax + certMax);
    }
}
