using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.UseCases.Goals;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Goals;

public sealed class GoalObjectiveWriteUseCasesTests
{
    private static readonly ClaimsPrincipal _user = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<IGoalRepository> _repository = new();
    private readonly Mock<ICollaboratorRepository> _collaboratorRepository = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authorizationGateway = new();

    [Fact]
    public async Task DefineMissionObjective_WhenTenantNotSelected_ReturnsForbidden()
    {
        _tenantProvider.SetupGet(t => t.TenantId).Returns((Guid?)null);

        var useCase = new CreateGoal(_repository.Object, _collaboratorRepository.Object, _tenantProvider.Object, _authorizationGateway.Object, NullLogger<CreateGoal>.Instance);

        var result = await useCase.ExecuteAsync(_user, new CreateGoalRequest
        {
            ParentId = Guid.NewGuid(),
            Name = "Objetivo",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = GoalStatus.Planned
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repository.Verify(r => r.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DefineMissionObjective_WhenAuthorized_CreatesObjective()
    {
        var organizationId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        _tenantProvider.SetupGet(t => t.TenantId).Returns(organizationId);

        _authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(_user, organizationId, It.IsAny<CancellationToken>()))
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

        var useCase = new CreateGoal(_repository.Object, _collaboratorRepository.Object, _tenantProvider.Object, _authorizationGateway.Object, NullLogger<CreateGoal>.Instance);

        var result = await useCase.ExecuteAsync(_user, new CreateGoalRequest
        {
            ParentId = parentId,
            Name = "Objetivo",
            Dimension = "Clientes",
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

        var useCase = new PatchGoal(_repository.Object, _collaboratorRepository.Object, _tenantProvider.Object, _authorizationGateway.Object, NullLogger<PatchGoal>.Instance);

        var result = await useCase.ExecuteAsync(_user, Guid.NewGuid(), new PatchGoalRequest { Name = "X" });

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
            .Setup(g => g.CanAccessTenantOrganizationAsync(_user, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new PatchGoal(_repository.Object, _collaboratorRepository.Object, _tenantProvider.Object, _authorizationGateway.Object, NullLogger<PatchGoal>.Instance);

        var result = await useCase.ExecuteAsync(_user, objective.Id, new PatchGoalRequest
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

        var useCase = new DeleteGoal(_repository.Object, _tenantProvider.Object, _authorizationGateway.Object, NullLogger<DeleteGoal>.Instance);

        var result = await useCase.ExecuteAsync(_user, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.RemoveAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
