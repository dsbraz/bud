using Bud.Server.Services;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Domain.Common.Specifications;

public sealed class CollaboratorSearchSpecification(string? search, bool isNpgsql) : IQuerySpecification<Collaborator>
{
    public IQueryable<Collaborator> Apply(IQueryable<Collaborator> query)
    {
        return SearchQueryHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            isNpgsql,
            (q, pattern) => q.Where(c => EF.Functions.ILike(c.FullName, pattern) || EF.Functions.ILike(c.Email, pattern)),
            (q, term) => q.Where(c =>
                c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
