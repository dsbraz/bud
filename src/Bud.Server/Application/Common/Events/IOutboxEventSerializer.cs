using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Application.Common.Events;

public interface IOutboxEventSerializer
{
    (string EventType, string Payload) Serialize(IDomainEvent domainEvent);
    IDomainEvent Deserialize(string eventType, string payload);
}
