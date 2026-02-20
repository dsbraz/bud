using System.Security.Claims;
using Bud.Server.Application.Collaborators;
using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Collaborators;

public sealed class CollaboratorCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<ICollaboratorRepository> _collabRepo = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private CollaboratorCommandUseCase CreateUseCase()
        => new(_collabRepo.Object, _authGateway.Object, _tenantProvider.Object);

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        _tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);
        _authGateway.Setup(g => g.IsOrganizationOwnerAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var request = new CreateCollaboratorRequest
        {
            FullName = "User",
            Email = "user@test.com",
            Role = CollaboratorRole.IndividualContributor
        };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateAsync_WhenCollaboratorNotFound_ReturnsNotFound()
    {
        _collabRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collaborator?)null);

        var useCase = CreateUseCase();
        var request = new UpdateCollaboratorRequest
        {
            FullName = "User",
            Email = "user@test.com",
            Role = CollaboratorRole.IndividualContributor
        };

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), request);

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

        var useCase = CreateUseCase();

        var result = await useCase.DeleteAsync(User, collaborator.Id);

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

        var useCase = CreateUseCase();

        var result = await useCase.DeleteAsync(User, collaborator.Id);

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

        var useCase = CreateUseCase();
        var request = new UpdateCollaboratorTeamsRequest { TeamIds = [] };

        var result = await useCase.UpdateTeamsAsync(User, collaborator.Id, request);

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

        var useCase = CreateUseCase();
        var request = new UpdateCollaboratorTeamsRequest { TeamIds = [] };

        var result = await useCase.UpdateTeamsAsync(User, collaborator.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }
}
