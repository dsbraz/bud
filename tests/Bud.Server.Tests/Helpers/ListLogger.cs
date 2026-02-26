using Microsoft.Extensions.Logging;

namespace Bud.Server.Tests.Helpers;

/// <summary>
/// A test logger that captures log entries for assertion in unit tests.
/// </summary>
public sealed class ListLogger<TCategoryName> : ILogger<TCategoryName>
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
