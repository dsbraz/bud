using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Server.Application.Teams;
using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Teams;

public sealed class TeamQueryUseCaseTests
{
    private readonly Mock<ITeamRepository> _teamRepo = new();

    [Fact]
    public async Task GetByIdAsync_WithExistingTeam_ReturnsSuccess()
    {
        var teamId = Guid.NewGuid();
        _teamRepo.Setup(r => r.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Team { Id = teamId, Name = "Produto", WorkspaceId = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), LeaderId = Guid.NewGuid() });

        var useCase = new TeamQueryUseCase(_teamRepo.Object);

        var result = await useCase.GetByIdAsync(teamId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(teamId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        _teamRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var useCase = new TeamQueryUseCase(_teamRepo.Object);

        var result = await useCase.GetByIdAsync(Guid.NewGuid());

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

        var useCase = new TeamQueryUseCase(_teamRepo.Object);

        var result = await useCase.GetCollaboratorSummariesAsync(teamId);

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

        var useCase = new TeamQueryUseCase(_teamRepo.Object);

        var result = await useCase.GetAvailableCollaboratorsAsync(teamId, "ana");

        result.IsSuccess.Should().BeTrue();
        _teamRepo.Verify(r => r.GetAvailableCollaboratorsAsync(teamId, orgId, "ana", 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSubTeamsAsync_WithNonExistingTeam_ReturnsNotFound()
    {
        _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var useCase = new TeamQueryUseCase(_teamRepo.Object);

        var result = await useCase.GetSubTeamsAsync(Guid.NewGuid(), 1, 10);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
