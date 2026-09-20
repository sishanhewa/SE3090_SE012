using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TalentFlow.Application.DTOs.Interviews;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Entities;
using TalentFlow.Infrastructure.Services;
using Xunit;

namespace TalentFlow.UnitTests.Services;

public class InterviewServiceTests
{
    private readonly Mock<IInterviewRepository> _interviewRepositoryMock;
    private readonly Mock<IApplicationRepository> _applicationRepositoryMock;
    private readonly InterviewService _sut;

    public InterviewServiceTests()
    {
        _interviewRepositoryMock = new Mock<IInterviewRepository>();
        _applicationRepositoryMock = new Mock<IApplicationRepository>();
        _sut = new InterviewService(_interviewRepositoryMock.Object, _applicationRepositoryMock.Object);
    }

    [Fact]
    public async Task ScheduleInterviewAsync_ShouldReturnNotFound_WhenApplicationDoesNotExist()
    {
        // Arrange
        _applicationRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentFlow.Domain.Entities.Application?)null);

        var request = new CreateInterviewRequest { ApplicationId = Guid.NewGuid() };

        // Act
        var result = await _sut.ScheduleInterviewAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
