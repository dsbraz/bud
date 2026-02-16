using Bud.Server.Data;
using Bud.Server.Domain.Specifications;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class TeamService(ApplicationDbContext dbContext) : ITeamService
{
    public async Task<ServiceResult<Team>> CreateAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
    {
        try
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

            var leaderValidation = await ValidateLeaderAsync(request.LeaderId, workspace.OrganizationId, cancellationToken);
            if (leaderValidation is not null)
            {
                return leaderValidation;
            }

            var team = Team.Create(
                Guid.NewGuid(),
                workspace.OrganizationId,
                request.WorkspaceId,
                request.Name,
                request.LeaderId,
                request.ParentTeamId);

            team.CollaboratorTeams.Add(new CollaboratorTeam
            {
                CollaboratorId = request.LeaderId,
                TeamId = team.Id,
                AssignedAt = DateTime.UtcNow
            });

            dbContext.Teams.Add(team);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<Team>.Success(team);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<Team>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<Team>> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken = default)
    {
        var team = await dbContext.Teams
            .Include(t => t.CollaboratorTeams)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

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

        var leaderValidation = await ValidateLeaderAsync(request.LeaderId, team.OrganizationId, cancellationToken);
        if (leaderValidation is not null)
        {
            return leaderValidation;
        }

        try
        {
            team.Rename(request.Name);
            team.AssignLeader(request.LeaderId);
            team.Reparent(request.ParentTeamId, team.Id);

            if (!team.CollaboratorTeams.Any(ct => ct.CollaboratorId == request.LeaderId))
            {
                team.CollaboratorTeams.Add(new CollaboratorTeam
                {
                    CollaboratorId = request.LeaderId,
                    TeamId = team.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<Team>.Success(team);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<Team>.Failure(ex.Message, ServiceErrorType.Validation);
        }
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

        var hasMissions = await dbContext.Missions.AnyAsync(m => m.TeamId == id, cancellationToken);
        if (hasMissions)
        {
            return ServiceResult.Failure(
                "Não é possível excluir o time porque existem missões associadas a ele.",
                ServiceErrorType.Conflict);
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

    private async Task<ServiceResult<Team>?> ValidateLeaderAsync(Guid leaderId, Guid organizationId, CancellationToken cancellationToken)
    {
        var leader = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == leaderId, cancellationToken);

        if (leader is null)
        {
            return ServiceResult<Team>.NotFound("Líder não encontrado.");
        }

        if (leader.Role != CollaboratorRole.Leader)
        {
            return ServiceResult<Team>.Failure("O colaborador selecionado como líder deve ter o perfil de Líder.", ServiceErrorType.Validation);
        }

        if (leader.OrganizationId != organizationId)
        {
            return ServiceResult<Team>.Failure("O líder deve pertencer à mesma organização do time.", ServiceErrorType.Validation);
        }

        return null;
    }

    public async Task<ServiceResult<PagedResult<Team>>> GetAllAsync(Guid? workspaceId, Guid? parentTeamId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        IQueryable<Team> query = dbContext.Teams.AsNoTracking().Include(t => t.Leader);

        if (workspaceId.HasValue)
        {
            query = query.Where(t => t.WorkspaceId == workspaceId.Value);
        }

        if (parentTeamId.HasValue)
        {
            query = query.Where(t => t.ParentTeamId == parentTeamId.Value);
        }

        query = new TeamSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

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
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

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
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

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

    public async Task<ServiceResult<List<CollaboratorSummaryDto>>> GetCollaboratorSummariesAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var team = await dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            return ServiceResult<List<CollaboratorSummaryDto>>.NotFound("Time não encontrado.");
        }

        var collaborators = await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct => ct.TeamId == teamId)
            .Include(ct => ct.Collaborator)
            .Select(ct => new CollaboratorSummaryDto
            {
                Id = ct.Collaborator.Id,
                FullName = ct.Collaborator.FullName,
                Email = ct.Collaborator.Email,
                Role = ct.Collaborator.Role
            })
            .OrderBy(c => c.FullName)
            .ToListAsync(cancellationToken);

        return ServiceResult<List<CollaboratorSummaryDto>>.Success(collaborators);
    }

    public async Task<ServiceResult> UpdateCollaboratorsAsync(Guid teamId, UpdateTeamCollaboratorsRequest request, CancellationToken cancellationToken = default)
    {
        var team = await dbContext.Teams
            .Include(t => t.CollaboratorTeams)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            return ServiceResult.NotFound("Time não encontrado.");
        }

        var distinctCollaboratorIds = request.CollaboratorIds.Distinct().ToList();

        if (!distinctCollaboratorIds.Contains(team.LeaderId))
        {
            return ServiceResult.Failure("O líder da equipe deve estar incluído na lista de membros.", ServiceErrorType.Validation);
        }

        if (distinctCollaboratorIds.Count > 0)
        {
            // Validate all collaborators exist and belong to same organization
            var validCollaborators = await dbContext.Collaborators
                .Where(c => distinctCollaboratorIds.Contains(c.Id) && c.OrganizationId == team.OrganizationId)
                .ToListAsync(cancellationToken);

            if (validCollaborators.Count != distinctCollaboratorIds.Count)
            {
                return ServiceResult.Failure("Um ou mais colaboradores são inválidos ou pertencem a outra organização.", ServiceErrorType.Validation);
            }
        }

        // Clear existing and add new
        team.CollaboratorTeams.Clear();

        foreach (var collaboratorId in distinctCollaboratorIds)
        {
            team.CollaboratorTeams.Add(new CollaboratorTeam
            {
                CollaboratorId = collaboratorId,
                TeamId = teamId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<List<CollaboratorSummaryDto>>> GetAvailableCollaboratorsAsync(Guid teamId, string? search = null, CancellationToken cancellationToken = default)
    {
        var team = await dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            return ServiceResult<List<CollaboratorSummaryDto>>.NotFound("Time não encontrado.");
        }

        var currentCollaboratorIds = await dbContext.CollaboratorTeams
            .Where(ct => ct.TeamId == teamId)
            .Select(ct => ct.CollaboratorId)
            .ToListAsync(cancellationToken);

        var query = dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.OrganizationId == team.OrganizationId)
            .Where(c => !currentCollaboratorIds.Contains(c.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = new CollaboratorSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);
        }

        var collaborators = await query
            .Select(c => new CollaboratorSummaryDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Role = c.Role
            })
            .OrderBy(c => c.FullName)
            .Take(50)
            .ToListAsync(cancellationToken);

        return ServiceResult<List<CollaboratorSummaryDto>>.Success(collaborators);
    }

}
