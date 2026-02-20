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

public sealed class MissionCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<IMissionRepository> _repo = new();
    private readonly Mock<IMissionScopeResolver> _scopeResolver = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();
    private readonly Mock<INotificationOrchestrator> _notificationOrchestrator = new();

    private MissionCommandUseCase CreateUseCase()
        => new(_repo.Object, _scopeResolver.Object, _authGateway.Object, _notificationOrchestrator.Object);

    [Fact]
    public async Task CreateAsync_WhenScopeResolutionFails_ReturnsNotFound()
    {
        _scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization, It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.NotFound("Organização não encontrada."));

        var useCase = CreateUseCase();
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

        var useCase = CreateUseCase();
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

        var useCase = CreateUseCase();
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

        var useCase = CreateUseCase();
        var request = new UpdateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Planned
        };

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), request);

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

        var useCase = CreateUseCase();
        var request = new UpdateMissionRequest
        {
            Name = "Missão Atualizada",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = MissionStatus.Active
        };

        var result = await useCase.UpdateAsync(User, missionId, request);

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

        var useCase = CreateUseCase();

        var result = await useCase.DeleteAsync(User, mission.Id);

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

        var useCase = CreateUseCase();

        var result = await useCase.DeleteAsync(User, missionId);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.RemoveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
        _notificationOrchestrator.Verify(
            o => o.NotifyMissionDeletedAsync(missionId, orgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
