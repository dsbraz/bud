using Bud.Server.Application.Common;
using Bud.Server.Application.MissionMetrics;
using Bud.Server.Application.Projections;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Infrastructure.Services;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionMetrics;

public sealed class MissionMetricReadUseCasesTests
{
    [Fact]
    public async Task ViewMissionMetricDetails_WhenMetricExists_ReturnsSuccess()
    {
        var metricId = Guid.NewGuid();
        var metricRepository = new Mock<IMissionMetricRepository>();

        metricRepository
            .Setup(repository => repository.GetByIdAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MissionMetric { Id = metricId, Name = "X", OrganizationId = Guid.NewGuid() });

        var useCase = new ViewMissionMetricDetails(metricRepository.Object);

        var result = await useCase.ExecuteAsync(metricId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(metricId);
    }

    [Fact]
    public async Task ViewMissionMetricDetails_WhenMetricNotFound_ReturnsNotFound()
    {
        var metricId = Guid.NewGuid();
        var metricRepository = new Mock<IMissionMetricRepository>();

        metricRepository
            .Setup(repository => repository.GetByIdAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionMetric?)null);

        var useCase = new ViewMissionMetricDetails(metricRepository.Object);

        var result = await useCase.ExecuteAsync(metricId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Métrica da missão não encontrada.");
    }

    [Fact]
    public async Task BrowseMissionMetrics_DelegatesToRepository()
    {
        var missionId = Guid.NewGuid();
        var metricRepository = new Mock<IMissionMetricRepository>();

        var pagedResult = new PagedResult<MissionMetric>
        {
            Items = [new MissionMetric { Id = Guid.NewGuid(), Name = "M1", OrganizationId = Guid.NewGuid() }],
            Total = 1,
            Page = 1,
            PageSize = 10
        };

        metricRepository
            .Setup(repository => repository.GetAllAsync(missionId, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new BrowseMissionMetrics(metricRepository.Object);

        var result = await useCase.ExecuteAsync(missionId, null, null, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        metricRepository.Verify(repository => repository.GetAllAsync(missionId, null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateMissionMetricProgress_DelegatesToProgressService()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        var progressService = new Mock<IMissionProgressService>();

        progressService
            .Setup(service => service.GetMetricProgressAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<MetricProgressSnapshot>>.Success([]));

        var useCase = new CalculateMissionMetricProgress(progressService.Object);

        var result = await useCase.ExecuteAsync(ids);

        result.IsSuccess.Should().BeTrue();
        progressService.Verify(service => service.GetMetricProgressAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
