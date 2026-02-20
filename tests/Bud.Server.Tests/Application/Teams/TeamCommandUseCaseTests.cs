using Bud.Server.Infrastructure.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Teams;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Teams;

public sealed class TeamCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<ITeamRepository> _teamRepo = new();
    private readonly Mock<IWorkspaceRepository> _wsRepo = new();
    private readonly Mock<ICollaboratorRepository> _collabRepo = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();

    private TeamCommandUseCase CreateUseCase()
        => new(_teamRepo.Object, _wsRepo.Object, _collabRepo.Object, _authGateway.Object);

    [Fact]
    public async Task CreateAsync_WhenWorkspaceNotFound_ReturnsNotFound()
    {
        _wsRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        var useCase = CreateUseCase();
        var request = new CreateTeamRequest { Name = "Team", WorkspaceId = Guid.NewGuid(), LeaderId = Guid.NewGuid() };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = Guid.NewGuid() };
        _wsRepo.Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _authGateway.Setup(g => g.IsOrganizationOwnerAsync(User, workspace.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var request = new CreateTeamRequest { Name = "Team", WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateAsync_WhenTeamNotFound_ReturnsNotFound()
    {
        _teamRepo.Setup(r => r.GetByIdWithCollaboratorTeamsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var useCase = CreateUseCase();
        var request = new UpdateTeamRequest { Name = "Novo Team", LeaderId = Guid.NewGuid() };

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid() };
        _teamRepo.Setup(r => r.GetByIdWithCollaboratorTeamsAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _authGateway.Setup(g => g.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var request = new UpdateTeamRequest { Name = "Novo Team", LeaderId = Guid.NewGuid() };

        var result = await useCase.UpdateAsync(User, team.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_Succeeds()
    {
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid() };
        _teamRepo.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        _teamRepo.Setup(r => r.HasSubTeamsAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _teamRepo.Setup(r => r.HasMissionsAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _authGateway.Setup(g => g.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateUseCase();

        var result = await useCase.DeleteAsync(User, team.Id);

        result.IsSuccess.Should().BeTrue();
        _teamRepo.Verify(r => r.RemoveAsync(team, It.IsAny<CancellationToken>()), Times.Once);
        _teamRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithSubTeams_ReturnsConflict()
    {
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid() };
        _teamRepo.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        _teamRepo.Setup(r => r.HasSubTeamsAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _authGateway.Setup(g => g.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateUseCase();

        var result = await useCase.DeleteAsync(User, team.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateCollaboratorsAsync_WhenAuthorized_Succeeds()
    {
        var leaderId = Guid.NewGuid();
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), LeaderId = leaderId };
        _teamRepo.Setup(r => r.GetByIdWithCollaboratorTeamsAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _authGateway.Setup(g => g.IsOrganizationOwnerAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _collabRepo.Setup(r => r.CountByIdsAndOrganizationAsync(It.IsAny<List<Guid>>(), team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var useCase = CreateUseCase();
        var request = new UpdateTeamCollaboratorsRequest { CollaboratorIds = [leaderId] };

        var result = await useCase.UpdateCollaboratorsAsync(User, team.Id, request);

        result.IsSuccess.Should().BeTrue();
        _teamRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCollaboratorsAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid() };
        _teamRepo.Setup(r => r.GetByIdWithCollaboratorTeamsAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _authGateway.Setup(g => g.IsOrganizationOwnerAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var request = new UpdateTeamCollaboratorsRequest { CollaboratorIds = [] };

        var result = await useCase.UpdateCollaboratorsAsync(User, team.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }
}
