using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Settings;
using Bud.Server.Tests.Helpers;
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
}
