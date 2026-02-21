using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Teams;
using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Teams;

public sealed class TeamQueryTests
{
    private readonly Mock<ITeamRepository> _teamRepo = new();

    [Fact]
    public async Task GetByIdAsync_WithExistingTeam_ReturnsSuccess()
    {
        var teamId = Guid.NewGuid();
        _teamRepo.Setup(r => r.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Team { Id = teamId, Name = "Produto", WorkspaceId = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), LeaderId = Guid.NewGuid() });

        var teamQuery = new TeamQuery(_teamRepo.Object);

        var result = await teamQuery.GetByIdAsync(teamId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(teamId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        _teamRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var teamQuery = new TeamQuery(_teamRepo.Object);

        var result = await teamQuery.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetCollaboratorSummariesAsync_WithExistingTeam_ReturnsSuccess()
    {
        var teamId = Guid.NewGuid();
        _teamRepo.Setup(r => r.ExistsAsync(teamId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _teamRepo.Setup(r => r.GetCollaboratorSummariesAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var teamQuery = new TeamQuery(_teamRepo.Object);

        var result = await teamQuery.GetCollaboratorSummariesAsync(teamId);

        result.IsSuccess.Should().BeTrue();
        _teamRepo.Verify(r => r.GetCollaboratorSummariesAsync(teamId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailableCollaboratorsAsync_WithExistingTeam_ReturnsSuccess()
    {
        var teamId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _teamRepo.Setup(r => r.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Team { Id = teamId, Name = "T", OrganizationId = orgId, WorkspaceId = Guid.NewGuid(), LeaderId = Guid.NewGuid() });
        _teamRepo.Setup(r => r.GetAvailableCollaboratorsAsync(teamId, orgId, "ana", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var teamQuery = new TeamQuery(_teamRepo.Object);

        var result = await teamQuery.GetAvailableCollaboratorsAsync(teamId, "ana");

        result.IsSuccess.Should().BeTrue();
        _teamRepo.Verify(r => r.GetAvailableCollaboratorsAsync(teamId, orgId, "ana", 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSubTeamsAsync_WithNonExistingTeam_ReturnsNotFound()
    {
        _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var teamQuery = new TeamQuery(_teamRepo.Object);

        var result = await teamQuery.GetSubTeamsAsync(Guid.NewGuid(), 1, 10);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
