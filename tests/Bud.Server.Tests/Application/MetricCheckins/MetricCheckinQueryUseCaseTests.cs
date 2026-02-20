using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.MetricCheckins;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MetricCheckins;

public sealed class MetricCheckinQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var checkin = new MetricCheckin
        {
            Id = checkinId,
            MissionMetricId = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinRepository = new Mock<IMetricCheckinRepository>();
        checkinRepository
            .Setup(r => r.GetByIdAsync(checkinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);

        var useCase = new MetricCheckinQueryUseCase(checkinRepository.Object);

        // Act
        var result = await useCase.GetByIdAsync(checkinId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(checkinId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var checkinRepository = new Mock<IMetricCheckinRepository>();
        checkinRepository
            .Setup(r => r.GetByIdAsync(checkinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MetricCheckin?)null);

        var useCase = new MetricCheckinQueryUseCase(checkinRepository.Object);

        // Act
        var result = await useCase.GetByIdAsync(checkinId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Check-in não encontrado.");
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        // Arrange
        var checkinRepository = new Mock<IMetricCheckinRepository>();
        checkinRepository
            .Setup(r => r.GetAllAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MetricCheckin>());

        var useCase = new MetricCheckinQueryUseCase(checkinRepository.Object);

        // Act
        var result = await useCase.GetAllAsync(null, null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        checkinRepository.Verify(r => r.GetAllAsync(null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
