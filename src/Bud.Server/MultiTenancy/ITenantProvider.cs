namespace Bud.Server.MultiTenancy;

public interface ITenantProvider
{
    Guid? TenantId { get; }
    bool IsAdmin { get; }
}
