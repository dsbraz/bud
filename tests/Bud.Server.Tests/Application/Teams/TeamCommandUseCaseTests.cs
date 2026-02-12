using System.Security.Claims;
using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Application.Teams;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Teams;

public sealed class TeamCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_WhenWorkspaceNotFound_ReturnsNotFound()
    {
        var teamService = new Mock<ITeamCommandService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        var useCase = new TeamCommandUseCase(teamService.Object, authorizationGateway.Object, entityLookup.Object);
        var request = new CreateTeamRequest { Name = "Team", WorkspaceId = Guid.NewGuid() };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
        teamService.VerifyNoOtherCalls();
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            OrganizationId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid()
        };

        var teamService = new Mock<ITeamCommandService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetTeamAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var useCase = new TeamCommandUseCase(teamService.Object, authorizationGateway.Object, entityLookup.Object);
        var request = new UpdateTeamRequest { Name = "Novo Team" };

        var result = await useCase.UpdateAsync(User, team.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Você não tem permissão para atualizar este time.");
        teamService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateCollaboratorsAsync_WhenAuthorized_DelegatesToService()
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            OrganizationId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid()
        };

        var request = new UpdateTeamCollaboratorsRequest { CollaboratorIds = [] };
        var teamService = new Mock<ITeamCommandService>();
        teamService
            .Setup(s => s.UpdateCollaboratorsAsync(team.Id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetTeamAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var useCase = new TeamCommandUseCase(teamService.Object, authorizationGateway.Object, entityLookup.Object);

        var result = await useCase.UpdateCollaboratorsAsync(User, team.Id, request);

        result.IsSuccess.Should().BeTrue();
        teamService.Verify(s => s.UpdateCollaboratorsAsync(team.Id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAuthorizedAndCreated_DispatchesDomainEvent()
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace",
            OrganizationId = Guid.NewGuid()
        };

        var createdTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            WorkspaceId = workspace.Id,
            OrganizationId = workspace.OrganizationId
        };

        var teamService = new Mock<ITeamCommandService>();
        teamService
            .Setup(s => s.CreateAsync(It.IsAny<CreateTeamRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Team>.Success(createdTeam));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.IsOrganizationOwnerAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetWorkspaceAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new TeamCommandUseCase(
            teamService.Object,
            authorizationGateway.Object,
            entityLookup.Object,
            null,
            dispatcher.Object);

        var request = new CreateTeamRequest { Name = "Team", WorkspaceId = workspace.Id };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Teams.Events.TeamCreatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAuthorizedAndUpdated_DispatchesDomainEvent()
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            WorkspaceId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };
        var request = new UpdateTeamRequest { Name = "Team 2" };

        var teamService = new Mock<ITeamCommandService>();
        teamService
            .Setup(s => s.UpdateAsync(team.Id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Team>.Success(new Team
            {
                Id = team.Id,
                Name = request.Name,
                WorkspaceId = team.WorkspaceId,
                OrganizationId = team.OrganizationId
            }));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup.Setup(l => l.GetTeamAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new TeamCommandUseCase(teamService.Object, authorizationGateway.Object, entityLookup.Object, null, dispatcher.Object);

        var result = await useCase.UpdateAsync(User, team.Id, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Teams.Events.TeamUpdatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorizedAndDeleted_DispatchesDomainEvent()
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            WorkspaceId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };

        var teamService = new Mock<ITeamCommandService>();
        teamService
            .Setup(s => s.DeleteAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup.Setup(l => l.GetTeamAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new TeamCommandUseCase(teamService.Object, authorizationGateway.Object, entityLookup.Object, null, dispatcher.Object);

        var result = await useCase.DeleteAsync(User, team.Id);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Teams.Events.TeamDeletedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
