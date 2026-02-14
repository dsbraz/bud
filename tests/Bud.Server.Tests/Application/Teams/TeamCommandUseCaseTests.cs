using System.Security.Claims;
using Bud.Server.Services;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Server.Application.Teams;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
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
        var teamService = new Mock<ITeamService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        var useCase = new TeamCommandUseCase(teamService.Object, authorizationGateway.Object, entityLookup.Object);
        var request = new CreateTeamRequest { Name = "Team", WorkspaceId = Guid.NewGuid(), LeaderId = Guid.NewGuid() };

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

        var teamService = new Mock<ITeamService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanWriteOrganizationAsync(User, team.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var entityLookup = new Mock<IApplicationEntityLookup>();
        entityLookup
            .Setup(l => l.GetTeamAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var useCase = new TeamCommandUseCase(teamService.Object, authorizationGateway.Object, entityLookup.Object);
        var request = new UpdateTeamRequest { Name = "Novo Team", LeaderId = Guid.NewGuid() };

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
        var teamService = new Mock<ITeamService>();
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

}
