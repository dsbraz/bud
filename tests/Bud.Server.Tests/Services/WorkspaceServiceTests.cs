using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class WorkspaceServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private async Task<(Organization org, Workspace publicWs, Workspace privateWsMember, Workspace privateWsNonMember, Collaborator collaborator)> CreateVisibilityTestHierarchy(ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };

        var publicWorkspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Public Workspace",
            Visibility = Visibility.Public,
            OrganizationId = org.Id
        };

        var privateWorkspaceMember = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Private Workspace (member)",
            Visibility = Visibility.Private,
            OrganizationId = org.Id
        };

        var privateWorkspaceNonMember = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Private Workspace (non-member)",
            Visibility = Visibility.Private,
            OrganizationId = org.Id
        };

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team A",
            OrganizationId = org.Id,
            WorkspaceId = privateWorkspaceMember.Id
        };

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "user@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.AddRange(publicWorkspace, privateWorkspaceMember, privateWorkspaceNonMember);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        return (org, publicWorkspace, privateWorkspaceMember, privateWorkspaceNonMember, collaborator);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateWorkspace_WithPublicVisibility_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new WorkspaceService(context, _tenantProvider);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var request = new CreateWorkspaceRequest
        {
            Name = "New Workspace",
            OrganizationId = org.Id,
            Visibility = Visibility.Public
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public async Task CreateWorkspace_WithPrivateVisibility_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new WorkspaceService(context, _tenantProvider);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var request = new CreateWorkspaceRequest
        {
            Name = "Private Workspace",
            OrganizationId = org.Id,
            Visibility = Visibility.Private
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Visibility.Should().Be(Visibility.Private);
    }

    #endregion

    #region GetAllAsync Visibility Tests

    [Fact]
    public async Task GetAllAsync_AsAdmin_ReturnsAllWorkspaces()
    {
        // Arrange
        _tenantProvider.IsAdmin = true;
        using var context = CreateInMemoryContext();
        var (org, _, _, _, _) = await CreateVisibilityTestHierarchy(context);
        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.GetAllAsync(org.Id, null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_AsOrgOwner_ReturnsAllWorkspaces()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        _tenantProvider.UserEmail = "user@test.com";
        using var context = CreateInMemoryContext();
        var (org, _, _, _, collaborator) = await CreateVisibilityTestHierarchy(context);

        // Make collaborator the org owner
        org.OwnerId = collaborator.Id;
        await context.SaveChangesAsync();

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.GetAllAsync(org.Id, null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_AsRegularCollaborator_ReturnsPublicAndMemberPrivateWorkspaces()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        _tenantProvider.UserEmail = "user@test.com";
        using var context = CreateInMemoryContext();
        var (org, publicWs, privateWsMember, privateWsNonMember, collaborator) =
            await CreateVisibilityTestHierarchy(context);

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.GetAllAsync(org.Id, null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Should().Contain(w => w.Id == publicWs.Id);
        result.Value.Items.Should().Contain(w => w.Id == privateWsMember.Id);
        result.Value.Items.Should().NotContain(w => w.Id == privateWsNonMember.Id);
    }

    #endregion

    #region GetByIdAsync Visibility Tests

    [Fact]
    public async Task GetByIdAsync_PublicWorkspace_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        _tenantProvider.UserEmail = "user@test.com";
        using var context = CreateInMemoryContext();
        var (org, publicWs, _, _, collaborator) = await CreateVisibilityTestHierarchy(context);

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.GetByIdAsync(publicWs.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(publicWs.Id);
    }

    [Fact]
    public async Task GetByIdAsync_PrivateWorkspace_AsMember_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        _tenantProvider.UserEmail = "user@test.com";
        using var context = CreateInMemoryContext();
        var (org, _, privateWsMember, _, collaborator) = await CreateVisibilityTestHierarchy(context);

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.GetByIdAsync(privateWsMember.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(privateWsMember.Id);
    }

    [Fact]
    public async Task GetByIdAsync_PrivateWorkspace_AsNonMember_ReturnsNotFound()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        using var context = CreateInMemoryContext();
        var (org, _, _, privateWsNonMember, collaborator) = await CreateVisibilityTestHierarchy(context);

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.GetByIdAsync(privateWsNonMember.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_PrivateWorkspace_AsOrgOwner_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        _tenantProvider.UserEmail = "user@test.com";
        using var context = CreateInMemoryContext();
        var (org, _, _, privateWsNonMember, collaborator) = await CreateVisibilityTestHierarchy(context);

        // Make collaborator the org owner
        org.OwnerId = collaborator.Id;
        await context.SaveChangesAsync();

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.GetByIdAsync(privateWsNonMember.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(privateWsNonMember.Id);
    }

    #endregion

    #region UpdateAsync Write Access Tests

    [Fact]
    public async Task UpdateAsync_AsNonMember_ReturnsForbidden()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        using var context = CreateInMemoryContext();
        var (org, _, _, privateWsNonMember, collaborator) = await CreateVisibilityTestHierarchy(context);

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        var request = new UpdateWorkspaceRequest
        {
            Name = "Updated Name",
            Visibility = Visibility.Private
        };

        // Act
        var result = await service.UpdateAsync(privateWsNonMember.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateAsync_AsMember_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        _tenantProvider.UserEmail = "user@test.com";
        using var context = CreateInMemoryContext();
        var (org, _, privateWsMember, _, collaborator) = await CreateVisibilityTestHierarchy(context);

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        var request = new UpdateWorkspaceRequest
        {
            Name = "Updated Name",
            Visibility = Visibility.Private
        };

        // Act
        var result = await service.UpdateAsync(privateWsMember.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_AsOrgOwner_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        _tenantProvider.UserEmail = "user@test.com";
        using var context = CreateInMemoryContext();
        var (org, _, _, privateWsNonMember, collaborator) = await CreateVisibilityTestHierarchy(context);

        // Make collaborator the org owner
        org.OwnerId = collaborator.Id;
        await context.SaveChangesAsync();

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        var request = new UpdateWorkspaceRequest
        {
            Name = "Updated by Owner",
            Visibility = Visibility.Public
        };

        // Act
        var result = await service.UpdateAsync(privateWsNonMember.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated by Owner");
        result.Value!.Visibility.Should().Be(Visibility.Public);
    }

    #endregion

    #region DeleteAsync Write Access Tests

    [Fact]
    public async Task DeleteAsync_AsNonMember_ReturnsForbidden()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        using var context = CreateInMemoryContext();
        var (org, publicWs, _, _, collaborator) = await CreateVisibilityTestHierarchy(context);

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act - try to delete the public workspace where user is not a member
        var result = await service.DeleteAsync(publicWs.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
    }

    [Fact]
    public async Task DeleteAsync_AsMember_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        _tenantProvider.UserEmail = "user@test.com";
        using var context = CreateInMemoryContext();
        var (org, _, privateWsMember, _, collaborator) = await CreateVisibilityTestHierarchy(context);

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.DeleteAsync(privateWsMember.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_AsOrgOwner_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        _tenantProvider.UserEmail = "user@test.com";
        using var context = CreateInMemoryContext();
        var (org, publicWs, _, _, collaborator) = await CreateVisibilityTestHierarchy(context);

        // Make collaborator the org owner
        org.OwnerId = collaborator.Id;
        await context.SaveChangesAsync();

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.DeleteAsync(publicWs.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region GetTeamsAsync Visibility Tests

    [Fact]
    public async Task GetTeamsAsync_PrivateWorkspace_AsNonMember_ReturnsNotFound()
    {
        // Arrange
        _tenantProvider.IsAdmin = false;
        using var context = CreateInMemoryContext();
        var (org, _, _, privateWsNonMember, collaborator) = await CreateVisibilityTestHierarchy(context);

        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context, _tenantProvider);

        // Act
        var result = await service.GetTeamsAsync(privateWsNonMember.Id, 1, 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    #endregion
}
