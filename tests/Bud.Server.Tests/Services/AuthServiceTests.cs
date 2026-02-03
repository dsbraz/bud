using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Settings;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bud.Server.Tests.Services;

public class AuthServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
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
        var service = new AuthService(context, Options.Create(new GlobalAdminSettings { Email = "admin@getbud.co" }));

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
    public async Task Login_WithNonGetbudGlobalAdminEmail_ReturnsNotFound(string email)
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AuthService(context, Options.Create(new GlobalAdminSettings { Email = "admin@getbud.co" }));

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
        var service = new AuthService(context, Options.Create(new GlobalAdminSettings { Email = "admin@getbud.co" }));

        // Create test hierarchy
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
    public async Task Login_WithNonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AuthService(context, Options.Create(new GlobalAdminSettings { Email = "admin@getbud.co" }));

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
        var service = new AuthService(context, Options.Create(new GlobalAdminSettings { Email = "admin@getbud.co" }));

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
        var service = new AuthService(context, Options.Create(new GlobalAdminSettings { Email = "admin@getbud.co" }));

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
        var service = new AuthService(context, Options.Create(new GlobalAdminSettings { Email = "admin@getbud.co" }));

        // Create test hierarchy
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
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = expectedEmail, // Stored in lowercase
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id,
            TeamId = team.Id
        };

        context.Organizations.Add(org);
        context.Workspaces.Add(workspace);
        context.Teams.Add(team);
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

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
}
