using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Collaborators;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Collaborators;

public sealed class CollaboratorQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        // Arrange
        var collaboratorId = Guid.NewGuid();
        var collaboratorService = new Mock<ICollaboratorQueryService>();
        collaboratorService
            .Setup(s => s.GetByIdAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Collaborator>.Success(new Collaborator { Id = collaboratorId, FullName = "Ana", Email = "ana@getbud.co", OrganizationId = Guid.NewGuid() }));

        var useCase = new CollaboratorQueryUseCase(collaboratorService.Object);

        // Act
        var result = await useCase.GetByIdAsync(collaboratorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(collaboratorId);
    }

    [Fact]
    public async Task GetLeadersAsync_DelegatesToService()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var collaboratorService = new Mock<ICollaboratorQueryService>();
        collaboratorService
            .Setup(s => s.GetLeadersAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<List<LeaderCollaboratorResponse>>.Success([]));

        var useCase = new CollaboratorQueryUseCase(collaboratorService.Object);

        // Act
        var result = await useCase.GetLeadersAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        collaboratorService.Verify(s => s.GetLeadersAsync(organizationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailableTeamsAsync_DelegatesToService()
    {
        // Arrange
        var collaboratorId = Guid.NewGuid();
        var collaboratorService = new Mock<ICollaboratorQueryService>();
        collaboratorService
            .Setup(s => s.GetAvailableTeamsAsync(collaboratorId, "produto", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<List<TeamSummaryDto>>.Success([]));

        var useCase = new CollaboratorQueryUseCase(collaboratorService.Object);

        // Act
        var result = await useCase.GetAvailableTeamsAsync(collaboratorId, "produto");

        // Assert
        result.IsSuccess.Should().BeTrue();
        collaboratorService.Verify(s => s.GetAvailableTeamsAsync(collaboratorId, "produto", It.IsAny<CancellationToken>()), Times.Once);
    }
}
