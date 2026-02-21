
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Querying;

public sealed class WorkspaceSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<Workspace>
{
    public IQueryable<Workspace> Apply(IQueryable<Workspace> query)
    {
        return QuerySearchHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(w => EF.Functions.ILike(w.Name, pattern)),
            (q, term) => q.Where(w => w.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
