namespace Bud.Domain.Primitives;

public sealed class DomainInvariantException(string message) : Exception(message);
