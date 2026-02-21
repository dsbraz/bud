
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Querying;

public sealed class OrganizationSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<Organization>
{
    public IQueryable<Organization> Apply(IQueryable<Organization> query)
    {
        return QuerySearchHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(o => EF.Functions.ILike(o.Name, pattern)),
            (q, term) => q.Where(o => o.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
