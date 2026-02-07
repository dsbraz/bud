using Bud.Server.Domain.Common.Events;

namespace Bud.Server.Domain.Collaborators.Events;

public sealed record CollaboratorUpdatedDomainEvent(Guid CollaboratorId, Guid OrganizationId) : IDomainEvent;
