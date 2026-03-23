namespace Bud.Application.Ports;

public interface ITenantProvider
{
    Guid? TenantId { get; }
    Guid? CollaboratorId { get; }
    bool IsGlobalAdmin { get; }
    string? UserEmail { get; }
}
