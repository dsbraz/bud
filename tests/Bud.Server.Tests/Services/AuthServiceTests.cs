using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Settings;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bud.Server.Tests.Services;

public class AuthServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private static readonly JwtSettings TestJwtSettings = new()
    {
        Key = "test-secret-key-for-unit-tests-minimum-32-characters",
        Issuer = "bud-test",
        Audience = "bud-test-api"
    };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static AuthService CreateService(ApplicationDbContext context)
    {
        return new AuthService(context, Options.Create(TestJwtSettings));
    }

    #region Global Admin Login Detection Tests

    [Theory]
    [InlineData("admin@getbud.co")]
    [InlineData("ADMIN@GETBUD.CO")]
    [InlineData("Admin@GetBud.Co")]
    public async Task Login_WithGlobalAdminEmail_ReturnsGlobalAdminUser(string email)
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Admin Org" };
        context.Organizations.Add(org);
        var adminCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador Global",
            Email = "admin@getbud.co",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            IsGlobalAdmin = true
        };
        context.Collaborators.Add(adminCollaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new AuthLoginRequest { Email = email };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsGlobalAdmin.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("Administrador Global");
        result.Value!.Email.Should().Be(email.ToLowerInvariant());
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("admin@company.com")]
    [InlineData("admin@test")]
    public async Task Login_WithNonExistentEmail_ReturnsNotFound(string email)
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new AuthLoginRequest { Email = email };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    #endregion

    #region Collaborator Login Tests

    [Fact]
    public async Task Login_WithExistingCollaborator_ReturnsCollaboratorData()
    {
        // Arrange
        using var context = CreateInMemoryContext();

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
            WorkspaceId = workspace.Id,
            LeaderId = Guid.NewGuid()
        };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new AuthLoginRequest { Email = "john.doe@example.com" };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsGlobalAdmin.Should().BeFalse();
        result.Value!.Email.Should().Be("john.doe@example.com");
        result.Value!.DisplayName.Should().Be("John Doe");
        result.Value!.CollaboratorId.Should().Be(collaborator.Id);
        result.Value!.Role.Should().Be(CollaboratorRole.IndividualContributor);
        result.Value!.OrganizationId.Should().Be(org.Id);
    }

    [Fact]
    public async Task Login_WithExistingCollaborator_RegistersAccessLog()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Test Workspace", OrganizationId = org.Id };
        var team = new Team { Id = Guid.NewGuid(), Name = "Test Team", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = "jane.doe@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.LoginAsync(new AuthLoginRequest { Email = collaborator.Email });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var accessLogs = await context.CollaboratorAccessLogs.ToListAsync();
        accessLogs.Should().ContainSingle();
        accessLogs[0].CollaboratorId.Should().Be(collaborator.Id);
        accessLogs[0].OrganizationId.Should().Be(org.Id);
        accessLogs[0].AccessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsNotFoundMessage()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new AuthLoginRequest { Email = "nonexistent@example.com" };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Usuário não encontrado.");
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new AuthLoginRequest { Email = "" };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Informe o e-mail.");
    }

    [Fact]
    public async Task Login_WithWhitespaceEmail_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new AuthLoginRequest { Email = "   " };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Informe o e-mail.");
    }

    #endregion

    #region Email Normalization Tests

    [Theory]
    [InlineData("Test@Example.Com", "test@example.com")]
    [InlineData("USER@DOMAIN.COM", "user@domain.com")]
    [InlineData("MixedCase@Email.Net", "mixedcase@email.net")]
    public async Task Login_WithUpperCaseEmail_NormalizesToLowerCase(string inputEmail, string expectedEmail)
    {
        // Arrange
        using var context = CreateInMemoryContext();

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
            WorkspaceId = workspace.Id,
            LeaderId = Guid.NewGuid()
        };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = expectedEmail,
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new AuthLoginRequest { Email = inputEmail };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(expectedEmail);
        result.Value!.CollaboratorId.Should().Be(collaborator.Id);
    }

    #endregion

    #region Global Admin with Collaborator Tests

    [Fact]
    public async Task Login_WithGlobalAdminEmailAndExistingCollaborator_ReturnsGlobalAdminWithCollaboratorData()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var globalAdminEmail = "admin@getbud.co";

        var org = new Organization { Id = Guid.NewGuid(), Name = "Admin Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Admin Workspace", OrganizationId = org.Id };
        var team = new Team { Id = Guid.NewGuid(), Name = "Admin Team", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };
        var adminCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Admin Collaborator",
            Email = globalAdminEmail,
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            TeamId = team.Id,
            IsGlobalAdmin = true
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(adminCollaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new AuthLoginRequest { Email = globalAdminEmail };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsGlobalAdmin.Should().BeTrue();
        result.Value!.CollaboratorId.Should().Be(adminCollaborator.Id);
        result.Value!.OrganizationId.Should().Be(org.Id);
        result.Value!.DisplayName.Should().Be("Admin Collaborator");
    }

    [Fact]
    public async Task Login_WithGlobalAdminEmailAndExistingCollaborator_RegistersAccessLog()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var globalAdminEmail = "admin@getbud.co";

        var org = new Organization { Id = Guid.NewGuid(), Name = "Admin Org" };
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Admin Workspace", OrganizationId = org.Id };
        var team = new Team { Id = Guid.NewGuid(), Name = "Admin Team", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };
        var adminCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Admin Collaborator",
            Email = globalAdminEmail,
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            TeamId = team.Id,
            IsGlobalAdmin = true
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(adminCollaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.LoginAsync(new AuthLoginRequest { Email = globalAdminEmail });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var accessLogs = await context.CollaboratorAccessLogs.ToListAsync();
        accessLogs.Should().ContainSingle();
        accessLogs[0].CollaboratorId.Should().Be(adminCollaborator.Id);
        accessLogs[0].OrganizationId.Should().Be(org.Id);
    }

    #endregion

    #region IsGlobalAdmin Database Flag Tests

    [Fact]
    public async Task Login_WithIsGlobalAdminTrue_ReturnsGlobalAdmin()
    {
        // Arrange - collaborator with arbitrary email but IsGlobalAdmin = true
        using var context = CreateInMemoryContext();

        var org = new Organization { Id = Guid.NewGuid(), Name = "Any Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Custom Admin",
            Email = "custom.admin@anycompany.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            IsGlobalAdmin = true
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.LoginAsync(new AuthLoginRequest { Email = "custom.admin@anycompany.com" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsGlobalAdmin.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("Custom Admin");
        result.Value!.CollaboratorId.Should().Be(collaborator.Id);
    }

    [Fact]
    public async Task Login_WithIsGlobalAdminFalse_AndAdminEmail_ReturnsRegularUser()
    {
        // Arrange - collaborator with admin@getbud.co email but IsGlobalAdmin = false
        // Proves that the email alone no longer grants admin privileges
        using var context = CreateInMemoryContext();

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Not Actually Admin",
            Email = "admin@getbud.co",
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            IsGlobalAdmin = false
        };

        context.Organizations.Add(org);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.LoginAsync(new AuthLoginRequest { Email = "admin@getbud.co" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsGlobalAdmin.Should().BeFalse();
        result.Value!.DisplayName.Should().Be("Not Actually Admin");
    }

    #endregion

    #region GetMyOrganizations Tests

    [Fact]
    public async Task GetMyOrganizations_WithIsGlobalAdminCollaborator_ReturnsAllOrganizations()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var org1 = new Organization { Id = Guid.NewGuid(), Name = "Org 1" };
        var org2 = new Organization { Id = Guid.NewGuid(), Name = "Org 2" };
        var org3 = new Organization { Id = Guid.NewGuid(), Name = "Org 3" };
        context.Organizations.AddRange(org1, org2, org3);

        var adminCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Global Admin",
            Email = "globaladmin@anycompany.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = org1.Id,
            IsGlobalAdmin = true
        };
        context.Collaborators.Add(adminCollaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetMyOrganizationsAsync("globaladmin@anycompany.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value!.Select(o => o.Name).Should().Contain(["Org 1", "Org 2", "Org 3"]);
    }

    [Fact]
    public async Task GetMyOrganizations_WithRegularUser_ReturnsOnlyMemberOrganizations()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var userEmail = "user@example.com";

        var org1 = new Organization { Id = Guid.NewGuid(), Name = "User Org" };
        var org2 = new Organization { Id = Guid.NewGuid(), Name = "Other Org" };
        context.Organizations.AddRange(org1, org2);

        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Workspace", OrganizationId = org1.Id };
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = org1.Id, WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = userEmail,
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org1.Id,
            TeamId = team.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetMyOrganizationsAsync(userEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].Id.Should().Be(org1.Id);
        result.Value[0].Name.Should().Be("User Org");
    }

    [Fact]
    public async Task GetMyOrganizations_WithOrganizationOwner_ReturnsOwnedOrganizations()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var ownerEmail = "owner@example.com";

        var org = new Organization { Id = Guid.NewGuid(), Name = "Owned Org" };
        context.Organizations.Add(org);

        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Workspace", OrganizationId = org.Id };
        var team = new Team { Id = Guid.NewGuid(), Name = "Team", OrganizationId = org.Id, WorkspaceId = workspace.Id, LeaderId = Guid.NewGuid() };
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);

        var ownerCollaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = ownerEmail,
            Role = CollaboratorRole.Leader,
            OrganizationId = org.Id,
            TeamId = team.Id
        };
        context.Collaborators.Add(ownerCollaborator);

        org.OwnerId = ownerCollaborator.Id;
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetMyOrganizationsAsync(ownerEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].Id.Should().Be(org.Id);
    }

    [Fact]
    public async Task GetMyOrganizations_WithEmptyEmail_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetMyOrganizationsAsync("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("E-mail é obrigatório.");
    }

    [Fact]
    public async Task GetMyOrganizations_WithWhitespaceEmail_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetMyOrganizationsAsync("   ");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("E-mail é obrigatório.");
    }

    #endregion
}
