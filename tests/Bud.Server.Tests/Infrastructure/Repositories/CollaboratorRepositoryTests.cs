using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Repositories;

public sealed class CollaboratorRepositoryTests
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

    private static Collaborator CreateTestCollaborator(
        Guid organizationId,
        string fullName = "Test Collaborator",
        string email = "test@example.com",
        CollaboratorRole role = CollaboratorRole.IndividualContributor,
        Guid? leaderId = null)
    {
        return new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Role = role,
            OrganizationId = organizationId,
            LeaderId = leaderId
        };
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenCollaboratorExists_ReturnsCollaborator()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(collaborator.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(collaborator.Id);
        result.FullName.Should().Be("Test Collaborator");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCollaboratorNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByIdWithCollaboratorTeamsAsync Tests

    [Fact]
    public async Task GetByIdWithCollaboratorTeamsAsync_WhenExists_ReturnsCollaboratorWithTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = CreateTestCollaborator(org.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var workspace = await CreateTestWorkspace(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        var collaborator = CreateTestCollaborator(org.Id, "Member", "member@test.com");
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = collaborator.Id,
            TeamId = team.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdWithCollaboratorTeamsAsync(collaborator.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(collaborator.Id);
        result.CollaboratorTeams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdWithCollaboratorTeamsAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);

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
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        for (int i = 0; i < 5; i++)
        {
            context.Collaborators.Add(CreateTestCollaborator(org.Id, $"Collaborator {i:D2}", $"collab{i}@test.com"));
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(null, null, 1, 2);

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
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        context.Collaborators.AddRange(
            CreateTestCollaborator(org.Id, "ALICE Smith", "alice@test.com"),
            CreateTestCollaborator(org.Id, "Bob Jones", "bob@test.com"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(null, "alice", 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("ALICE Smith");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByTeamId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = CreateTestCollaborator(org.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var workspace = await CreateTestWorkspace(context, org.Id);
        var team = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id);

        var memberA = CreateTestCollaborator(org.Id, "Member A", "a@test.com");
        var memberB = CreateTestCollaborator(org.Id, "Member B", "b@test.com");
        context.Collaborators.AddRange(memberA, memberB);
        await context.SaveChangesAsync();

        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = memberA.Id,
            TeamId = team.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(team.Id, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Member A");
    }

    #endregion

    #region GetLeadersAsync Tests

    [Fact]
    public async Task GetLeadersAsync_ReturnsOnlyLeaders()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        context.Collaborators.AddRange(
            CreateTestCollaborator(org.Id, "Leader One", "leader1@test.com", CollaboratorRole.Leader),
            CreateTestCollaborator(org.Id, "IC One", "ic1@test.com", CollaboratorRole.IndividualContributor));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetLeadersAsync(null);

        // Assert
        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Leader One");
    }

    [Fact]
    public async Task GetLeadersAsync_FiltersByOrganizationId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org1 = await CreateTestOrganization(context, "Org 1");
        var org2 = await CreateTestOrganization(context, "Org 2");

        context.Collaborators.AddRange(
            CreateTestCollaborator(org1.Id, "Leader Org1", "leader1@test.com", CollaboratorRole.Leader),
            CreateTestCollaborator(org2.Id, "Leader Org2", "leader2@test.com", CollaboratorRole.Leader));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetLeadersAsync(org1.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Leader Org1");
    }

    #endregion

    #region GetSubordinatesAsync Tests

    [Fact]
    public async Task GetSubordinatesAsync_ReturnsDirectSubordinates()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = CreateTestCollaborator(org.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var sub1 = CreateTestCollaborator(org.Id, "Sub 1", "sub1@test.com", leaderId: leader.Id);
        var sub2 = CreateTestCollaborator(org.Id, "Sub 2", "sub2@test.com", leaderId: leader.Id);
        context.Collaborators.AddRange(sub1, sub2);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetSubordinatesAsync(leader.Id, 1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSubordinatesAsync_RespectsMaxDepth()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = CreateTestCollaborator(org.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var mid = CreateTestCollaborator(org.Id, "Mid Manager", "mid@test.com", CollaboratorRole.Leader, leader.Id);
        context.Collaborators.Add(mid);
        await context.SaveChangesAsync();

        var deep = CreateTestCollaborator(org.Id, "Deep IC", "deep@test.com", leaderId: mid.Id);
        context.Collaborators.Add(deep);
        await context.SaveChangesAsync();

        // Act - depth 1 should only return direct subordinates
        var result = await repository.GetSubordinatesAsync(leader.Id, 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Mid Manager");
    }

    [Fact]
    public async Task GetSubordinatesAsync_WhenNoSubordinates_ReturnsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetSubordinatesAsync(collaborator.Id, 5);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_ReturnsTeamsForCollaborator()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = CreateTestCollaborator(org.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var workspace = await CreateTestWorkspace(context, org.Id);
        var team1 = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Team Alpha");
        var team2 = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Team Beta");

        var collaborator = CreateTestCollaborator(org.Id, "Member", "member@test.com");
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        context.CollaboratorTeams.AddRange(
            new CollaboratorTeam { CollaboratorId = collaborator.Id, TeamId = team1.Id, AssignedAt = DateTime.UtcNow },
            new CollaboratorTeam { CollaboratorId = collaborator.Id, TeamId = team2.Id, AssignedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTeamsAsync(collaborator.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTeamsAsync_WhenNoTeams_ReturnsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTeamsAsync(collaborator.Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetEligibleTeamsForAssignmentAsync Tests

    [Fact]
    public async Task GetEligibleTeamsForAssignmentAsync_ExcludesAlreadyAssignedTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = CreateTestCollaborator(org.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var workspace = await CreateTestWorkspace(context, org.Id);
        var teamAssigned = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Assigned Team");
        var teamEligible = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Eligible Team");

        var collaborator = CreateTestCollaborator(org.Id, "Member", "member@test.com");
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = collaborator.Id,
            TeamId = teamAssigned.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetEligibleTeamsForAssignmentAsync(collaborator.Id, org.Id, null, 10);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Eligible Team");
    }

    #endregion

    #region GetLookupAsync Tests

    [Fact]
    public async Task GetLookupAsync_ReturnsCollaboratorsUpToLimit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        for (int i = 0; i < 5; i++)
        {
            context.Collaborators.Add(CreateTestCollaborator(org.Id, $"Collab {i:D2}", $"c{i}@test.com"));
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetLookupAsync(null, 3);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetLookupAsync_WithSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        context.Collaborators.AddRange(
            CreateTestCollaborator(org.Id, "Alice Wonder", "alice@test.com"),
            CreateTestCollaborator(org.Id, "Bob Builder", "bob@test.com"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetLookupAsync("alice", 10);

        // Assert
        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Alice Wonder");
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.ExistsAsync(collaborator.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);

        // Act
        var result = await repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsEmailUniqueAsync Tests

    [Fact]
    public async Task IsEmailUniqueAsync_WhenEmailNotUsed_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);

        // Act
        var result = await repository.IsEmailUniqueAsync("unique@test.com", null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailUniqueAsync_WhenEmailUsed_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        context.Collaborators.Add(CreateTestCollaborator(org.Id, "Existing", "taken@test.com"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsEmailUniqueAsync("taken@test.com", null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEmailUniqueAsync_WhenEmailUsedBySameId_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id, "Self", "self@test.com");
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsEmailUniqueAsync("self@test.com", collaborator.Id);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region HasSubordinatesAsync Tests

    [Fact]
    public async Task HasSubordinatesAsync_WhenHasSubordinates_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = CreateTestCollaborator(org.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var sub = CreateTestCollaborator(org.Id, "Sub", "sub@test.com", leaderId: leader.Id);
        context.Collaborators.Add(sub);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.HasSubordinatesAsync(leader.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSubordinatesAsync_WhenNoSubordinates_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.HasSubordinatesAsync(collaborator.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsOrganizationOwnerAsync Tests

    [Fact]
    public async Task IsOrganizationOwnerAsync_WhenIsOwner_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id, "Owner", "owner@test.com");
        context.Collaborators.Add(collaborator);
        org.OwnerId = collaborator.Id;
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsOrganizationOwnerAsync(collaborator.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsOrganizationOwnerAsync_WhenNotOwner_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsOrganizationOwnerAsync(collaborator.Id);

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
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        context.Missions.Add(new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = MissionStatus.Planned,
            OrganizationId = org.Id,
            CollaboratorId = collaborator.Id
        });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.HasMissionsAsync(collaborator.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMissionsAsync_WhenNoMissions_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.HasMissionsAsync(collaborator.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CountTeamsByIdsAndOrganizationAsync Tests

    [Fact]
    public async Task CountTeamsByIdsAndOrganizationAsync_ReturnsCorrectCount()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = CreateTestCollaborator(org.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var workspace = await CreateTestWorkspace(context, org.Id);
        var team1 = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Team 1");
        var team2 = await CreateTestTeam(context, org.Id, workspace.Id, leader.Id, "Team 2");

        // Act
        var result = await repository.CountTeamsByIdsAndOrganizationAsync(
            new List<Guid> { team1.Id, team2.Id, Guid.NewGuid() }, org.Id);

        // Assert
        result.Should().Be(2);
    }

    #endregion

    #region CountByIdsAndOrganizationAsync Tests

    [Fact]
    public async Task CountByIdsAndOrganizationAsync_ReturnsCorrectCount()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var c1 = CreateTestCollaborator(org.Id, "C1", "c1@test.com");
        var c2 = CreateTestCollaborator(org.Id, "C2", "c2@test.com");
        context.Collaborators.AddRange(c1, c2);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.CountByIdsAndOrganizationAsync(
            new List<Guid> { c1.Id, c2.Id, Guid.NewGuid() }, org.Id);

        // Assert
        result.Should().Be(2);
    }

    #endregion

    #region IsValidLeaderAsync Tests

    [Fact]
    public async Task IsValidLeaderAsync_WhenIsLeaderInOrganization_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var leader = CreateTestCollaborator(org.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsValidLeaderAsync(leader.Id, org.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidLeaderAsync_WhenNotLeaderRole_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var ic = CreateTestCollaborator(org.Id, "IC", "ic@test.com", CollaboratorRole.IndividualContributor);
        context.Collaborators.Add(ic);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsValidLeaderAsync(ic.Id, org.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidLeaderAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);

        // Act
        var result = await repository.IsValidLeaderAsync(Guid.NewGuid(), null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidLeaderAsync_WhenLeaderInDifferentOrganization_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org1 = await CreateTestOrganization(context, "Org 1");
        var org2 = await CreateTestOrganization(context, "Org 2");

        var leader = CreateTestCollaborator(org1.Id, "Leader", "leader@test.com", CollaboratorRole.Leader);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsValidLeaderAsync(leader.Id, org2.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AddAsync / RemoveAsync / SaveChangesAsync Tests

    [Fact]
    public async Task AddAsync_PersistsCollaborator()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id, "New Collab", "new@test.com");

        // Act
        await repository.AddAsync(collaborator);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Collaborators.FindAsync(collaborator.Id);
        persisted.Should().NotBeNull();
        persisted!.FullName.Should().Be("New Collab");
    }

    [Fact]
    public async Task RemoveAsync_DeletesCollaborator()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new CollaboratorRepository(context);
        var org = await CreateTestOrganization(context);

        var collaborator = CreateTestCollaborator(org.Id, "To Delete", "delete@test.com");
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Re-fetch tracked entity
        var tracked = await context.Collaborators.FirstAsync(c => c.Id == collaborator.Id);

        // Act
        await repository.RemoveAsync(tracked);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Collaborators.FindAsync(collaborator.Id);
        persisted.Should().BeNull();
    }

    #endregion
}
