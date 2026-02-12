using Bud.Server.Application.Abstractions;
using Bud.Server.Data;
using Bud.Server.Domain.Common.ValueObjects;
using Bud.Server.MultiTenancy;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class CollaboratorValidationService(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider) : ICollaboratorValidationService
{
    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!EmailAddress.TryCreate(email, out var emailAddress))
        {
            return false;
        }

#pragma warning disable CA1304, CA1311, CA1862
        var exists = await dbContext.Collaborators
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Email.ToLower() == emailAddress.Value, cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862

        return !exists;
    }

    public async Task<bool> IsValidLeaderForCreateAsync(Guid leaderId, CancellationToken cancellationToken = default)
    {
        var currentOrgId = tenantProvider.TenantId;
        if (!currentOrgId.HasValue)
        {
            return false;
        }

        var leader = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == leaderId, cancellationToken);

        return leader is not null
            && leader.OrganizationId == currentOrgId.Value
            && leader.Role == CollaboratorRole.Leader;
    }

    public async Task<bool> IsValidLeaderForUpdateAsync(Guid leaderId, CancellationToken cancellationToken = default)
    {
        var leader = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == leaderId, cancellationToken);

        return leader is not null && leader.Role == CollaboratorRole.Leader;
    }
}
