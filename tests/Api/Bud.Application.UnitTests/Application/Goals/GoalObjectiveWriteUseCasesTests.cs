using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Application.UnitTests.Application.Goals;

public sealed class GoalObjectiveWriteUseCasesTests
{
    private readonly Mock<IGoalRepository> _repository = new();
    private readonly Mock<ICollaboratorRepository> _collaboratorRepository = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    [Fact]
    public async Task DefineMissionObjective_WhenTenantNotSelected_ReturnsForbidden()
    {
        _tenantProvider.SetupGet(t => t.TenantId).Returns((Guid?)null);

        var useCase = new CreateGoal(_repository.Object, _collaboratorRepository.Object, _tenantProvider.Object, NullLogger<CreateGoal>.Instance);

        var result = await useCase.ExecuteAsync(new CreateGoalCommand(
            "Objetivo",
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            GoalStatus.Planned,
            Guid.NewGuid(),
            null));

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

        var useCase = new CreateGoal(_repository.Object, _collaboratorRepository.Object, _tenantProvider.Object, NullLogger<CreateGoal>.Instance);

        var result = await useCase.ExecuteAsync(new CreateGoalCommand(
            "Objetivo",
            null,
            "Clientes",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            GoalStatus.Planned,
            parentId,
            null));

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

        var useCase = new PatchGoal(_repository.Object, _collaboratorRepository.Object, _tenantProvider.Object, NullLogger<PatchGoal>.Instance);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), new PatchGoalCommand("X", default, default, default, default, default, default));

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

        var useCase = new PatchGoal(_repository.Object, _collaboratorRepository.Object, _tenantProvider.Object, NullLogger<PatchGoal>.Instance);

        var result = await useCase.ExecuteAsync(objective.Id, new PatchGoalCommand(
            "Atualizado",
            "Nova descrição",
            default,
            default,
            default,
            default,
            default));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Atualizado");
    }

    [Fact]
    public async Task RemoveMissionObjective_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var useCase = new DeleteGoal(_repository.Object, _tenantProvider.Object, NullLogger<DeleteGoal>.Instance);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.RemoveAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
