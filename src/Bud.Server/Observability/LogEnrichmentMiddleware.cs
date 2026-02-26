using System.Diagnostics;

namespace Bud.Server.Observability;

/// <summary>
/// Middleware that enriches all log entries within a request with trace context:
/// TraceId, SpanId (from the active OpenTelemetry Activity), and CorrelationId
/// (from the ASP.NET Core TraceIdentifier). These values are stored in a log scope
/// so that formatters (e.g. CloudLoggingJsonFormatter) can include them in every entry.
/// </summary>
public sealed class LogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILoggerFactory loggerFactory)
    {
        var correlationId = context.TraceIdentifier;
        var activity = Activity.Current;

        var scopeValues = new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId
        };

        if (activity is not null)
        {
            scopeValues["TraceId"] = activity.TraceId.ToString();
            scopeValues["SpanId"] = activity.SpanId.ToString();
        }

        var logger = loggerFactory.CreateLogger("Bud.Server.TraceContext");
        using (logger.BeginScope(scopeValues))
        {
            await next(context);
        }
    }
}
