using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class TeamServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<(Organization org, Workspace workspace, Collaborator leader)> CreateTestHierarchy(ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            OrganizationId = org.Id
        };
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader User",
            Email = "leader@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        return (org, workspace, leader);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateTeam_WithNonExistentWorkspace_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);

        var request = new CreateTeamRequest
        {
            Name = "Test Team",
            WorkspaceId = Guid.NewGuid(),
            LeaderId = Guid.NewGuid()
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
    }

    [Fact]
    public async Task CreateTeam_WithNonExistentParentTeam_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (_, workspace, leader) = await CreateTestHierarchy(context);

        var request = new CreateTeamRequest
        {
            Name = "Sub Team",
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id,
            ParentTeamId = Guid.NewGuid()
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Time pai não encontrado.");
    }

    [Fact]
    public async Task CreateTeam_WithParentTeamInDifferentWorkspace_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);

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
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader",
            Email = "leader@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.AddRange(workspace1, workspace2);
        context.Collaborators.Add(leader);

        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace1.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(parentTeam);
        await context.SaveChangesAsync();

        var request = new CreateTeamRequest
        {
            Name = "Child Team",
            WorkspaceId = workspace2.Id,
            LeaderId = leader.Id,
            ParentTeamId = parentTeam.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("O time pai deve pertencer ao mesmo workspace.");
    }

    [Fact]
    public async Task CreateTeam_WithValidParentTeam_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(parentTeam);
        await context.SaveChangesAsync();

        var request = new CreateTeamRequest
        {
            Name = "Child Team",
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id,
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
        result.Value!.LeaderId.Should().Be(leader.Id);
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
            Role = CollaboratorRole.Leader,
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
        context.Workspaces.Add(workspace);
        context.Collaborators.AddRange(owner, regularCollaborator);
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        var request = new CreateTeamRequest
        {
            Name = "New Team",
            WorkspaceId = workspace.Id,
            LeaderId = owner.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
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
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        org.OwnerId = owner.Id;

        _tenantProvider.IsGlobalAdmin = false;
        _tenantProvider.TenantId = org.Id;
        _tenantProvider.CollaboratorId = owner.Id;

        using var context = CreateInMemoryContext();
        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Collaborators.Add(owner);
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        var request = new CreateTeamRequest
        {
            Name = "New Team",
            WorkspaceId = workspace.Id,
            LeaderId = owner.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Team");
    }

    [Fact]
    public async Task CreateTeam_WithNonExistentLeader_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (_, workspace, _) = await CreateTestHierarchy(context);

        var request = new CreateTeamRequest
        {
            Name = "Test Team",
            WorkspaceId = workspace.Id,
            LeaderId = Guid.NewGuid()
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Líder não encontrado.");
    }

    [Fact]
    public async Task CreateTeam_WithNonLeaderRole_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, _) = await CreateTestHierarchy(context);

        var ic = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "IC User",
            Email = "ic@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id
        };
        context.Collaborators.Add(ic);
        await context.SaveChangesAsync();

        var request = new CreateTeamRequest
        {
            Name = "Test Team",
            WorkspaceId = workspace.Id,
            LeaderId = ic.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("O colaborador selecionado como líder deve ter o perfil de Líder.");
    }

    [Fact]
    public async Task CreateTeam_WithLeaderFromDifferentOrg_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (_, workspace, _) = await CreateTestHierarchy(context);

        var otherOrg = new Organization { Id = Guid.NewGuid(), Name = "Other Org" };
        context.Organizations.Add(otherOrg);
        var otherLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Other Leader",
            Email = "other-leader@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = otherOrg.Id
        };
        context.Collaborators.Add(otherLeader);
        await context.SaveChangesAsync();

        var request = new CreateTeamRequest
        {
            Name = "Test Team",
            WorkspaceId = workspace.Id,
            LeaderId = otherLeader.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("O líder deve pertencer à mesma organização do time.");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateTeam_SettingSelfAsParent_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var request = new UpdateTeamRequest
        {
            Name = "Test Team",
            LeaderId = leader.Id,
            ParentTeamId = team.Id
        };

        // Act
        var result = await service.UpdateAsync(team.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Um time não pode ser seu próprio pai.");
    }

    [Fact]
    public async Task UpdateTeam_ChangingParentTeamToInvalidWorkspace_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);

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
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader",
            Email = "leader@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.AddRange(workspace1, workspace2);
        context.Collaborators.Add(leader);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team 1",
            OrganizationId = org.Id,
            WorkspaceId = workspace1.Id,
            LeaderId = leader.Id
        };

        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace2.Id,
            LeaderId = leader.Id
        };

        context.Teams.AddRange(team, parentTeam);
        await context.SaveChangesAsync();

        var request = new UpdateTeamRequest
        {
            Name = "Team 1",
            LeaderId = leader.Id,
            ParentTeamId = parentTeam.Id
        };

        // Act
        var result = await service.UpdateAsync(team.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("O time pai deve pertencer ao mesmo workspace.");
    }

    [Fact]
    public async Task UpdateTeam_WithValidParentTeamUpdate_UpdatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };

        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };

        context.Teams.AddRange(team, parentTeam);
        await context.SaveChangesAsync();

        var request = new UpdateTeamRequest
        {
            Name = "Updated Name",
            LeaderId = leader.Id,
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

    [Fact]
    public async Task UpdateTeam_WithNonExistentLeader_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var request = new UpdateTeamRequest
        {
            Name = "Test Team",
            LeaderId = Guid.NewGuid()
        };

        // Act
        var result = await service.UpdateAsync(team.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Líder não encontrado.");
    }

    [Fact]
    public async Task UpdateTeam_WithNonLeaderRole_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var ic = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "IC User",
            Email = "ic@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id
        };
        context.Collaborators.Add(ic);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var request = new UpdateTeamRequest
        {
            Name = "Test Team",
            LeaderId = ic.Id
        };

        // Act
        var result = await service.UpdateAsync(team.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("O colaborador selecionado como líder deve ter o perfil de Líder.");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteTeam_WithSubTeams_ReturnsConflict()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var parentTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Parent Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };

        var subTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Sub Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            ParentTeamId = parentTeam.Id,
            LeaderId = leader.Id
        };

        context.Teams.AddRange(parentTeam, subTeam);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(parentTeam.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Error.Should().Be("Não é possível excluir um time com sub-times. Exclua os sub-times primeiro.");
    }

    [Fact]
    public async Task DeleteTeam_WithoutSubTeams_DeletesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(team.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deletedTeam = await context.Teams.FindAsync(team.Id);
        deletedTeam.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTeam_WithCollaborators_DeletesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
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

        // Act
        var result = await service.DeleteAsync(team.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deletedTeam = await context.Teams.FindAsync(team.Id);
        deletedTeam.Should().BeNull();
    }

    #endregion

    #region GetCollaboratorSummariesAsync Tests

    [Fact]
    public async Task GetCollaboratorSummariesAsync_WithValidTeam_ReturnsCollaborators()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);

        var collaborator1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User 1",
            Email = "user1@example.com",
            OrganizationId = org.Id
        };
        var collaborator2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User 2",
            Email = "user2@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.AddRange(collaborator1, collaborator2);

        context.CollaboratorTeams.AddRange(
            new CollaboratorTeam { CollaboratorId = collaborator1.Id, TeamId = team.Id },
            new CollaboratorTeam { CollaboratorId = collaborator2.Id, TeamId = team.Id }
        );
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        // Act
        var result = await service.GetCollaboratorSummariesAsync(team.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(c => c.FullName == "User 1");
        result.Value.Should().Contain(c => c.FullName == "User 2");
    }

    [Fact]
    public async Task GetCollaboratorSummariesAsync_WithNonExistentTeam_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);

        // Act
        var result = await service.GetCollaboratorSummariesAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Time não encontrado.");
    }

    [Fact]
    public async Task CreateTeam_ShouldAddLeaderToCollaboratorTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var request = new CreateTeamRequest
        {
            Name = "Test Team",
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var teamId = result.Value!.Id;

        var collaboratorTeams = await context.CollaboratorTeams
            .Where(ct => ct.TeamId == teamId)
            .ToListAsync();
        collaboratorTeams.Should().ContainSingle();
        collaboratorTeams.First().CollaboratorId.Should().Be(leader.Id);
    }

    [Fact]
    public async Task UpdateTeam_WithNewLeader_ShouldAddNewLeaderToCollaboratorTeams()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var newLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "New Leader",
            Email = "new-leader@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id
        };
        context.Collaborators.Add(newLeader);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);

        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = leader.Id,
            TeamId = team.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var request = new UpdateTeamRequest
        {
            Name = "Test Team",
            LeaderId = newLeader.Id
        };

        // Act
        var result = await service.UpdateAsync(team.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var collaboratorTeams = await context.CollaboratorTeams
            .Where(ct => ct.TeamId == team.Id)
            .ToListAsync();
        collaboratorTeams.Should().Contain(ct => ct.CollaboratorId == newLeader.Id);
    }

    [Fact]
    public async Task UpdateTeam_WithExistingLeaderInCollaboratorTeams_ShouldNotDuplicate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);
        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = leader.Id,
            TeamId = team.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var request = new UpdateTeamRequest
        {
            Name = "Updated Name",
            LeaderId = leader.Id
        };

        // Act
        var result = await service.UpdateAsync(team.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var collaboratorTeams = await context.CollaboratorTeams
            .Where(ct => ct.TeamId == team.Id)
            .ToListAsync();
        collaboratorTeams.Should().ContainSingle(ct => ct.CollaboratorId == leader.Id);
    }

    #endregion

    #region UpdateCollaboratorsAsync Tests

    [Fact]
    public async Task UpdateCollaboratorsAsync_WithValidCollaborators_UpdatesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);

        var collaborator1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User 1",
            Email = "user1@example.com",
            OrganizationId = org.Id
        };
        var collaborator2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User 2",
            Email = "user2@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.AddRange(collaborator1, collaborator2);

        context.CollaboratorTeams.Add(new CollaboratorTeam { CollaboratorId = collaborator1.Id, TeamId = team.Id });
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        // Act
        var result = await service.UpdateCollaboratorsAsync(team.Id, new UpdateTeamCollaboratorsRequest
        {
            CollaboratorIds = new List<Guid> { leader.Id, collaborator2.Id }
        });

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedCollaborators = await context.CollaboratorTeams
            .Where(ct => ct.TeamId == team.Id)
            .ToListAsync();
        updatedCollaborators.Should().HaveCount(2);
        updatedCollaborators.Should().Contain(ct => ct.CollaboratorId == leader.Id);
        updatedCollaborators.Should().Contain(ct => ct.CollaboratorId == collaborator2.Id);
    }

    [Fact]
    public async Task UpdateCollaboratorsAsync_WithoutLeaderInList_ShouldReturnValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Regular User",
            Email = "regular@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);

        context.CollaboratorTeams.Add(new CollaboratorTeam
        {
            CollaboratorId = leader.Id,
            TeamId = team.Id,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        // Act: update collaborators WITHOUT including the leader
        var result = await service.UpdateCollaboratorsAsync(team.Id, new UpdateTeamCollaboratorsRequest
        {
            CollaboratorIds = new List<Guid> { collaborator.Id }
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("O líder da equipe deve estar incluído na lista de membros.");
    }

    [Fact]
    public async Task UpdateCollaboratorsAsync_WithLeaderInList_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Regular User",
            Email = "regular@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        // Act: update collaborators WITH the leader included
        var result = await service.UpdateCollaboratorsAsync(team.Id, new UpdateTeamCollaboratorsRequest
        {
            CollaboratorIds = new List<Guid> { leader.Id, collaborator.Id }
        });

        // Assert
        result.IsSuccess.Should().BeTrue();

        var collaboratorTeams = await context.CollaboratorTeams
            .Where(ct => ct.TeamId == team.Id)
            .ToListAsync();
        collaboratorTeams.Should().HaveCount(2);
        collaboratorTeams.Should().Contain(ct => ct.CollaboratorId == leader.Id);
        collaboratorTeams.Should().Contain(ct => ct.CollaboratorId == collaborator.Id);
    }

    [Fact]
    public async Task UpdateCollaboratorsAsync_WithNonExistentTeam_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);

        // Act
        var result = await service.UpdateCollaboratorsAsync(Guid.NewGuid(), new UpdateTeamCollaboratorsRequest
        {
            CollaboratorIds = new List<Guid> { Guid.NewGuid() }
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Time não encontrado.");
    }

    [Fact]
    public async Task UpdateCollaboratorsAsync_WithCollaboratorsFromDifferentOrg_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org1, workspace1, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org1.Id,
            WorkspaceId = workspace1.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);

        var org2 = new Organization { Id = Guid.NewGuid(), Name = "Org 2" };
        context.Organizations.Add(org2);

        var collaboratorInOrg2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User in Org 2",
            Email = "user@org2.com",
            OrganizationId = org2.Id
        };
        context.Collaborators.Add(collaboratorInOrg2);
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        // Act
        var result = await service.UpdateCollaboratorsAsync(team.Id, new UpdateTeamCollaboratorsRequest
        {
            CollaboratorIds = new List<Guid> { leader.Id, collaboratorInOrg2.Id }
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Um ou mais colaboradores são inválidos ou pertencem a outra organização.");
    }

    #endregion

    #region GetAvailableCollaboratorsAsync Tests

    [Fact]
    public async Task GetAvailableCollaboratorsAsync_ExcludesAlreadyAssignedCollaborators()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);

        var collaborator1 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User 1",
            Email = "user1@example.com",
            OrganizationId = org.Id
        };
        var collaborator2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User 2",
            Email = "user2@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.AddRange(collaborator1, collaborator2);

        context.CollaboratorTeams.Add(new CollaboratorTeam { CollaboratorId = collaborator1.Id, TeamId = team.Id });
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        // Act
        var result = await service.GetAvailableCollaboratorsAsync(team.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // leader + collaborator2 are available (collaborator1 is assigned)
        result.Value.Should().Contain(c => c.Id == collaborator2.Id);
        result.Value.Should().NotContain(c => c.Id == collaborator1.Id);
    }

    [Fact]
    public async Task GetAvailableCollaboratorsAsync_WithSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = org.Id,
            WorkspaceId = workspace.Id,
            LeaderId = leader.Id
        };
        context.Teams.Add(team);

        var alice = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Alice Smith",
            Email = "alice@example.com",
            OrganizationId = org.Id
        };
        var bob = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Bob Jones",
            Email = "bob@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.AddRange(alice, bob);
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        // Act
        var result = await service.GetAvailableCollaboratorsAsync(team.Id, "alice");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var collaborators = result.Value!;
        collaborators.Should().HaveCount(1);
        collaborators[0].FullName.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (org, workspace, leader) = await CreateTestHierarchy(context);

        context.Teams.AddRange(
            new Team
            {
                Id = Guid.NewGuid(),
                Name = "ALPHA Team",
                OrganizationId = org.Id,
                WorkspaceId = workspace.Id,
                LeaderId = leader.Id
            },
            new Team
            {
                Id = Guid.NewGuid(),
                Name = "Beta Team",
                OrganizationId = org.Id,
                WorkspaceId = workspace.Id,
                LeaderId = leader.Id
            });
        await context.SaveChangesAsync();

        var service = new TeamService(context);

        // Act
        var result = await service.GetAllAsync(workspace.Id, null, "alpha", 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("ALPHA Team");
    }

    [Fact]
    public async Task GetAvailableCollaboratorsAsync_WithNonExistentTeam_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new TeamService(context);

        // Act
        var result = await service.GetAvailableCollaboratorsAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Time não encontrado.");
    }

    #endregion
}
