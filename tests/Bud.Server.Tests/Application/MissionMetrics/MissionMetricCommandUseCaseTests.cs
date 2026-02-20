using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.MissionMetrics;
using Bud.Server.Application.Ports;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionMetrics;

public sealed class MissionMetricCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        var metricRepository = new Mock<IMissionMetricRepository>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        metricRepository
            .Setup(r => r.GetMissionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = new MissionMetricCommandUseCase(metricRepository.Object, authorizationGateway.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Metrica",
            Type = MetricType.Qualitative,
            TargetText = "Descricao"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Missão não encontrada.");
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            OrganizationId = orgId
        };

        var metricRepository = new Mock<IMissionMetricRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(r => r.GetMissionByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new MissionMetricCommandUseCase(metricRepository.Object, authorizationGateway.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Metrica",
            Type = MetricType.Qualitative,
            TargetText = "Descricao"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorized_CreatesMetricViaRepository()
    {
        var orgId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            OrganizationId = orgId
        };

        var metricRepository = new Mock<IMissionMetricRepository>();
        metricRepository
            .Setup(r => r.GetMissionByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        metricRepository
            .Setup(r => r.AddAsync(It.IsAny<MissionMetric>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        metricRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new MissionMetricCommandUseCase(metricRepository.Object, authorizationGateway.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = mission.Id,
            Name = "Quality Assessment",
            Type = MetricType.Qualitative,
            TargetText = "Achieve excellent quality"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Quality Assessment");
        result.Value!.MissionId.Should().Be(mission.Id);
        result.Value!.OrganizationId.Should().Be(orgId);
        metricRepository.Verify(r => r.AddAsync(It.IsAny<MissionMetric>(), It.IsAny<CancellationToken>()), Times.Once);
        metricRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithObjectiveFromDifferentMission_ReturnsValidationError()
    {
        var orgId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = missionId,
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            OrganizationId = orgId
        };

        var objective = new MissionObjective
        {
            Id = Guid.NewGuid(),
            Name = "Other Objective",
            MissionId = Guid.NewGuid(), // Different mission
            OrganizationId = orgId
        };

        var metricRepository = new Mock<IMissionMetricRepository>();
        metricRepository
            .Setup(r => r.GetMissionByIdAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        metricRepository
            .Setup(r => r.GetObjectiveByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new MissionMetricCommandUseCase(metricRepository.Object, authorizationGateway.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = missionId,
            MissionObjectiveId = objective.Id,
            Name = "Metric",
            Type = MetricType.Qualitative,
            TargetText = "Description"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Be("Objetivo deve pertencer à mesma missão.");
    }

    [Fact]
    public async Task UpdateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Metrica",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = orgId
        };

        var metricRepository = new Mock<IMissionMetricRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(r => r.GetByIdAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new MissionMetricCommandUseCase(metricRepository.Object, authorizationGateway.Object);

        var request = new UpdateMissionMetricRequest
        {
            Name = "Nova Metrica",
            Type = MetricType.Qualitative,
            TargetText = "Descricao"
        };

        var result = await useCase.UpdateAsync(User, metric.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateAsync_WhenAuthorized_UpdatesMetricViaRepository()
    {
        var orgId = Guid.NewGuid();
        var metricId = Guid.NewGuid();
        var metric = new MissionMetric
        {
            Id = metricId,
            Name = "Original",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = orgId
        };

        var metricRepository = new Mock<IMissionMetricRepository>();
        metricRepository
            .Setup(r => r.GetByIdAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(r => r.GetByIdTrackingAsync(metricId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new MissionMetricCommandUseCase(metricRepository.Object, authorizationGateway.Object);

        var request = new UpdateMissionMetricRequest
        {
            Name = "Updated Metric",
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepAbove,
            MinValue = 100m,
            Unit = MetricUnit.Points
        };

        var result = await useCase.UpdateAsync(User, metricId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Metric");
        result.Value!.Type.Should().Be(MetricType.Quantitative);
        metricRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_DelegatesToRepository()
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
            .Setup(r => r.GetByIdAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(r => r.GetByIdTrackingAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);
        metricRepository
            .Setup(r => r.RemoveAsync(metric, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        metricRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, metric.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new MissionMetricCommandUseCase(metricRepository.Object, authorizationGateway.Object);

        var result = await useCase.DeleteAsync(User, metric.Id);

        result.IsSuccess.Should().BeTrue();
        metricRepository.Verify(r => r.RemoveAsync(metric, It.IsAny<CancellationToken>()), Times.Once);
        metricRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMetricNotFound_ReturnsNotFound()
    {
        var metricRepository = new Mock<IMissionMetricRepository>(MockBehavior.Strict);
        metricRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionMetric?)null);

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);

        var useCase = new MissionMetricCommandUseCase(metricRepository.Object, authorizationGateway.Object);

        var result = await useCase.DeleteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Métrica da missão não encontrada.");
        authorizationGateway.VerifyNoOtherCalls();
    }
}
