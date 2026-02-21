namespace Bud.Server.Domain.Model;

public sealed class DomainInvariantException(string message) : Exception(message);
