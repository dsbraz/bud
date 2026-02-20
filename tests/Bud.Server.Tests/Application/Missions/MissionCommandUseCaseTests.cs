using System.Security.Claims;
using Bud.Server.Authorization;
using Bud.Server.Services;
using Bud.Server.Services;
using Bud.Server.Application.Missions;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Missions;

public sealed class MissionCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenScopeResolutionFails_ReturnsNotFound()
    {
        var missionService = new Mock<IMissionService>(MockBehavior.Strict);
        var scopeResolver = new Mock<IMissionScopeResolver>();
        scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization,
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Guid>.NotFound("Organização não encontrada."));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IEntityLookupService>(MockBehavior.Strict);
        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid()
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
        missionService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var missionService = new Mock<IMissionService>(MockBehavior.Strict);
        var scopeResolver = new Mock<IMissionScopeResolver>();
        scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization,
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Guid>.Success(orgId));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IEntityLookupService>(MockBehavior.Strict);
        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid()
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        missionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenSuccess_TriggersNotification()
    {
        var orgId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = missionId,
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            OrganizationId = orgId
        };

        var missionService = new Mock<IMissionService>();
        missionService
            .Setup(s => s.CreateAsync(It.IsAny<CreateMissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Mission>.Success(mission));

        var scopeResolver = new Mock<IMissionScopeResolver>();
        scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization,
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Guid>.Success(orgId));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IEntityLookupService>();
        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = orgId
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        notificationOrchestrator.Verify(
            o => o.NotifyMissionCreatedAsync(missionId, orgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        var missionService = new Mock<IMissionService>(MockBehavior.Strict);
        var scopeResolver = new Mock<IMissionScopeResolver>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IEntityLookupService>();
        entityLookup
            .Setup(l => l.GetMissionAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

        var request = new UpdateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned
        };

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        authorizationGateway.VerifyNoOtherCalls();
        missionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenSuccess_TriggersNotification()
    {
        var orgId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = missionId,
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            OrganizationId = orgId
        };

        var missionService = new Mock<IMissionService>();
        missionService
            .Setup(s => s.UpdateAsync(missionId, It.IsAny<UpdateMissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Mission>.Success(mission));

        var scopeResolver = new Mock<IMissionScopeResolver>();
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IEntityLookupService>();
        entityLookup
            .Setup(l => l.GetMissionAsync(missionId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

        var request = new UpdateMissionRequest
        {
            Name = "Missão Atualizada",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Active
        };

        var result = await useCase.UpdateAsync(User, missionId, request);

        result.IsSuccess.Should().BeTrue();
        notificationOrchestrator.Verify(
            o => o.NotifyMissionUpdatedAsync(missionId, orgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            OrganizationId = Guid.NewGuid()
        };

        var missionService = new Mock<IMissionService>(MockBehavior.Strict);
        var scopeResolver = new Mock<IMissionScopeResolver>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, mission.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IEntityLookupService>();
        entityLookup
            .Setup(l => l.GetMissionAsync(mission.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

        var result = await useCase.DeleteAsync(User, mission.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        missionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_WhenSuccess_TriggersNotification()
    {
        var orgId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = missionId,
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            OrganizationId = orgId
        };

        var missionService = new Mock<IMissionService>();
        missionService
            .Setup(s => s.DeleteAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var scopeResolver = new Mock<IMissionScopeResolver>();
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IEntityLookupService>();
        entityLookup
            .Setup(l => l.GetMissionAsync(missionId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var notificationOrchestrator = new Mock<INotificationOrchestrator>();
        var useCase = new MissionCommandUseCase(
            missionService.Object,
            scopeResolver.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            notificationOrchestrator.Object);

        var result = await useCase.DeleteAsync(User, missionId);

        result.IsSuccess.Should().BeTrue();
        notificationOrchestrator.Verify(
            o => o.NotifyMissionDeletedAsync(missionId, orgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
