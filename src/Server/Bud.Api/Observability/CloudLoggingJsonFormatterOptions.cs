using Microsoft.Extensions.Logging.Console;

namespace Bud.Api.Observability;

/// <summary>
/// Options for the Cloud Logging JSON console formatter.
/// </summary>
public sealed class CloudLoggingJsonFormatterOptions : ConsoleFormatterOptions
{
    /// <summary>
    /// GCP project ID used to build the full trace resource name
    /// in the format "projects/{projectId}/traces/{traceId}".
    /// When null or empty, the raw trace ID is written instead.
    /// </summary>
    public string? GcpProjectId { get; set; }
}
