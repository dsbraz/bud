using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.UseCases.Objectives;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Objectives;

public sealed class MissionObjectiveWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<IMissionRepository> _missionRepository = new();
    private readonly Mock<IObjectiveRepository> _objectiveRepository = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authorizationGateway = new();

    [Fact]
    public async Task DefineMissionObjective_WhenMissionNotFound_ReturnsNotFound()
    {
        _missionRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = new CreateObjective(_missionRepository.Object, _objectiveRepository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, new CreateObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo"
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _objectiveRepository.Verify(repository => repository.AddAsync(It.IsAny<Objective>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DefineMissionObjective_WhenAuthorized_CreatesObjective()
    {
        var organizationId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            OrganizationId = organizationId,
            Status = MissionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _missionRepository
            .Setup(repository => repository.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateObjective(_missionRepository.Object, _objectiveRepository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, new CreateObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo",
            Dimension = "Clientes"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrganizationId.Should().Be(organizationId);
        result.Value.MissionId.Should().Be(mission.Id);
        result.Value.Dimension.Should().Be("Clientes");
        _objectiveRepository.Verify(repository => repository.AddAsync(It.IsAny<Objective>(), It.IsAny<CancellationToken>()), Times.Once);
        _objectiveRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviseMissionObjective_WhenObjectiveNotFound_ReturnsNotFound()
    {
        _objectiveRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Objective?)null);

        var useCase = new PatchObjective(_objectiveRepository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), new PatchObjectiveRequest { Name = "X" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ReviseMissionObjective_WhenAuthorized_UpdatesObjective()
    {
        var organizationId = Guid.NewGuid();
        var objective = Objective.Create(Guid.NewGuid(), organizationId, Guid.NewGuid(), "Obj", null);

        _objectiveRepository
            .Setup(repository => repository.GetByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);
        _objectiveRepository
            .Setup(repository => repository.GetByIdTrackedAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new PatchObjective(_objectiveRepository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, objective.Id, new PatchObjectiveRequest
        {
            Name = "Atualizado",
            Description = "Nova descrição"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Atualizado");
        _objectiveRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMissionObjective_WhenNotFound_ReturnsNotFound()
    {
        _objectiveRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Objective?)null);

        var useCase = new DeleteObjective(_objectiveRepository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _objectiveRepository.Verify(repository => repository.RemoveAsync(It.IsAny<Objective>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
