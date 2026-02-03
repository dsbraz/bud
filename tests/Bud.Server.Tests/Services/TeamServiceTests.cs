using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class TeamServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private async Task<(Organization org, Workspace workspace)> CreateTestHierarchy(ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        return (org, workspace);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateTeam_WithNonExistentWorkspace_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);

        var request = new CreateTeamRequest
        {
            Name = "Test Team",
            WorkspaceId = Guid.NewGuid() // Non-existent workspace
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Workspace not found.");
    }

    [Fact]
    public async Task CreateTeam_WithNonExistentParentTeam_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);
        var (_, workspace) = await CreateTestHierarchy(context);

        var request = new CreateTeamRequest
        {
            Name = "Sub Team",
            WorkspaceId = workspace.Id,
            ParentTeamId = Guid.NewGuid() // Non-existent parent team
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Parent team not found.");
    }

    [Fact]
    public async Task CreateTeam_WithParentTeamInDifferentWorkspace_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);

        // Create two different workspaces
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace1 = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace 1",
            OrganizationId = org.Id
        };
        var workspace2 = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace 2",
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.AddRange(workspace1, workspace2);

        // Create parent team in workspace1
        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace1.Id
        };
        context.Teams.Add(parentTeam);
        await context.SaveChangesAsync();

        // Try to create child team in workspace2 with parent from workspace1
        var request = new CreateTeamRequest
        {
            Name = "Child Team",
            WorkspaceId = workspace2.Id,
            ParentTeamId = parentTeam.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Parent team must belong to the same workspace.");
    }

    [Fact]
    public async Task CreateTeam_WithValidParentTeam_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);
        var (org, workspace) = await CreateTestHierarchy(context);

        // Create parent team
        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };
        context.Teams.Add(parentTeam);
        await context.SaveChangesAsync();

        // Create child team
        var request = new CreateTeamRequest
        {
            Name = "Child Team",
            WorkspaceId = workspace.Id,
            ParentTeamId = parentTeam.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Child Team");
        result.Value!.WorkspaceId.Should().Be(workspace.Id);
        result.Value!.OrganizationId.Should().Be(org.Id);
        result.Value!.ParentTeamId.Should().Be(parentTeam.Id);
    }

    [Fact]
    public async Task CreateTeam_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            OrganizationId = org.Id
        };

        var owner = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = "owner@test.com",
            OrganizationId = org.Id
        };
        org.OwnerId = owner.Id;

        var regularCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Regular User",
            Email = "user@test.com",
            OrganizationId = org.Id
        };

        _tenantProvider.IsAdmin = false;
        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = regularCollaborator.Id;

        using var context = CreateInMemoryContext();
        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.AddRange(owner, regularCollaborator);
        await context.SaveChangesAsync();

        var service = new TeamService(context, _tenantProvider);

        var request = new CreateTeamRequest
        {
            Name = "New Team",
            WorkspaceId = workspace.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Only the organization owner can create teams.");
    }

    [Fact]
    public async Task CreateTeam_AsOwner_CreatesSuccessfully()
    {
        // Arrange
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            OrganizationId = org.Id
        };

        var owner = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = "owner@test.com",
            OrganizationId = org.Id
        };
        org.OwnerId = owner.Id;

        _tenantProvider.IsAdmin = false;
        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = owner.Id;

        using var context = CreateInMemoryContext();
        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(owner);
        await context.SaveChangesAsync();

        var service = new TeamService(context, _tenantProvider);

        var request = new CreateTeamRequest
        {
            Name = "New Team",
            WorkspaceId = workspace.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Team");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateTeam_SettingSelfAsParent_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);
        var (org, workspace) = await CreateTestHierarchy(context);

        // Create team
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Try to set itself as parent
        var request = new UpdateTeamRequest
        {
            Name = "Test Team",
            ParentTeamId = team.Id // Self as parent
        };

        // Act
        var result = await service.UpdateAsync(team.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("A team cannot be its own parent.");
    }

    [Fact]
    public async Task UpdateTeam_ChangingParentTeamToInvalidWorkspace_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);

        // Create two workspaces
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace1 = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace 1",
            OrganizationId = org.Id
        };
        var workspace2 = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace 2",
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.AddRange(workspace1, workspace2);

        // Create team in workspace1
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team 1",
            OrganizationId = org.Id,
            WorkspaceId = workspace1.Id
        };

        // Create parent team in workspace2
        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace2.Id
        };

        context.Teams.AddRange(team, parentTeam);
        await context.SaveChangesAsync();

        // Try to update team to have parent from different workspace
        var request = new UpdateTeamRequest
        {
            Name = "Team 1",
            ParentTeamId = parentTeam.Id
        };

        // Act
        var result = await service.UpdateAsync(team.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Parent team must belong to the same workspace.");
    }

    [Fact]
    public async Task UpdateTeam_WithValidParentTeamUpdate_UpdatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);
        var (org, workspace) = await CreateTestHierarchy(context);

        // Create team without parent
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };

        // Create parent team
        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };

        context.Teams.AddRange(team, parentTeam);
        await context.SaveChangesAsync();

        // Update team with new name and parent
        var request = new UpdateTeamRequest
        {
            Name = "Updated Name",
            ParentTeamId = parentTeam.Id
        };

        // Act
        var result = await service.UpdateAsync(team.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value!.ParentTeamId.Should().Be(parentTeam.Id);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteTeam_WithSubTeams_ReturnsConflict()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);
        var (org, workspace) = await CreateTestHierarchy(context);

        // Create parent team
        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };

        // Create sub-team
        var subTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Sub Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            ParentTeamId = parentTeam.Id
        };

        context.Teams.AddRange(parentTeam, subTeam);
        await context.SaveChangesAsync();

        // Act - Try to delete parent team
        var result = await service.DeleteAsync(parentTeam.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Error.Should().Be("Cannot delete team with sub-teams. Delete sub-teams first.");
    }

    [Fact]
    public async Task DeleteTeam_WithoutSubTeams_DeletesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);
        var (org, workspace) = await CreateTestHierarchy(context);

        // Create team without sub-teams
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(team.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify team was deleted
        var deletedTeam = await context.Teams.FindAsync(team.Id);
        deletedTeam.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTeam_WithCollaborators_DeletesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context, _tenantProvider);
        var (org, workspace) = await CreateTestHierarchy(context);

        // Create team with collaborators
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act - Delete team (should cascade delete collaborators)
        var result = await service.DeleteAsync(team.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify team was deleted
        var deletedTeam = await context.Teams.FindAsync(team.Id);
        deletedTeam.Should().BeNull();
    }

    #endregion
}
