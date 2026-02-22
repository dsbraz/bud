using System.Security.Claims;
using Bud.Server.Application.Collaborators;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Collaborators;

public sealed class CollaboratorWriteUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<ICollaboratorRepository> _collaboratorRepository = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authorizationGateway = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    [Fact]
    public async Task CreateCollaborator_WhenUnauthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();

        _tenantProvider.SetupGet(provider => provider.TenantId).Returns(tenantId);
        _authorizationGateway
            .Setup(gateway => gateway.IsOrganizationOwnerAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateCollaborator(
            _collaboratorRepository.Object,
            _authorizationGateway.Object,
            _tenantProvider.Object);

        var request = new CreateCollaboratorRequest
        {
            FullName = "User",
            Email = "user@test.com",
            Role = Bud.Shared.Contracts.CollaboratorRole.IndividualContributor
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateCollaboratorProfile_WhenCollaboratorNotFound_ReturnsNotFound()
    {
        _collaboratorRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collaborator?)null);

        var useCase = new PatchCollaborator(
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var request = new PatchCollaboratorRequest
        {
            FullName = "User",
            Email = "user@test.com",
            Role = Bud.Shared.Contracts.CollaboratorRole.IndividualContributor
        };

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteCollaborator_WhenAuthorized_Succeeds()
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            OrganizationId = Guid.NewGuid()
        };

        _collaboratorRepository
            .Setup(repository => repository.GetByIdAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collaborator);
        _collaboratorRepository
            .Setup(repository => repository.IsOrganizationOwnerAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _collaboratorRepository
            .Setup(repository => repository.HasSubordinatesAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _collaboratorRepository
            .Setup(repository => repository.HasMissionsAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _authorizationGateway
            .Setup(gateway => gateway.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DeleteCollaborator(
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, collaborator.Id);

        result.IsSuccess.Should().BeTrue();
        _collaboratorRepository.Verify(repository => repository.RemoveAsync(collaborator, It.IsAny<CancellationToken>()), Times.Once);
        _collaboratorRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCollaborator_WhenOrganizationOwner_ReturnsConflict()
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            OrganizationId = Guid.NewGuid()
        };

        _collaboratorRepository
            .Setup(repository => repository.GetByIdAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collaborator);
        _collaboratorRepository
            .Setup(repository => repository.IsOrganizationOwnerAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _authorizationGateway
            .Setup(gateway => gateway.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DeleteCollaborator(
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, collaborator.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateCollaboratorTeams_WhenAuthorized_Succeeds()
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            OrganizationId = Guid.NewGuid()
        };

        _collaboratorRepository
            .Setup(repository => repository.GetByIdWithCollaboratorTeamsAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collaborator);
        _authorizationGateway
            .Setup(gateway => gateway.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new PatchCollaboratorTeams(
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var request = new PatchCollaboratorTeamsRequest { TeamIds = [] };

        var result = await useCase.ExecuteAsync(User, collaborator.Id, request);

        result.IsSuccess.Should().BeTrue();
        _collaboratorRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCollaboratorTeams_WhenUnauthorized_ReturnsForbidden()
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            OrganizationId = Guid.NewGuid()
        };

        _collaboratorRepository
            .Setup(repository => repository.GetByIdWithCollaboratorTeamsAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collaborator);
        _authorizationGateway
            .Setup(gateway => gateway.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new PatchCollaboratorTeams(
            _collaboratorRepository.Object,
            _authorizationGateway.Object);

        var request = new PatchCollaboratorTeamsRequest { TeamIds = [] };

        var result = await useCase.ExecuteAsync(User, collaborator.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }
}
