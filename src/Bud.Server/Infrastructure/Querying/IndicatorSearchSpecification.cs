using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Querying;

public sealed class IndicatorSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<Indicator>
{
    public IQueryable<Indicator> Apply(IQueryable<Indicator> query)
    {
        return QuerySearchHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(i => EF.Functions.ILike(i.Name, pattern)),
            (q, term) => q.Where(i => i.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
