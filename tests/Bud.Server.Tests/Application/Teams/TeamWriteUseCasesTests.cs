using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Teams;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Teams;

public sealed class TeamWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<ITeamRepository> _teamRepository = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepository = new();
    private readonly Mock<ICollaboratorRepository> _collaboratorRepository = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authorizationGateway = new();

    [Fact]
    public async Task CreateTeam_WhenWorkspaceNotFound_ReturnsNotFound()
    {
        _workspaceRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        var useCase = new CreateTeam(
            _teamRepository.Object,
            _workspaceRepository.Object,
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var request = new CreateTeamRequest
        {
            Name = "Team",
            WorkspaceId = Guid.NewGuid(),
            LeaderId = Guid.NewGuid()
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
    }

    [Fact]
    public async Task CreateTeam_WhenUnauthorized_ReturnsForbidden()
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = Guid.NewGuid() };

        _workspaceRepository
            .Setup(repository => repository.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _authorizationGateway
            .Setup(gateway => gateway.IsOrganizationOwnerAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateTeam(
            _teamRepository.Object,
            _workspaceRepository.Object,
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var request = new CreateTeamRequest
        {
            Name = "Team",
            WorkspaceId = workspace.Id,
            LeaderId = Guid.NewGuid()
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateTeam_WhenTeamNotFound_ReturnsNotFound()
    {
        _teamRepository
            .Setup(repository => repository.GetByIdWithCollaboratorTeamsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var useCase = new UpdateTeam(
            _teamRepository.Object,
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var request = new UpdateTeamRequest { Name = "Novo Team", LeaderId = Guid.NewGuid() };

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateTeam_WhenUnauthorized_ReturnsForbidden()
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            OrganizationId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid()
        };

        _teamRepository
            .Setup(repository => repository.GetByIdWithCollaboratorTeamsAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _authorizationGateway
            .Setup(gateway => gateway.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new UpdateTeam(
            _teamRepository.Object,
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var request = new UpdateTeamRequest { Name = "Novo Team", LeaderId = Guid.NewGuid() };

        var result = await useCase.ExecuteAsync(User, team.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task RemoveTeam_WhenAuthorized_Succeeds()
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            OrganizationId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid()
        };

        _teamRepository.Setup(repository => repository.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        _teamRepository.Setup(repository => repository.HasSubTeamsAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _teamRepository.Setup(repository => repository.HasMissionsAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _authorizationGateway
            .Setup(gateway => gateway.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DeleteTeam(_teamRepository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, team.Id);

        result.IsSuccess.Should().BeTrue();
        _teamRepository.Verify(repository => repository.RemoveAsync(team, It.IsAny<CancellationToken>()), Times.Once);
        _teamRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTeam_WithSubTeams_ReturnsConflict()
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            OrganizationId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid()
        };

        _teamRepository.Setup(repository => repository.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        _teamRepository.Setup(repository => repository.HasSubTeamsAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _authorizationGateway
            .Setup(gateway => gateway.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DeleteTeam(_teamRepository.Object, _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, team.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateTeamCollaborators_WhenAuthorized_Succeeds()
    {
        var leaderId = Guid.NewGuid();
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            OrganizationId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            LeaderId = leaderId
        };

        _teamRepository
            .Setup(repository => repository.GetByIdWithCollaboratorTeamsAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _authorizationGateway
            .Setup(gateway => gateway.IsOrganizationOwnerAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _collaboratorRepository
            .Setup(repository => repository.CountByIdsAndOrganizationAsync(It.IsAny<List<Guid>>(), team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var useCase = new UpdateTeamCollaborators(
            _teamRepository.Object,
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var request = new UpdateTeamCollaboratorsRequest { CollaboratorIds = [leaderId] };

        var result = await useCase.ExecuteAsync(User, team.Id, request);

        result.IsSuccess.Should().BeTrue();
        _teamRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTeamCollaborators_WhenUnauthorized_ReturnsForbidden()
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            OrganizationId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid()
        };

        _teamRepository
            .Setup(repository => repository.GetByIdWithCollaboratorTeamsAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _authorizationGateway
            .Setup(gateway => gateway.IsOrganizationOwnerAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new UpdateTeamCollaborators(
            _teamRepository.Object,
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var request = new UpdateTeamCollaboratorsRequest { CollaboratorIds = [] };

        var result = await useCase.ExecuteAsync(User, team.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }
}
