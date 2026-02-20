using Bud.Server.Domain.Querying;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Domain.Specifications;

public sealed class MissionMetricSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<MissionMetric>
{
    public IQueryable<MissionMetric> Apply(IQueryable<MissionMetric> query)
    {
        return QuerySearchHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(m => EF.Functions.ILike(m.Name, pattern)),
            (q, term) => q.Where(m => m.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
