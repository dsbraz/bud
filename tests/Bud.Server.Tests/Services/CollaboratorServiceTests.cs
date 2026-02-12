using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class CollaboratorServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<(Organization org, Workspace workspace, Team team)> CreateTestHierarchy(ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            OrganizationId = org.Id
        };
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        return (org, workspace, team);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateCollaborator_AsNonOwner_CreatesSuccessfully()
    {
        // Arrange
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            OrganizationId = org.Id
        };
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };

        var owner = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = "owner@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        org.OwnerId = owner.Id;

        var regularCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Regular User",
            Email = "user@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        _tenantProvider.IsGlobalAdmin = false;
        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = regularCollaborator.Id;

        using var context = CreateInMemoryContext();
        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.AddRange(owner, regularCollaborator);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        var request = new CreateCollaboratorRequest
        {
            FullName = "New Collaborator",
            Email = "new@test.com",
            Role = CollaboratorRole.IndividualContributor,
            TeamId = team.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCollaborator_AsOwner_CreatesSuccessfully()
    {
        // Arrange
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            OrganizationId = org.Id
        };
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };

        var owner = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = "owner@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        org.OwnerId = owner.Id;

        _tenantProvider.IsGlobalAdmin = false;
        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = owner.Id;

        using var context = CreateInMemoryContext();
        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(owner);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        var request = new CreateCollaboratorRequest
        {
            FullName = "New Collaborator",
            Email = "new@test.com",
            Role = CollaboratorRole.IndividualContributor,
            TeamId = team.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FullName.Should().Be("New Collaborator");
        result.Value!.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task CreateCollaborator_AsAdmin_CreatesSuccessfully()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = true;
        using var context = CreateInMemoryContext();
        var (org, _, team) = await CreateTestHierarchy(context);
        _tenantProvider.TenantId = org.Id;

        var service = new CollaboratorService(context, _tenantProvider);

        var request = new CreateCollaboratorRequest
        {
            FullName = "New Collaborator",
            Email = "new@test.com",
            Role = CollaboratorRole.IndividualContributor,
            TeamId = team.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FullName.Should().Be("New Collaborator");
    }

    [Fact]
    public async Task CreateCollaborator_WithInvalidPersonName_ReturnsValidationError()
    {
        // Arrange
        _tenantProvider.IsGlobalAdmin = true;
        using var context = CreateInMemoryContext();
        var (org, _, team) = await CreateTestHierarchy(context);
        _tenantProvider.TenantId = org.Id;

        var service = new CollaboratorService(context, _tenantProvider);
        var request = new CreateCollaboratorRequest
        {
            FullName = "A",
            Email = "valid@test.com",
            Role = CollaboratorRole.IndividualContributor,
            TeamId = team.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("O nome do colaborador é obrigatório.");
    }

    #endregion

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_WithValidCollaborator_ReturnsTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, team) = await CreateTestHierarchy(context);

        var team2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team 2",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };
        context.Teams.Add(team2);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);

        // Add collaborator to both teams via junction table
        context.CollaboratorTeams.AddRange(
            new CollaboratorTeam { CollaboratorId = collaborator.Id, TeamId = team.Id },
            new CollaboratorTeam { CollaboratorId = collaborator.Id, TeamId = team2.Id }
        );
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetTeamsAsync(collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(t => t.Name == "Test Team");
        result.Value.Should().Contain(t => t.Name == "Team 2");
    }

    [Fact]
    public async Task GetTeamsAsync_WithNonExistentCollaborator_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetTeamsAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Colaborador não encontrado.");
    }

    [Fact]
    public async Task GetTeamsAsync_WithNoTeams_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, _, _) = await CreateTestHierarchy(context);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetTeamsAsync(collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region UpdateTeamsAsync Tests

    [Fact]
    public async Task UpdateTeamsAsync_WithValidTeams_UpdatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, team) = await CreateTestHierarchy(context);

        var team2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team 2",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };
        context.Teams.Add(team2);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);

        // Initially in team1
        context.CollaboratorTeams.Add(new CollaboratorTeam { CollaboratorId = collaborator.Id, TeamId = team.Id });
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act - Update to team2 only
        var result = await service.UpdateTeamsAsync(collaborator.Id, new UpdateCollaboratorTeamsRequest
        {
            TeamIds = new List<Guid> { team2.Id }
        });

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedTeams = await context.CollaboratorTeams
            .Where(ct => ct.CollaboratorId == collaborator.Id)
            .ToListAsync();
        updatedTeams.Should().HaveCount(1);
        updatedTeams.First().TeamId.Should().Be(team2.Id);
    }

    [Fact]
    public async Task UpdateTeamsAsync_WithNonExistentCollaborator_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.UpdateTeamsAsync(Guid.NewGuid(), new UpdateCollaboratorTeamsRequest
        {
            TeamIds = new List<Guid> { Guid.NewGuid() }
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Colaborador não encontrado.");
    }

    [Fact]
    public async Task UpdateTeamsAsync_WithTeamsFromDifferentOrg_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org1, workspace1, _) = await CreateTestHierarchy(context);

        var org2 = new Organization { Id = Guid.NewGuid(), Name = "Org 2" };
        var workspace2 = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace 2",
            OrganizationId = org2.Id
        };
        var teamInOrg2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team in Org 2",
            OrganizationId = org2.Id,
            WorkspaceId = workspace2.Id
        };

        context.Organizations.Add(org2);
        context.Workspaces.Add(workspace2);
        context.Teams.Add(teamInOrg2);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            OrganizationId = org1.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act - Try to assign to team from different org
        var result = await service.UpdateTeamsAsync(collaborator.Id, new UpdateCollaboratorTeamsRequest
        {
            TeamIds = new List<Guid> { teamInOrg2.Id }
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Uma ou mais equipes são inválidas ou pertencem a outra organização.");
    }

    [Fact]
    public async Task UpdateTeamsAsync_WithEmptyList_RemovesAllTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, team) = await CreateTestHierarchy(context);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        context.CollaboratorTeams.Add(new CollaboratorTeam { CollaboratorId = collaborator.Id, TeamId = team.Id });
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.UpdateTeamsAsync(collaborator.Id, new UpdateCollaboratorTeamsRequest
        {
            TeamIds = new List<Guid>()
        });

        // Assert
        result.IsSuccess.Should().BeTrue();

        var teams = await context.CollaboratorTeams
            .Where(ct => ct.CollaboratorId == collaborator.Id)
            .ToListAsync();
        teams.Should().BeEmpty();
    }

    #endregion

    #region GetSubordinatesAsync Tests

    [Fact]
    public async Task GetSubordinatesAsync_WithNonExistentCollaborator_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetSubordinatesAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Colaborador não encontrado.");
    }

    [Fact]
    public async Task GetSubordinatesAsync_WithNoSubordinates_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, _, _) = await CreateTestHierarchy(context);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader One",
            Email = "leader@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetSubordinatesAsync(leader.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubordinatesAsync_WithDirectSubordinates_ReturnsOneLevel()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, _, _) = await CreateTestHierarchy(context);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader One",
            Email = "leader@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        var sub1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Alice Sub",
            Email = "alice@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            LeaderId = leader.Id
        };

        var sub2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Bob Sub",
            Email = "bob@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            LeaderId = leader.Id
        };

        context.Collaborators.AddRange(leader, sub1, sub2);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetSubordinatesAsync(leader.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].FullName.Should().Be("Alice Sub");
        result.Value![1].FullName.Should().Be("Bob Sub");
        result.Value![0].Children.Should().BeEmpty();
        result.Value![1].Children.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubordinatesAsync_WithRecursiveSubordinates_ReturnsMultipleLevels()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, _, _) = await CreateTestHierarchy(context);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Top Leader",
            Email = "top@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        var midLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Mid Leader",
            Email = "mid@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            LeaderId = leader.Id
        };

        var contributor = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Contributor",
            Email = "contrib@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            LeaderId = midLeader.Id
        };

        context.Collaborators.AddRange(leader, midLeader, contributor);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetSubordinatesAsync(leader.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].FullName.Should().Be("Mid Leader");
        result.Value![0].Children.Should().HaveCount(1);
        result.Value![0].Children[0].FullName.Should().Be("Contributor");
        result.Value![0].Children[0].Children.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubordinatesAsync_WithMaxDepthReached_StopsRecursion()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, _, _) = await CreateTestHierarchy(context);

        // Create a chain of 4 levels: L1 -> L2 -> L3 -> L4
        var l1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Level 1",
            Email = "l1@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        var l2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Level 2",
            Email = "l2@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            LeaderId = l1.Id
        };

        var l3 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Level 3",
            Email = "l3@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            LeaderId = l2.Id
        };

        var l4 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Level 4",
            Email = "l4@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            LeaderId = l3.Id
        };

        context.Collaborators.AddRange(l1, l2, l3, l4);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act - maxDepth 2 means only 2 levels of children
        var result = await service.GetSubordinatesAsync(l1.Id, maxDepth: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1); // L2
        result.Value![0].FullName.Should().Be("Level 2");
        result.Value![0].Children.Should().HaveCount(1); // L3
        result.Value![0].Children[0].FullName.Should().Be("Level 3");
        result.Value![0].Children[0].Children.Should().BeEmpty(); // L4 is cut off at maxDepth
    }

    [Fact]
    public async Task GetSubordinatesAsync_ReturnsChildrenOrderedByName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, _, _) = await CreateTestHierarchy(context);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader",
            Email = "leader@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        var zara = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Zara Last",
            Email = "zara@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            LeaderId = leader.Id
        };

        var alice = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Alice First",
            Email = "alice@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            LeaderId = leader.Id
        };

        context.Collaborators.AddRange(leader, zara, alice);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetSubordinatesAsync(leader.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].FullName.Should().Be("Alice First");
        result.Value![1].FullName.Should().Be("Zara Last");
    }

    #endregion

    #region GetAvailableTeamsAsync Tests

    [Fact]
    public async Task GetAvailableTeamsAsync_ExcludesAlreadyAssignedTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, team) = await CreateTestHierarchy(context);

        var team2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team 2",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };
        context.Teams.Add(team2);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);

        // Collaborator is already in team1
        context.CollaboratorTeams.Add(new CollaboratorTeam { CollaboratorId = collaborator.Id, TeamId = team.Id });
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetAvailableTeamsAsync(collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().Contain(t => t.Id == team2.Id);
        result.Value.Should().NotContain(t => t.Id == team.Id);
    }

    [Fact]
    public async Task GetAvailableTeamsAsync_WithSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, _) = await CreateTestHierarchy(context);

        var alphaTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Alpha Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };
        var betaTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Beta Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id
        };
        context.Teams.AddRange(alphaTeam, betaTeam);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetAvailableTeamsAsync(collaborator.Id, "alpha");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var teams = result.Value!;
        teams.Should().HaveCount(1);
        teams[0].Name.Should().Be("Alpha Team");
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersByNameAndEmail()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, _, _) = await CreateTestHierarchy(context);

        context.Collaborators.AddRange(
            new Collaborator
            {
                Id = Guid.NewGuid(),
                FullName = "ALICE Johnson",
                Email = "alice@example.com",
                OrganizationId = org.Id
            },
            new Collaborator
            {
                Id = Guid.NewGuid(),
                FullName = "Bob Smith",
                Email = "bob@example.com",
                OrganizationId = org.Id
            });
        await context.SaveChangesAsync();

        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var byName = await service.GetAllAsync(null, "alice", 1, 10);
        var byEmail = await service.GetAllAsync(null, "ALICE@EXAMPLE.COM", 1, 10);

        // Assert
        byName.IsSuccess.Should().BeTrue();
        byName.Value!.Items.Should().HaveCount(1);
        byName.Value.Items[0].FullName.Should().Be("ALICE Johnson");

        byEmail.IsSuccess.Should().BeTrue();
        byEmail.Value!.Items.Should().HaveCount(1);
        byEmail.Value.Items[0].Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GetAvailableTeamsAsync_WithNonExistentCollaborator_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CollaboratorService(context, _tenantProvider);

        // Act
        var result = await service.GetAvailableTeamsAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Colaborador não encontrado.");
    }

    #endregion
}
