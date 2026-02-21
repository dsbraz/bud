using Bud.Server.Application.Common;
using Bud.Server.Application.Projections;
using Bud.Server.Application.Teams;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Teams;

public sealed class TeamReadUseCasesTests
{
    private readonly Mock<ITeamRepository> _teamRepository = new();

    [Fact]
    public async Task GetTeamById_WithExistingTeam_ReturnsSuccess()
    {
        var teamId = Guid.NewGuid();

        _teamRepository
            .Setup(repository => repository.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Team
            {
                Id = teamId,
                Name = "Produto",
                WorkspaceId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                LeaderId = Guid.NewGuid()
            });

        var useCase = new ViewTeamDetails(_teamRepository.Object);

        var result = await useCase.ExecuteAsync(teamId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(teamId);
    }

    [Fact]
    public async Task GetTeamById_WithNonExistingId_ReturnsNotFound()
    {
        _teamRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var useCase = new ViewTeamDetails(_teamRepository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ListTeamCollaboratorSummaries_WithExistingTeam_ReturnsSuccess()
    {
        var teamId = Guid.NewGuid();

        _teamRepository
            .Setup(repository => repository.ExistsAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _teamRepository
            .Setup(repository => repository.GetCollaboratorSummariesAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var useCase = new ListTeamCollaboratorSummaries(_teamRepository.Object);

        var result = await useCase.ExecuteAsync(teamId);

        result.IsSuccess.Should().BeTrue();
        _teamRepository.Verify(repository => repository.GetCollaboratorSummariesAsync(teamId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAvailableTeamCollaborators_WithExistingTeam_ReturnsSuccess()
    {
        var teamId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _teamRepository
            .Setup(repository => repository.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Team
            {
                Id = teamId,
                Name = "T",
                OrganizationId = organizationId,
                WorkspaceId = Guid.NewGuid(),
                LeaderId = Guid.NewGuid()
            });

        _teamRepository
            .Setup(repository => repository.GetAvailableCollaboratorsAsync(teamId, organizationId, "ana", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var useCase = new ListAvailableTeamCollaborators(_teamRepository.Object);

        var result = await useCase.ExecuteAsync(teamId, "ana");

        result.IsSuccess.Should().BeTrue();
        _teamRepository.Verify(
            repository => repository.GetAvailableCollaboratorsAsync(teamId, organizationId, "ana", 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListSubTeams_WithNonExistingTeam_ReturnsNotFound()
    {
        _teamRepository
            .Setup(repository => repository.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new ListSubTeams(_teamRepository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), 1, 10);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
