namespace Bud.Server.Middleware;

public sealed partial class SecurityHeadersMiddleware(
    RequestDelegate next,
    ILogger<SecurityHeadersMiddleware> logger)
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'wasm-unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("X-Frame-Options", "DENY");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        headers.TryAdd("X-XSS-Protection", "0");
        headers.TryAdd("Content-Security-Policy", ContentSecurityPolicy);

        LogSecurityHeadersApplied(logger, context.Request.Path);

        return next(context);
    }

    [LoggerMessage(
        EventId = 3300,
        Level = LogLevel.Debug,
        Message = "Security headers aplicados para {Path}")]
    private static partial void LogSecurityHeadersApplied(ILogger logger, PathString path);
}
