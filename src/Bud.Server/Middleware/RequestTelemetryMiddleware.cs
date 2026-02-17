using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bud.Server.Middleware;

public sealed partial class RequestTelemetryMiddleware(
    RequestDelegate next,
    ILogger<RequestTelemetryMiddleware> logger)
{
    private static readonly Meter _meter = new("Bud.Server.Http", "1.0.0");
    private static readonly Counter<long> _requestCounter = _meter.CreateCounter<long>("http.requests.total");
    private static readonly Counter<long> _errorCounter = _meter.CreateCounter<long>("http.requests.errors");
    private static readonly Histogram<double> _durationMsHistogram = _meter.CreateHistogram<double>("http.request.duration.ms");

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

            _requestCounter.Add(1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("status_code", statusCode));

            _durationMsHistogram.Record(elapsedMs,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("status_code", statusCode));

            if (statusCode >= 500)
            {
                _errorCounter.Add(1,
                    new KeyValuePair<string, object?>("method", method),
                    new KeyValuePair<string, object?>("path", path),
                    new KeyValuePair<string, object?>("status_code", statusCode));
            }

            LogHttpRequest(logger, method, path, statusCode, elapsedMs, correlationId);
        }
    }

    [LoggerMessage(
        EventId = 3200,
        Level = LogLevel.Information,
        Message = "HTTP {Method} {Path} respondeu {StatusCode} em {ElapsedMs} ms (CorrelationId: {CorrelationId})")]
    private static partial void LogHttpRequest(ILogger logger, string method, string path, int statusCode, double elapsedMs, string correlationId);
}
