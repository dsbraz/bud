using System.Security.Claims;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Application.MetricCheckins;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
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

        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

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
    public async Task CreateAsync_WhenSuccess_TriggersNotification()
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
        var checkin = new MetricCheckin
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
            .ReturnsAsync(ServiceResult<MetricCheckin>.Success(checkin));

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
        entityLookup
            .Setup(l => l.GetCollaboratorAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collaborator { Id = collaboratorId, FullName = "Test", Email = "test@test.com", OrganizationId = orgId });

        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Text = "ok",
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        notificationOrchestrator.Verify(
            o => o.NotifyMetricCheckinCreatedAsync(checkin.Id, metric.Id, orgId, collaboratorId, It.IsAny<CancellationToken>()),
            Times.Once);
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

        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

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

        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MetricCheckinCommandUseCase(
            checkinService.Object,
            authorizationGateway.Object,
            tenantProvider.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

        var result = await useCase.DeleteAsync(User, checkin.Id);

        result.IsSuccess.Should().BeTrue();
        checkinService.Verify(s => s.DeleteAsync(checkin.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
