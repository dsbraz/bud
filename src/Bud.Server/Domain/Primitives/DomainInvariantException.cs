namespace Bud.Server.Domain.Primitives;

public sealed class DomainInvariantException(string message) : Exception(message);
