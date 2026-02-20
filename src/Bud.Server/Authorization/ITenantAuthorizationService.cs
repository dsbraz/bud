namespace Bud.Server.Authorization;

public interface ITenantAuthorizationService
{
    Task<bool> UserBelongsToTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetUserTenantIdsAsync(CancellationToken cancellationToken = default);
}
