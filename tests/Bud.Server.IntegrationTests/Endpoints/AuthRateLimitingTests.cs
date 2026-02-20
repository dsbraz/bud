using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public sealed class AuthRateLimitingTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Login_ExceedingRateLimit_Returns429()
    {
        // Arrange
        var client = factory.CreateClient();

        var payload = new AuthLoginRequest { Email = "admin@getbud.co" };

        // Act — send requests up to and beyond the default limit (10/min)
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 12; i++)
        {
            responses.Add(await client.PostAsJsonAsync("/api/auth/login", payload));
        }

        // Assert — first 10 should pass (non-429), remaining should be 429
        responses.Take(10).Should().AllSatisfy(r =>
            r.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests));

        responses.Skip(10).Should().AllSatisfy(r =>
            r.StatusCode.Should().Be(HttpStatusCode.TooManyRequests));
    }
}
