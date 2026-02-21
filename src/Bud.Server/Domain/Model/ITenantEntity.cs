namespace Bud.Server.Domain.Model;

public interface ITenantEntity
{
    Guid OrganizationId { get; set; }
}
