using Bud.Server.Application.Collaborators;
using Bud.Server.Application.Common;
using Bud.Server.Application.Projections;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Collaborators;

public sealed class CollaboratorReadUseCasesTests
{
    private readonly Mock<ICollaboratorRepository> _collaboratorRepository = new();

    [Fact]
    public async Task GetCollaboratorById_WithExistingCollaborator_ReturnsSuccess()
    {
        var collaboratorId = Guid.NewGuid();

        _collaboratorRepository
            .Setup(repository => repository.GetByIdAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collaborator
            {
                Id = collaboratorId,
                FullName = "Ana",
                Email = "ana@getbud.co",
                OrganizationId = Guid.NewGuid()
            });

        var useCase = new ViewCollaboratorProfile(_collaboratorRepository.Object);

        var result = await useCase.ExecuteAsync(collaboratorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(collaboratorId);
    }

    [Fact]
    public async Task GetCollaboratorById_WithNonExistingId_ReturnsNotFound()
    {
        _collaboratorRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collaborator?)null);

        var useCase = new ViewCollaboratorProfile(_collaboratorRepository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ListLeaders_ReturnsSuccess()
    {
        var organizationId = Guid.NewGuid();

        _collaboratorRepository
            .Setup(repository => repository.GetLeadersAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var useCase = new ListLeaders(_collaboratorRepository.Object);

        var result = await useCase.ExecuteAsync(organizationId);

        result.IsSuccess.Should().BeTrue();
        _collaboratorRepository.Verify(repository => repository.GetLeadersAsync(organizationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAvailableCollaboratorTeams_WithExistingCollaborator_ReturnsSuccess()
    {
        var collaboratorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _collaboratorRepository
            .Setup(repository => repository.GetByIdAsync(collaboratorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collaborator
            {
                Id = collaboratorId,
                FullName = "Ana",
                Email = "ana@getbud.co",
                OrganizationId = organizationId
            });
        _collaboratorRepository
            .Setup(repository => repository.GetAvailableTeamsAsync(collaboratorId, organizationId, "produto", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var useCase = new ListAvailableCollaboratorTeams(_collaboratorRepository.Object);

        var result = await useCase.ExecuteAsync(collaboratorId, "produto");

        result.IsSuccess.Should().BeTrue();
        _collaboratorRepository.Verify(
            repository => repository.GetAvailableTeamsAsync(collaboratorId, organizationId, "produto", 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCollaboratorHierarchy_WithNonExistingCollaborator_ReturnsNotFound()
    {
        _collaboratorRepository
            .Setup(repository => repository.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new GetCollaboratorHierarchy(_collaboratorRepository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
