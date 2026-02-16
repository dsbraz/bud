using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class WorkspaceServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<(Organization org, Workspace ws1, Workspace ws2, Workspace ws3, Collaborator collaborator)> CreateTestHierarchy(
        ApplicationDbContext context,
        Guid? organizationId = null)
    {
        var org = new Organization { Id = organizationId ?? Guid.NewGuid(), Name = "Test Org" };

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

        var workspace3 = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace 3",
            OrganizationId = org.Id
        };

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team A",
            OrganizationId = org.Id,
            WorkspaceId = workspace2.Id,
            LeaderId = Guid.NewGuid()
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
        context.Workspaces.AddRange(workspace1, workspace2, workspace3);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        return (org, workspace1, workspace2, workspace3, collaborator);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateWorkspace_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new WorkspaceService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var request = new CreateWorkspaceRequest
        {
            Name = "New Workspace",
            OrganizationId = org.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Workspace");
    }

    [Fact]
    public async Task CreateWorkspace_AsNonOwner_CreatesSuccessfully()
    {
        // Arrange
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
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

        _tenantProvider.IsGlobalAdmin = false;
        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = regularCollaborator.Id;

        using var context = CreateInMemoryContext();
        context.Organizations.Add(org);
        context.Collaborators.AddRange(owner, regularCollaborator);
        await context.SaveChangesAsync();

        var service = new WorkspaceService(context);

        var request = new CreateWorkspaceRequest
        {
            Name = "New Workspace",
            OrganizationId = org.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateWorkspace_AsOwner_CreatesSuccessfully()
    {
        // Arrange
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var owner = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = "owner@test.com",
            OrganizationId = org.Id
        };
        org.OwnerId = owner.Id;

        _tenantProvider.IsGlobalAdmin = false;
        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = owner.Id;

        using var context = CreateInMemoryContext();
        context.Organizations.Add(org);
        context.Collaborators.Add(owner);
        await context.SaveChangesAsync();

        var service = new WorkspaceService(context);

        var request = new CreateWorkspaceRequest
        {
            Name = "New Workspace",
            OrganizationId = org.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Workspace");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_AsAdmin_ReturnsAllWorkspaces()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = true;
        using var context = CreateInMemoryContext();
        var (org, _, _, _, _) = await CreateTestHierarchy(context);
        var service = new WorkspaceService(context);

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
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, _, _, _, collaborator) = await CreateTestHierarchy(context, orgId);

        // Make collaborator the org owner
        org.OwnerId = collaborator.Id;
        await context.SaveChangesAsync();

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        // Act
        var result = await service.GetAllAsync(org.Id, null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersCorrectly()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = true;
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        context.Workspaces.AddRange(
            new Workspace { Id = Guid.NewGuid(), Name = "ALPHA Workspace", OrganizationId = org.Id },
            new Workspace { Id = Guid.NewGuid(), Name = "Beta Workspace", OrganizationId = org.Id });
        await context.SaveChangesAsync();
        var service = new WorkspaceService(context);

        // Act
        var result = await service.GetAllAsync(org.Id, "alpha", 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("ALPHA Workspace");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingWorkspace_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, ws1, _, _, collaborator) = await CreateTestHierarchy(context, orgId);

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        // Act
        var result = await service.GetByIdAsync(ws1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(ws1.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WorkspaceWithMembership_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, _, ws2, _, collaborator) = await CreateTestHierarchy(context, orgId);

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        // Act
        var result = await service.GetByIdAsync(ws2.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(ws2.Id);
    }

    [Fact]
    public async Task GetByIdAsync_AsOrgOwner_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, _, _, ws3, collaborator) = await CreateTestHierarchy(context, orgId);

        // Make collaborator the org owner
        org.OwnerId = collaborator.Id;
        await context.SaveChangesAsync();

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        // Act
        var result = await service.GetByIdAsync(ws3.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(ws3.Id);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, _, _, ws3, collaborator) = await CreateTestHierarchy(context, orgId);

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        var request = new UpdateWorkspaceRequest
        {
            Name = "Updated Name"
        };

        // Act
        var result = await service.UpdateAsync(ws3.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_AsMember_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, _, ws2, _, collaborator) = await CreateTestHierarchy(context, orgId);

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        var request = new UpdateWorkspaceRequest
        {
            Name = "Updated Name"
        };

        // Act
        var result = await service.UpdateAsync(ws2.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_AsOrgOwner_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, _, _, ws3, collaborator) = await CreateTestHierarchy(context, orgId);

        // Make collaborator the org owner
        org.OwnerId = collaborator.Id;
        await context.SaveChangesAsync();

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        var request = new UpdateWorkspaceRequest
        {
            Name = "Updated by Owner"
        };

        // Act
        var result = await service.UpdateAsync(ws3.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated by Owner");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithAssociatedMissions_ReturnsConflict()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = true;
        using var context = CreateInMemoryContext();
        var (org, ws1, _, _, _) = await CreateTestHierarchy(context);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            OrganizationId = org.Id,
            WorkspaceId = ws1.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = MissionStatus.Active
        };
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        var service = new WorkspaceService(context);

        // Act
        var result = await service.DeleteAsync(ws1.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Error.Should().Be("Não é possível excluir o workspace porque existem missões associadas a ele.");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, ws1, _, _, collaborator) = await CreateTestHierarchy(context, orgId);

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        // Act
        var result = await service.DeleteAsync(ws1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_AsMember_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, _, ws2, _, collaborator) = await CreateTestHierarchy(context, orgId);

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        // Act
        var result = await service.DeleteAsync(ws2.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_AsOrgOwner_ReturnsSuccess()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, ws1, _, _, collaborator) = await CreateTestHierarchy(context, orgId);

        // Make collaborator the org owner
        org.OwnerId = collaborator.Id;
        await context.SaveChangesAsync();

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        // Act
        var result = await service.DeleteAsync(ws1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_ExistingWorkspace_ReturnsTeams()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = false;
        var orgId = Guid.NewGuid();
        _tenantProvider.TenantId = orgId;
        using var context = CreateInMemoryContext();
        var (org, _, ws2, _, collaborator) = await CreateTestHierarchy(context, orgId);

        _tenantProvider.CollaboratorId = collaborator.Id;

        var service = new WorkspaceService(context);

        // Act
        var result = await service.GetTeamsAsync(ws2.Id, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
    }

    #endregion
}
