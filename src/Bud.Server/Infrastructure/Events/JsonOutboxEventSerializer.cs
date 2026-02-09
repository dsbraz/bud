using System.Text.Json;
using Bud.Server.Application.Common.Events;
using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Infrastructure.Events;

public sealed class JsonOutboxEventSerializer : IOutboxEventSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public (string EventType, string Payload) Serialize(IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType().AssemblyQualifiedName
            ?? throw new InvalidOperationException("Tipo do evento de domínio inválido para serialização.");

        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        return (eventType, payload);
    }

    public IDomainEvent Deserialize(string eventType, string payload)
    {
        var type = Type.GetType(eventType, throwOnError: false)
            ?? throw new InvalidOperationException($"Tipo de evento '{eventType}' não pôde ser resolvido.");

        if (!typeof(IDomainEvent).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Tipo '{eventType}' não implementa IDomainEvent.");
        }

        var deserialized = JsonSerializer.Deserialize(payload, type, SerializerOptions)
            ?? throw new InvalidOperationException("Falha ao desserializar mensagem de outbox.");

        return (IDomainEvent)deserialized;
    }
}
