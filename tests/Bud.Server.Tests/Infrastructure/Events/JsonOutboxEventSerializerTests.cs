using Bud.Server.Domain.Common.Events;
using Bud.Server.Infrastructure.Events;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Events;

public sealed class JsonOutboxEventSerializerTests
{
    [Fact]
    public void Serialize_ShouldAppendDefaultVersion()
    {
        var serializer = new JsonOutboxEventSerializer();

        var (eventType, payload) = serializer.Serialize(new DefaultEvent(Guid.NewGuid()));

        eventType.Should().Contain("|v1");
        payload.Should().Contain("id");
    }

    [Fact]
    public void Serialize_WithVersionAttribute_ShouldAppendConfiguredVersion()
    {
        var serializer = new JsonOutboxEventSerializer();

        var (eventType, _) = serializer.Serialize(new VersionedEvent(Guid.NewGuid()));

        eventType.Should().Contain("|v3");
    }

    [Fact]
    public void Deserialize_WithVersionedEventType_ShouldRestoreEvent()
    {
        var serializer = new JsonOutboxEventSerializer();
        var @event = new DefaultEvent(Guid.NewGuid());
        var (eventType, payload) = serializer.Serialize(@event);

        var deserialized = serializer.Deserialize(eventType, payload);

        deserialized.Should().BeOfType<DefaultEvent>();
    }

    [Fact]
    public void Deserialize_WithoutVersionSuffix_ShouldRemainBackwardCompatible()
    {
        var serializer = new JsonOutboxEventSerializer();
        var @event = new DefaultEvent(Guid.NewGuid());
        var eventType = typeof(DefaultEvent).AssemblyQualifiedName!;
        var payload = "{\"id\":\"" + @event.Id + "\"}";

        var deserialized = serializer.Deserialize(eventType, payload);

        deserialized.Should().BeOfType<DefaultEvent>();
    }

    private sealed record DefaultEvent(Guid Id) : IDomainEvent;

    [DomainEventVersion(3)]
    private sealed record VersionedEvent(Guid Id) : IDomainEvent;
}
