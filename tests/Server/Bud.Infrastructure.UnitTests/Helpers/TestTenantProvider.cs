using Bud.Application.Ports;

namespace Bud.Infrastructure.UnitTests.Helpers;

public sealed class TestTenantProvider : ITenantProvider
{
    public Guid? TenantId { get; set; }
    public Guid? CollaboratorId { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public string? UserEmail { get; set; }
}
