using Bud.Infrastructure.Persistence;
using Bud.Infrastructure.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Infrastructure.UnitTests.Infrastructure.Repositories;

public sealed class WorkspaceRepositoryTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<Organization> CreateTestOrganization(ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();
        return org;
    }

    private static async Task<Workspace> CreateTestWorkspace(
        ApplicationDbContext context,
        Guid organizationId,
        string name = "Test Workspace")
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrganizationId = organizationId
        };
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();
        return workspace;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenWorkspaceExists_ReturnsWorkspace()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);

        // Act
        var result = await repository.GetByIdAsync(workspace.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(workspace.Id);
        result.Name.Should().Be("Test Workspace");
    }

    [Fact]
    public async Task GetByIdAsync_WhenWorkspaceNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_FiltersByOrganizationId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org1 = await CreateTestOrganization(context);
        var org2 = await CreateTestOrganization(context);

        await CreateTestWorkspace(context, org1.Id, "Workspace A");
        await CreateTestWorkspace(context, org2.Id, "Workspace B");

        // Act
        var result = await repository.GetAllAsync(org1.Id, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Workspace A");
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);

        await CreateTestWorkspace(context, org.Id, "Product");
        await CreateTestWorkspace(context, org.Id, "Marketing");

        // Act
        var result = await repository.GetAllAsync(org.Id, "prod", 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Product");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);

        for (int i = 0; i < 5; i++)
        {
            await CreateTestWorkspace(context, org.Id, $"Workspace {i:D2}");
        }

        // Act
        var result = await repository.GetAllAsync(org.Id, null, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);

        await CreateTestWorkspace(context, org.Id, "Charlie");
        await CreateTestWorkspace(context, org.Id, "Alpha");
        await CreateTestWorkspace(context, org.Id, "Bravo");

        // Act
        var result = await repository.GetAllAsync(org.Id, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Alpha");
        result.Items[1].Name.Should().Be("Bravo");
        result.Items[2].Name.Should().Be("Charlie");
    }

    #endregion

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_ReturnsTeamsForWorkspace()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test Leader",
            Email = "leader@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        context.Teams.AddRange(
            new Team
            {
                Id = Guid.NewGuid(),
                Name = "Team A",
                WorkspaceId = workspace.Id,
                OrganizationId = org.Id,
                LeaderId = collaborator.Id
            },
            new Team
            {
                Id = Guid.NewGuid(),
                Name = "Team B",
                WorkspaceId = workspace.Id,
                OrganizationId = org.Id,
                LeaderId = collaborator.Id
            });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTeamsAsync(workspace.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test Leader",
            Email = "leader@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            context.Teams.Add(new Team
            {
                Id = Guid.NewGuid(),
                Name = $"Team {i:D2}",
                WorkspaceId = workspace.Id,
                OrganizationId = org.Id,
                LeaderId = collaborator.Id
            });
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTeamsAsync(workspace.Id, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetTeamsAsync_DoesNotReturnTeamsFromOtherWorkspaces()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace1 = await CreateTestWorkspace(context, org.Id, "Workspace 1");
        var workspace2 = await CreateTestWorkspace(context, org.Id, "Workspace 2");

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test Leader",
            Email = "leader@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        context.Teams.AddRange(
            new Team
            {
                Id = Guid.NewGuid(),
                Name = "Team WS1",
                WorkspaceId = workspace1.Id,
                OrganizationId = org.Id,
                LeaderId = collaborator.Id
            },
            new Team
            {
                Id = Guid.NewGuid(),
                Name = "Team WS2",
                WorkspaceId = workspace2.Id,
                OrganizationId = org.Id,
                LeaderId = collaborator.Id
            });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTeamsAsync(workspace1.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Team WS1");
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenWorkspaceExists_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);

        // Act
        var result = await repository.ExistsAsync(workspace.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenWorkspaceNotFound_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);

        // Act
        var result = await repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsNameUniqueAsync Tests

    [Fact]
    public async Task IsNameUniqueAsync_WhenNameIsUnique_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        await CreateTestWorkspace(context, org.Id, "Existing Workspace");

        // Act
        var result = await repository.IsNameUniqueAsync(org.Id, "New Workspace");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsNameUniqueAsync_WhenNameAlreadyExists_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        await CreateTestWorkspace(context, org.Id, "Existing Workspace");

        // Act
        var result = await repository.IsNameUniqueAsync(org.Id, "Existing Workspace");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsNameUniqueAsync_WhenExcludingOwnId_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id, "My Workspace");

        // Act
        var result = await repository.IsNameUniqueAsync(org.Id, "My Workspace", excludeId: workspace.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsNameUniqueAsync_DuplicateInDifferentOrganization_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org1 = await CreateTestOrganization(context);
        var org2 = await CreateTestOrganization(context);
        await CreateTestWorkspace(context, org1.Id, "Same Name");

        // Act
        var result = await repository.IsNameUniqueAsync(org2.Id, "Same Name");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region HasMissionsAsync Tests

    [Fact]
    public async Task HasMissionsAsync_AlwaysReturnsFalse_BecauseGoalsNoLongerHaveWorkspaceId()
    {
        // Arrange — Goals no longer have WorkspaceId, so HasGoalsAsync always returns false.
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);

        // Act
        var result = await repository.HasGoalsAsync(workspace.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasMissionsAsync_WhenWorkspaceHasNoMissions_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);

        // Act
        var result = await repository.HasGoalsAsync(workspace.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AddAsync / RemoveAsync / SaveChangesAsync Tests

    [Fact]
    public async Task AddAsync_PersistsWorkspace()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);

        var workspace = Workspace.Create(Guid.NewGuid(), org.Id, "New Workspace");

        // Act
        await repository.AddAsync(workspace);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Workspaces.FindAsync(workspace.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("New Workspace");
    }

    [Fact]
    public async Task RemoveAsync_DeletesWorkspace()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new WorkspaceRepository(context);
        var org = await CreateTestOrganization(context);

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            OrganizationId = org.Id
        };
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(workspace);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Workspaces.FindAsync(workspace.Id);
        persisted.Should().BeNull();
    }

    #endregion
}
