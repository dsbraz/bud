using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Features.Goals;

public sealed class GoalSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<Goal>
{
    public IQueryable<Goal> Apply(IQueryable<Goal> query)
    {
        return QuerySearchHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(g => EF.Functions.ILike(g.Name, pattern)),
            (q, term) => q.Where(g => g.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
