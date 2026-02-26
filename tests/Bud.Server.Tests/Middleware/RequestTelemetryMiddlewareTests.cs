using Bud.Server.Middleware;
using Bud.Server.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bud.Server.Tests.Middleware;

public sealed class RequestTelemetryMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldAddCorrelationIdHeader()
    {
        var logger = new ListLogger<RequestTelemetryMiddleware>();
        var middleware = new RequestTelemetryMiddleware(_ => Task.CompletedTask, logger);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.TraceIdentifier = "corr-123";

        await middleware.InvokeAsync(context);

        context.Response.Headers.Should().ContainKey("X-Correlation-Id");
        context.Response.Headers["X-Correlation-Id"].ToString().Should().Be("corr-123");
        logger.Entries.Should().ContainSingle(e => e.EventId == 3200);
    }

    [Fact]
    public async Task InvokeAsync_WhenServerError_ShouldStillLogAndComplete()
    {
        var logger = new ListLogger<RequestTelemetryMiddleware>();
        var middleware = new RequestTelemetryMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }, logger);

        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.TraceIdentifier = "corr-500";

        await middleware.InvokeAsync(context);

        logger.Entries.Should().ContainSingle(e => e.Message.Contains("500"));
    }

}
