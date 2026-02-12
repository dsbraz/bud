using System.Text.Json;
using Bud.Mcp.Protocol;
using Bud.Mcp.Tests.Helpers;
using Bud.Mcp.Tools;

namespace Bud.Mcp.Tests.Protocol;

public sealed class McpJsonRpcDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_Initialize_ReturnsSuccessResponse()
    {
        var dispatcher = new McpJsonRpcDispatcher();
        var toolService = CreateToolService();
        using var document = JsonDocument.Parse("""
        {
          "jsonrpc": "2.0",
          "id": 1,
          "method": "initialize"
        }
        """);

        var response = await dispatcher.DispatchAsync(document.RootElement, toolService);

        response.Should().NotBeNull();
        response!["result"]!["protocolVersion"]!.GetValue<string>().Should().Be("2025-06-18");
        response["result"]!["capabilities"]!["prompts"].Should().NotBeNull();
    }

    [Fact]
    public async Task DispatchAsync_PromptsList_ReturnsEmptyList()
    {
        var dispatcher = new McpJsonRpcDispatcher();
        var toolService = CreateToolService();
        using var document = JsonDocument.Parse("""
        {
          "jsonrpc": "2.0",
          "id": 2,
          "method": "prompts/list"
        }
        """);

        var response = await dispatcher.DispatchAsync(document.RootElement, toolService);

        response.Should().NotBeNull();
        response!["result"]!["prompts"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_NotificationWithoutId_ReturnsNull()
    {
        var dispatcher = new McpJsonRpcDispatcher();
        var toolService = CreateToolService();
        using var document = JsonDocument.Parse("""
        {
          "jsonrpc": "2.0",
          "method": "notifications/initialized"
        }
        """);

        var response = await dispatcher.DispatchAsync(document.RootElement, toolService);

        response.Should().BeNull();
    }

    private static McpToolService CreateToolService()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Request inesperada."));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bud.test") };
        var options = new BudMcpOptions("http://bud.test", null, null, 30, 30);
        var session = new BudApiSession(httpClient, options);
        var client = new BudApiClient(httpClient, session);
        return new McpToolService(client, session);
    }
}
