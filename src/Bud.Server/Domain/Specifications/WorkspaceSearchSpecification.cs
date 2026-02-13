using Bud.Server.Services;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Domain.Specifications;

public sealed class WorkspaceSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<Workspace>
{
    public IQueryable<Workspace> Apply(IQueryable<Workspace> query)
    {
        return SearchQueryHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(w => EF.Functions.ILike(w.Name, pattern)),
            (q, term) => q.Where(w => w.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
