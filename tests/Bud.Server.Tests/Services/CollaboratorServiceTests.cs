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

    private async Task<(Organization org, Workspace workspace, Team team)> CreateTestHierarchy(ApplicationDbContext context)
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
    public async Task CreateCollaborator_AsNonOwner_ReturnsForbidden()
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
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Apenas o proprietário da organização pode criar colaboradores.");
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

    #endregion
}
