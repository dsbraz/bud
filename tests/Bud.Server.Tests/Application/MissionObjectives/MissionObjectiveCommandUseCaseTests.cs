using Bud.Server.Infrastructure.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.MissionObjectives;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionObjectives;

public sealed class MissionObjectiveCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<IMissionObjectiveRepository> _repo = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();

    private MissionObjectiveCommandUseCase CreateUseCase()
        => new(_repo.Object, _authGateway.Object);

    [Fact]
    public async Task CreateAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        _repo
            .Setup(r => r.GetMissionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = CreateUseCase();
        var request = new CreateMissionObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Missão não encontrada.");
        _repo.Verify(r => r.AddAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            OrganizationId = orgId,
            Status = MissionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _repo
            .Setup(r => r.GetMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.AddAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorized_CreatesObjective()
    {
        var orgId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            OrganizationId = orgId,
            Status = MissionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _repo
            .Setup(r => r.GetMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateUseCase();
        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Objetivo");
        result.Value.OrganizationId.Should().Be(orgId);
        result.Value.MissionId.Should().Be(mission.Id);
        _repo.Verify(r => r.AddAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDimensionFromAnotherOrganization_ReturnsValidation()
    {
        var orgId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            OrganizationId = orgId,
            Status = MissionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _repo
            .Setup(r => r.GetMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repo
            .Setup(r => r.DimensionBelongsToOrganizationAsync(dimensionId, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo",
            ObjectiveDimensionId = dimensionId
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Contain("Dimensão do objetivo");
        _repo.Verify(r => r.AddAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithValidDimension_CreatesObjective()
    {
        var orgId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Missão",
            OrganizationId = orgId,
            Status = MissionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _repo
            .Setup(r => r.GetMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repo
            .Setup(r => r.DimensionBelongsToOrganizationAsync(dimensionId, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateUseCase();
        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo",
            ObjectiveDimensionId = dimensionId
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ObjectiveDimensionId.Should().Be(dimensionId);
        _repo.Verify(r => r.AddAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenObjectiveNotFound_ReturnsNotFound()
    {
        _repo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionObjective?)null);

        var useCase = CreateUseCase();

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), new UpdateMissionObjectiveRequest { Name = "X" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var objective = MissionObjective.Create(Guid.NewGuid(), orgId, Guid.NewGuid(), "Obj", null);

        _repo
            .Setup(r => r.GetByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();

        var result = await useCase.UpdateAsync(User, objective.Id, new UpdateMissionObjectiveRequest { Name = "X" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAuthorized_UpdatesObjective()
    {
        var orgId = Guid.NewGuid();
        var objective = MissionObjective.Create(Guid.NewGuid(), orgId, Guid.NewGuid(), "Obj", null);

        _repo
            .Setup(r => r.GetByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repo
            .Setup(r => r.GetByIdTrackedAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var useCase = CreateUseCase();

        var result = await useCase.UpdateAsync(User, objective.Id, new UpdateMissionObjectiveRequest
        {
            Name = "Atualizado",
            Description = "Nova descrição"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Atualizado");
        result.Value.Description.Should().Be("Nova descrição");
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithDimensionFromAnotherOrganization_ReturnsValidation()
    {
        var orgId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var objective = MissionObjective.Create(Guid.NewGuid(), orgId, Guid.NewGuid(), "Obj", null);

        _repo
            .Setup(r => r.GetByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repo
            .Setup(r => r.DimensionBelongsToOrganizationAsync(dimensionId, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();

        var result = await useCase.UpdateAsync(User, objective.Id, new UpdateMissionObjectiveRequest
        {
            Name = "Atualizado",
            ObjectiveDimensionId = dimensionId
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Contain("Dimensão do objetivo");
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenObjectiveNotFound_ReturnsNotFound()
    {
        _repo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionObjective?)null);

        var useCase = CreateUseCase();

        var result = await useCase.DeleteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _repo.Verify(r => r.RemoveAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var objective = MissionObjective.Create(Guid.NewGuid(), orgId, Guid.NewGuid(), "Obj", null);

        _repo
            .Setup(r => r.GetByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();

        var result = await useCase.DeleteAsync(User, objective.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.RemoveAsync(It.IsAny<MissionObjective>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_DeletesObjective()
    {
        var orgId = Guid.NewGuid();
        var objective = MissionObjective.Create(Guid.NewGuid(), orgId, Guid.NewGuid(), "Obj", null);

        _repo
            .Setup(r => r.GetByIdAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repo
            .Setup(r => r.GetByIdTrackedAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var useCase = CreateUseCase();

        var result = await useCase.DeleteAsync(User, objective.Id);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.RemoveAsync(objective, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
