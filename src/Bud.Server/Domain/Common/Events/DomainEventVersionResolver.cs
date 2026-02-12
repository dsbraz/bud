using System.Reflection;

namespace Bud.Server.Domain.Common.Events;

public static class DomainEventVersionResolver
{
    public const int DefaultVersion = 1;
    private const string VersionSeparator = "|v";

    public static int Resolve(IDomainEvent domainEvent)
        => Resolve(domainEvent.GetType(), domainEvent);

    public static int Resolve(Type eventType, IDomainEvent? domainEvent = null)
    {
        if (domainEvent is IVersionedDomainEvent versionedEvent)
        {
            return Math.Max(versionedEvent.Version, DefaultVersion);
        }

        var versionedType = eventType.GetCustomAttribute<DomainEventVersionAttribute>();
        if (versionedType is not null)
        {
            return Math.Max(versionedType.Version, DefaultVersion);
        }

        return DefaultVersion;
    }

    public static string AppendVersion(string eventTypeName, int version)
        => $"{eventTypeName}{VersionSeparator}{Math.Max(version, DefaultVersion)}";

    public static (string EventTypeName, int Version) Parse(string versionedEventType)
    {
        var markerIndex = versionedEventType.LastIndexOf(VersionSeparator, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return (versionedEventType, DefaultVersion);
        }

        var name = versionedEventType[..markerIndex];
        var versionRaw = versionedEventType[(markerIndex + VersionSeparator.Length)..];
        return int.TryParse(versionRaw, out var version) && version > 0
            ? (name, version)
            : (name, DefaultVersion);
    }
}
