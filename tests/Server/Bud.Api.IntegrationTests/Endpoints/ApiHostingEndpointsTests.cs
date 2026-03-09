using System.Net;
using FluentAssertions;
using Xunit;

namespace Bud.Api.IntegrationTests.Endpoints;

public sealed class ApiHostingEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task RootPath_ReturnsNotFound_WhenApiDoesNotHostSpa()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
