using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Server.Validators;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class CreateCollaboratorValidatorTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreateCollaboratorValidator _validator;
    private readonly Organization _organization;
    private readonly Workspace _workspace;
    private readonly Team _team;

    public CreateCollaboratorValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var mockTenantProvider = new Mock<ITenantProvider>();
        _validator = new CreateCollaboratorValidator(_context, mockTenantProvider.Object);

        _organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "test.com"
        };

        mockTenantProvider.SetupGet(p => p.TenantId).Returns(_organization.Id);

        _workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            OrganizationId = _organization.Id,
            Visibility = Visibility.Private
        };

        _team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            OrganizationId = _organization.Id,
            WorkspaceId = _workspace.Id
        };

        _context.Organizations.Add(_organization);
        _context.Workspaces.Add(_workspace);
        _context.Teams.Add(_team);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Validate_WithValidData_ShouldPass()
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            TeamId = _team.Id
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyFullName_ShouldFail(string? fullName)
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = fullName!,
            Email = "john.doe@example.com",
            TeamId = _team.Id
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyEmail_ShouldFail(string? email)
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = email!,
            TeamId = _team.Id
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@invalid")]
    [InlineData("invalid.com")]
    public async Task Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = email,
            TeamId = _team.Id
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("E-mail deve ser válido"));
    }

    [Fact]
    public async Task Validate_WithDuplicateEmail_ShouldFail()
    {
        // Arrange
        var existingCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Existing User",
            Email = "existing@example.com",
            OrganizationId = _organization.Id,
            TeamId = _team.Id
        };
        _context.Collaborators.Add(existingCollaborator);
        await _context.SaveChangesAsync();

        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "existing@example.com",
            TeamId = _team.Id
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("E-mail já está em uso"));
    }

    [Fact]
    public async Task Validate_WithValidLeader_ShouldPass()
    {
        // Arrange
        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Team Leader",
            Email = "leader@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = _organization.Id,
            TeamId = _team.Id
        };
        _context.Collaborators.Add(leader);
        await _context.SaveChangesAsync();

        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            TeamId = _team.Id,
            LeaderId = leader.Id
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithNonExistentLeader_ShouldFail()
    {
        // Arrange
        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            TeamId = _team.Id,
            LeaderId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeaderId");
    }

    [Fact]
    public async Task Validate_WithLeaderFromDifferentOrganization_ShouldFail()
    {
        // Arrange
        var otherOrganization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "other.com"
        };
        var otherWorkspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Other Workspace",
            OrganizationId = otherOrganization.Id,
            Visibility = Visibility.Private
        };
        var otherTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Other Team",
            OrganizationId = otherOrganization.Id,
            WorkspaceId = otherWorkspace.Id
        };

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Other Leader",
            Email = "otherleader@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = otherOrganization.Id,
            TeamId = otherTeam.Id
        };

        _context.Organizations.Add(otherOrganization);
        _context.Workspaces.Add(otherWorkspace);
        _context.Teams.Add(otherTeam);
        _context.Collaborators.Add(leader);
        await _context.SaveChangesAsync();

        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            TeamId = _team.Id,
            LeaderId = leader.Id
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeaderId");
    }

    [Fact]
    public async Task Validate_WithLeaderWhoIsNotLeaderRole_ShouldFail()
    {
        // Arrange
        var notALeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Not a Leader",
            Email = "notleader@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = _organization.Id,
            TeamId = _team.Id
        };
        _context.Collaborators.Add(notALeader);
        await _context.SaveChangesAsync();

        var request = new CreateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            TeamId = _team.Id,
            LeaderId = notALeader.Id
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeaderId");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
