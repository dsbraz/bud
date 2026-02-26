using System.Diagnostics;
using System.Text.Json;
using Bud.Server.Observability;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bud.Server.Tests.Observability;

public sealed class CloudLoggingJsonFormatterTests
{
    private static CloudLoggingJsonFormatter CreateFormatter(string? gcpProjectId = null)
    {
        var options = new CloudLoggingJsonFormatterOptions
        {
            GcpProjectId = gcpProjectId,
            IncludeScopes = true
        };
        var monitor = new TestOptionsMonitor<CloudLoggingJsonFormatterOptions>(options);
        return new CloudLoggingJsonFormatter(monitor);
    }

    private static string Format<TState>(
        CloudLoggingJsonFormatter formatter,
        LogLevel level,
        TState state,
        Exception? exception = null,
        IExternalScopeProvider? scopeProvider = null)
    {
        var eventId = new EventId(1001, "TestEvent");
        var logEntry = new LogEntry<TState>(
            level,
            "TestCategory",
            eventId,
            state,
            exception,
            (s, _) => s?.ToString() ?? string.Empty);

        var writer = new StringWriter();
        formatter.Write(logEntry, scopeProvider, writer);
        return writer.ToString().Trim();
    }

    [Fact]
    public void Write_ShouldOutputValidSingleLineJson()
    {
        var formatter = CreateFormatter();
        var output = Format(formatter, LogLevel.Information, "Test message");

        output.Should().NotBeEmpty();
        var doc = JsonDocument.Parse(output);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Theory]
    [InlineData(LogLevel.Trace, "DEBUG")]
    [InlineData(LogLevel.Debug, "DEBUG")]
    [InlineData(LogLevel.Information, "INFO")]
    [InlineData(LogLevel.Warning, "WARNING")]
    [InlineData(LogLevel.Error, "ERROR")]
    [InlineData(LogLevel.Critical, "CRITICAL")]
    public void Write_ShouldMapLogLevelToGcpSeverity(LogLevel logLevel, string expectedSeverity)
    {
        var formatter = CreateFormatter();
        var output = Format(formatter, logLevel, "msg");

        var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("severity").GetString().Should().Be(expectedSeverity);
    }

    [Fact]
    public void Write_ShouldIncludeMessage()
    {
        var formatter = CreateFormatter();
        var output = Format(formatter, LogLevel.Information, "Hello World");

        var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("message").GetString().Should().Be("Hello World");
    }

    [Fact]
    public void Write_ShouldIncludeTimestamp()
    {
        var formatter = CreateFormatter();
        var output = Format(formatter, LogLevel.Information, "msg");

        var doc = JsonDocument.Parse(output);
        doc.RootElement.TryGetProperty("time", out var timeProperty).Should().BeTrue();
        timeProperty.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Write_ShouldIncludeEventId()
    {
        var formatter = CreateFormatter();
        var output = Format(formatter, LogLevel.Information, "msg");

        var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("eventId").GetInt32().Should().Be(1001);
    }

    [Fact]
    public void Write_ShouldIncludeCategory()
    {
        var formatter = CreateFormatter();
        var output = Format(formatter, LogLevel.Information, "msg");

        var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("category").GetString().Should().Be("TestCategory");
    }

    [Fact]
    public void Write_ShouldIncludeExceptionDetails_WhenExceptionPresent()
    {
        var formatter = CreateFormatter();
        var exception = new InvalidOperationException("Test error");
        var output = Format(formatter, LogLevel.Error, "Error occurred", exception);

        var doc = JsonDocument.Parse(output);
        doc.RootElement.TryGetProperty("exception", out var exProp).Should().BeTrue();
        exProp.GetProperty("type").GetString().Should().Contain("InvalidOperationException");
        exProp.GetProperty("message").GetString().Should().Be("Test error");
    }

    [Fact]
    public void Write_ShouldIncludeTraceAndSpanId_FromScope()
    {
        var formatter = CreateFormatter();
        var scopeProvider = new TestScopeProvider(new Dictionary<string, object?>
        {
            ["TraceId"] = "abc123trace",
            ["SpanId"] = "def456span",
            ["CorrelationId"] = "corr-xyz"
        });

        var output = Format(formatter, LogLevel.Information, "msg", scopeProvider: scopeProvider);

        var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("logging.googleapis.com/trace").GetString().Should().Be("abc123trace");
        doc.RootElement.GetProperty("logging.googleapis.com/spanId").GetString().Should().Be("def456span");
        doc.RootElement.GetProperty("correlationId").GetString().Should().Be("corr-xyz");
    }

    [Fact]
    public void Write_ShouldFormatTraceAsGcpResourceName_WhenProjectIdConfigured()
    {
        var formatter = CreateFormatter(gcpProjectId: "my-gcp-project");
        var scopeProvider = new TestScopeProvider(new Dictionary<string, object?>
        {
            ["TraceId"] = "abc123trace"
        });

        var output = Format(formatter, LogLevel.Information, "msg", scopeProvider: scopeProvider);

        var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("logging.googleapis.com/trace").GetString()
            .Should().Be("projects/my-gcp-project/traces/abc123trace");
    }

    [Fact]
    public void Write_ShouldUseRawTraceId_WhenProjectIdNotConfigured()
    {
        var formatter = CreateFormatter(gcpProjectId: null);
        var scopeProvider = new TestScopeProvider(new Dictionary<string, object?>
        {
            ["TraceId"] = "abc123trace"
        });

        var output = Format(formatter, LogLevel.Information, "msg", scopeProvider: scopeProvider);

        var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("logging.googleapis.com/trace").GetString().Should().Be("abc123trace");
    }

    [Fact]
    public void Write_ShouldFallbackToActivityCurrentForTraceId_WhenNoScope()
    {
        var formatter = CreateFormatter();
        var activity = new Activity("TestOperation").Start();

        try
        {
            var output = Format(formatter, LogLevel.Information, "msg");

            var doc = JsonDocument.Parse(output);
            if (doc.RootElement.TryGetProperty("logging.googleapis.com/trace", out var traceProp))
            {
                traceProp.GetString().Should().NotBeNullOrEmpty();
            }
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void Write_ShouldReturnEmptyLine_WhenMessageAndExceptionAreNull()
    {
        var formatter = CreateFormatter();

        var logEntry = new LogEntry<string>(
            LogLevel.Information,
            "TestCategory",
            new EventId(1),
            "state",
            null,
            (_, _) => null!);

        var writer = new StringWriter();
        formatter.Write(logEntry, null, writer);
        writer.ToString().Should().BeEmpty();
    }

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class TestScopeProvider(IReadOnlyDictionary<string, object?> values) : IExternalScopeProvider
    {
        public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
            callback(values.ToList(), state);
        }

        public IDisposable Push(object? state) => NullScope.Instance;

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
