using Bud.Mcp.Http;
using Bud.Mcp.Tools;

namespace Bud.Mcp.Auth;

public sealed class McpSessionContext(
    string sessionId,
    HttpClient httpClient,
    BudApiSession session,
    BudApiClient apiClient,
    McpToolService toolService) : IDisposable
{
    public string SessionId { get; } = sessionId;
    public HttpClient HttpClient { get; } = httpClient;
    public BudApiSession SessionResponse { get; } = session;
    public BudApiClient ApiClient { get; } = apiClient;
    public McpToolService ToolService { get; } = toolService;

    public void Dispose()
    {
        HttpClient.Dispose();
    }
}
