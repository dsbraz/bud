using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.UseCases.Goals;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Goals;

public sealed class GoalObjectiveWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<IGoalRepository> _repository = new();
    private readonly Mock<IGoalScopeResolver> _scopeResolver = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authorizationGateway = new();

    [Fact]
    public async Task DefineMissionObjective_WhenScopeResolutionFails_ReturnsNotFound()
    {
        _scopeResolver
            .Setup(s => s.ResolveScopeOrganizationIdAsync(
                It.IsAny<GoalScopeType>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.NotFound("Organização não encontrada."));

        var useCase = new CreateGoal(_repository.Object, _scopeResolver.Object, _authorizationGateway.Object, NullLogger<CreateGoal>.Instance);

        var result = await useCase.ExecuteAsync(User, new CreateGoalRequest
        {
            ParentId = Guid.NewGuid(),
            Name = "Objetivo",
            ScopeType = GoalScopeType.Organization,
            ScopeId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = GoalStatus.Planned
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DefineMissionObjective_WhenAuthorized_CreatesObjective()
    {
        var organizationId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        _scopeResolver
            .Setup(s => s.ResolveScopeOrganizationIdAsync(
                GoalScopeType.Organization, It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(organizationId));

        _authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var parentGoal = new Goal
        {
            Id = parentId,
            Name = "Missão pai",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(60),
            Status = GoalStatus.Planned,
            OrganizationId = organizationId
        };

        _repository
            .Setup(r => r.GetByIdReadOnlyAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentGoal);

        var useCase = new CreateGoal(_repository.Object, _scopeResolver.Object, _authorizationGateway.Object, NullLogger<CreateGoal>.Instance);

        var result = await useCase.ExecuteAsync(User, new CreateGoalRequest
        {
            ParentId = parentId,
            Name = "Objetivo",
            Dimension = "Clientes",
            ScopeType = GoalScopeType.Organization,
            ScopeId = organizationId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = GoalStatus.Planned
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrganizationId.Should().Be(organizationId);
        result.Value.ParentId.Should().Be(parentId);
        result.Value.Dimension.Should().Be("Clientes");
        _repository.Verify(r => r.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviseMissionObjective_WhenObjectiveNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var useCase = new PatchGoal(_repository.Object, _scopeResolver.Object, _authorizationGateway.Object, NullLogger<PatchGoal>.Instance);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), new PatchGoalRequest { Name = "X" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ReviseMissionObjective_WhenAuthorized_UpdatesObjective()
    {
        var organizationId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var objective = new Goal
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ParentId = parentId,
            Name = "Obj",
            Status = GoalStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _repository
            .Setup(r => r.GetByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        _authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new PatchGoal(_repository.Object, _scopeResolver.Object, _authorizationGateway.Object, NullLogger<PatchGoal>.Instance);

        var result = await useCase.ExecuteAsync(User, objective.Id, new PatchGoalRequest
        {
            Name = "Atualizado",
            Description = "Nova descrição"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Atualizado");
    }

    [Fact]
    public async Task RemoveMissionObjective_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var useCase = new DeleteGoal(_repository.Object, _authorizationGateway.Object, NullLogger<DeleteGoal>.Instance);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.RemoveAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
