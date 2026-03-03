using Bud.Server.Domain.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.UseCases.Goals;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Events;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Goals;

public sealed class GoalWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<IGoalRepository> _repo = new();
    private readonly Mock<ICollaboratorRepository> _collaboratorRepo = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();

    private CreateGoal CreatePlanningUseCase()
        => new(_repo.Object, _collaboratorRepo.Object, _tenantProvider.Object, _authGateway.Object, NullLogger<CreateGoal>.Instance);

    private PatchGoal CreateReplanningUseCase()
        => new(_repo.Object, _collaboratorRepo.Object, _authGateway.Object, NullLogger<PatchGoal>.Instance);

    private DeleteGoal CreateRemoveUseCase()
        => new(_repo.Object, _authGateway.Object, NullLogger<DeleteGoal>.Instance);

    [Fact]
    public async Task CreateAsync_WhenTenantNotSelected_ReturnsForbidden()
    {
        _tenantProvider.SetupGet(t => t.TenantId).Returns((Guid?)null);

        var useCase = CreatePlanningUseCase();
        var request = new CreateGoalRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.GoalStatus.Planned
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        _tenantProvider.SetupGet(t => t.TenantId).Returns(orgId);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreatePlanningUseCase();
        var request = new CreateGoalRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.GoalStatus.Planned
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSuccess_RegistersMissionCreatedDomainEvent()
    {
        var orgId = Guid.NewGuid();
        _tenantProvider.SetupGet(t => t.TenantId).Returns(orgId);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreatePlanningUseCase();
        var request = new CreateGoalRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.GoalStatus.Planned
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdEvent = result.Value!.DomainEvents.Should().ContainSingle().Subject;
        var created = createdEvent.Should().BeOfType<GoalCreatedDomainEvent>().Subject;
        created.GoalId.Should().Be(result.Value.Id);
        created.OrganizationId.Should().Be(orgId);
    }

    [Fact]
    public async Task CreateAsync_WhenChildStartDateBeforeParent_ReturnsValidation()
    {
        var orgId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var parentGoal = new Goal
        {
            Id = parentId,
            Name = "Missão pai",
            StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = GoalStatus.Planned,
            OrganizationId = orgId
        };

        _tenantProvider.SetupGet(t => t.TenantId).Returns(orgId);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.GetByIdReadOnlyAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentGoal);

        var useCase = CreatePlanningUseCase();
        var request = new CreateGoalRequest
        {
            Name = "Meta filha",
            StartDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), // Before parent's start
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = GoalStatus.Planned,
            ParentId = parentId
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Contain("data de início");
        _repo.Verify(r => r.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenChildStartDateEqualsParent_Succeeds()
    {
        var orgId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var parentStartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var parentGoal = new Goal
        {
            Id = parentId,
            Name = "Missão pai",
            StartDate = parentStartDate,
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = GoalStatus.Planned,
            OrganizationId = orgId
        };

        _tenantProvider.SetupGet(t => t.TenantId).Returns(orgId);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.GetByIdReadOnlyAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentGoal);

        var useCase = CreatePlanningUseCase();
        var request = new CreateGoalRequest
        {
            Name = "Meta filha",
            StartDate = parentStartDate, // Same as parent — should be allowed
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = GoalStatus.Planned,
            ParentId = parentId
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_WhenChildStartDateBeforeParent_ReturnsValidation()
    {
        var orgId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var parentGoal = new Goal
        {
            Id = parentId,
            Name = "Missão pai",
            StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = GoalStatus.Planned,
            OrganizationId = orgId
        };
        var childGoal = new Goal
        {
            Id = goalId,
            ParentId = parentId,
            Name = "Meta filha",
            StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = GoalStatus.Planned,
            OrganizationId = orgId
        };

        _repo.Setup(r => r.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(childGoal);
        _repo.Setup(r => r.GetByIdReadOnlyAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentGoal);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateReplanningUseCase();
        var request = new PatchGoalRequest
        {
            StartDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc) // Before parent's start
        };

        var result = await useCase.ExecuteAsync(User, goalId, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Contain("data de início");
    }

    [Fact]
    public async Task UpdateAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var useCase = CreateReplanningUseCase();
        var request = new PatchGoalRequest
        {
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.GoalStatus.Planned
        };

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenSuccess_RegistersMissionUpdatedDomainEvent()
    {
        var orgId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var mission = new Goal
        {
            Id = goalId,
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = GoalStatus.Planned,
            OrganizationId = orgId
        };

        _repo.Setup(r => r.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateReplanningUseCase();
        var request = new PatchGoalRequest
        {
            Name = "Missão Atualizada",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = Bud.Shared.Contracts.GoalStatus.Active
        };

        var result = await useCase.ExecuteAsync(User, goalId, request);

        result.IsSuccess.Should().BeTrue();
        var updatedEvent = mission.DomainEvents.Should().ContainSingle().Subject;
        var updated = updatedEvent.Should().BeOfType<GoalUpdatedDomainEvent>().Subject;
        updated.GoalId.Should().Be(goalId);
        updated.OrganizationId.Should().Be(orgId);
    }

    [Fact]
    public async Task DeleteAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = GoalStatus.Planned,
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
        _repo.Verify(r => r.RemoveAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenSuccess_RegistersMissionDeletedDomainEvent()
    {
        var orgId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var mission = new Goal
        {
            Id = goalId,
            Name = "Missão",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = GoalStatus.Planned,
            OrganizationId = orgId
        };

        _repo.Setup(r => r.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateRemoveUseCase();

        var result = await useCase.ExecuteAsync(User, goalId);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.RemoveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
        var deletedEvent = mission.DomainEvents.Should().ContainSingle().Subject;
        var deleted = deletedEvent.Should().BeOfType<GoalDeletedDomainEvent>().Subject;
        deleted.GoalId.Should().Be(goalId);
        deleted.OrganizationId.Should().Be(orgId);
    }
}
