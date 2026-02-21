using Bud.Server.Infrastructure.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Collaborators;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Collaborators;

public sealed class CollaboratorCommandTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<ICollaboratorRepository> _collabRepo = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private CollaboratorCommand CreateCommand()
        => new(_collabRepo.Object, _authGateway.Object, _tenantProvider.Object);

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        _tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);
        _authGateway.Setup(g => g.IsOrganizationOwnerAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var collaboratorCommand = CreateCommand();
        var request = new CreateCollaboratorRequest
        {
            FullName = "User",
            Email = "user@test.com",
            Role = CollaboratorRole.IndividualContributor
        };

        var result = await collaboratorCommand.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateAsync_WhenCollaboratorNotFound_ReturnsNotFound()
    {
        _collabRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collaborator?)null);

        var collaboratorCommand = CreateCommand();
        var request = new UpdateCollaboratorRequest
        {
            FullName = "User",
            Email = "user@test.com",
            Role = CollaboratorRole.IndividualContributor
        };

        var result = await collaboratorCommand.UpdateAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorized_Succeeds()
    {
        var collaborator = new Collaborator { Id = Guid.NewGuid(), FullName = "User", Email = "user@test.com", OrganizationId = Guid.NewGuid() };
        _collabRepo.Setup(r => r.GetByIdAsync(collaborator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collaborator);
        _collabRepo.Setup(r => r.IsOrganizationOwnerAsync(collaborator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _collabRepo.Setup(r => r.HasSubordinatesAsync(collaborator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _collabRepo.Setup(r => r.HasMissionsAsync(collaborator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _authGateway.Setup(g => g.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var collaboratorCommand = CreateCommand();

        var result = await collaboratorCommand.DeleteAsync(User, collaborator.Id);

        result.IsSuccess.Should().BeTrue();
        _collabRepo.Verify(r => r.RemoveAsync(collaborator, It.IsAny<CancellationToken>()), Times.Once);
        _collabRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenOrganizationOwner_ReturnsConflict()
    {
        var collaborator = new Collaborator { Id = Guid.NewGuid(), FullName = "User", Email = "user@test.com", OrganizationId = Guid.NewGuid() };
        _collabRepo.Setup(r => r.GetByIdAsync(collaborator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collaborator);
        _collabRepo.Setup(r => r.IsOrganizationOwnerAsync(collaborator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _authGateway.Setup(g => g.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var collaboratorCommand = CreateCommand();

        var result = await collaboratorCommand.DeleteAsync(User, collaborator.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateTeamsAsync_WhenAuthorized_Succeeds()
    {
        var collaborator = new Collaborator { Id = Guid.NewGuid(), FullName = "User", Email = "user@test.com", OrganizationId = Guid.NewGuid() };
        _collabRepo.Setup(r => r.GetByIdWithCollaboratorTeamsAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collaborator);
        _authGateway.Setup(g => g.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var collaboratorCommand = CreateCommand();
        var request = new UpdateCollaboratorTeamsRequest { TeamIds = [] };

        var result = await collaboratorCommand.UpdateTeamsAsync(User, collaborator.Id, request);

        result.IsSuccess.Should().BeTrue();
        _collabRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTeamsAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var collaborator = new Collaborator { Id = Guid.NewGuid(), FullName = "User", Email = "user@test.com", OrganizationId = Guid.NewGuid() };
        _collabRepo.Setup(r => r.GetByIdWithCollaboratorTeamsAsync(collaborator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collaborator);
        _authGateway.Setup(g => g.IsOrganizationOwnerAsync(User, collaborator.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var collaboratorCommand = CreateCommand();
        var request = new UpdateCollaboratorTeamsRequest { TeamIds = [] };

        var result = await collaboratorCommand.UpdateTeamsAsync(User, collaborator.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }
}
