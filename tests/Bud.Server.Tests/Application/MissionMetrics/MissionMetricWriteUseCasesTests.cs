using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.UseCases.Indicators;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Metrics;

public sealed class MissionMetricWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task DefineMissionMetric_WhenMissionNotFound_ReturnsNotFound()
    {
        var metricRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);

        metricRepository
            .Setup(repository => repository.GetGoalByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var useCase = new CreateIndicator(metricRepository.Object, authorizationGateway.Object, NullLogger<CreateIndicator>.Instance);

        var request = new CreateIndicatorRequest
        {
            GoalId = Guid.NewGuid(),
            Name = "Metrica",
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = "Descricao"
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Meta não encontrada.");
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DefineMissionMetric_WhenUnauthorized_ReturnsForbidden()
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

        var metricRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(repository => repository.GetGoalByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateIndicator(metricRepository.Object, authorizationGateway.Object, NullLogger<CreateIndicator>.Instance);

        var request = new CreateIndicatorRequest
        {
            GoalId = mission.Id,
            Name = "Metrica",
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = "Descricao"
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
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

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateIndicator(metricRepository.Object, authorizationGateway.Object, NullLogger<CreateIndicator>.Instance);

        var request = new CreateIndicatorRequest
        {
            GoalId = mission.Id,
            Name = "Quality Assessment",
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = "Achieve excellent quality"
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Quality Assessment");
        result.Value.GoalId.Should().Be(mission.Id);
        result.Value.OrganizationId.Should().Be(organizationId);
        metricRepository.Verify(repository => repository.AddAsync(It.IsAny<Indicator>(), It.IsAny<CancellationToken>()), Times.Once);
        metricRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviseMissionMetricDefinition_WhenUnauthorized_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "Metrica",
            Type = IndicatorType.Qualitative,
            GoalId = Guid.NewGuid(),
            OrganizationId = organizationId
        };

        var metricRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(repository => repository.GetByIdAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new PatchIndicator(metricRepository.Object, authorizationGateway.Object, NullLogger<PatchIndicator>.Instance);

        var request = new PatchIndicatorRequest
        {
            Name = "Nova Metrica",
            Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
            TargetText = "Descricao"
        };

        var result = await useCase.ExecuteAsync(User, metric.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
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
            .Setup(repository => repository.GetByIdAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(indicatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new PatchIndicator(metricRepository.Object, authorizationGateway.Object, NullLogger<PatchIndicator>.Instance);

        var request = new PatchIndicatorRequest
        {
            Name = "Updated Metric",
            Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.KeepAbove,
            MinValue = 100m,
            Unit = Bud.Shared.Contracts.IndicatorUnit.Points
        };

        var result = await useCase.ExecuteAsync(User, indicatorId, request);

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
            .Setup(repository => repository.GetByIdAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(repository => repository.RemoveAsync(metric, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        metricRepository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, metric.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DeleteIndicator(metricRepository.Object, authorizationGateway.Object, NullLogger<DeleteIndicator>.Instance);

        var result = await useCase.ExecuteAsync(User, metric.Id);

        result.IsSuccess.Should().BeTrue();
        metricRepository.Verify(repository => repository.RemoveAsync(metric, It.IsAny<CancellationToken>()), Times.Once);
        metricRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMissionMetric_WhenMetricNotFound_ReturnsNotFound()
    {
        var metricRepository = new Mock<IIndicatorRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Indicator?)null);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);

        var useCase = new DeleteIndicator(metricRepository.Object, authorizationGateway.Object, NullLogger<DeleteIndicator>.Instance);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Indicador não encontrado.");
        authorizationGateway.VerifyNoOtherCalls();
    }
}
