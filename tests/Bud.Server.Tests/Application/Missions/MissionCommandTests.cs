using Bud.Server.Infrastructure.Services;
using Bud.Server.Infrastructure.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Missions;
using Bud.Server.Authorization;
using Bud.Server.Application.Notifications;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Missions;

public sealed class MissionCommandTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<IMissionRepository> _repo = new();
    private readonly Mock<IMissionScopeResolver> _scopeResolver = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();
    private readonly Mock<NotificationOrchestrator> _notificationOrchestrator = CreateNotificationOrchestratorMock();

    private MissionCommand CreateCommand()
        => new(_repo.Object, _scopeResolver.Object, _authGateway.Object, _notificationOrchestrator.Object);

    private static Mock<NotificationOrchestrator> CreateNotificationOrchestratorMock()
    {
        var repositoryMock = new Mock<INotificationRepository>();
        var recipientResolverMock = new Mock<INotificationRecipientResolver>();
        var notificationOrchestratorMock = new Mock<NotificationOrchestrator>(
            repositoryMock.Object,
            recipientResolverMock.Object);

        notificationOrchestratorMock
            .Setup(o => o.NotifyMissionCreatedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notificationOrchestratorMock
            .Setup(o => o.NotifyMissionUpdatedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notificationOrchestratorMock
            .Setup(o => o.NotifyMissionDeletedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return notificationOrchestratorMock;
    }

    [Fact]
    public async Task CreateAsync_WhenScopeResolutionFails_ReturnsNotFound()
    {
        _scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization, It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.NotFound("Organização não encontrada."));

        var missionCommand = CreateCommand();
        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid()
        };

        var result = await missionCommand.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _repo.Verify(r => r.AddAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        _scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization, It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(orgId));
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var missionCommand = CreateCommand();
        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = orgId
        };

        var result = await missionCommand.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.AddAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSuccess_TriggersNotification()
    {
        var orgId = Guid.NewGuid();
        _scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization, It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(orgId));
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var missionCommand = CreateCommand();
        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = orgId
        };

        var result = await missionCommand.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationOrchestrator.Verify(
            o => o.NotifyMissionCreatedAsync(It.IsAny<Guid>(), orgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var missionCommand = CreateCommand();
        var request = new UpdateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned
        };

        var result = await missionCommand.UpdateAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
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

        _repo.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var missionCommand = CreateCommand();
        var request = new UpdateMissionRequest
        {
            Name = "Missão Atualizada",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Active
        };

        var result = await missionCommand.UpdateAsync(User, missionId, request);

        result.IsSuccess.Should().BeTrue();
        _notificationOrchestrator.Verify(
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

        _repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, mission.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var missionCommand = CreateCommand();

        var result = await missionCommand.DeleteAsync(User, mission.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.RemoveAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()), Times.Never);
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

        _repo.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var missionCommand = CreateCommand();

        var result = await missionCommand.DeleteAsync(User, missionId);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.RemoveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
        _notificationOrchestrator.Verify(
            o => o.NotifyMissionDeletedAsync(missionId, orgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
