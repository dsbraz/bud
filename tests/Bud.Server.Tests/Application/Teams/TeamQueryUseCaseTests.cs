using Bud.Server.Services;
using Bud.Server.Application.Teams;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Teams;

public sealed class TeamQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamService = new Mock<ITeamService>();
        teamService
            .Setup(s => s.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Team>.Success(new Team { Id = teamId, Name = "Produto", WorkspaceId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() }));

        var useCase = new TeamQueryUseCase(teamService.Object);

        // Act
        var result = await useCase.GetByIdAsync(teamId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(teamId);
    }

    [Fact]
    public async Task GetCollaboratorSummariesAsync_DelegatesToService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamService = new Mock<ITeamService>();
        teamService
            .Setup(s => s.GetCollaboratorSummariesAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<List<CollaboratorSummaryDto>>.Success([]));

        var useCase = new TeamQueryUseCase(teamService.Object);

        // Act
        var result = await useCase.GetCollaboratorSummariesAsync(teamId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        teamService.Verify(s => s.GetCollaboratorSummariesAsync(teamId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailableCollaboratorsAsync_DelegatesToService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamService = new Mock<ITeamService>();
        teamService
            .Setup(s => s.GetAvailableCollaboratorsAsync(teamId, "ana", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<List<CollaboratorSummaryDto>>.Success([]));

        var useCase = new TeamQueryUseCase(teamService.Object);

        // Act
        var result = await useCase.GetAvailableCollaboratorsAsync(teamId, "ana");

        // Assert
        result.IsSuccess.Should().BeTrue();
        teamService.Verify(s => s.GetAvailableCollaboratorsAsync(teamId, "ana", It.IsAny<CancellationToken>()), Times.Once);
    }
}
