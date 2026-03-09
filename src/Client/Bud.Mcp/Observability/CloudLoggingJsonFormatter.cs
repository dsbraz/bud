using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Bud.Mcp.Observability;

/// <summary>
/// Console formatter that emits structured JSON logs compatible with Google Cloud Logging.
/// Each log entry is a single JSON line containing the fields expected by Cloud Logging:
/// severity, message, timestamp, logging.googleapis.com/trace, logging.googleapis.com/spanId.
/// </summary>
public sealed class CloudLoggingJsonFormatter : ConsoleFormatter
{
    public const string FormatterName = "cloud-logging-json";

    private readonly IOptionsMonitor<CloudLoggingJsonFormatterOptions> _optionsMonitor;

    public CloudLoggingJsonFormatter(IOptionsMonitor<CloudLoggingJsonFormatterOptions> optionsMonitor)
        : base(FormatterName)
    {
        _optionsMonitor = optionsMonitor;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var options = _optionsMonitor.CurrentValue;
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

        if (message is null && logEntry.Exception is null)
        {
            return;
        }

        using var buffer = new PooledByteBufferWriter(1024);
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false });

        writer.WriteStartObject();

        writer.WriteString("severity", MapLogLevelToSeverity(logEntry.LogLevel));
        writer.WriteString("message", message ?? string.Empty);
        writer.WriteString("time", DateTimeOffset.UtcNow.ToString("O"));

        writer.WritePropertyName("logging.googleapis.com/sourceLocation");
        writer.WriteStartObject();
        writer.WriteString("function", logEntry.Category);
        writer.WriteString("line", logEntry.EventId.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteEndObject();

        writer.WriteNumber("eventId", logEntry.EventId.Id);
        writer.WriteString("category", logEntry.Category);

        var (traceId, spanId, correlationId) = ExtractTraceContext(scopeProvider);

        if (string.IsNullOrEmpty(traceId))
        {
            var activity = Activity.Current;
            if (activity is not null)
            {
                traceId = activity.TraceId.ToString();
                spanId = activity.SpanId.ToString();
            }
        }

        if (!string.IsNullOrEmpty(traceId))
        {
            var gcpProjectId = options.GcpProjectId;
            var traceValue = string.IsNullOrEmpty(gcpProjectId)
                ? traceId
                : $"projects/{gcpProjectId}/traces/{traceId}";
            writer.WriteString("logging.googleapis.com/trace", traceValue);
        }

        if (!string.IsNullOrEmpty(spanId))
        {
            writer.WriteString("logging.googleapis.com/spanId", spanId);
        }

        if (!string.IsNullOrEmpty(correlationId))
        {
            writer.WriteString("correlationId", correlationId);
        }

        if (logEntry.Exception is not null)
        {
            writer.WritePropertyName("exception");
            writer.WriteStartObject();
            writer.WriteString("type", logEntry.Exception.GetType().FullName);
            writer.WriteString("message", logEntry.Exception.Message);
            writer.WriteString("stackTrace", logEntry.Exception.ToString());
            writer.WriteEndObject();
        }

        if (options.IncludeScopes && scopeProvider is not null)
        {
            scopeProvider.ForEachScope((scope, w) =>
            {
                if (scope is IReadOnlyList<KeyValuePair<string, object?>> kvList)
                {
                    foreach (var kv in kvList)
                    {
                        if (kv.Key is "TraceId" or "SpanId" or "CorrelationId")
                        {
                            continue;
                        }

                        try
                        {
                            w.WriteString(kv.Key, kv.Value?.ToString());
                        }
                        catch
                        {
                            // Ignore serialisation errors for individual scope values
                        }
                    }
                }
            }, writer);
        }

        writer.WriteEndObject();
        writer.Flush();

        textWriter.Write(System.Text.Encoding.UTF8.GetString(buffer.WrittenMemory.Span));
        textWriter.Write(Environment.NewLine);
    }

    private static string MapLogLevelToSeverity(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "DEBUG",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARNING",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRITICAL",
        _ => "DEFAULT"
    };

    private static (string? traceId, string? spanId, string? correlationId) ExtractTraceContext(
        IExternalScopeProvider? scopeProvider)
    {
        string? traceId = null;
        string? spanId = null;
        string? correlationId = null;

        if (scopeProvider is null)
        {
            return (traceId, spanId, correlationId);
        }

        scopeProvider.ForEachScope((scope, _) =>
        {
            if (scope is IReadOnlyList<KeyValuePair<string, object?>> kvList)
            {
                foreach (var kv in kvList)
                {
                    switch (kv.Key)
                    {
                        case "TraceId" when kv.Value is string t:
                            traceId = t;
                            break;
                        case "SpanId" when kv.Value is string s:
                            spanId = s;
                            break;
                        case "CorrelationId" when kv.Value is string c:
                            correlationId = c;
                            break;
                    }
                }
            }
        }, 0);

        return (traceId, spanId, correlationId);
    }

    private sealed class PooledByteBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private byte[] _buffer;
        private int _index;

        public PooledByteBufferWriter(int initialCapacity)
        {
            _buffer = new byte[initialCapacity];
            _index = 0;
        }

        public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _index);

        public void Advance(int count) => _index += count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_index);
        }

        private void EnsureCapacity(int sizeHint)
        {
            var needed = _index + Math.Max(sizeHint, 256);
            if (needed > _buffer.Length)
            {
                var newSize = Math.Max(needed, _buffer.Length * 2);
                var newBuffer = new byte[newSize];
                _buffer.AsSpan(0, _index).CopyTo(newBuffer);
                _buffer = newBuffer;
            }
        }

        public void Dispose() { }
    }
}
