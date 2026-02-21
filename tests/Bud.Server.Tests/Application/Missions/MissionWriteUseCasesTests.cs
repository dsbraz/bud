using Bud.Server.Infrastructure.Services;
using Bud.Server.Domain.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Missions;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Events;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Missions;

public sealed class MissionWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<IMissionRepository> _repo = new();
    private readonly Mock<IMissionScopeResolver> _scopeResolver = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();

    private PlanMission CreatePlanningUseCase()
        => new(_repo.Object, _scopeResolver.Object, _authGateway.Object);

    private ReplanMission CreateReplanningUseCase()
        => new(_repo.Object, _scopeResolver.Object, _authGateway.Object);

    private DeleteMission CreateRemoveUseCase()
        => new(_repo.Object, _authGateway.Object);

    [Fact]
    public async Task CreateAsync_WhenScopeResolutionFails_ReturnsNotFound()
    {
        _scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization, It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.NotFound("Organização não encontrada."));

        var useCase = CreatePlanningUseCase();
        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.MissionStatus.Planned,
            ScopeType = Bud.Shared.Contracts.MissionScopeType.Organization,
            ScopeId = Guid.NewGuid()
        };

        var result = await useCase.ExecuteAsync(User, request);

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

        var useCase = CreatePlanningUseCase();
        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.MissionStatus.Planned,
            ScopeType = Bud.Shared.Contracts.MissionScopeType.Organization,
            ScopeId = orgId
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.AddAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSuccess_RegistersMissionCreatedDomainEvent()
    {
        var orgId = Guid.NewGuid();
        _scopeResolver.Setup(s => s.ResolveScopeOrganizationIdAsync(
                MissionScopeType.Organization, It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(orgId));
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreatePlanningUseCase();
        var request = new CreateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.MissionStatus.Planned,
            ScopeType = Bud.Shared.Contracts.MissionScopeType.Organization,
            ScopeId = orgId
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdEvent = result.Value!.DomainEvents.Should().ContainSingle().Subject;
        var created = createdEvent.Should().BeOfType<MissionCreatedDomainEvent>().Subject;
        created.MissionId.Should().Be(result.Value.Id);
        created.OrganizationId.Should().Be(orgId);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = CreateReplanningUseCase();
        var request = new UpdateMissionRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.MissionStatus.Planned
        };

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenSuccess_RegistersMissionUpdatedDomainEvent()
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

        var useCase = CreateReplanningUseCase();
        var request = new UpdateMissionRequest
        {
            Name = "Missão Atualizada",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.MissionStatus.Active
        };

        var result = await useCase.ExecuteAsync(User, missionId, request);

        result.IsSuccess.Should().BeTrue();
        var updatedEvent = mission.DomainEvents.Should().ContainSingle().Subject;
        var updated = updatedEvent.Should().BeOfType<MissionUpdatedDomainEvent>().Subject;
        updated.MissionId.Should().Be(missionId);
        updated.OrganizationId.Should().Be(orgId);
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

        var useCase = CreateRemoveUseCase();

        var result = await useCase.ExecuteAsync(User, mission.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.RemoveAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenSuccess_RegistersMissionDeletedDomainEvent()
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

        var useCase = CreateRemoveUseCase();

        var result = await useCase.ExecuteAsync(User, missionId);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.RemoveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
        var deletedEvent = mission.DomainEvents.Should().ContainSingle().Subject;
        var deleted = deletedEvent.Should().BeOfType<MissionDeletedDomainEvent>().Subject;
        deleted.MissionId.Should().Be(missionId);
        deleted.OrganizationId.Should().Be(orgId);
    }
}
