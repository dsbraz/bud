using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Bud.Mcp.Tests.Endpoints;

public sealed class McpHttpEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public McpHttpEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BudMcp:ApiBaseUrl"] = "http://bud.test",
                    ["BudMcp:SessionIdleTtlMinutes"] = "30"
                });
            });
        });
    }

    [Fact]
    public async Task PostMcp_Initialize_ReturnsSessionHeader()
    {
        using var client = _factory.CreateClient();
        var payload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize"
        };

        var response = await client.PostAsJsonAsync("/", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("MCP-Session-Id").Should().BeTrue();
        response.Headers.Contains("X-Mcp-Session-Id").Should().BeTrue();
    }

    [Fact]
    public async Task PostMcp_ToolsListWithoutSession_ReturnsJsonRpcError()
    {
        using var client = _factory.CreateClient();
        var payload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        };

        var response = await client.PostAsJsonAsync("/", payload);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!["error"]!["message"]!.GetValue<string>()
            .Should().Be("Header Mcp-Session-Id é obrigatório para este método.");
    }

    [Fact]
    public async Task PostMcp_ToolsListWithSession_ReturnsTools()
    {
        using var client = _factory.CreateClient();
        var initializePayload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize"
        };
        var initializeResponse = await client.PostAsJsonAsync("/", initializePayload);
        var sessionId = initializeResponse.Headers.GetValues("MCP-Session-Id").Single();

        var listRequest = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = JsonContent.Create(new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/list"
            })
        };
        listRequest.Headers.Add("MCP-Session-Id", sessionId);

        var listResponse = await client.SendAsync(listRequest);
        var body = await listResponse.Content.ReadFromJsonAsync<JsonObject>();

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!["result"]!["tools"]!.AsArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostMcp_ToolsListWithLegacySessionHeader_ReturnsTools()
    {
        using var client = _factory.CreateClient();
        var initializePayload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize"
        };
        var initializeResponse = await client.PostAsJsonAsync("/", initializePayload);
        var sessionId = initializeResponse.Headers.GetValues("MCP-Session-Id").Single();

        var listRequest = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = JsonContent.Create(new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/list"
            })
        };
        listRequest.Headers.Add("X-Mcp-Session-Id", sessionId);

        var listResponse = await client.SendAsync(listRequest);
        var body = await listResponse.Content.ReadFromJsonAsync<JsonObject>();

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!["result"]!["tools"]!.AsArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostMcp_PromptsListWithSession_ReturnsEmptyPrompts()
    {
        using var client = _factory.CreateClient();
        var initializePayload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize"
        };
        var initializeResponse = await client.PostAsJsonAsync("/", initializePayload);
        var sessionId = initializeResponse.Headers.GetValues("MCP-Session-Id").Single();

        var promptsRequest = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = JsonContent.Create(new
            {
                jsonrpc = "2.0",
                id = 3,
                method = "prompts/list"
            })
        };
        promptsRequest.Headers.Add("MCP-Session-Id", sessionId);

        var promptsResponse = await client.SendAsync(promptsRequest);
        var body = await promptsResponse.Content.ReadFromJsonAsync<JsonObject>();

        promptsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!["result"]!["prompts"]!.AsArray().Should().BeEmpty();
    }
}
