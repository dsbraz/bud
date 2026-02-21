using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.Organizations;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Organizations;

public sealed class OrganizationCommandTests
{
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<ICollaboratorRepository> _collabRepo = new();

    private OrganizationCommand CreateCommand(string protectedOrgName = "getbud.co")
    {
        var settings = Options.Create(new GlobalAdminSettings
        {
            Email = "admin@getbud.co",
            OrganizationName = protectedOrgName
        });
        return new OrganizationCommand(_orgRepo.Object, _collabRepo.Object, settings);
    }

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new Collaborator { Id = ownerId, FullName = "Leader", Email = "leader@test.com", Role = CollaboratorRole.Leader };
        _collabRepo.Setup(r => r.GetByIdAsync(ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new Organization { Id = id, Name = "Test Org", OwnerId = ownerId, Owner = owner });

        var organizationCommand = CreateCommand();
        var request = new CreateOrganizationRequest { Name = "Test Org", OwnerId = ownerId };

        // Act
        var result = await organizationCommand.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _orgRepo.Verify(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Once);
        _orgRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentOwner_ReturnsNotFound()
    {
        // Arrange
        _collabRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Collaborator?)null);

        var organizationCommand = CreateCommand();
        var request = new CreateOrganizationRequest { Name = "Test Org", OwnerId = Guid.NewGuid() };

        // Act
        var result = await organizationCommand.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("O líder selecionado não foi encontrado.");
    }

    [Fact]
    public async Task CreateAsync_WithOwnerNotLeader_ReturnsValidationError()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var nonLeader = new Collaborator { Id = ownerId, FullName = "Non Leader", Email = "nonleader@test.com", Role = CollaboratorRole.IndividualContributor };
        _collabRepo.Setup(r => r.GetByIdAsync(ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(nonLeader);

        var organizationCommand = CreateCommand();
        var request = new CreateOrganizationRequest { Name = "Test Org", OwnerId = ownerId };

        // Act
        var result = await organizationCommand.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Be("O proprietário da organização deve ter a função de Líder.");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WithNonExistentOrganization_ReturnsNotFound()
    {
        // Arrange
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var organizationCommand = CreateCommand();

        // Act
        var result = await organizationCommand.UpdateAsync(Guid.NewGuid(), new UpdateOrganizationRequest { Name = "New Name" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task UpdateAsync_ProtectedOrganization_ReturnsValidationError()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var org = new Organization { Id = orgId, Name = "getbud.co" };
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var organizationCommand = CreateCommand("getbud.co");

        // Act
        var result = await organizationCommand.UpdateAsync(orgId, new UpdateOrganizationRequest { Name = "New Name" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Be("Esta organização está protegida e não pode ser alterada.");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentOwner_ReturnsNotFound()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var org = new Organization { Id = orgId, Name = "Test Org" };
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _collabRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Collaborator?)null);

        var organizationCommand = CreateCommand();

        // Act
        var result = await organizationCommand.UpdateAsync(orgId, new UpdateOrganizationRequest { Name = "New Name", OwnerId = Guid.NewGuid() });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("O líder selecionado não foi encontrado.");
    }

    [Fact]
    public async Task UpdateAsync_WithOwnerNotLeader_ReturnsValidationError()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var org = new Organization { Id = orgId, Name = "Test Org" };
        var nonLeaderId = Guid.NewGuid();
        var nonLeader = new Collaborator { Id = nonLeaderId, FullName = "Non Leader", Email = "nl@test.com", Role = CollaboratorRole.IndividualContributor };
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _collabRepo.Setup(r => r.GetByIdAsync(nonLeaderId, It.IsAny<CancellationToken>())).ReturnsAsync(nonLeader);

        var organizationCommand = CreateCommand();

        // Act
        var result = await organizationCommand.UpdateAsync(orgId, new UpdateOrganizationRequest { Name = "New Name", OwnerId = nonLeaderId });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Be("O proprietário da organização deve ter a função de Líder.");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WithNonExistentOrganization_ReturnsNotFound()
    {
        // Arrange
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var organizationCommand = CreateCommand();

        // Act
        var result = await organizationCommand.DeleteAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task DeleteAsync_ProtectedOrganization_ReturnsValidationError()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var org = new Organization { Id = orgId, Name = "getbud.co" };
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var organizationCommand = CreateCommand("getbud.co");

        // Act
        var result = await organizationCommand.DeleteAsync(orgId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Be("Esta organização está protegida e não pode ser excluída.");
    }

    [Fact]
    public async Task DeleteAsync_WithWorkspaces_ReturnsConflict()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var org = new Organization { Id = orgId, Name = "Test Org" };
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _orgRepo.Setup(r => r.HasWorkspacesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var organizationCommand = CreateCommand();

        // Act
        var result = await organizationCommand.DeleteAsync(orgId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.Should().Be("Não é possível excluir a organização porque ela possui workspaces associados. Exclua os workspaces primeiro.");
    }

    [Fact]
    public async Task DeleteAsync_WithCollaborators_ReturnsConflict()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var org = new Organization { Id = orgId, Name = "Test Org" };
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _orgRepo.Setup(r => r.HasWorkspacesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _orgRepo.Setup(r => r.HasCollaboratorsAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var organizationCommand = CreateCommand();

        // Act
        var result = await organizationCommand.DeleteAsync(orgId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.Should().Be("Não é possível excluir a organização porque ela possui colaboradores associados. Remova os colaboradores primeiro.");
    }

    [Fact]
    public async Task DeleteAsync_WithValidOrganization_Succeeds()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var org = new Organization { Id = orgId, Name = "Test Org" };
        _orgRepo.Setup(r => r.GetByIdWithOwnerAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _orgRepo.Setup(r => r.HasWorkspacesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _orgRepo.Setup(r => r.HasCollaboratorsAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var organizationCommand = CreateCommand();

        // Act
        var result = await organizationCommand.DeleteAsync(orgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _orgRepo.Verify(r => r.RemoveAsync(org, It.IsAny<CancellationToken>()), Times.Once);
        _orgRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
