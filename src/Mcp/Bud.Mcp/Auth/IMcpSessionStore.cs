namespace Bud.Mcp.Auth;

public interface IMcpSessionStore : IDisposable
{
    Task<(McpSessionContext Context, bool Created)> GetOrCreateAsync(string? sessionId, CancellationToken cancellationToken = default);

    Task<McpSessionContext?> GetExistingAsync(string sessionId, CancellationToken cancellationToken = default);
}
