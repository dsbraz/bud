using System.Collections.Concurrent;
using Bud.Mcp.Configuration;
using Bud.Mcp.Http;
using Bud.Mcp.Tools;

namespace Bud.Mcp.Auth;

public sealed class InMemoryMcpSessionStore(BudMcpOptions options) : IMcpSessionStore
{
    private readonly BudMcpOptions _options = options;
    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new(StringComparer.Ordinal);
    private readonly TimeSpan _sessionIdleTtl = TimeSpan.FromMinutes(options.SessionIdleTtlMinutes);
    private TimeSpan _clockOffset = TimeSpan.Zero;
    private bool _disposed;

    public async Task<(McpSessionContext Context, bool Created)> GetOrCreateAsync(string? sessionId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        CleanupExpiredSessions();

        if (!string.IsNullOrWhiteSpace(sessionId) && _sessions.TryGetValue(sessionId, out var existing))
        {
            var touched = existing with { LastAccessUtc = UtcNow() };
            _sessions[sessionId] = touched;
            return (touched.Context, Created: false);
        }

        var normalizedSessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString() : sessionId;
        var context = await CreateContextAsync(normalizedSessionId, cancellationToken);
        _sessions[normalizedSessionId] = new SessionEntry(context, UtcNow());
        return (context, Created: true);
    }

    public Task<McpSessionContext?> GetExistingAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        CleanupExpiredSessions();

        if (!_sessions.TryGetValue(sessionId, out var existing))
        {
            return Task.FromResult<McpSessionContext?>(null);
        }

        _sessions[sessionId] = existing with { LastAccessUtc = UtcNow() };
        return Task.FromResult<McpSessionContext?>(existing.Context);
    }

    public void AdvanceClock(TimeSpan offset)
    {
        _clockOffset += offset;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var session in _sessions.Values)
        {
            session.Context.Dispose();
        }

        _sessions.Clear();
        _disposed = true;
    }

    private async Task<McpSessionContext> CreateContextAsync(string sessionId, CancellationToken cancellationToken)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.ApiBaseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds)
        };

        var session = new BudApiSession(httpClient, _options);
        await session.InitializeAsync(cancellationToken);

        var apiClient = new BudApiClient(httpClient, session);
        var toolService = new McpToolService(apiClient, session);
        return new McpSessionContext(sessionId, httpClient, session, apiClient, toolService);
    }

    private void CleanupExpiredSessions()
    {
        var now = UtcNow();
        foreach (var (sessionId, entry) in _sessions)
        {
            if (now - entry.LastAccessUtc <= _sessionIdleTtl)
            {
                continue;
            }

            if (_sessions.TryRemove(sessionId, out var removed))
            {
                removed.Context.Dispose();
            }
        }
    }

    private DateTimeOffset UtcNow()
    {
        return DateTimeOffset.UtcNow.Add(_clockOffset);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed record SessionEntry(McpSessionContext Context, DateTimeOffset LastAccessUtc);
}
