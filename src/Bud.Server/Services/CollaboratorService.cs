using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class CollaboratorService(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider) : ICollaboratorService
{
    public async Task<ServiceResult<Collaborator>> CreateAsync(CreateCollaboratorRequest request, CancellationToken cancellationToken = default)
    {
        // Obter OrganizationId do TenantProvider
        var organizationId = tenantProvider.TenantId;

        if (!organizationId.HasValue)
        {
            return ServiceResult<Collaborator>.Failure(
                "Contexto de organização não encontrado.",
                ServiceErrorType.Validation);
        }

        // Criar colaborador SEM team
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = request.Role,
            OrganizationId = organizationId.Value,
            TeamId = null, // Sempre null para novos colaboradores
            LeaderId = request.LeaderId,
        };

        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Collaborator>.Success(collaborator);
    }

    public async Task<ServiceResult<Collaborator>> UpdateAsync(Guid id, UpdateCollaboratorRequest request, CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators.FindAsync([id], cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<Collaborator>.NotFound("Colaborador não encontrado.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (collaborator.Email != normalizedEmail)
        {
            var emailExists = await dbContext.Collaborators
                .AnyAsync(c => c.Email == normalizedEmail && c.Id != id, cancellationToken);

            if (emailExists)
            {
                return ServiceResult<Collaborator>.Failure("O email já está em uso.", ServiceErrorType.Validation);
            }
        }

        if (request.LeaderId.HasValue)
        {
            var leader = await dbContext.Collaborators
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.LeaderId.Value, cancellationToken);

            if (leader is null)
            {
                return ServiceResult<Collaborator>.NotFound("Líder não encontrado.");
            }

            if (leader.OrganizationId != collaborator.OrganizationId)
            {
                return ServiceResult<Collaborator>.Failure("O líder deve pertencer à mesma organização.", ServiceErrorType.Validation);
            }

            if (leader.Role != CollaboratorRole.Leader)
            {
                return ServiceResult<Collaborator>.Failure("O colaborador selecionado não é um líder.", ServiceErrorType.Validation);
            }
        }

        // Validação: Se estiver tentando mudar de Leader para IndividualContributor
        if (collaborator.Role == CollaboratorRole.Leader &&
            request.Role == CollaboratorRole.IndividualContributor)
        {
            // Validação 1: Verificar se tem membros de equipe
            var hasTeamMembers = await dbContext.Collaborators
                .AnyAsync(c => c.LeaderId == id, cancellationToken);

            if (hasTeamMembers)
            {
                return ServiceResult<Collaborator>.Failure(
                    "Não é possível alterar o perfil. Este líder possui membros de equipe.",
                    ServiceErrorType.Validation
                );
            }

            // Validação 2: Verificar se é owner de alguma organização
            var isOrgOwner = await dbContext.Organizations
                .AnyAsync(o => o.OwnerId == id, cancellationToken);

            if (isOrgOwner)
            {
                return ServiceResult<Collaborator>.Failure(
                    "Não é possível alterar o perfil. Este líder é proprietário de uma organização.",
                    ServiceErrorType.Validation
                );
            }
        }

        collaborator.FullName = request.FullName.Trim();
        collaborator.Email = normalizedEmail;
        collaborator.Role = request.Role;
        collaborator.LeaderId = request.LeaderId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Collaborator>.Success(collaborator);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators.FindAsync([id], cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult.NotFound("Colaborador não encontrado.");
        }

        // Validação 1: Verificar se é owner de alguma organização
        var isOrgOwner = await dbContext.Organizations
            .AnyAsync(o => o.OwnerId == id, cancellationToken);

        if (isOrgOwner)
        {
            return ServiceResult.Failure(
                "Não é possível excluir o colaborador. Ele é proprietário de uma organização.",
                ServiceErrorType.Conflict
            );
        }

        // Validação 2: Verificar se tem membros de equipe (é líder de outros)
        var hasTeamMembers = await dbContext.Collaborators
            .AnyAsync(c => c.LeaderId == id, cancellationToken);

        if (hasTeamMembers)
        {
            return ServiceResult.Failure(
                "Não é possível excluir o colaborador. Ele é líder de outros colaboradores.",
                ServiceErrorType.Conflict
            );
        }

        dbContext.Collaborators.Remove(collaborator);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<Collaborator>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return collaborator is null
            ? ServiceResult<Collaborator>.NotFound("Colaborador não encontrado.")
            : ServiceResult<Collaborator>.Success(collaborator);
    }

    public async Task<ServiceResult<PagedResult<Collaborator>>> GetAllAsync(Guid? teamId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Collaborators.AsNoTracking();

        if (teamId.HasValue)
        {
            var teamCollaboratorIds = dbContext.CollaboratorTeams
                .Where(ct => ct.TeamId == teamId.Value)
                .Select(ct => ct.CollaboratorId);
            query = query.Where(c => teamCollaboratorIds.Contains(c.Id));
        }

        query = ApplyCollaboratorSearch(query, search);

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

    public async Task<ServiceResult<List<LeaderCollaboratorResponse>>> GetLeadersAsync(Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Collaborators
            .Include(c => c.Organization)
            .Include(c => c.Team!)
                .ThenInclude(t => t.Workspace)
            .Where(c => c.Role == CollaboratorRole.Leader);

        // Filter by organization if provided
        if (organizationId.HasValue)
        {
            query = query.Where(c => c.OrganizationId == organizationId.Value);
        }

        var leaders = await query
            .OrderBy(c => c.FullName)
            .Select(c => new LeaderCollaboratorResponse
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                TeamName = c.Team != null ? c.Team.Name : null,
                WorkspaceName = c.Team != null && c.Team.Workspace != null ? c.Team.Workspace.Name : null,
                OrganizationName = c.Organization.Name
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<List<LeaderCollaboratorResponse>>.Success(leaders);
    }

    public async Task<ServiceResult<List<TeamSummaryDto>>> GetTeamsAsync(Guid collaboratorId, CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<List<TeamSummaryDto>>.NotFound("Colaborador não encontrado.");
        }

        var teams = await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct => ct.CollaboratorId == collaboratorId)
            .Include(ct => ct.Team)
                .ThenInclude(t => t.Workspace)
            .Select(ct => new TeamSummaryDto
            {
                Id = ct.Team.Id,
                Name = ct.Team.Name,
                WorkspaceName = ct.Team.Workspace.Name
            })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return ServiceResult<List<TeamSummaryDto>>.Success(teams);
    }

    public async Task<ServiceResult> UpdateTeamsAsync(Guid collaboratorId, UpdateCollaboratorTeamsRequest request, CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators
            .Include(c => c.CollaboratorTeams)
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult.NotFound("Colaborador não encontrado.");
        }

        var distinctTeamIds = request.TeamIds.Distinct().ToList();

        if (distinctTeamIds.Count > 0)
        {
            // Validate all teams exist and belong to same organization
            var validTeams = await dbContext.Teams
                .Where(t => distinctTeamIds.Contains(t.Id) && t.OrganizationId == collaborator.OrganizationId)
                .ToListAsync(cancellationToken);

            if (validTeams.Count != distinctTeamIds.Count)
            {
                return ServiceResult.Failure("Uma ou mais equipes são inválidas ou pertencem a outra organização.", ServiceErrorType.Validation);
            }
        }

        // Clear existing and add new
        collaborator.CollaboratorTeams.Clear();

        foreach (var teamId in distinctTeamIds)
        {
            collaborator.CollaboratorTeams.Add(new CollaboratorTeam
            {
                CollaboratorId = collaboratorId,
                TeamId = teamId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<List<TeamSummaryDto>>> GetAvailableTeamsAsync(Guid collaboratorId, string? search = null, CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<List<TeamSummaryDto>>.NotFound("Colaborador não encontrado.");
        }

        var currentTeamIds = await dbContext.CollaboratorTeams
            .Where(ct => ct.CollaboratorId == collaboratorId)
            .Select(ct => ct.TeamId)
            .ToListAsync(cancellationToken);

        var query = dbContext.Teams
            .AsNoTracking()
            .Include(t => t.Workspace)
            .Where(t => t.OrganizationId == collaborator.OrganizationId)
            .Where(t => !currentTeamIds.Contains(t.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = ApplyTeamSearch(query, search);
        }

        var teams = await query
            .Select(t => new TeamSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                WorkspaceName = t.Workspace.Name
            })
            .OrderBy(t => t.Name)
            .Take(50)
            .ToListAsync(cancellationToken);

        return ServiceResult<List<TeamSummaryDto>>.Success(teams);
    }

    private IQueryable<Collaborator> ApplyCollaboratorSearch(IQueryable<Collaborator> query, string? search)
    {
        return SearchQueryHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            dbContext.Database.IsNpgsql(),
            (q, pattern) => q.Where(c =>
                EF.Functions.ILike(c.FullName, pattern) ||
                EF.Functions.ILike(c.Email, pattern)),
            (q, term) => q.Where(c =>
                c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private IQueryable<Team> ApplyTeamSearch(IQueryable<Team> query, string? search)
    {
        return SearchQueryHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            dbContext.Database.IsNpgsql(),
            (q, pattern) => q.Where(t =>
                EF.Functions.ILike(t.Name, pattern) ||
                EF.Functions.ILike(t.Workspace.Name, pattern)),
            (q, term) => q.Where(t =>
                t.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Workspace.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
