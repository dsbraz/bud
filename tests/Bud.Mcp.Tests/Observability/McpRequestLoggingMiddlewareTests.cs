using Bud.Mcp.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bud.Mcp.Tests.Observability;

public sealed class McpRequestLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldAddCorrelationIdHeader()
    {
        var logger = new ListLogger<McpRequestLoggingMiddleware>();
        var middleware = new McpRequestLoggingMiddleware(_ => Task.CompletedTask, logger);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.TraceIdentifier = "mcp-corr-123";

        await middleware.InvokeAsync(context);

        context.Response.Headers.Should().ContainKey("X-Correlation-Id");
        context.Response.Headers["X-Correlation-Id"].ToString().Should().Be("mcp-corr-123");
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogRequestDetails()
    {
        var logger = new ListLogger<McpRequestLoggingMiddleware>();
        var middleware = new McpRequestLoggingMiddleware(_ => Task.CompletedTask, logger);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.TraceIdentifier = "mcp-corr-log";
        context.Request.Method = "POST";
        context.Request.Path = "/";

        await middleware.InvokeAsync(context);

        logger.Entries.Should().ContainSingle(e => e.EventId == 5000);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ShouldStillLogAndRethrow()
    {
        var logger = new ListLogger<McpRequestLoggingMiddleware>();
        var middleware = new McpRequestLoggingMiddleware(_ => throw new InvalidOperationException("test error"), logger);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.TraceIdentifier = "mcp-corr-error";

        var act = async () => await middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
        logger.Entries.Should().ContainSingle(e => e.EventId == 5000);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotOverwriteExistingCorrelationIdHeader()
    {
        var logger = new ListLogger<McpRequestLoggingMiddleware>();
        var middleware = new McpRequestLoggingMiddleware(_ => Task.CompletedTask, logger);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.TraceIdentifier = "new-id";
        context.Response.Headers["X-Correlation-Id"] = "existing-id";

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Correlation-Id"].ToString().Should().Be("existing-id");
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
            public void Dispose() { }
        }
    }
}
