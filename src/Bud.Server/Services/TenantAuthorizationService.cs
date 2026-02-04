using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public interface ITenantAuthorizationService
{
    Task<bool> UserBelongsToTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetUserTenantIdsAsync(CancellationToken cancellationToken = default);
}

public sealed class TenantAuthorizationService(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider) : ITenantAuthorizationService
{
    public async Task<bool> UserBelongsToTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Global admin tem acesso a tudo
        if (tenantProvider.IsGlobalAdmin)
        {
            return true;
        }

        if (string.IsNullOrEmpty(tenantProvider.UserEmail))
        {
            return false;
        }

        // Verificar se usuário é owner ou colaborador da organização
        var isOwner = await dbContext.Organizations
            .AnyAsync(o =>
                o.Id == tenantId &&
                o.Owner != null &&
                o.Owner.Email == tenantProvider.UserEmail,
                cancellationToken);

        if (isOwner)
        {
            return true;
        }

        // Verificar se é colaborador
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

        // Organizações onde o usuário é owner
        var ownedOrgIds = await dbContext.Organizations
            .Where(o => o.Owner != null && o.Owner.Email == tenantProvider.UserEmail)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        // Organizações onde o usuário é colaborador
        var collaboratorOrgIds = await dbContext.Collaborators
            .Where(c => c.Email == tenantProvider.UserEmail)
            .Select(c => c.OrganizationId)
            .ToListAsync(cancellationToken);

        // Combinar e remover duplicatas
        return ownedOrgIds.Union(collaboratorOrgIds).ToList();
    }
}
