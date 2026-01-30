using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class MissionServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    #region ResolveScopeAsync Tests

    [Fact]
    public async Task CreateMission_WithValidOrganizationScope_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMission_WithValidWorkspaceScope_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

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

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Workspace,
            ScopeId = workspace.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMission_WithValidTeamScope_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

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
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Team,
            ScopeId = team.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMission_WithValidCollaboratorScope_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

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
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id
        };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            TeamId = team.Id,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Collaborator,
            ScopeId = collaborator.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMission_WithInvalidScopeId_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = Guid.NewGuid() // Non-existent ID
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    #endregion

    #region ApplyScope Tests

    [Fact]
    public async Task ApplyScope_WithOrganizationScope_SetsOnlyOrganizationId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrganizationId.Should().Be(org.Id);
        result.Value!.WorkspaceId.Should().BeNull();
        result.Value!.TeamId.Should().BeNull();
        result.Value!.CollaboratorId.Should().BeNull();
    }

    [Fact]
    public async Task ApplyScope_WithWorkspaceScope_SetsOnlyWorkspaceId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

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

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Workspace,
            ScopeId = workspace.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrganizationId.Should().Be(org.Id);
        result.Value!.WorkspaceId.Should().Be(workspace.Id);
        result.Value!.TeamId.Should().BeNull();
        result.Value!.CollaboratorId.Should().BeNull();
    }

    [Fact]
    public async Task ApplyScope_WithTeamScope_SetsOnlyTeamId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

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
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Team,
            ScopeId = team.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrganizationId.Should().Be(org.Id);
        result.Value!.WorkspaceId.Should().BeNull();
        result.Value!.TeamId.Should().Be(team.Id);
        result.Value!.CollaboratorId.Should().BeNull();
    }

    [Fact]
    public async Task ApplyScope_WithCollaboratorScope_SetsOnlyCollaboratorId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

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
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id
        };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            TeamId = team.Id,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Collaborator,
            ScopeId = collaborator.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrganizationId.Should().Be(org.Id);
        result.Value!.WorkspaceId.Should().BeNull();
        result.Value!.TeamId.Should().BeNull();
        result.Value!.CollaboratorId.Should().Be(collaborator.Id);
    }

    #endregion

    #region NormalizeToUtc Tests

    [Fact]
    public async Task NormalizeToUtc_WithUtcDateTime_PreservesUtcKind()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var utcDate = DateTime.SpecifyKind(new DateTime(2024, 1, 1, 12, 0, 0), DateTimeKind.Utc);
        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = utcDate,
            EndDate = utcDate.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StartDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task NormalizeToUtc_WithUnspecifiedKind_SpecifiesAsUtc()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var unspecifiedDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = unspecifiedDate,
            EndDate = unspecifiedDate.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StartDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task NormalizeToUtc_WithLocalDateTime_ConvertsToUtc()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var localDate = DateTime.SpecifyKind(new DateTime(2024, 1, 1, 12, 0, 0), DateTimeKind.Local);
        var request = new CreateMissionRequest
        {
            Name = "Test Mission",
            StartDate = localDate,
            EndDate = localDate.AddDays(7),
            Status = MissionStatus.Planned,
            ScopeType = MissionScopeType.Organization,
            ScopeId = org.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StartDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region GetMyMissionsAsync Tests

    [Fact]
    public async Task GetMyMissions_WithValidCollaborator_ReturnsAllHierarchyMissions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

        // Create hierarchy: Organization > Workspace > Team > Collaborator
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
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id
        };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            TeamId = team.Id,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);

        // Create missions at each level
        context.Missions.AddRange(
            new Mission
            {
                Id = Guid.NewGuid(),
                Name = "Org Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                OrganizationId = org.Id
            },
            new Mission
            {
                Id = Guid.NewGuid(),
                Name = "Workspace Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                OrganizationId = org.Id,
                WorkspaceId = workspace.Id
            },
            new Mission
            {
                Id = Guid.NewGuid(),
                Name = "Team Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                OrganizationId = org.Id,
                TeamId = team.Id
            },
            new Mission
            {
                Id = Guid.NewGuid(),
                Name = "Collaborator Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                OrganizationId = org.Id,
                CollaboratorId = collaborator.Id
            }
        );

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMyMissionsAsync(collaborator.Id, null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(4);
        result.Value!.Total.Should().Be(4);
    }

    [Fact]
    public async Task GetMyMissions_WithInvalidCollaboratorId_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

        // Act
        var result = await service.GetMyMissionsAsync(Guid.NewGuid(), null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task GetMyMissions_WithSearchFilter_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

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
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id
        };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            TeamId = team.Id,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);

        context.Missions.AddRange(
            new Mission
            {
                Id = Guid.NewGuid(),
                Name = "Important Mission",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                OrganizationId = org.Id,
                CollaboratorId = collaborator.Id
            },
            new Mission
            {
                Id = Guid.NewGuid(),
                Name = "Regular Task",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = MissionStatus.Planned,
                OrganizationId = org.Id,
                CollaboratorId = collaborator.Id
            }
        );

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMyMissionsAsync(collaborator.Id, "Important", 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value!.Items.First().Name.Should().Be("Important Mission");
    }

    [Fact]
    public async Task GetMyMissions_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionService(context);

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
            WorkspaceId = workspace.Id,
            OrganizationId = org.Id
        };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            TeamId = team.Id,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);

        // Create 15 missions
        for (int i = 1; i <= 15; i++)
        {
            context.Missions.Add(new Mission
            {
                Id = Guid.NewGuid(),
                Name = $"Mission {i}",
                StartDate = DateTime.UtcNow.AddDays(i),
                EndDate = DateTime.UtcNow.AddDays(i + 7),
                Status = MissionStatus.Planned,
                OrganizationId = org.Id,
                CollaboratorId = collaborator.Id
            });
        }

        await context.SaveChangesAsync();

        // Act - Get page 1 with page size 10
        var result = await service.GetMyMissionsAsync(collaborator.Id, null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(10);
        result.Value!.Total.Should().Be(15);
        result.Value!.Page.Should().Be(1);
        result.Value!.PageSize.Should().Be(10);
    }

    #endregion
}
