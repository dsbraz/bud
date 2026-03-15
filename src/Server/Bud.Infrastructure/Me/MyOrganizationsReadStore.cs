using Bud.Application.Common;
using Bud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Me;

public sealed class MyOrganizationsReadStore(ApplicationDbContext dbContext) : IMyOrganizationsReadStore
{
    public async Task<Result<List<OrganizationSnapshot>>> GetMyOrganizationsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Result<List<OrganizationSnapshot>>.Failure("E-mail é obrigatório.");
        }

        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

        if (collaborator?.IsGlobalAdmin == true)
        {
            var allOrgs = await dbContext.Organizations
                .AsNoTracking()
                .IgnoreQueryFilters()
                .OrderBy(o => o.Name)
                .Select(o => new OrganizationSnapshot
                {
                    Id = o.Id,
                    Name = o.Name
                })
                .ToListAsync(cancellationToken);

            return Result<List<OrganizationSnapshot>>.Success(allOrgs);
        }

        var orgsFromMembership = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.Email == normalizedEmail)
            .Include(c => c.Organization)
            .Select(c => new OrganizationSnapshot
            {
                Id = c.Organization.Id,
                Name = c.Organization.Name
            })
            .ToListAsync(cancellationToken);

        var orgsFromOwnership = await dbContext.Organizations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Owner != null && o.Owner.Email == normalizedEmail)
            .Select(o => new OrganizationSnapshot
            {
                Id = o.Id,
                Name = o.Name
            })
            .ToListAsync(cancellationToken);

        var organizations = orgsFromMembership
            .Concat(orgsFromOwnership)
            .GroupBy(o => o.Id)
            .Select(g => g.First())
            .OrderBy(o => o.Name)
            .ToList();

        return Result<List<OrganizationSnapshot>>.Success(organizations);
    }
}
