using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.Organizations.Events;

public sealed record OrganizationDeletedDomainEvent(Guid OrganizationId) : IDomainEvent;
