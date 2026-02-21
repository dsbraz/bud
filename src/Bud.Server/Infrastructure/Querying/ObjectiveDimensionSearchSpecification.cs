
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Querying;

public sealed class ObjectiveDimensionSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<ObjectiveDimension>
{
    public IQueryable<ObjectiveDimension> Apply(IQueryable<ObjectiveDimension> query)
    {
        return QuerySearchHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(d => EF.Functions.ILike(d.Name, pattern)),
            (q, term) => q.Where(d => d.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
