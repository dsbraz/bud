namespace Bud.Server.MultiTenancy;

public interface ITenantProvider
{
    Guid? TenantId { get; }
    Guid? CollaboratorId { get; }
    bool IsAdmin { get; }
}
