using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TalentFlow.Application.DTOs.Employees;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Domain.Entities;
using TalentFlow.Infrastructure.Services;
using Xunit;

namespace TalentFlow.UnitTests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly EmployeeService _sut;

    public EmployeeServiceTests()
    {
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _sut = new EmployeeService(_employeeRepositoryMock.Object);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
    {
        // Arrange
        _employeeRepositoryMock.Setup(repo => repo.GetEmployeeWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _sut.GetEmployeeByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
