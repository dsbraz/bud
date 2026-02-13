namespace Bud.Shared.Domain;

public sealed class DomainInvariantException(string message) : Exception(message);
