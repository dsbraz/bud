using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Collaborators;
using Bud.Server.Application.Common;
using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Collaborators;

public sealed class CollaboratorQueryUseCaseTests
{
    private readonly Mock<ICollaboratorRepository> _collabRepo = new();

    [Fact]
    public async Task GetByIdAsync_WithExistingCollaborator_ReturnsSuccess()
    {
        var collaboratorId = Guid.NewGuid();
        _collabRepo.Setup(r => r.GetByIdAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collaborator { Id = collaboratorId, FullName = "Ana", Email = "ana@getbud.co", OrganizationId = Guid.NewGuid() });

        var useCase = new CollaboratorQueryUseCase(_collabRepo.Object);

        var result = await useCase.GetByIdAsync(collaboratorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(collaboratorId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        _collabRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collaborator?)null);

        var useCase = new CollaboratorQueryUseCase(_collabRepo.Object);

        var result = await useCase.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetLeadersAsync_ReturnsSuccess()
    {
        var organizationId = Guid.NewGuid();
        _collabRepo.Setup(r => r.GetLeadersAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var useCase = new CollaboratorQueryUseCase(_collabRepo.Object);

        var result = await useCase.GetLeadersAsync(organizationId);

        result.IsSuccess.Should().BeTrue();
        _collabRepo.Verify(r => r.GetLeadersAsync(organizationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailableTeamsAsync_WithExistingCollaborator_ReturnsSuccess()
    {
        var collaboratorId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _collabRepo.Setup(r => r.GetByIdAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collaborator { Id = collaboratorId, FullName = "Ana", Email = "ana@getbud.co", OrganizationId = orgId });
        _collabRepo.Setup(r => r.GetAvailableTeamsAsync(collaboratorId, orgId, "produto", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var useCase = new CollaboratorQueryUseCase(_collabRepo.Object);

        var result = await useCase.GetAvailableTeamsAsync(collaboratorId, "produto");

        result.IsSuccess.Should().BeTrue();
        _collabRepo.Verify(r => r.GetAvailableTeamsAsync(collaboratorId, orgId, "produto", 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSubordinatesAsync_WithNonExistingCollaborator_ReturnsNotFound()
    {
        _collabRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var useCase = new CollaboratorQueryUseCase(_collabRepo.Object);

        var result = await useCase.GetSubordinatesAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
