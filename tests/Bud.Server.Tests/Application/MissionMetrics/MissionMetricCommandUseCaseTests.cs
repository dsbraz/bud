using System.Security.Claims;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Application.MissionMetrics;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
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
        var metricService = new Mock<IMissionMetricService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = new MissionMetricCommandUseCase(metricService.Object, authorizationGateway.Object, entityLookup.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            TargetText = "Descrição"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Missão não encontrada.");
        metricService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = orgId
        };

        var metricService = new Mock<IMissionMetricService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionMetricAsync(metric.Id, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var useCase = new MissionMetricCommandUseCase(metricService.Object, authorizationGateway.Object, entityLookup.Object);

        var request = new UpdateMissionMetricRequest
        {
            Name = "Nova Métrica",
            Type = MetricType.Qualitative,
            TargetText = "Descrição"
        };

        var result = await useCase.UpdateAsync(User, metric.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        metricService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_DelegatesToService()
    {
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };

        var metricService = new Mock<IMissionMetricService>();
        metricService.Setup(s => s.DeleteAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, metric.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionMetricAsync(metric.Id, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var useCase = new MissionMetricCommandUseCase(metricService.Object, authorizationGateway.Object, entityLookup.Object);

        var result = await useCase.DeleteAsync(User, metric.Id);

        result.IsSuccess.Should().BeTrue();
        metricService.Verify(s => s.DeleteAsync(metric.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorizedAndCreated_DispatchesDomainEvent()
    {
        var missionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = missionId,
            Name = "Missão",
            OrganizationId = organizationId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned
        };

        var createdMetric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            MissionId = missionId,
            OrganizationId = organizationId,
            Name = "Métrica",
            Type = MetricType.Qualitative
        };

        var metricService = new Mock<IMissionMetricService>();
        metricService
            .Setup(s => s.CreateAsync(It.IsAny<CreateMissionMetricRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionMetric>.Success(createdMetric));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionAsync(missionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MissionMetricCommandUseCase(
            metricService.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var request = new CreateMissionMetricRequest
        {
            MissionId = missionId,
            Name = "Métrica",
            Type = MetricType.Qualitative,
            TargetText = "Texto"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.MissionMetrics.Events.MissionMetricCreatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAuthorizedAndUpdated_DispatchesDomainEvent()
    {
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };
        var request = new UpdateMissionMetricRequest
        {
            Name = "Métrica 2",
            Type = MetricType.Qualitative,
            TargetText = "Novo alvo"
        };

        var metricService = new Mock<IMissionMetricService>();
        metricService
            .Setup(s => s.UpdateAsync(metric.Id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionMetric>.Success(new MissionMetric
            {
                Id = metric.Id,
                Name = request.Name,
                Type = request.Type,
                TargetText = request.TargetText,
                MissionId = metric.MissionId,
                OrganizationId = metric.OrganizationId
            }));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, metric.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionMetricAsync(metric.Id, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MissionMetricCommandUseCase(
            metricService.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var result = await useCase.UpdateAsync(User, metric.Id, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.MissionMetrics.Events.MissionMetricUpdatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorizedAndDeleted_DispatchesDomainEvent()
    {
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            MissionId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };

        var metricService = new Mock<IMissionMetricService>();
        metricService.Setup(s => s.DeleteAsync(metric.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, metric.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionMetricAsync(metric.Id, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MissionMetricCommandUseCase(
            metricService.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var result = await useCase.DeleteAsync(User, metric.Id);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.MissionMetrics.Events.MissionMetricDeletedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
