using Bud.Server.Data;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class MissionScopeResolver(ApplicationDbContext dbContext) : IMissionScopeResolver
{
    public async Task<ServiceResult<Guid>> ResolveScopeOrganizationIdAsync(
        MissionScopeType scopeType,
        Guid scopeId,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        var organizationId = scopeType switch
        {
            MissionScopeType.Organization => await QueryOrganizations(ignoreQueryFilters)
                .Where(o => o.Id == scopeId)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefaultAsync(cancellationToken),
            MissionScopeType.Workspace => await QueryWorkspaces(ignoreQueryFilters)
                .Where(w => w.Id == scopeId)
                .Select(w => (Guid?)w.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken),
            MissionScopeType.Team => await QueryTeams(ignoreQueryFilters)
                .Where(t => t.Id == scopeId)
                .Select(t => (Guid?)t.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken),
            MissionScopeType.Collaborator => await QueryCollaborators(ignoreQueryFilters)
                .Where(c => c.Id == scopeId)
                .Select(c => (Guid?)c.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken),
            _ => null
        };

        return organizationId.HasValue
            ? ServiceResult<Guid>.Success(organizationId.Value)
            : ServiceResult<Guid>.NotFound(GetScopeNotFoundMessage(scopeType));
    }

    private IQueryable<Organization> QueryOrganizations(bool ignoreQueryFilters)
    {
        var query = dbContext.Organizations.AsNoTracking();
        return ignoreQueryFilters ? query.IgnoreQueryFilters() : query;
    }

    private IQueryable<Workspace> QueryWorkspaces(bool ignoreQueryFilters)
    {
        var query = dbContext.Workspaces.AsNoTracking();
        return ignoreQueryFilters ? query.IgnoreQueryFilters() : query;
    }

    private IQueryable<Team> QueryTeams(bool ignoreQueryFilters)
    {
        var query = dbContext.Teams.AsNoTracking();
        return ignoreQueryFilters ? query.IgnoreQueryFilters() : query;
    }

    private IQueryable<Collaborator> QueryCollaborators(bool ignoreQueryFilters)
    {
        var query = dbContext.Collaborators.AsNoTracking();
        return ignoreQueryFilters ? query.IgnoreQueryFilters() : query;
    }

    private static string GetScopeNotFoundMessage(MissionScopeType scopeType)
    {
        return scopeType switch
        {
            MissionScopeType.Organization => "Organização não encontrada.",
            MissionScopeType.Workspace => "Workspace não encontrado.",
            MissionScopeType.Team => "Time não encontrado.",
            MissionScopeType.Collaborator => "Colaborador não encontrado.",
            _ => "Escopo não encontrado."
        };
    }
}
