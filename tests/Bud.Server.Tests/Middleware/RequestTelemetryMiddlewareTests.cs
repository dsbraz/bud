using Bud.Server.Middleware;
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

    private sealed class ListLogger<TCategoryName> : ILogger<TCategoryName>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId.Id, formatter(state, exception)));
        }

        public sealed record LogEntry(LogLevel Level, int EventId, string Message);

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
