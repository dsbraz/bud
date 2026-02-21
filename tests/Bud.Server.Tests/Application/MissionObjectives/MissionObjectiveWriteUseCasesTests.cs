using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.MissionObjectives;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionObjectives;

public sealed class MissionObjectiveWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<IMissionObjectiveRepository> _repository = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authorizationGateway = new();

    [Fact]
    public async Task DefineMissionObjective_WhenMissionNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetMissionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = new DefineMissionObjective(_repository.Object, _authorizationGateway.Object);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo"
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Missão não encontrada.");
        _repository.Verify(repository => repository.AddAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DefineMissionObjective_WhenUnauthorized_ReturnsForbidden()
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

        _repository
            .Setup(repository => repository.GetMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new DefineMissionObjective(_repository.Object, _authorizationGateway.Object);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo"
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repository.Verify(repository => repository.AddAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Never);
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

        _repository
            .Setup(repository => repository.GetMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DefineMissionObjective(_repository.Object, _authorizationGateway.Object);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo"
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Objetivo");
        result.Value.OrganizationId.Should().Be(organizationId);
        result.Value.MissionId.Should().Be(mission.Id);
        _repository.Verify(repository => repository.AddAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DefineMissionObjective_WithDimensionFromAnotherOrganization_ReturnsValidation()
    {
        var organizationId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            OrganizationId = organizationId,
            Status = MissionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _repository
            .Setup(repository => repository.GetMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repository
            .Setup(repository => repository.DimensionBelongsToOrganizationAsync(dimensionId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new DefineMissionObjective(_repository.Object, _authorizationGateway.Object);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo",
            ObjectiveDimensionId = dimensionId
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Contain("Dimensão do objetivo");
        _repository.Verify(repository => repository.AddAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviseMissionObjective_WhenObjectiveNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionObjective?)null);

        var useCase = new ReviseMissionObjective(_repository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), new UpdateMissionObjectiveRequest { Name = "X" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviseMissionObjective_WhenUnauthorized_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var objective = MissionObjective.Create(Guid.NewGuid(), organizationId, Guid.NewGuid(), "Obj", null);

        _repository
            .Setup(repository => repository.GetByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new ReviseMissionObjective(_repository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, objective.Id, new UpdateMissionObjectiveRequest { Name = "X" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviseMissionObjective_WhenAuthorized_UpdatesObjective()
    {
        var organizationId = Guid.NewGuid();
        var objective = MissionObjective.Create(Guid.NewGuid(), organizationId, Guid.NewGuid(), "Obj", null);

        _repository
            .Setup(repository => repository.GetByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repository
            .Setup(repository => repository.GetByIdTrackedAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var useCase = new ReviseMissionObjective(_repository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(
            User,
            objective.Id,
            new UpdateMissionObjectiveRequest
            {
                Name = "Atualizado",
                Description = "Nova descrição"
            });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Atualizado");
        result.Value.Description.Should().Be("Nova descrição");
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMissionObjective_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionObjective?)null);

        var useCase = new RemoveMissionObjective(_repository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _repository.Verify(repository => repository.RemoveAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
