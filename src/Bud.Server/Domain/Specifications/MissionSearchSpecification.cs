using Bud.Server.Services;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Domain.Specifications;

public sealed class MissionSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<Mission>
{
    public IQueryable<Mission> Apply(IQueryable<Mission> query)
    {
        return SearchQueryHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(m => EF.Functions.ILike(m.Name, pattern)),
            (q, term) => q.Where(m => m.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
