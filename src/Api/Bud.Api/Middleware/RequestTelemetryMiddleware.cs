using System.Diagnostics;

namespace Bud.Api.Middleware;

public sealed partial class RequestTelemetryMiddleware(
    RequestDelegate next,
    ILogger<RequestTelemetryMiddleware> logger)
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

            LogHttpRequest(logger, method, path, statusCode, elapsedMs, correlationId);
        }
    }

    [LoggerMessage(
        EventId = 3200,
        Level = LogLevel.Information,
        Message = "HTTP {Method} {Path} respondeu {StatusCode} em {ElapsedMs} ms (CorrelationId: {CorrelationId})")]
    private static partial void LogHttpRequest(ILogger logger, string method, string path, int statusCode, double elapsedMs, string correlationId);
}
