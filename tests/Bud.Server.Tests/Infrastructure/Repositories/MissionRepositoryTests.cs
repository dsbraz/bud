using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Repositories;

public sealed class MissionRepositoryTests
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

    private static async Task<Team> CreateTestTeam(
        ApplicationDbContext context, Guid organizationId, Guid workspaceId, Guid leaderId, string name = "Test Team")
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrganizationId = organizationId,
            WorkspaceId = workspaceId,
            LeaderId = leaderId
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        return team;
    }

    private static Mission CreateTestMission(
        Guid organizationId,
        string name = "Test Mission",
        MissionStatus status = MissionStatus.Planned)
    {
        return new Mission
        {
            Id = Guid.NewGuid(),
            Name = name,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = status,
            OrganizationId = organizationId
        };
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenMissionExists_ReturnsMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id);
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(mission.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(mission.Id);
        result.Name.Should().Be("Test Mission");
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissionNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByIdReadOnlyAsync Tests

    [Fact]
    public async Task GetByIdReadOnlyAsync_WhenExists_ReturnsMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id, "ReadOnly Mission");
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdReadOnlyAsync(mission.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(mission.Id);
        result.Name.Should().Be("ReadOnly Mission");
    }

    [Fact]
    public async Task GetByIdReadOnlyAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);

        // Act
        var result = await repository.GetByIdReadOnlyAsync(Guid.NewGuid());

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
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        for (int i = 0; i < 5; i++)
        {
            context.Missions.Add(CreateTestMission(org.Id, $"Mission {i:D2}"));
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(null, null, null, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        context.Missions.AddRange(
            CreateTestMission(org.Id, "ALPHA Mission"),
            CreateTestMission(org.Id, "Beta Mission"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(null, null, "alpha", 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("ALPHA Mission");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        context.Missions.AddRange(
            CreateTestMission(org.Id, "Zebra"),
            CreateTestMission(org.Id, "Alpha"),
            CreateTestMission(org.Id, "Mango"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(null, null, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Alpha");
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].Name.Should().Be("Zebra");
    }

    #endregion

    #region GetMyMissionsAsync Tests

    [Fact]
    public async Task GetMyMissionsAsync_ReturnsMissionsForCollaborator()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test",
            Email = "test@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var myMission = CreateTestMission(org.Id, "My Mission");
        myMission.CollaboratorId = collaborator.Id;

        var otherMission = CreateTestMission(org.Id, "Other Mission");
        otherMission.CollaboratorId = Guid.NewGuid();

        context.Missions.AddRange(myMission, otherMission);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetMyMissionsAsync(
            collaborator.Id, org.Id, new List<Guid>(), new List<Guid>(), null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("My Mission");
    }

    [Fact]
    public async Task GetMyMissionsAsync_IncludesTeamMissions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader",
            Email = "leader@test.com",
            OrganizationId = org.Id,
            Role = CollaboratorRole.Leader
        };
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var workspace = await CreateTestWorkspace(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        var teamMission = CreateTestMission(org.Id, "Team Mission");
        teamMission.TeamId = team.Id;
        context.Missions.Add(teamMission);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetMyMissionsAsync(
            leader.Id, org.Id, new List<Guid> { team.Id }, new List<Guid>(), null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Team Mission");
    }

    [Fact]
    public async Task GetMyMissionsAsync_IncludesOrganizationLevelMissions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test",
            Email = "test@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Organization-level mission (no workspace, team, or collaborator scope)
        var orgMission = CreateTestMission(org.Id, "Org Mission");
        context.Missions.Add(orgMission);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetMyMissionsAsync(
            collaborator.Id, org.Id, new List<Guid>(), new List<Guid>(), null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Org Mission");
    }

    [Fact]
    public async Task GetMyMissionsAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test",
            Email = "test@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            var m = CreateTestMission(org.Id, $"Mission {i:D2}");
            m.CollaboratorId = collaborator.Id;
            context.Missions.Add(m);
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetMyMissionsAsync(
            collaborator.Id, org.Id, new List<Guid>(), new List<Guid>(), null, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    #endregion

    #region FindCollaboratorForMyMissionsAsync Tests

    [Fact]
    public async Task FindCollaboratorForMyMissionsAsync_WhenExists_ReturnsCollaborator()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test Collaborator",
            Email = "test@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.FindCollaboratorForMyMissionsAsync(collaborator.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(collaborator.Id);
    }

    [Fact]
    public async Task FindCollaboratorForMyMissionsAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);

        // Act
        var result = await repository.FindCollaboratorForMyMissionsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCollaboratorTeamIdsAsync Tests

    [Fact]
    public async Task GetCollaboratorTeamIdsAsync_ReturnsBothPrimaryAndAdditionalTeamIds()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader",
            Email = "leader@test.com",
            OrganizationId = org.Id,
            Role = CollaboratorRole.Leader
        };
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var workspace = await CreateTestWorkspace(context, org.Id);
        var primaryTeam = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Primary");
        var additionalTeam = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Additional");

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Member",
            Email = "member@test.com",
            OrganizationId = org.Id,
            TeamId = primaryTeam.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = collaborator.Id,
            TeamId = additionalTeam.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetCollaboratorTeamIdsAsync(collaborator.Id, primaryTeam.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(primaryTeam.Id);
        result.Should().Contain(additionalTeam.Id);
    }

    [Fact]
    public async Task GetCollaboratorTeamIdsAsync_WhenNoPrimaryTeam_ReturnsOnlyAdditional()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader",
            Email = "leader@test.com",
            OrganizationId = org.Id,
            Role = CollaboratorRole.Leader
        };
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var workspace = await CreateTestWorkspace(context, org.Id);
        var additionalTeam = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Additional");

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Member",
            Email = "member@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = collaborator.Id,
            TeamId = additionalTeam.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetCollaboratorTeamIdsAsync(collaborator.Id, null);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(additionalTeam.Id);
    }

    #endregion

    #region GetWorkspaceIdsForTeamsAsync Tests

    [Fact]
    public async Task GetWorkspaceIdsForTeamsAsync_ReturnsDistinctWorkspaceIds()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader",
            Email = "leader@test.com",
            OrganizationId = org.Id,
            Role = CollaboratorRole.Leader
        };
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var workspace1 = await CreateTestWorkspace(context, org.Id, "WS 1");
        var workspace2 = await CreateTestWorkspace(context, org.Id, "WS 2");
        var team1 = await CreateTestTeam(context, org.Id, workspace1.Id, leader.Id, "Team 1");
        var team2 = await CreateTestTeam(context, org.Id, workspace1.Id, leader.Id, "Team 2");
        var team3 = await CreateTestTeam(context, org.Id, workspace2.Id, leader.Id, "Team 3");

        // Act
        var result = await repository.GetWorkspaceIdsForTeamsAsync(
            new List<Guid> { team1.Id, team2.Id, team3.Id });

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(workspace1.Id);
        result.Should().Contain(workspace2.Id);
    }

    [Fact]
    public async Task GetWorkspaceIdsForTeamsAsync_WhenEmptyList_ReturnsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);

        // Act
        var result = await repository.GetWorkspaceIdsForTeamsAsync(new List<Guid>());

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetMetricsAsync Tests

    [Fact]
    public async Task GetMetricsAsync_ReturnsMetricsForMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id);
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        context.Metrics.AddRange(
            new Metric
            {
                Id = Guid.NewGuid(),
                Name = "Metric A",
                Type = MetricType.Qualitative,
                MissionId = mission.Id,
                OrganizationId = org.Id
            },
            new Metric
            {
                Id = Guid.NewGuid(),
                Name = "Metric B",
                Type = MetricType.Quantitative,
                MissionId = mission.Id,
                OrganizationId = org.Id
            });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetMetricsAsync(mission.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetMetricsAsync_DoesNotReturnMetricsFromOtherMissions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var mission1 = CreateTestMission(org.Id, "Mission 1");
        var mission2 = CreateTestMission(org.Id, "Mission 2");
        context.Missions.AddRange(mission1, mission2);
        await context.SaveChangesAsync();

        context.Metrics.AddRange(
            new Metric { Id = Guid.NewGuid(), Name = "Metric M1", Type = MetricType.Qualitative, MissionId = mission1.Id, OrganizationId = org.Id },
            new Metric { Id = Guid.NewGuid(), Name = "Metric M2", Type = MetricType.Qualitative, MissionId = mission2.Id, OrganizationId = org.Id });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetMetricsAsync(mission1.Id, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Metric M1");
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id);
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            context.Metrics.Add(new Metric
            {
                Id = Guid.NewGuid(),
                Name = $"Metric {i:D2}",
                Type = MetricType.Qualitative,
                MissionId = mission.Id,
                OrganizationId = org.Id
            });
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetMetricsAsync(mission.Id, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenMissionExists_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id);
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.ExistsAsync(mission.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenMissionNotFound_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);

        // Act
        var result = await repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AddAsync / RemoveAsync / SaveChangesAsync Tests

    [Fact]
    public async Task AddAsync_PersistsMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id, "New Mission");

        // Act
        await repository.AddAsync(mission);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Missions.FindAsync(mission.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("New Mission");
    }

    [Fact]
    public async Task RemoveAsync_DeletesMission()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MissionRepository(context);
        var org = await CreateTestOrganization(context);

        var mission = CreateTestMission(org.Id, "To Delete");
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        // Re-fetch tracked entity
        var tracked = await context.Missions.FirstAsync(m => m.Id == mission.Id);

        // Act
        await repository.RemoveAsync(tracked);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Missions.FindAsync(mission.Id);
        persisted.Should().BeNull();
    }

    #endregion
}
