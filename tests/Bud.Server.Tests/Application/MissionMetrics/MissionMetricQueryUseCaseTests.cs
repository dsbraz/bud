using Bud.Server.Application.MissionMetrics;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionMetrics;

public sealed class MissionMetricQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        // Arrange
        var metricId = Guid.NewGuid();
        var metricService = new Mock<IMissionMetricService>();
        var progressService = new Mock<IMissionProgressService>();
        metricService
            .Setup(s => s.GetByIdAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionMetric>.Success(new MissionMetric { Id = metricId, Name = "X", OrganizationId = Guid.NewGuid() }));

        var useCase = new MissionMetricQueryUseCase(metricService.Object, progressService.Object);

        // Act
        var result = await useCase.GetByIdAsync(metricId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(metricId);
    }

    [Fact]
    public async Task GetProgressAsync_DelegatesToProgressService()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        var metricService = new Mock<IMissionMetricService>();
        var progressService = new Mock<IMissionProgressService>();
        progressService
            .Setup(s => s.GetMetricProgressAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<List<MetricProgressDto>>.Success([]));

        var useCase = new MissionMetricQueryUseCase(metricService.Object, progressService.Object);

        // Act
        var result = await useCase.GetProgressAsync(ids);

        // Assert
        result.IsSuccess.Should().BeTrue();
        progressService.Verify(s => s.GetMetricProgressAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
