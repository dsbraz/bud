namespace Bud.Shared.Models;

public sealed class DomainInvariantException(string message) : Exception(message);
