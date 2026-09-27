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
        _sut = new ApplicationService(_applicationRepositoryMock.Object, _jobRepositoryMock.Object);
    }

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
}
