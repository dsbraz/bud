using Bud.Server.Application.Common;
using Bud.Server.Application.MissionMetrics;
using Bud.Server.Application.Ports;
using Bud.Server.Domain.ReadModels;

using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionMetrics;

public sealed class MissionMetricQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_WhenMetricExists_ReturnsSuccess()
    {
        // Arrange
        var metricId = Guid.NewGuid();
        var metricRepository = new Mock<IMissionMetricRepository>();
        var progressService = new Mock<IMissionProgressService>();
        metricRepository
            .Setup(r => r.GetByIdAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MissionMetric { Id = metricId, Name = "X", OrganizationId = Guid.NewGuid() });

        var useCase = new MissionMetricQueryUseCase(metricRepository.Object, progressService.Object);

        // Act
        var result = await useCase.GetByIdAsync(metricId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(metricId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMetricNotFound_ReturnsNotFound()
    {
        // Arrange
        var metricId = Guid.NewGuid();
        var metricRepository = new Mock<IMissionMetricRepository>();
        var progressService = new Mock<IMissionProgressService>();
        metricRepository
            .Setup(r => r.GetByIdAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionMetric?)null);

        var useCase = new MissionMetricQueryUseCase(metricRepository.Object, progressService.Object);

        // Act
        var result = await useCase.GetByIdAsync(metricId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Métrica da missão não encontrada.");
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var metricRepository = new Mock<IMissionMetricRepository>();
        var progressService = new Mock<IMissionProgressService>();
        var pagedResult = new PagedResult<MissionMetric>
        {
            Items = [new MissionMetric { Id = Guid.NewGuid(), Name = "M1", OrganizationId = Guid.NewGuid() }],
            Total = 1,
            Page = 1,
            PageSize = 10
        };
        metricRepository
            .Setup(r => r.GetAllAsync(missionId, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new MissionMetricQueryUseCase(metricRepository.Object, progressService.Object);

        // Act
        var result = await useCase.GetAllAsync(missionId, null, null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        metricRepository.Verify(r => r.GetAllAsync(missionId, null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProgressAsync_DelegatesToProgressService()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        var metricRepository = new Mock<IMissionMetricRepository>();
        var progressService = new Mock<IMissionProgressService>();
        progressService
            .Setup(s => s.GetMetricProgressAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<MetricProgressSnapshot>>.Success([]));

        var useCase = new MissionMetricQueryUseCase(metricRepository.Object, progressService.Object);

        // Act
        var result = await useCase.GetProgressAsync(ids);

        // Assert
        result.IsSuccess.Should().BeTrue();
        progressService.Verify(s => s.GetMetricProgressAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
