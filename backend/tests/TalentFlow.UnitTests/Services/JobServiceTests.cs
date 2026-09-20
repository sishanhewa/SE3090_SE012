using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TalentFlow.Application.DTOs.Jobs;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Entities;
using TalentFlow.Domain.Enums;
using TalentFlow.Infrastructure.Services;
using Xunit;

namespace TalentFlow.UnitTests.Services;

public class JobServiceTests
{
    private readonly Mock<IJobRepository> _jobRepositoryMock;
    private readonly JobService _sut;

    public JobServiceTests()
    {
        _jobRepositoryMock = new Mock<IJobRepository>();
        _sut = new JobService(_jobRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateJobAsync_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var request = new CreateJobRequest
        {
            Title = "Software Engineer",
            Description = "A great job",
            DepartmentId = Guid.NewGuid(),
            Location = "Remote",
            EmploymentType = "FullTime",
            MinimumExperience = 2
        };

        _jobRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Job()); // Assuming it returns Task<Job>

        // Act
        var result = await _sut.CreateJobAsync(request, companyId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be(request.Title);
        result.Data.Status.Should().Be(JobStatus.Draft.ToString());
        
        _jobRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Job>(j => j.Title == request.Title), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetJobByIdAsync_ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        _jobRepositoryMock.Setup(repo => repo.GetJobWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        // Act
        var result = await _sut.GetJobByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
