using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public sealed class AuthRateLimitingTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Login_ExceedingRateLimit_Returns429()
    {
        // Arrange — override rate limit to a low value for fast testing
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimitSettings:LoginPermitLimit"] = "2",
                    ["RateLimitSettings:LoginWindowSeconds"] = "60"
                });
            });
        }).CreateClient();

        var payload = new AuthLoginRequest { Email = "test@example.com" };

        // Act — send requests up to and beyond the limit
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 4; i++)
        {
            responses.Add(await client.PostAsJsonAsync("/api/auth/login", payload));
        }

        // Assert — first 2 should pass (non-429), remaining should be 429
        responses.Take(2).Should().AllSatisfy(r =>
            r.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests));

        responses.Skip(2).Should().AllSatisfy(r =>
            r.StatusCode.Should().Be(HttpStatusCode.TooManyRequests));
    }
}
