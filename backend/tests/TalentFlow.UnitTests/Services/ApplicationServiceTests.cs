using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TalentFlow.Application.DTOs.Applications;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Services;
using Xunit;

namespace TalentFlow.UnitTests.Services;

public class ApplicationServiceTests
{
    private readonly Mock<IApplicationRepository> _applicationRepositoryMock;
    private readonly Mock<IJobRepository> _jobRepositoryMock;
    private readonly ApplicationService _sut;

    public ApplicationServiceTests()
    {
        _applicationRepositoryMock = new Mock<IApplicationRepository>();
        _jobRepositoryMock = new Mock<IJobRepository>();
        _sut = new ApplicationService(_applicationRepositoryMock.Object, _jobRepositoryMock.Object, null!);
    }

    // ── CREATE ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateApplicationAsync_ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        _jobRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var request = new CreateApplicationRequest();

        // Act
        var result = await _sut.CreateApplicationAsync(Guid.NewGuid(), request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task CreateApplicationAsync_ShouldReturnBadRequest_WhenJobIsNotPublished()
    {
        // Arrange
        var job = new Job { Id = Guid.NewGuid(), Status = JobStatus.Draft };
        _jobRepositoryMock.Setup(repo => repo.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var request = new CreateApplicationRequest();

        // Act
        var result = await _sut.CreateApplicationAsync(job.Id, request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("applications");
    }

    [Fact]
    public async Task CreateApplicationAsync_ShouldFail_WhenJobIsClosed()
    {
        // Arrange
        var job = new Job { Id = Guid.NewGuid(), Status = JobStatus.Closed };
        _jobRepositoryMock.Setup(repo => repo.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var request = new CreateApplicationRequest();

        // Act
        var result = await _sut.CreateApplicationAsync(job.Id, request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateApplicationAsync_ShouldFail_WhenDeadlineHasPassed()
    {
        // Arrange – published job but deadline is in the past
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Status = JobStatus.Published,
            ApplicationDeadline = DateTime.UtcNow.AddDays(-1) // Expired
        };
        _jobRepositoryMock.Setup(repo => repo.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var request = new CreateApplicationRequest();

        // Act
        var result = await _sut.CreateApplicationAsync(job.Id, request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("deadline");
    }

    [Fact]
    public async Task CreateApplicationAsync_ShouldFail_WhenDuplicateApplication()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var job = new Job { Id = jobId, Status = JobStatus.Published };

        _jobRepositoryMock.Setup(repo => repo.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _applicationRepositoryMock.Setup(repo => repo.HasAppliedAsync(jobId, candidateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateApplicationRequest();

        // Act
        var result = await _sut.CreateApplicationAsync(jobId, request, candidateId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CONFLICT");
    }

    // ── WITHDRAW ─────────────────────────────────────────────────────────

    [Fact]
    public async Task WithdrawApplicationAsync_ShouldReturnNotFound_WhenApplicationDoesNotExist()
    {
        // Arrange
        _applicationRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentFlow.Domain.Entities.Application?)null);

        // Act
        var result = await _sut.WithdrawApplicationAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    // ── GET BY ID ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetApplicationByIdAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        _applicationRepositoryMock.Setup(repo => repo.GetApplicationWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentFlow.Domain.Entities.Application?)null);

        // Act
        var result = await _sut.GetApplicationByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}
