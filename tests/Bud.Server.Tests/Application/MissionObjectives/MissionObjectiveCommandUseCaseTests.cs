using System.Security.Claims;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Application.MissionObjectives;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionObjectives;

public sealed class MissionObjectiveCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenMissionNotFound_ReturnsNotFound()
    {
        var objectiveService = new Mock<IMissionObjectiveService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var useCase = new MissionObjectiveCommandUseCase(objectiveService.Object, authorizationGateway.Object, entityLookup.Object);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = Guid.NewGuid(),
            Name = "Objetivo"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Missão não encontrada.");
        objectiveService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
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

        var objectiveService = new Mock<IMissionObjectiveService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var useCase = new MissionObjectiveCommandUseCase(objectiveService.Object, authorizationGateway.Object, entityLookup.Object);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        objectiveService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorized_DelegatesToService()
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

        var expectedObjective = MissionObjective.Create(Guid.NewGuid(), orgId, mission.Id, "Objetivo", null);

        var objectiveService = new Mock<IMissionObjectiveService>();
        objectiveService
            .Setup(s => s.CreateAsync(It.IsAny<CreateMissionObjectiveRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionObjective>.Success(expectedObjective));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionAsync(mission.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var useCase = new MissionObjectiveCommandUseCase(objectiveService.Object, authorizationGateway.Object, entityLookup.Object);

        var request = new CreateMissionObjectiveRequest
        {
            MissionId = mission.Id,
            Name = "Objetivo"
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        objectiveService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenObjectiveNotFound_ReturnsNotFound()
    {
        var objectiveService = new Mock<IMissionObjectiveService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionObjectiveAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionObjective?)null);

        var useCase = new MissionObjectiveCommandUseCase(objectiveService.Object, authorizationGateway.Object, entityLookup.Object);

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), new UpdateMissionObjectiveRequest { Name = "X" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        objectiveService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var objective = MissionObjective.Create(Guid.NewGuid(), orgId, Guid.NewGuid(), "Obj", null);

        var objectiveService = new Mock<IMissionObjectiveService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionObjectiveAsync(objective.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var useCase = new MissionObjectiveCommandUseCase(objectiveService.Object, authorizationGateway.Object, entityLookup.Object);

        var result = await useCase.UpdateAsync(User, objective.Id, new UpdateMissionObjectiveRequest { Name = "X" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        objectiveService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_DelegatesToService()
    {
        var orgId = Guid.NewGuid();
        var objective = MissionObjective.Create(Guid.NewGuid(), orgId, Guid.NewGuid(), "Obj", null);

        var objectiveService = new Mock<IMissionObjectiveService>();
        objectiveService
            .Setup(s => s.DeleteAsync(objective.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionObjectiveAsync(objective.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objective);

        var useCase = new MissionObjectiveCommandUseCase(objectiveService.Object, authorizationGateway.Object, entityLookup.Object);

        var result = await useCase.DeleteAsync(User, objective.Id);

        result.IsSuccess.Should().BeTrue();
        objectiveService.Verify(s => s.DeleteAsync(objective.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenObjectiveNotFound_ReturnsNotFound()
    {
        var objectiveService = new Mock<IMissionObjectiveService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetMissionObjectiveAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionObjective?)null);

        var useCase = new MissionObjectiveCommandUseCase(objectiveService.Object, authorizationGateway.Object, entityLookup.Object);

        var result = await useCase.DeleteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        objectiveService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
    }
}
