
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Querying;

public sealed class MissionTemplateSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<MissionTemplate>
{
    public IQueryable<MissionTemplate> Apply(IQueryable<MissionTemplate> query)
    {
        return QuerySearchHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(t => EF.Functions.ILike(t.Name, pattern)),
            (q, term) => q.Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
