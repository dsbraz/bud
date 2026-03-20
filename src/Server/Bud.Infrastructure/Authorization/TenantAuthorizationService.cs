using Bud.Infrastructure.Persistence;
using Bud.Application.Ports;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Authorization;

public sealed class TenantAuthorizationService(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider) : ITenantAuthorizationService
{
    public async Task<bool> UserBelongsToTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantProvider.IsGlobalAdmin)
        {
            return true;
        }

        if (string.IsNullOrEmpty(tenantProvider.UserEmail))
        {
            return false;
        }

        return await dbContext.Collaborators
            .AnyAsync(c =>
                c.OrganizationId == tenantId &&
                c.Email == tenantProvider.UserEmail,
                cancellationToken);
    }

    public async Task<List<Guid>> GetUserTenantIdsAsync(CancellationToken cancellationToken = default)
    {
        if (tenantProvider.IsGlobalAdmin)
        {
            return await dbContext.Organizations.Select(o => o.Id).ToListAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(tenantProvider.UserEmail))
        {
            return [];
        }

        return await dbContext.Collaborators
            .Where(c => c.Email == tenantProvider.UserEmail)
            .Select(c => c.OrganizationId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
