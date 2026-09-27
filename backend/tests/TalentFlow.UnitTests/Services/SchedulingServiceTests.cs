using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Persistence;
using TalentFlow.Infrastructure.Services;
using Xunit;

namespace TalentFlow.UnitTests.Services;

/// <summary>
/// Tests for scheduling conflict detection service.
/// </summary>
public class SchedulingServiceTests
{
    private AppDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task HasCandidateConflict_NoExistingInterviews_ReturnsFalse()
    {
        using var context = CreateTestContext();
        var service = new SchedulingService(context);

        var result = await service.HasCandidateConflictAsync(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            60);

        Assert.True(result.IsSuccess);
        Assert.False(result.Data);
    }

    [Fact]
    public async Task HasInterviewerConflict_NoExistingInterviews_ReturnsFalse()
    {
        using var context = CreateTestContext();
        var service = new SchedulingService(context);

        var result = await service.HasInterviewerConflictAsync(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            60);

        Assert.True(result.IsSuccess);
        Assert.False(result.Data);
    }

    [Fact]
    public async Task GetAvailableSlots_EmptySchedule_ReturnsSlots()
    {
        using var context = CreateTestContext();
        var service = new SchedulingService(context);

        var result = await service.GetAvailableSlotsAsync(
            Guid.NewGuid(),
            new System.Collections.Generic.List<Guid> { Guid.NewGuid() },
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5),
            60);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Data);
    }
}
