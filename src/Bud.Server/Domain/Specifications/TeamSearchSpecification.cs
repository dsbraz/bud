using Bud.Server.Domain.Querying;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Domain.Specifications;

public sealed class TeamSearchSpecification(string? search, bool isNpgsql, bool includeWorkspaceName = false) : IQuerySpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> query)
    {
        return QuerySearchHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            ApplyNpgsql,
            ApplyInMemory);
    }

    private IQueryable<Team> ApplyNpgsql(IQueryable<Team> query, string pattern)
    {
        return includeWorkspaceName
            ? query.Where(t => EF.Functions.ILike(t.Name, pattern) || EF.Functions.ILike(t.Workspace.Name, pattern))
            : query.Where(t => EF.Functions.ILike(t.Name, pattern));
    }

    private IQueryable<Team> ApplyInMemory(IQueryable<Team> query, string term)
    {
        return includeWorkspaceName
            ? query.Where(t =>
                t.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Workspace.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            : query.Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
