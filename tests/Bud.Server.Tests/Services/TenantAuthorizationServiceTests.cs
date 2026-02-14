using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class TenantAuthorizationServiceTests
{
    private static ApplicationDbContext CreateInMemoryContext(TestTenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, tenantProvider);
    }

    #region UserBelongsToTenantAsync Tests

    [Fact]
    public async Task UserBelongsToTenantAsync_WhenGlobalAdmin_ReturnsTrue()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = new TenantAuthorizationService(context, tenantProvider);

        var tenantId = Guid.NewGuid();

        // Act
        var result = await service.UserBelongsToTenantAsync(tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserBelongsToTenantAsync_WhenNoUserEmail_ReturnsFalse()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = false, UserEmail = null };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = new TenantAuthorizationService(context, tenantProvider);

        var tenantId = Guid.NewGuid();

        // Act
        var result = await service.UserBelongsToTenantAsync(tenantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UserBelongsToTenantAsync_WhenEmptyUserEmail_ReturnsFalse()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = false, UserEmail = "" };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = new TenantAuthorizationService(context, tenantProvider);

        var tenantId = Guid.NewGuid();

        // Act
        var result = await service.UserBelongsToTenantAsync(tenantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UserBelongsToTenantAsync_WhenUserIsOwner_ReturnsTrue()
    {
        // Arrange
        var userEmail = "owner@example.com";
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true, UserEmail = userEmail };
        using var context = CreateInMemoryContext(tenantProvider);

        // Create org with owner
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Workspace", OrganizationId = org.Id };
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };
        var owner = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = userEmail,
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        org.OwnerId = owner.Id;

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(owner);
        await context.SaveChangesAsync();

        // Now create service with non-admin tenant provider
        var regularTenantProvider = new TestTenantProvider { IsGlobalAdmin = false, UserEmail = userEmail };
        var service = new TenantAuthorizationService(context, regularTenantProvider);

        // Act
        var result = await service.UserBelongsToTenantAsync(org.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserBelongsToTenantAsync_WhenUserIsCollaborator_ReturnsTrue()
    {
        // Arrange
        var userEmail = "collaborator@example.com";
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true, UserEmail = userEmail };
        using var context = CreateInMemoryContext(tenantProvider);

        // Create org with collaborator
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Workspace", OrganizationId = org.Id };
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Collaborator",
            Email = userEmail,
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Now create service with non-admin tenant provider
        var regularTenantProvider = new TestTenantProvider { IsGlobalAdmin = false, UserEmail = userEmail };
        var service = new TenantAuthorizationService(context, regularTenantProvider);

        // Act
        var result = await service.UserBelongsToTenantAsync(org.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserBelongsToTenantAsync_WhenUserNotInOrganization_ReturnsFalse()
    {
        // Arrange
        var userEmail = "other@example.com";
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);

        // Create org without the user
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        // Now create service with non-admin tenant provider
        var regularTenantProvider = new TestTenantProvider { IsGlobalAdmin = false, UserEmail = userEmail };
        var service = new TenantAuthorizationService(context, regularTenantProvider);

        // Act
        var result = await service.UserBelongsToTenantAsync(org.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetUserTenantIdsAsync Tests

    [Fact]
    public async Task GetUserTenantIdsAsync_WhenGlobalAdmin_ReturnsAllOrganizations()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = new TenantAuthorizationService(context, tenantProvider);

        var org1 = new Organization { Id = Guid.NewGuid(), Name = "Org 1" };
        var org2 = new Organization { Id = Guid.NewGuid(), Name = "Org 2" };
        var org3 = new Organization { Id = Guid.NewGuid(), Name = "Org 3" };
        context.Organizations.AddRange(org1, org2, org3);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserTenantIdsAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain([org1.Id, org2.Id, org3.Id]);
    }

    [Fact]
    public async Task GetUserTenantIdsAsync_WhenNoUserEmail_ReturnsEmptyList()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = false, UserEmail = null };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = new TenantAuthorizationService(context, tenantProvider);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserTenantIdsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserTenantIdsAsync_WhenEmptyUserEmail_ReturnsEmptyList()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = false, UserEmail = "" };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = new TenantAuthorizationService(context, tenantProvider);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserTenantIdsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserTenantIdsAsync_ReturnsOwnedAndCollaboratorOrganizations()
    {
        // Arrange
        var userEmail = "user@example.com";
        var setupTenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(setupTenantProvider);

        // Create organizations
        var ownedOrg = new Organization { Id = Guid.NewGuid(), Name = "Owned Org" };
        var memberOrg = new Organization { Id = Guid.NewGuid(), Name = "Member Org" };
        var otherOrg = new Organization { Id = Guid.NewGuid(), Name = "Other Org" };
        context.Organizations.AddRange(ownedOrg, memberOrg, otherOrg);

        // Create workspace and teams
        var ownedWorkspace = new Workspace { Id = Guid.NewGuid(), Name = "Owned Workspace", OrganizationId = ownedOrg.Id };
        var memberWorkspace = new Workspace { Id = Guid.NewGuid(), Name = "Member Workspace", OrganizationId = memberOrg.Id };
        context.Workspaces.AddRange(ownedWorkspace, memberWorkspace);

        var ownedTeam = new Team { Id = Guid.NewGuid(), Name = "Owned Team", OrganizationId = ownedOrg.Id, WorkspaceId = ownedWorkspace.Id, LeaderId = Guid.NewGuid() };
        var memberTeam = new Team { Id = Guid.NewGuid(), Name = "Member Team", OrganizationId = memberOrg.Id, WorkspaceId = memberWorkspace.Id, LeaderId = Guid.NewGuid() };
        context.Teams.AddRange(ownedTeam, memberTeam);

        // Create owner collaborator in ownedOrg
        var owner = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = userEmail,
            Role = CollaboratorRole.Leader,
            OrganizationId = ownedOrg.Id,
            TeamId = ownedTeam.Id
        };
        ownedOrg.OwnerId = owner.Id;
        context.Collaborators.Add(owner);

        // Create member collaborator in memberOrg
        var member = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User Member",
            Email = userEmail,
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = memberOrg.Id,
            TeamId = memberTeam.Id
        };
        context.Collaborators.Add(member);

        await context.SaveChangesAsync();

        // Now create service with regular user tenant provider
        var regularTenantProvider = new TestTenantProvider { IsGlobalAdmin = false, UserEmail = userEmail };
        var service = new TenantAuthorizationService(context, regularTenantProvider);

        // Act
        var result = await service.GetUserTenantIdsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain([ownedOrg.Id, memberOrg.Id]);
        result.Should().NotContain(otherOrg.Id);
    }

    [Fact]
    public async Task GetUserTenantIdsAsync_DeduplicatesOrganizations()
    {
        // Arrange: User is both owner and collaborator of the same org
        var userEmail = "user@example.com";
        var setupTenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(setupTenantProvider);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Same Org" };
        context.Organizations.Add(org);

        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Workspace", OrganizationId = org.Id };
        context.Workspaces.Add(workspace);

        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };
        context.Teams.Add(team);

        // User is owner and collaborator of the same org
        var ownerCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = userEmail,
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        org.OwnerId = ownerCollaborator.Id;
        context.Collaborators.Add(ownerCollaborator);

        await context.SaveChangesAsync();

        var regularTenantProvider = new TestTenantProvider { IsGlobalAdmin = false, UserEmail = userEmail };
        var service = new TenantAuthorizationService(context, regularTenantProvider);

        // Act
        var result = await service.GetUserTenantIdsAsync();

        // Assert: Should return the org only once
        result.Should().ContainSingle();
        result.Should().Contain(org.Id);
    }

    #endregion
}
