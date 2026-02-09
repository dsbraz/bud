using System.Security.Claims;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Application.MetricCheckins;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MetricCheckins;

public sealed class MetricCheckinCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenCollaboratorNotIdentified_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Active,
            OrganizationId = orgId
        };
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            MissionId = mission.Id,
            Mission = mission,
            OrganizationId = orgId
        };

        var checkinService = new Mock<IMetricCheckinService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        authorizationGateway
            .Setup(g => g.CanAccessMissionScopeAsync(User, mission.WorkspaceId, mission.TeamId, mission.CollaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns((Guid?)null);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionMetricAsync(metric.Id, false, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Text = "ok",
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Colaborador não identificado.");
        checkinService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenNotAuthorAndNotGlobalAdmin_ReturnsForbidden()
    {
        var checkin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            MissionMetricId = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinService = new Mock<IMetricCheckinService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, checkin.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(Guid.NewGuid());

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMetricCheckinAsync(checkin.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);

        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object);

        var request = new UpdateMetricCheckinRequest
        {
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 2
        };

        var result = await useCase.UpdateAsync(User, checkin.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Apenas o autor pode editar este check-in.");
        checkinService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_WhenGlobalAdmin_DelegatesToService()
    {
        var checkin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            MissionMetricId = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinService = new Mock<IMetricCheckinService>();
        checkinService.Setup(s => s.DeleteAsync(checkin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, checkin.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMetricCheckinAsync(checkin.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);

        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object);

        var result = await useCase.DeleteAsync(User, checkin.Id);

        result.IsSuccess.Should().BeTrue();
        checkinService.Verify(s => s.DeleteAsync(checkin.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorizedAndCreated_DispatchesDomainEvent()
    {
        var orgId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Active,
            OrganizationId = orgId
        };
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            Name = "Métrica",
            Type = MetricType.Qualitative,
            MissionId = mission.Id,
            Mission = mission,
            OrganizationId = orgId
        };

        var createdCheckin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            MissionMetricId = metric.Id,
            CollaboratorId = collaboratorId,
            OrganizationId = orgId,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinService = new Mock<IMetricCheckinService>();
        checkinService
            .Setup(s => s.CreateAsync(It.IsAny<CreateMetricCheckinRequest>(), collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MetricCheckin>.Success(createdCheckin));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        authorizationGateway
            .Setup(g => g.CanAccessMissionScopeAsync(User, mission.WorkspaceId, mission.TeamId, mission.CollaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(collaboratorId);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionMetricAsync(metric.Id, false, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Text = "ok",
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.MetricCheckins.Events.MetricCheckinCreatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAuthorizedAndUpdated_DispatchesDomainEvent()
    {
        var checkin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            MissionMetricId = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };
        var request = new UpdateMetricCheckinRequest
        {
            Value = 55m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 4
        };

        var checkinService = new Mock<IMetricCheckinService>();
        checkinService
            .Setup(s => s.UpdateAsync(checkin.Id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MetricCheckin>.Success(new MetricCheckin
            {
                Id = checkin.Id,
                MissionMetricId = checkin.MissionMetricId,
                CollaboratorId = checkin.CollaboratorId,
                OrganizationId = checkin.OrganizationId,
                CheckinDate = request.CheckinDate,
                ConfidenceLevel = request.ConfidenceLevel,
                Value = request.Value
            }));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, checkin.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.CollaboratorId).Returns(checkin.CollaboratorId);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMetricCheckinAsync(checkin.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var result = await useCase.UpdateAsync(User, checkin.Id, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.MetricCheckins.Events.MetricCheckinUpdatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorizedAndDeleted_DispatchesDomainEvent()
    {
        var checkin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            MissionMetricId = Guid.NewGuid(),
            CollaboratorId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var checkinService = new Mock<IMetricCheckinService>();
        checkinService.Setup(s => s.DeleteAsync(checkin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, checkin.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMetricCheckinAsync(checkin.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkin);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var result = await useCase.DeleteAsync(User, checkin.Id);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.MetricCheckins.Events.MetricCheckinDeletedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
