using Bud.Server.Application.Common;
using Bud.Server.Application.Metrics;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Metrics;

public sealed class MissionMetricReadUseCasesTests
{
    [Fact]
    public async Task ViewMissionMetricDetails_WhenMetricExists_ReturnsSuccess()
    {
        var metricId = Guid.NewGuid();
        var metricRepository = new Mock<IMetricRepository>();

        metricRepository
            .Setup(repository => repository.GetByIdAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Metric { Id = metricId, Name = "X", OrganizationId = Guid.NewGuid() });

        var useCase = new GetMetricById(metricRepository.Object);

        var result = await useCase.ExecuteAsync(metricId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(metricId);
    }

    [Fact]
    public async Task ViewMissionMetricDetails_WhenMetricNotFound_ReturnsNotFound()
    {
        var metricId = Guid.NewGuid();
        var metricRepository = new Mock<IMetricRepository>();

        metricRepository
            .Setup(repository => repository.GetByIdAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Metric?)null);

        var useCase = new GetMetricById(metricRepository.Object);

        var result = await useCase.ExecuteAsync(metricId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Métrica da missão não encontrada.");
    }

    [Fact]
    public async Task BrowseMissionMetrics_DelegatesToRepository()
    {
        var missionId = Guid.NewGuid();
        var metricRepository = new Mock<IMetricRepository>();

        var pagedResult = new PagedResult<Metric>
        {
            Items = [new Metric { Id = Guid.NewGuid(), Name = "M1", OrganizationId = Guid.NewGuid() }],
            Total = 1,
            Page = 1,
            PageSize = 10
        };

        metricRepository
            .Setup(repository => repository.GetAllAsync(missionId, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new ListMetrics(metricRepository.Object);

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

        var useCase = new ListMetricProgress(progressService.Object);

        var result = await useCase.ExecuteAsync(ids);

        result.IsSuccess.Should().BeTrue();
        progressService.Verify(service => service.GetMetricProgressAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
