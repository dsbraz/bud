using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Settings;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bud.Server.Tests.Services;

public class OrganizationServiceTests
{
    private static ApplicationDbContext CreateInMemoryContext(TestTenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, tenantProvider);
    }

    private static OrganizationService CreateService(ApplicationDbContext context)
    {
        var settings = Options.Create(new GlobalAdminSettings
        {
            Email = "admin@getbud.co",
            OrganizationName = "getbud.co"
        });

        return new OrganizationService(context, settings);
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ReturnsNotFoundInPortuguese()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task GetWorkspaces_WithNonExistingOrganization_ReturnsNotFoundInPortuguese()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        // Act
        var result = await service.GetWorkspacesAsync(Guid.NewGuid(), 1, 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_ReturnsFilteredOrganizations()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        var owner = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = "owner@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = Guid.NewGuid()
        };
        var org1 = new Organization { Id = Guid.NewGuid(), Name = "ALPHA Corp", OwnerId = owner.Id };
        var org2 = new Organization { Id = Guid.NewGuid(), Name = "Beta Corp", OwnerId = owner.Id };
        context.Collaborators.Add(owner);
        context.Organizations.AddRange(org1, org2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync("alpha", 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("ALPHA Corp");
    }
}
