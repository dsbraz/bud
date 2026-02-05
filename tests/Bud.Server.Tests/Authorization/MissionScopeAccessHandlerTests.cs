using System.Security.Claims;
using Bud.Server.Authorization.Handlers;
using Bud.Server.Authorization.Requirements;
using Bud.Server.Authorization.ResourceScopes;
using Bud.Server.Data;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Authorization;

public sealed class MissionScopeAccessHandlerTests
{
    private static ApplicationDbContext CreateInMemoryContext(TestTenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, tenantProvider);
    }

    private static AuthorizationHandlerContext CreateAuthContext(
        MissionScopeAccessRequirement requirement,
        MissionScopeResource resource)
    {
        return new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource);
    }

    #region GlobalAdmin

    [Fact]
    public async Task Handle_WhenGlobalAdmin_ShouldAlwaysSucceed()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var handler = new MissionScopeAccessHandler(tenantProvider, context);
        var requirement = new MissionScopeAccessRequirement();
        var resource = new MissionScopeResource(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var authContext = CreateAuthContext(requirement, resource);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    #endregion

    #region CollaboratorId null in ITenantProvider

    [Fact]
    public async Task Handle_WhenCollaboratorIdIsNull_ShouldFail()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            CollaboratorId = null
        };
        using var context = CreateInMemoryContext(tenantProvider);
        var handler = new MissionScopeAccessHandler(tenantProvider, context);
        var requirement = new MissionScopeAccessRequirement();
        var resource = new MissionScopeResource(null, null, null);
        var authContext = CreateAuthContext(requirement, resource);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeFalse();
    }

    #endregion

    #region Org-scoped (all FKs null)

    [Fact]
    public async Task Handle_WhenOrgScoped_ShouldSucceedForAnyCollaborator()
    {
        // Arrange
        var collaboratorId = Guid.NewGuid();
        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            CollaboratorId = collaboratorId
        };
        using var context = CreateInMemoryContext(tenantProvider);
        var handler = new MissionScopeAccessHandler(tenantProvider, context);
        var requirement = new MissionScopeAccessRequirement();
        var resource = new MissionScopeResource(null, null, null);
        var authContext = CreateAuthContext(requirement, resource);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    #endregion

    #region Collaborator-scoped

    [Fact]
    public async Task Handle_WhenCollaboratorScoped_AndCollaboratorIdMatches_ShouldSucceed()
    {
        // Arrange
        var collaboratorId = Guid.NewGuid();
        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            CollaboratorId = collaboratorId
        };
        using var context = CreateInMemoryContext(tenantProvider);
        var handler = new MissionScopeAccessHandler(tenantProvider, context);
        var requirement = new MissionScopeAccessRequirement();
        var resource = new MissionScopeResource(null, null, collaboratorId);
        var authContext = CreateAuthContext(requirement, resource);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenCollaboratorScoped_AndCollaboratorIdDoesNotMatch_ShouldFail()
    {
        // Arrange
        var collaboratorId = Guid.NewGuid();
        var otherCollaboratorId = Guid.NewGuid();
        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            CollaboratorId = collaboratorId
        };
        using var context = CreateInMemoryContext(tenantProvider);
        var handler = new MissionScopeAccessHandler(tenantProvider, context);
        var requirement = new MissionScopeAccessRequirement();
        var resource = new MissionScopeResource(null, null, otherCollaboratorId);
        var authContext = CreateAuthContext(requirement, resource);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeFalse();
    }

    #endregion

    #region Team-scoped

    [Fact]
    public async Task Handle_WhenTeamScoped_AndCollaboratorBelongsToTeam_ShouldSucceed()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();

        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            TenantId = orgId,
            CollaboratorId = collaboratorId
        };

        using var context = CreateInMemoryContext(tenantProvider);

        context.Organizations.Add(new Organization { Id = orgId, Name = "Test Org" });
        context.Workspaces.Add(new Workspace { Id = workspaceId, Name = "Test Workspace", OrganizationId = orgId });
        context.Teams.Add(new Team { Id = teamId, Name = "Test Team", OrganizationId = orgId, WorkspaceId = workspaceId });
        context.Collaborators.Add(new Collaborator
        {
            Id = collaboratorId,
            FullName = "Test User",
            Email = "team-member@example.com",
            OrganizationId = orgId
        });
        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = collaboratorId,
            TeamId = teamId,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new MissionScopeAccessHandler(tenantProvider, context);
        var requirement = new MissionScopeAccessRequirement();
        var resource = new MissionScopeResource(null, teamId, null);
        var authContext = CreateAuthContext(requirement, resource);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTeamScoped_AndCollaboratorDoesNotBelongToTeam_ShouldFail()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();

        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            TenantId = orgId,
            CollaboratorId = collaboratorId
        };

        using var context = CreateInMemoryContext(tenantProvider);

        context.Organizations.Add(new Organization { Id = orgId, Name = "Test Org" });
        context.Workspaces.Add(new Workspace { Id = workspaceId, Name = "Test Workspace", OrganizationId = orgId });
        context.Teams.Add(new Team { Id = teamId, Name = "Test Team", OrganizationId = orgId, WorkspaceId = workspaceId });
        context.Collaborators.Add(new Collaborator
        {
            Id = collaboratorId,
            FullName = "Test User",
            Email = "outsider@example.com",
            OrganizationId = orgId
        });
        // No CollaboratorTeam link -- collaborator is NOT in the team
        await context.SaveChangesAsync();

        var handler = new MissionScopeAccessHandler(tenantProvider, context);
        var requirement = new MissionScopeAccessRequirement();
        var resource = new MissionScopeResource(null, teamId, null);
        var authContext = CreateAuthContext(requirement, resource);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeFalse();
    }

    #endregion

    #region Workspace-scoped

    [Fact]
    public async Task Handle_WhenWorkspaceScoped_AndCollaboratorIsInTeamOfWorkspace_ShouldSucceed()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();

        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            TenantId = orgId,
            CollaboratorId = collaboratorId
        };

        using var context = CreateInMemoryContext(tenantProvider);

        context.Organizations.Add(new Organization { Id = orgId, Name = "Test Org" });
        context.Workspaces.Add(new Workspace { Id = workspaceId, Name = "Test Workspace", OrganizationId = orgId });
        context.Teams.Add(new Team { Id = teamId, Name = "Test Team", OrganizationId = orgId, WorkspaceId = workspaceId });
        context.Collaborators.Add(new Collaborator
        {
            Id = collaboratorId,
            FullName = "Test User",
            Email = "member@example.com",
            OrganizationId = orgId
        });
        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = collaboratorId,
            TeamId = teamId,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new MissionScopeAccessHandler(tenantProvider, context);
        var requirement = new MissionScopeAccessRequirement();
        var resource = new MissionScopeResource(workspaceId, null, null);
        var authContext = CreateAuthContext(requirement, resource);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceScoped_AndCollaboratorNotInAnyTeamOfWorkspace_ShouldFail()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();
        var teamInOtherWorkspaceId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();

        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            TenantId = orgId,
            CollaboratorId = collaboratorId
        };

        using var context = CreateInMemoryContext(tenantProvider);

        context.Organizations.Add(new Organization { Id = orgId, Name = "Test Org" });
        context.Workspaces.Add(new Workspace { Id = workspaceId, Name = "Target Workspace", OrganizationId = orgId });
        context.Workspaces.Add(new Workspace { Id = otherWorkspaceId, Name = "Other Workspace", OrganizationId = orgId });
        context.Teams.Add(new Team
        {
            Id = teamInOtherWorkspaceId,
            Name = "Team in Other Workspace",
            OrganizationId = orgId,
            WorkspaceId = otherWorkspaceId
        });
        context.Collaborators.Add(new Collaborator
        {
            Id = collaboratorId,
            FullName = "Test User",
            Email = "outsider@example.com",
            OrganizationId = orgId
        });
        // Collaborator belongs to a team in a DIFFERENT workspace
        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = collaboratorId,
            TeamId = teamInOtherWorkspaceId,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new MissionScopeAccessHandler(tenantProvider, context);
        var requirement = new MissionScopeAccessRequirement();
        var resource = new MissionScopeResource(workspaceId, null, null);
        var authContext = CreateAuthContext(requirement, resource);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeFalse();
    }

    #endregion
}
