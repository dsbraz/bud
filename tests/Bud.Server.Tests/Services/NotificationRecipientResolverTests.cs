using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class NotificationRecipientResolverTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<(Organization org, Workspace workspace, Team team, Collaborator c1, Collaborator c2, Collaborator leader)> CreateTestHierarchy(ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Test Workspace", OrganizationId = org.Id };
        var team = new Team { Id = Guid.NewGuid(), Name = "Test Team", WorkspaceId = workspace.Id, OrganizationId = org.Id, LeaderId = Guid.NewGuid() };
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader",
            Email = $"leader-{Guid.NewGuid()}@example.com",
            OrganizationId = org.Id,
            Role = CollaboratorRole.Leader
        };
        var c1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Collab 1",
            Email = $"c1-{Guid.NewGuid()}@example.com",
            OrganizationId = org.Id,
            LeaderId = leader.Id
        };
        var c2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Collab 2",
            Email = $"c2-{Guid.NewGuid()}@example.com",
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.AddRange(leader, c1, c2);
        context.CollaboratorTeams.AddRange(
            new CollaboratorTeam { CollaboratorId = c1.Id, TeamId = team.Id },
            new CollaboratorTeam { CollaboratorId = c2.Id, TeamId = team.Id }
        );
        await context.SaveChangesAsync();

        return (org, workspace, team, c1, c2, leader);
    }

    [Fact]
    public async Task ResolveMissionRecipients_TeamScope_ReturnsTeamCollaborators()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var resolver = new NotificationRecipientResolver(context);
        var (org, _, team, c1, c2, _) = await CreateTestHierarchy(context);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Team Mission",
            OrganizationId = org.Id,
            TeamId = team.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var recipients = await resolver.ResolveMissionRecipientsAsync(mission.Id, org.Id);

        // Assert
        recipients.Should().Contain(c1.Id);
        recipients.Should().Contain(c2.Id);
    }

    [Fact]
    public async Task ResolveMissionRecipients_CollaboratorScope_ReturnsCollaboratorAndLeader()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var resolver = new NotificationRecipientResolver(context);
        var (org, _, _, c1, _, leader) = await CreateTestHierarchy(context);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Collaborator Mission",
            OrganizationId = org.Id,
            CollaboratorId = c1.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var recipients = await resolver.ResolveMissionRecipientsAsync(mission.Id, org.Id);

        // Assert
        recipients.Should().Contain(c1.Id);
        recipients.Should().Contain(leader.Id);
    }

    [Fact]
    public async Task ResolveMissionRecipients_OrganizationScope_ReturnsAllOrgCollaborators()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var resolver = new NotificationRecipientResolver(context);
        var (org, _, _, c1, c2, leader) = await CreateTestHierarchy(context);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Org Mission",
            OrganizationId = org.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var recipients = await resolver.ResolveMissionRecipientsAsync(mission.Id, org.Id);

        // Assert
        recipients.Should().Contain(c1.Id);
        recipients.Should().Contain(c2.Id);
        recipients.Should().Contain(leader.Id);
    }

    [Fact]
    public async Task ResolveMissionRecipients_WorkspaceScope_ReturnsWorkspaceCollaborators()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var resolver = new NotificationRecipientResolver(context);
        var (org, workspace, _, c1, c2, _) = await CreateTestHierarchy(context);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Workspace Mission",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var recipients = await resolver.ResolveMissionRecipientsAsync(mission.Id, org.Id);

        // Assert
        recipients.Should().Contain(c1.Id);
        recipients.Should().Contain(c2.Id);
    }

    [Fact]
    public async Task ResolveMissionRecipients_ExcludesSpecifiedCollaborator()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var resolver = new NotificationRecipientResolver(context);
        var (org, _, team, c1, c2, _) = await CreateTestHierarchy(context);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Team Mission",
            OrganizationId = org.Id,
            TeamId = team.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        // Act
        var recipients = await resolver.ResolveMissionRecipientsAsync(mission.Id, org.Id, excludeCollaboratorId: c1.Id);

        // Assert
        recipients.Should().NotContain(c1.Id);
        recipients.Should().Contain(c2.Id);
    }

    [Fact]
    public async Task ResolveMissionRecipients_MissionNotFound_ReturnsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var resolver = new NotificationRecipientResolver(context);

        // Act
        var recipients = await resolver.ResolveMissionRecipientsAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        recipients.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveMissionIdFromMetric_ExistingMetric_ReturnsMissionId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var resolver = new NotificationRecipientResolver(context);
        var (org, _, _, _, _, _) = await CreateTestHierarchy(context);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            OrganizationId = org.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Missions.Add(mission);

        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            MissionId = mission.Id,
            OrganizationId = org.Id,
            Name = "Test Metric",
            Type = MetricType.Quantitative
        };
        context.MissionMetrics.Add(metric);
        await context.SaveChangesAsync();

        // Act
        var result = await resolver.ResolveMissionIdFromMetricAsync(metric.Id);

        // Assert
        result.Should().Be(mission.Id);
    }

    [Fact]
    public async Task ResolveMissionIdFromMetric_NonExistentMetric_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var resolver = new NotificationRecipientResolver(context);

        // Act
        var result = await resolver.ResolveMissionIdFromMetricAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }
}
