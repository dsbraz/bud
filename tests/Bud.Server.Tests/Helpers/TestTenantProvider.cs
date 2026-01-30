using Bud.Server.MultiTenancy;

namespace Bud.Server.Tests.Helpers;

public sealed class TestTenantProvider : ITenantProvider
{
    public Guid? TenantId { get; set; }
    public bool IsAdmin { get; set; }
}
