using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Application.UnitTests.Application.Indicators;

public sealed class IndicatorWriteUseCasesTests
{
    [Fact]
    public async Task DefineMissionMetric_WhenMissionNotFound_ReturnsNotFound()
    {
        var metricRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);

        metricRepository
            .Setup(repository => repository.GetGoalByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var useCase = new CreateIndicator(metricRepository.Object, NullLogger<CreateIndicator>.Instance);

        var result = await useCase.ExecuteAsync(new CreateIndicatorCommand(
            Guid.NewGuid(),
            "Metrica",
            IndicatorType.Qualitative,
            null,
            null,
            null,
            null,
            "Descricao"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Meta não encontrada.");
    }

    [Fact]
    public async Task DefineMissionMetric_WhenAuthorized_CreatesMetricViaRepository()
    {
        var organizationId = Guid.NewGuid();
        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = GoalStatus.Planned,
            OrganizationId = organizationId
        };

        var metricRepository = new Mock<IIndicatorRepository>();
        metricRepository
            .Setup(repository => repository.GetGoalByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        metricRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Indicator>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        metricRepository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new CreateIndicator(metricRepository.Object, NullLogger<CreateIndicator>.Instance);

        var result = await useCase.ExecuteAsync(new CreateIndicatorCommand(
            mission.Id,
            "Quality Assessment",
            IndicatorType.Qualitative,
            null,
            null,
            null,
            null,
            "Achieve excellent quality"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Quality Assessment");
        result.Value.GoalId.Should().Be(mission.Id);
        result.Value.OrganizationId.Should().Be(organizationId);
        metricRepository.Verify(repository => repository.AddAsync(It.IsAny<Indicator>(), It.IsAny<CancellationToken>()), Times.Once);
        metricRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviseMissionMetricDefinition_WhenNotFound_ReturnsNotFound()
    {
        var metricRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Indicator?)null);

        var useCase = new PatchIndicator(metricRepository.Object, NullLogger<PatchIndicator>.Instance);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), new PatchIndicatorCommand(
            "Nova Metrica",
            IndicatorType.Qualitative,
            default,
            default,
            default,
            default,
            "Descricao"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ReviseMissionMetricDefinition_WhenAuthorized_UpdatesMetricViaRepository()
    {
        var organizationId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();

        var metric = new Indicator
        {
            Id = indicatorId,
            Name = "Original",
            Type = IndicatorType.Qualitative,
            GoalId = Guid.NewGuid(),
            OrganizationId = organizationId
        };

        var metricRepository = new Mock<IIndicatorRepository>();
        metricRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new PatchIndicator(metricRepository.Object, NullLogger<PatchIndicator>.Instance);

        var result = await useCase.ExecuteAsync(indicatorId, new PatchIndicatorCommand(
            "Updated Metric",
            IndicatorType.Quantitative,
            QuantitativeIndicatorType.KeepAbove,
            (decimal?)100m,
            default,
            IndicatorUnit.Points,
            default));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Metric");
        result.Value.Type.Should().Be(IndicatorType.Quantitative);
        metricRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMissionMetric_WhenAuthorized_DelegatesToRepository()
    {
        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "Metrica",
            Type = IndicatorType.Qualitative,
            GoalId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };

        var metricRepository = new Mock<IIndicatorRepository>();
        metricRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(repository => repository.RemoveAsync(metric, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        metricRepository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new DeleteIndicator(metricRepository.Object, NullLogger<DeleteIndicator>.Instance);

        var result = await useCase.ExecuteAsync(metric.Id);

        result.IsSuccess.Should().BeTrue();
        metricRepository.Verify(repository => repository.RemoveAsync(metric, It.IsAny<CancellationToken>()), Times.Once);
        metricRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMissionMetric_WhenMetricNotFound_ReturnsNotFound()
    {
        var metricRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Indicator?)null);

        var useCase = new DeleteIndicator(metricRepository.Object, NullLogger<DeleteIndicator>.Instance);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Indicador não encontrado.");
    }
}
