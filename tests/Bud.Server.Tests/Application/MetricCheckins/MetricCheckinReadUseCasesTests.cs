using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.UseCases.Metrics;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Checkins;

public sealed class MetricCheckinReadUseCasesTests
{
    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        // Arrange
        var metricId = Guid.NewGuid();
        var checkinId = Guid.NewGuid();
        var checkin = new MetricCheckin
        {
            Id = checkinId,
            MetricId = metricId,
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinRepository = new Mock<IMetricRepository>();
        checkinRepository
            .Setup(r => r.GetCheckinByIdAsync(checkinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);

        var useCase = new GetMetricCheckinById(checkinRepository.Object);

        // Act
        var result = await useCase.ExecuteAsync(metricId, checkinId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(checkinId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var metricId = Guid.NewGuid();
        var checkinId = Guid.NewGuid();
        var checkinRepository = new Mock<IMetricRepository>();
        checkinRepository
            .Setup(r => r.GetCheckinByIdAsync(checkinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MetricCheckin?)null);

        var useCase = new GetMetricCheckinById(checkinRepository.Object);

        // Act
        var result = await useCase.ExecuteAsync(metricId, checkinId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Check-in não encontrado.");
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        // Arrange
        var checkinRepository = new Mock<IMetricRepository>();
        checkinRepository
            .Setup(r => r.GetCheckinsAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MetricCheckin>());

        var useCase = new ListMetricCheckins(checkinRepository.Object);
        var metricId = Guid.NewGuid();

        // Act
        var result = await useCase.ExecuteAsync(metricId, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        checkinRepository.Verify(r => r.GetCheckinsAsync(metricId, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
