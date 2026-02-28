using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.UseCases.Indicators;
using Bud.Server.Application.ReadModels;
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
        var indicatorId = Guid.NewGuid();
        var metricRepository = new Mock<IIndicatorRepository>();

        metricRepository
            .Setup(repository => repository.GetByIdAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Indicator { Id = indicatorId, Name = "X", OrganizationId = Guid.NewGuid() });

        var useCase = new GetIndicatorById(metricRepository.Object);

        var result = await useCase.ExecuteAsync(indicatorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(indicatorId);
    }

    [Fact]
    public async Task ViewMissionMetricDetails_WhenMetricNotFound_ReturnsNotFound()
    {
        var indicatorId = Guid.NewGuid();
        var metricRepository = new Mock<IIndicatorRepository>();

        metricRepository
            .Setup(repository => repository.GetByIdAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Indicator?)null);

        var useCase = new GetIndicatorById(metricRepository.Object);

        var result = await useCase.ExecuteAsync(indicatorId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Indicador não encontrado.");
    }

    [Fact]
    public async Task BrowseMissionMetrics_DelegatesToRepository()
    {
        var goalId = Guid.NewGuid();
        var metricRepository = new Mock<IIndicatorRepository>();

        var pagedResult = new PagedResult<Indicator>
        {
            Items = [new Indicator { Id = Guid.NewGuid(), Name = "M1", OrganizationId = Guid.NewGuid() }],
            Total = 1,
            Page = 1,
            PageSize = 10
        };

        metricRepository
            .Setup(repository => repository.GetAllAsync(goalId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new ListIndicators(metricRepository.Object);

        var result = await useCase.ExecuteAsync(goalId, null, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        metricRepository.Verify(repository => repository.GetAllAsync(goalId, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateMissionMetricProgress_DelegatesToProgressService()
    {
        var indicatorId = Guid.NewGuid();
        var progressService = new Mock<IGoalProgressService>();

        var snapshot = new IndicatorProgressSnapshot { IndicatorId = indicatorId };
        progressService
            .Setup(service => service.GetIndicatorProgressAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IndicatorProgressSnapshot?>.Success(snapshot));

        var useCase = new GetIndicatorProgress(progressService.Object);

        var result = await useCase.ExecuteAsync(indicatorId);

        result.IsSuccess.Should().BeTrue();
        progressService.Verify(service => service.GetIndicatorProgressAsync(indicatorId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
