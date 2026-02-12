namespace Bud.Server.Domain.Common.Events;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class DomainEventVersionAttribute(int version) : Attribute
{
    public int Version { get; } = version;
}
