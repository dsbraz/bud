using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Repositories;

public sealed class TeamRepositoryTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<Organization> CreateTestOrganization(ApplicationDbContext context, string name = "Test Org")
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = name };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();
        return org;
    }

    private static async Task<Workspace> CreateTestWorkspace(ApplicationDbContext context, Guid organizationId, string name = "Test Workspace")
    {
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = name, OrganizationId = organizationId };
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();
        return workspace;
    }

    private static async Task<Collaborator> CreateTestCollaborator(
        ApplicationDbContext context,
        Guid organizationId,
        string fullName = "Test Collaborator",
        string email = "test@example.com",
        CollaboratorRole role = CollaboratorRole.Leader)
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Role = role,
            OrganizationId = organizationId
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();
        return collaborator;
    }

    private static async Task<Team> CreateTestTeam(
        ApplicationDbContext context,
        Guid organizationId,
        Guid workspaceId,
        Guid leaderId,
        string name = "Test Team",
        Guid? parentTeamId = null)
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrganizationId = organizationId,
            WorkspaceId = workspaceId,
            LeaderId = leaderId,
            ParentTeamId = parentTeamId
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        return team;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenTeamExists_ReturnsTeam()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        // Act
        var result = await repository.GetByIdAsync(team.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(team.Id);
        result.Name.Should().Be("Test Team");
    }

    [Fact]
    public async Task GetByIdAsync_WhenTeamNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByIdWithCollaboratorTeamsAsync Tests

    [Fact]
    public async Task GetByIdWithCollaboratorTeamsAsync_WhenExists_ReturnsTeamWithCollaboratorTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        var member = await CreateTestCollaborator(context, org.Id, "Member", "member@test.com", CollaboratorRole.IndividualContributor);
        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = member.Id,
            TeamId = team.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdWithCollaboratorTeamsAsync(team.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(team.Id);
        result.CollaboratorTeams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdWithCollaboratorTeamsAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);

        // Act
        var result = await repository.GetByIdWithCollaboratorTeamsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);

        for (int i = 0; i < 5; i++)
        {
            await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, $"Team {i:D2}");
        }

        // Act
        var result = await repository.GetAllAsync(null, null, null, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByWorkspaceId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace1 = await CreateTestWorkspace(context, org.Id, "WS 1");
        var workspace2 = await CreateTestWorkspace(context, org.Id, "WS 2");
        var leader = await CreateTestCollaborator(context, org.Id);

        await CreateTestTeam(context, org.Id, workspace1.Id, leader.Id, "Team WS1");
        await CreateTestTeam(context, org.Id, workspace2.Id, leader.Id, "Team WS2");

        // Act
        var result = await repository.GetAllAsync(workspace1.Id, null, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Team WS1");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByParentTeamId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);

        var parentTeam = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Parent Team");
        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Child Team", parentTeam.Id);
        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Root Team");

        // Act
        var result = await repository.GetAllAsync(null, parentTeam.Id, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Child Team");
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);

        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "ALPHA Team");
        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Beta Team");

        // Act
        var result = await repository.GetAllAsync(null, null, "alpha", 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("ALPHA Team");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);

        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Zebra");
        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Alpha");
        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Mango");

        // Act
        var result = await repository.GetAllAsync(null, null, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Alpha");
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].Name.Should().Be("Zebra");
    }

    #endregion

    #region GetSubTeamsAsync Tests

    [Fact]
    public async Task GetSubTeamsAsync_ReturnsSubTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);

        var parentTeam = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Parent");
        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Sub A", parentTeam.Id);
        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Sub B", parentTeam.Id);
        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Other Root");

        // Act
        var result = await repository.GetSubTeamsAsync(parentTeam.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetSubTeamsAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);

        var parentTeam = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Parent");
        for (int i = 0; i < 5; i++)
        {
            await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, $"Sub {i:D2}", parentTeam.Id);
        }

        // Act
        var result = await repository.GetSubTeamsAsync(parentTeam.Id, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    #endregion

    #region GetCollaboratorsAsync Tests

    [Fact]
    public async Task GetCollaboratorsAsync_ReturnsCollaboratorsWithPrimaryTeam()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        var member1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Member A",
            Email = "a@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        var member2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Member B",
            Email = "b@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        context.Collaborators.AddRange(member1, member2);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetCollaboratorsAsync(team.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetCollaboratorsAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        for (int i = 0; i < 5; i++)
        {
            context.Collaborators.Add(new Collaborator
            {
                Id = Guid.NewGuid(),
                FullName = $"Member {i:D2}",
                Email = $"member{i}@test.com",
                OrganizationId = org.Id,
                TeamId = team.Id
            });
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetCollaboratorsAsync(team.Id, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    #endregion

    #region GetCollaboratorLookupAsync Tests

    [Fact]
    public async Task GetCollaboratorLookupAsync_ReturnsCollaboratorsViaCollaboratorTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        var member = await CreateTestCollaborator(context, org.Id, "Member", "member@test.com", CollaboratorRole.IndividualContributor);
        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = member.Id,
            TeamId = team.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetCollaboratorLookupAsync(team.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Member");
    }

    [Fact]
    public async Task GetCollaboratorLookupAsync_WhenNoMembers_ReturnsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        // Act
        var result = await repository.GetCollaboratorLookupAsync(team.Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetEligibleCollaboratorsForAssignmentAsync Tests

    [Fact]
    public async Task GetEligibleCollaboratorsForAssignmentAsync_ExcludesAlreadyAssigned()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        var assigned = await CreateTestCollaborator(context, org.Id, "Assigned", "assigned@test.com", CollaboratorRole.IndividualContributor);
        var eligible = await CreateTestCollaborator(context, org.Id, "Eligible", "eligible@test.com", CollaboratorRole.IndividualContributor);

        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = assigned.Id,
            TeamId = team.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetEligibleCollaboratorsForAssignmentAsync(team.Id, org.Id, null, 10);

        // Assert
        result.Should().Contain(c => c.Id == eligible.Id);
        result.Should().NotContain(c => c.Id == assigned.Id);
    }

    [Fact]
    public async Task GetEligibleCollaboratorsForAssignmentAsync_RespectsLimit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        for (int i = 0; i < 5; i++)
        {
            await CreateTestCollaborator(context, org.Id, $"Eligible {i}", $"e{i}@test.com", CollaboratorRole.IndividualContributor);
        }

        // Act
        var result = await repository.GetEligibleCollaboratorsForAssignmentAsync(team.Id, org.Id, null, 3);

        // Assert
        result.Should().HaveCountLessOrEqualTo(3);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenTeamExists_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        // Act
        var result = await repository.ExistsAsync(team.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenTeamNotFound_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);

        // Act
        var result = await repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasSubTeamsAsync Tests

    [Fact]
    public async Task HasSubTeamsAsync_WhenHasSubTeams_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);

        var parentTeam = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Parent");
        await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Child", parentTeam.Id);

        // Act
        var result = await repository.HasSubTeamsAsync(parentTeam.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSubTeamsAsync_WhenNoSubTeams_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        // Act
        var result = await repository.HasSubTeamsAsync(team.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasMissionsAsync Tests

    [Fact]
    public async Task HasMissionsAsync_WhenHasMissions_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        context.Goals.Add(new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Team Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = GoalStatus.Planned,
            OrganizationId = org.Id,
            TeamId = team.Id
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.HasGoalsAsync(team.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMissionsAsync_WhenNoMissions_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        // Act
        var result = await repository.HasGoalsAsync(team.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AddAsync / RemoveAsync / SaveChangesAsync Tests

    [Fact]
    public async Task AddAsync_PersistsTeam()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "New Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };

        // Act
        await repository.AddAsync(team);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Teams.FindAsync(team.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("New Team");
    }

    [Fact]
    public async Task RemoveAsync_DeletesTeam()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TeamRepository(context);
        var org = await CreateTestOrganization(context);
        var workspace = await CreateTestWorkspace(context, org.Id);
        var leader = await CreateTestCollaborator(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "To Delete");

        // Re-fetch tracked entity
        var tracked = await context.Teams.FirstAsync(t => t.Id == team.Id);

        // Act
        await repository.RemoveAsync(tracked);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Teams.FindAsync(team.Id);
        persisted.Should().BeNull();
    }

    #endregion
}
