using Bud.Infrastructure.Persistence;
using Bud.Infrastructure.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Infrastructure.UnitTests.Infrastructure.Repositories;

public class DashboardReadStoreTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    [Fact]
    public async Task GetMyDashboardAsync_CollaboratorNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(Guid.NewGuid(), null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMyDashboardAsync_CollaboratorWithLeader_ReturnsLeaderInfo()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var team = new Team { Id = Guid.NewGuid(), Name = "Diretoria de RH", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Maria Silva",
            Email = "maria@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Joao Santos",
            Email = "joao@test.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            LeaderId = leader.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.AddRange(leader, collaborator);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.Leader.Should().NotBeNull();
        result.TeamHealth.Leader!.FullName.Should().Be("Maria Silva");
        result.TeamHealth.Leader.Initials.Should().Be("MS");
        result.TeamHealth.Leader.TeamName.Should().Be("Diretoria de RH");
    }

    [Fact]
    public async Task GetMyDashboardAsync_CollaboratorIsLeader_ReturnsSelfAsLeader()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Maria Silva",
            Email = "maria@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        var team = new Team { Id = Guid.NewGuid(), Name = "Engenharia", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = leader.Id };
        leader.TeamId = team.Id;

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(leader);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(leader.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.Leader.Should().NotBeNull();
        result.TeamHealth.Leader!.Id.Should().Be(leader.Id);
        result.TeamHealth.Leader.TeamName.Should().Be("Engenharia");
    }

    [Fact]
    public async Task GetMyDashboardAsync_LeaderWithoutTeam_ReturnsEmptyTeamName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Carlos Souza",
            Email = "carlos@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(leader.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.Leader.Should().NotBeNull();
        result.TeamHealth.Leader!.TeamName.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyDashboardAsync_CollaboratorWithNoLeader_ReturnsNullLeader()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Joao Santos",
            Email = "joao@test.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.Leader.Should().BeNull();
    }

    [Fact]
    public async Task GetMyDashboardAsync_LeaderWithTeamMembers_ReturnsTeamMembersFromCollaboratorTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Maria Silva",
            Email = "maria@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        var team = new Team { Id = Guid.NewGuid(), Name = "Engenharia", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = leader.Id };
        leader.TeamId = team.Id;
        var member1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Ana Lima",
            Email = "ana@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        var member2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Carlos Souza",
            Email = "carlos@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(leader);
        context.Teams.Add(team);
        context.Collaborators.AddRange(member1, member2);
        context.Set<CollaboratorTeam>().AddRange(
            new CollaboratorTeam { CollaboratorId = member1.Id, TeamId = team.Id },
            new CollaboratorTeam { CollaboratorId = member2.Id, TeamId = team.Id }
        );
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(leader.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.TeamMembers.Should().HaveCount(2);
        var memberNames = result.TeamHealth.TeamMembers.Select(m => m.FullName).ToList();
        memberNames.Should().Contain("Ana Lima");
        memberNames.Should().Contain("Carlos Souza");
    }

    [Fact]
    public async Task GetMyDashboardAsync_CollaboratorWithPrimaryTeam_ReturnsTeamMembersFromCollaboratorTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Maria Silva",
            Email = "maria@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        var team = new Team { Id = Guid.NewGuid(), Name = "Engenharia", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = leader.Id };
        leader.TeamId = team.Id;
        var member1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Ana Lima",
            Email = "ana@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        var member2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Carlos Souza",
            Email = "carlos@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(leader);
        context.Teams.Add(team);
        context.Collaborators.AddRange(member1, member2);
        context.Set<CollaboratorTeam>().AddRange(
            new CollaboratorTeam { CollaboratorId = member1.Id, TeamId = team.Id },
            new CollaboratorTeam { CollaboratorId = member2.Id, TeamId = team.Id }
        );
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act — member1 views "Meu time" (no teamId)
        var result = await repository.GetMyDashboardAsync(member1.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.TeamMembers.Should().HaveCount(2);
        var memberNames = result.TeamHealth.TeamMembers.Select(m => m.FullName).ToList();
        memberNames.Should().Contain("Ana Lima");
        memberNames.Should().Contain("Carlos Souza");
    }

    [Fact]
    public async Task GetMyDashboardAsync_WeeklyAccess_CalculatesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader Test",
            Email = "leader@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = leader.Id };
        leader.TeamId = team.Id;
        var member1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "M1 Test",
            Email = "m1@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        var member2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "M2 Test",
            Email = "m2@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(leader);
        context.Teams.Add(team);
        context.Collaborators.AddRange(member1, member2);
        context.Set<CollaboratorTeam>().AddRange(
            new CollaboratorTeam { CollaboratorId = member1.Id, TeamId = team.Id },
            new CollaboratorTeam { CollaboratorId = member2.Id, TeamId = team.Id }
        );

        // Member 1 accessed this week
        context.CollaboratorAccessLogs.Add(new CollaboratorAccessLog
        {
            Id = Guid.NewGuid(),
            CollaboratorId = member1.Id,
            OrganizationId = org.Id,
            AccessedAt = DateTime.UtcNow.AddDays(-1)
        });

        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(leader.Id, null);

        // Assert
        result.Should().NotBeNull();
        // 1 out of 3 (leader + 2 members) accessed = 33%
        result!.TeamHealth.WeeklyAccess.Percentage.Should().Be(33);
    }

    [Fact]
    public async Task GetMyDashboardAsync_EngagementScore_HighLevel()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader Test",
            Email = "leader@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = leader.Id };
        leader.TeamId = team.Id;
        var member = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Member One",
            Email = "member1@test.com",
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(leader);
        context.Teams.Add(team);
        context.Collaborators.Add(member);
        context.Set<CollaboratorTeam>().Add(
            new CollaboratorTeam { CollaboratorId = member.Id, TeamId = team.Id }
        );

        // Access log (100% access)
        context.CollaboratorAccessLogs.Add(new CollaboratorAccessLog
        {
            Id = Guid.NewGuid(),
            CollaboratorId = member.Id,
            OrganizationId = org.Id,
            AccessedAt = DateTime.UtcNow.AddDays(-1)
        });

        // Active mission with recent checkin (100% missions updated)
        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "M1",
            OrganizationId = org.Id,
            CollaboratorId = member.Id,
            Status = GoalStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Goals.Add(mission);

        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "KR1",
            GoalId = mission.Id,
            OrganizationId = org.Id,
            Type = IndicatorType.Quantitative
        };
        context.Indicators.Add(metric);

        // Recent checkin with high confidence
        context.Checkins.Add(new Checkin
        {
            Id = Guid.NewGuid(),
            IndicatorId = metric.Id,
            CollaboratorId = member.Id,
            OrganizationId = org.Id,
            CheckinDate = DateTime.UtcNow.AddDays(-1),
            ConfidenceLevel = 5,
            Value = 80
        });

        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(leader.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.Engagement.Score.Should().BeGreaterThanOrEqualTo(70);
        result.TeamHealth.Engagement.Level.Should().Be("high");
    }

    [Fact]
    public async Task GetMyDashboardAsync_NoTeamMembers_ReturnsZeroIndicators()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Alone Person",
            Email = "alone@test.com",
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.WeeklyAccess.Percentage.Should().Be(0);
        result.TeamHealth.MissionsUpdated.Percentage.Should().Be(0);
        result.TeamHealth.FormsResponded.IsPlaceholder.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyDashboardAsync_PendingTasks_ReturnsOverdueMissions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Task Person",
            Email = "task@test.com",
            OrganizationId = org.Id
        };

        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Overdue Mission",
            OrganizationId = org.Id,
            CollaboratorId = collaborator.Id,
            Status = GoalStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "KR",
            GoalId = mission.Id,
            OrganizationId = org.Id,
            Type = IndicatorType.Quantitative
        };

        // Checkin older than 7 days
        var checkin = new Checkin
        {
            Id = Guid.NewGuid(),
            IndicatorId = metric.Id,
            CollaboratorId = collaborator.Id,
            OrganizationId = org.Id,
            CheckinDate = DateTime.UtcNow.AddDays(-10),
            ConfidenceLevel = 3,
            Value = 50
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        context.Goals.Add(mission);
        context.Indicators.Add(metric);
        context.Checkins.Add(checkin);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.PendingTasks.Should().HaveCount(1);
        result.PendingTasks[0].Title.Should().Be("Overdue Mission");
        result.PendingTasks[0].TaskType.Should().Be("goal_checkin");
    }

    [Fact]
    public async Task GetMyDashboardAsync_RecentCheckin_NoPendingTask()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Updated Person",
            Email = "updated@test.com",
            OrganizationId = org.Id
        };

        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Up to date Mission",
            OrganizationId = org.Id,
            CollaboratorId = collaborator.Id,
            Status = GoalStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "KR",
            GoalId = mission.Id,
            OrganizationId = org.Id,
            Type = IndicatorType.Quantitative
        };

        // Recent checkin (within 7 days)
        var checkin = new Checkin
        {
            Id = Guid.NewGuid(),
            IndicatorId = metric.Id,
            CollaboratorId = collaborator.Id,
            OrganizationId = org.Id,
            CheckinDate = DateTime.UtcNow.AddDays(-2),
            ConfidenceLevel = 4,
            Value = 70
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        context.Goals.Add(mission);
        context.Indicators.Add(metric);
        context.Checkins.Add(checkin);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.PendingTasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyDashboardAsync_WithTeamId_ReturnsTeamMembersFromCollaboratorTeam()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var teamLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Alpha Leader",
            Email = "alpha-leader@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        var team = new Team { Id = Guid.NewGuid(), Name = "Squad Alpha", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = teamLeader.Id };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Viewer User",
            Email = "viewer@test.com",
            OrganizationId = org.Id
        };
        var member1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Alice Team",
            Email = "alice@test.com",
            OrganizationId = org.Id
        };
        var member2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Bob Team",
            Email = "bob@test.com",
            OrganizationId = org.Id
        };
        var outsider = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Charlie Outside",
            Email = "charlie@test.com",
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(teamLeader);
        context.Teams.Add(team);
        context.Collaborators.AddRange(collaborator, member1, member2, outsider);
        context.Set<CollaboratorTeam>().AddRange(
            new CollaboratorTeam { CollaboratorId = member1.Id, TeamId = team.Id },
            new CollaboratorTeam { CollaboratorId = member2.Id, TeamId = team.Id }
        );
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, teamId: team.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.TeamMembers.Should().HaveCount(2);
        var names = result.TeamHealth.TeamMembers.Select(m => m.FullName).ToList();
        names.Should().Contain("Alice Team");
        names.Should().Contain("Bob Team");
        names.Should().NotContain("Charlie Outside");
    }

    [Fact]
    public async Task GetMyDashboardAsync_WithTeamId_ReturnsTeamLeader()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var teamLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Beta Leader",
            Email = "beta-leader@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        var team = new Team { Id = Guid.NewGuid(), Name = "Squad Beta", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = teamLeader.Id };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Viewer User",
            Email = "viewer@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(teamLeader);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        context.Set<CollaboratorTeam>().Add(
            new CollaboratorTeam { CollaboratorId = collaborator.Id, TeamId = team.Id }
        );
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, teamId: team.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.Leader.Should().NotBeNull();
        result.TeamHealth.Leader!.FullName.Should().Be("Beta Leader");
        result.TeamHealth.Leader.TeamName.Should().Be("Squad Beta");
    }

    [Fact]
    public async Task GetMyDashboardAsync_WithTeamId_IndicatorsFromTeamMembers()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var member1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "M1 Test",
            Email = "m1@test.com",
            OrganizationId = org.Id
        };
        var member2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "M2 Test",
            Email = "m2@test.com",
            OrganizationId = org.Id
        };
        var team = new Team { Id = Guid.NewGuid(), Name = "Squad Gamma", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = member1.Id };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.AddRange(member1, member2);
        context.Teams.Add(team);
        context.Set<CollaboratorTeam>().AddRange(
            new CollaboratorTeam { CollaboratorId = member1.Id, TeamId = team.Id },
            new CollaboratorTeam { CollaboratorId = member2.Id, TeamId = team.Id }
        );

        // Only member1 accessed this week
        context.CollaboratorAccessLogs.Add(new CollaboratorAccessLog
        {
            Id = Guid.NewGuid(),
            CollaboratorId = member1.Id,
            OrganizationId = org.Id,
            AccessedAt = DateTime.UtcNow.AddDays(-1)
        });

        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(member1.Id, teamId: team.Id);

        // Assert
        result.Should().NotBeNull();
        // 1 out of 2 team members accessed = 50%
        result!.TeamHealth.WeeklyAccess.Percentage.Should().Be(50);
    }

    [Fact]
    public async Task GetMyDashboardAsync_WithTeamId_PendingTasksRemainPersonal()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "WS", OrganizationId = org.Id };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Task Person",
            Email = "task@test.com",
            OrganizationId = org.Id
        };
        var team = new Team { Id = Guid.NewGuid(), Name = "Squad Delta", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = collaborator.Id };

        var mission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "My Personal Mission",
            OrganizationId = org.Id,
            CollaboratorId = collaborator.Id,
            Status = GoalStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "KR",
            GoalId = mission.Id,
            OrganizationId = org.Id,
            Type = IndicatorType.Quantitative
        };
        var checkin = new Checkin
        {
            Id = Guid.NewGuid(),
            IndicatorId = metric.Id,
            CollaboratorId = collaborator.Id,
            OrganizationId = org.Id,
            CheckinDate = DateTime.UtcNow.AddDays(-10),
            ConfidenceLevel = 3,
            Value = 50
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(collaborator);
        context.Teams.Add(team);
        context.Set<CollaboratorTeam>().Add(
            new CollaboratorTeam { CollaboratorId = collaborator.Id, TeamId = team.Id }
        );
        context.Goals.Add(mission);
        context.Indicators.Add(metric);
        context.Checkins.Add(checkin);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, teamId: team.Id);

        // Assert
        result.Should().NotBeNull();
        // Pending tasks remain personal even when viewing a team
        result!.PendingTasks.Should().HaveCount(1);
        result.PendingTasks[0].Title.Should().Be("My Personal Mission");
    }

    [Fact]
    public async Task GetMyDashboardAsync_WithNonExistentTeamId_ReturnsEmptyTeamHealth()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Alone Person",
            Email = "alone@test.com",
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, teamId: Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.Leader.Should().BeNull();
        result.TeamHealth.TeamMembers.Should().BeEmpty();
        result.TeamHealth.WeeklyAccess.Percentage.Should().Be(0);
    }

    [Fact]
    public async Task GetMyDashboardAsync_EngagementScore_LowLevel()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Low Engagement",
            Email = "low@test.com",
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.TeamHealth.Engagement.Score.Should().BeLessThan(40);
        result.TeamHealth.Engagement.Level.Should().Be("low");
    }

    [Fact]
    public async Task GetMyDashboardAsync_OrgScopedMission_IncludedInPendingTasks()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Admin User",
            Email = "admin@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        // Org-scoped mission (CollaboratorId = null)
        var orgMission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Org Mission",
            OrganizationId = org.Id,
            CollaboratorId = null,
            Status = GoalStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        var metric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "KR",
            GoalId = orgMission.Id,
            OrganizationId = org.Id,
            Type = IndicatorType.Quantitative
        };
        // Old checkin (> 7 days)
        var checkin = new Checkin
        {
            Id = Guid.NewGuid(),
            IndicatorId = metric.Id,
            CollaboratorId = collaborator.Id,
            OrganizationId = org.Id,
            CheckinDate = DateTime.UtcNow.AddDays(-10),
            ConfidenceLevel = 3,
            Value = 50
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        context.Goals.Add(orgMission);
        context.Indicators.Add(metric);
        context.Checkins.Add(checkin);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.PendingTasks.Should().HaveCount(1);
        result.PendingTasks[0].Title.Should().Be("Org Mission");
    }

    [Fact]
    public async Task GetMyDashboardAsync_MissionsUpdated_IncludesOrgScopedMissions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Admin User",
            Email = "admin@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        // Personal mission with recent checkin
        var personalMission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Personal",
            OrganizationId = org.Id,
            CollaboratorId = collaborator.Id,
            Status = GoalStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        var personalMetric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "KR1",
            GoalId = personalMission.Id,
            OrganizationId = org.Id,
            Type = IndicatorType.Quantitative
        };
        context.Checkins.Add(new Checkin
        {
            Id = Guid.NewGuid(),
            IndicatorId = personalMetric.Id,
            CollaboratorId = collaborator.Id,
            OrganizationId = org.Id,
            CheckinDate = DateTime.UtcNow.AddDays(-1),
            ConfidenceLevel = 4,
            Value = 80
        });

        // Org-scoped mission without checkin
        var orgMission = new Goal
        {
            Id = Guid.NewGuid(),
            Name = "Org Mission",
            OrganizationId = org.Id,
            CollaboratorId = null,
            Status = GoalStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        var orgMetric = new Indicator
        {
            Id = Guid.NewGuid(),
            Name = "KR2",
            GoalId = orgMission.Id,
            OrganizationId = org.Id,
            Type = IndicatorType.Quantitative
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        context.Goals.AddRange(personalMission, orgMission);
        context.Indicators.AddRange(personalMetric, orgMetric);
        await context.SaveChangesAsync();

        var repository = new DashboardReadStore(context);

        // Act
        var result = await repository.GetMyDashboardAsync(collaborator.Id, null);

        // Assert
        result.Should().NotBeNull();
        // 1 of 2 missions updated = 50%
        result!.TeamHealth.MissionsUpdated.Percentage.Should().Be(50);
    }
}
