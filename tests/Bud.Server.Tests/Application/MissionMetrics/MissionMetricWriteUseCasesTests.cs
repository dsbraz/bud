using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.MissionMetrics;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionMetrics;

public sealed class MissionMetricWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task DefineMissionMetric_WhenMissionNotFound_ReturnsNotFound()
    {
        var metricRepository = new Mock<IMissionMetricRepository>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);

        metricRepository
            .Setup(repository => repository.GetMissionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = new DefineMissionMetric(metricRepository.Object, authorizationGateway.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Metrica",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
            TargetText = "Descricao"
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Missão não encontrada.");
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DefineMissionMetric_WhenUnauthorized_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            OrganizationId = organizationId
        };

        var metricRepository = new Mock<IMissionMetricRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(repository => repository.GetMissionByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new DefineMissionMetric(metricRepository.Object, authorizationGateway.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Metrica",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            OrganizationId = organizationId
        };

        var metricRepository = new Mock<IMissionMetricRepository>();
        metricRepository
            .Setup(repository => repository.GetMissionByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        metricRepository
            .Setup(repository => repository.AddAsync(It.IsAny<MissionMetric>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        metricRepository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DefineMissionMetric(metricRepository.Object, authorizationGateway.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Quality Assessment",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
            TargetText = "Achieve excellent quality"
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Quality Assessment");
        result.Value.MissionId.Should().Be(mission.Id);
        result.Value.OrganizationId.Should().Be(organizationId);
        metricRepository.Verify(repository => repository.AddAsync(It.IsAny<MissionMetric>(), It.IsAny<CancellationToken>()), Times.Once);
        metricRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DefineMissionMetric_WithObjectiveFromDifferentMission_ReturnsValidationError()
    {
        var organizationId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var mission = new Mission
        {
            Id = missionId,
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            OrganizationId = organizationId
        };

        var objective = new MissionObjective
        {
            Id = Guid.NewGuid(),
            Name = "Other Objective",
            MissionId = Guid.NewGuid(),
            OrganizationId = organizationId
        };

        var metricRepository = new Mock<IMissionMetricRepository>();
        metricRepository
            .Setup(repository => repository.GetMissionByIdAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        metricRepository
            .Setup(repository => repository.GetObjectiveByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DefineMissionMetric(metricRepository.Object, authorizationGateway.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = missionId,
            MissionObjectiveId = objective.Id,
            Name = "Metric",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
            TargetText = "Description"
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Be("Objetivo deve pertencer à mesma missão.");
    }

    [Fact]
    public async Task ReviseMissionMetricDefinition_WhenUnauthorized_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Metrica",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = organizationId
        };

        var metricRepository = new Mock<IMissionMetricRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(repository => repository.GetByIdAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new ReviseMissionMetricDefinition(metricRepository.Object, authorizationGateway.Object);

        var request = new UpdateMissionMetricRequest
        {
            Name = "Nova Metrica",
            Type = Bud.Shared.Contracts.MetricType.Qualitative,
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
        var metricId = Guid.NewGuid();

        var metric = new MissionMetric
        {
            Id = metricId,
            Name = "Original",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = organizationId
        };

        var metricRepository = new Mock<IMissionMetricRepository>();
        metricRepository
            .Setup(repository => repository.GetByIdAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(repository => repository.GetByIdTrackingAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new ReviseMissionMetricDefinition(metricRepository.Object, authorizationGateway.Object);

        var request = new UpdateMissionMetricRequest
        {
            Name = "Updated Metric",
            Type = Bud.Shared.Contracts.MetricType.Quantitative,
            QuantitativeType = Bud.Shared.Contracts.QuantitativeMetricType.KeepAbove,
            MinValue = 100m,
            Unit = Bud.Shared.Contracts.MetricUnit.Points
        };

        var result = await useCase.ExecuteAsync(User, metricId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Metric");
        result.Value.Type.Should().Be(MetricType.Quantitative);
        metricRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMissionMetric_WhenAuthorized_DelegatesToRepository()
    {
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Metrica",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };

        var metricRepository = new Mock<IMissionMetricRepository>();
        metricRepository
            .Setup(repository => repository.GetByIdAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(repository => repository.GetByIdTrackingAsync(metric.Id, It.IsAny<CancellationToken>()))
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

        var useCase = new RemoveMissionMetric(metricRepository.Object, authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, metric.Id);

        result.IsSuccess.Should().BeTrue();
        metricRepository.Verify(repository => repository.RemoveAsync(metric, It.IsAny<CancellationToken>()), Times.Once);
        metricRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMissionMetric_WhenMetricNotFound_ReturnsNotFound()
    {
        var metricRepository = new Mock<IMissionMetricRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionMetric?)null);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);

        var useCase = new RemoveMissionMetric(metricRepository.Object, authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Métrica da missão não encontrada.");
        authorizationGateway.VerifyNoOtherCalls();
    }
}
