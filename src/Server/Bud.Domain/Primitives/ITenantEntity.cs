namespace Bud.Domain.Primitives;

public interface ITenantEntity
{
    Guid OrganizationId { get; set; }
}
