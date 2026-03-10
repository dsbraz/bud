
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Features.Templates;

public sealed class TemplateSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<Template>
{
    public IQueryable<Template> Apply(IQueryable<Template> query)
    {
        return QuerySearchHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(t => EF.Functions.ILike(t.Name, pattern)),
            (q, term) => q.Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
