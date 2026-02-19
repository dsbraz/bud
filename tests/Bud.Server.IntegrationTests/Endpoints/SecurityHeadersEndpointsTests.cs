using FluentAssertions;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public sealed class SecurityHeadersEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task HealthEndpoint_ReturnsSecurityHeaders()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.GetValues("Referrer-Policy").Should().Contain("strict-origin-when-cross-origin");

        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.GetValues("X-XSS-Protection").Should().Contain("0");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsCspWithWasmSupport()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.Content.Headers.Should().ContainKey("Content-Security-Policy");
        var csp = response.Content.Headers.GetValues("Content-Security-Policy").First();
        csp.Should().Contain("'wasm-unsafe-eval'");
        csp.Should().Contain("frame-ancestors 'none'");
    }
}
