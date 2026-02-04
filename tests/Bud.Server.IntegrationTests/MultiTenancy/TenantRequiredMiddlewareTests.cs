using System.Net;
using FluentAssertions;
using Xunit;

namespace Bud.Server.IntegrationTests.MultiTenancy;

public class TenantRequiredMiddlewareTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TenantRequiredMiddlewareTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RequestWithoutTenant_ForNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateUserClientWithoutTenant("user-no-tenant@test.com");

        // Act
        var response = await client.GetAsync("/api/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
