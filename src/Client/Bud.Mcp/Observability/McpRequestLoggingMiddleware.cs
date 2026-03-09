using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bud.Mcp.Observability;

public sealed partial class McpRequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<McpRequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;
        context.Response.Headers.TryAdd("X-Correlation-Id", correlationId);

        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            var elapsedMs = sw.Elapsed.TotalMilliseconds;
            var statusCode = context.Response.StatusCode;
            var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
            var method = context.Request.Method;

            LogMcpRequest(logger, method, path, statusCode, elapsedMs, correlationId);
        }
    }

    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Information,
        Message = "MCP {Method} {Path} respondeu {StatusCode} em {ElapsedMs} ms (CorrelationId: {CorrelationId})")]
    private static partial void LogMcpRequest(ILogger logger, string method, string path, int statusCode, double elapsedMs, string correlationId);
}
