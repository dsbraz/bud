using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class TeamService(ApplicationDbContext dbContext) : ITeamService
{
    public async Task<ServiceResult<Team>> CreateAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken);

        if (workspace is null)
        {
            return ServiceResult<Team>.NotFound("Workspace não encontrado.");
        }

        if (request.ParentTeamId.HasValue)
        {
            var parentTeam = await dbContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.ParentTeamId.Value, cancellationToken);

            if (parentTeam is null)
            {
                return ServiceResult<Team>.NotFound("Time pai não encontrado.");
            }

            if (parentTeam.WorkspaceId != request.WorkspaceId)
            {
                return ServiceResult<Team>.Failure("O time pai deve pertencer ao mesmo workspace.");
            }
        }

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            OrganizationId = workspace.OrganizationId,
            WorkspaceId = request.WorkspaceId,
            ParentTeamId = request.ParentTeamId,
        };

        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Team>.Success(team);
    }

    public async Task<ServiceResult<Team>> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken = default)
    {
        var team = await dbContext.Teams.FindAsync([id], cancellationToken);

        if (team is null)
        {
            return ServiceResult<Team>.NotFound("Time não encontrado.");
        }

        if (request.ParentTeamId.HasValue && request.ParentTeamId != team.ParentTeamId)
        {
            if (request.ParentTeamId == id)
            {
                return ServiceResult<Team>.Failure("Um time não pode ser seu próprio pai.");
            }

            var parentTeam = await dbContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.ParentTeamId.Value, cancellationToken);

            if (parentTeam is null)
            {
                return ServiceResult<Team>.NotFound("Time pai não encontrado.");
            }

            if (parentTeam.WorkspaceId != team.WorkspaceId)
            {
                return ServiceResult<Team>.Failure("O time pai deve pertencer ao mesmo workspace.");
            }
        }

        team.Name = request.Name.Trim();
        team.ParentTeamId = request.ParentTeamId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Team>.Success(team);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await dbContext.Teams.FindAsync([id], cancellationToken);

        if (team is null)
        {
            return ServiceResult.NotFound("Time não encontrado.");
        }

        var hasSubTeams = await dbContext.Teams.AnyAsync(t => t.ParentTeamId == id, cancellationToken);
        if (hasSubTeams)
        {
            return ServiceResult.Failure("Não é possível excluir um time com sub-times. Exclua os sub-times primeiro.", ServiceErrorType.Conflict);
        }

        dbContext.Teams.Remove(team);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<Team>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return team is null
            ? ServiceResult<Team>.NotFound("Time não encontrado.")
            : ServiceResult<Team>.Success(team);
    }

    public async Task<ServiceResult<PagedResult<Team>>> GetAllAsync(Guid? workspaceId, Guid? parentTeamId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var query = dbContext.Teams.AsNoTracking();

        if (workspaceId.HasValue)
        {
            query = query.Where(t => t.WorkspaceId == workspaceId.Value);
        }

        if (parentTeamId.HasValue)
        {
            query = query.Where(t => t.ParentTeamId == parentTeamId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Name.Contains(search.Trim()));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Team>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<Team>>.Success(result);
    }

    public async Task<ServiceResult<PagedResult<Team>>> GetSubTeamsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var teamExists = await dbContext.Teams.AnyAsync(t => t.Id == id, cancellationToken);
        if (!teamExists)
        {
            return ServiceResult<PagedResult<Team>>.NotFound("Time não encontrado.");
        }

        var query = dbContext.Teams
            .AsNoTracking()
            .Where(t => t.ParentTeamId == id);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Team>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<Team>>.Success(result);
    }

    public async Task<ServiceResult<PagedResult<Collaborator>>> GetCollaboratorsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var teamExists = await dbContext.Teams.AnyAsync(t => t.Id == id, cancellationToken);
        if (!teamExists)
        {
            return ServiceResult<PagedResult<Collaborator>>.NotFound("Time não encontrado.");
        }

        var query = dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.TeamId == id);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Collaborator>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<Collaborator>>.Success(result);
    }
}
