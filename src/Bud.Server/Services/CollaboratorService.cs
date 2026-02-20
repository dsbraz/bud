using Bud.Server.Data;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Domain.Specifications;
using Bud.Server.Domain.ValueObjects;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
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

        if (!EmailAddress.TryCreate(request.Email, out var emailAddress))
        {
            return ServiceResult<Collaborator>.Failure("E-mail inválido.", ServiceErrorType.Validation);
        }

        if (!PersonName.TryCreate(request.FullName, out var personName))
        {
            return ServiceResult<Collaborator>.Failure("O nome do colaborador é obrigatório.", ServiceErrorType.Validation);
        }

        try
        {
            // Criar colaborador SEM team
            var collaborator = Collaborator.Create(
                Guid.NewGuid(),
                organizationId.Value,
                personName.Value,
                emailAddress.Value,
                request.Role,
                request.LeaderId);

            dbContext.Collaborators.Add(collaborator);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<Collaborator>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<Collaborator>> UpdateAsync(Guid id, UpdateCollaboratorRequest request, CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators.FindAsync([id], cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<Collaborator>.NotFound("Colaborador não encontrado.");
        }

        if (!EmailAddress.TryCreate(request.Email, out var emailAddress))
        {
            return ServiceResult<Collaborator>.Failure("E-mail inválido.", ServiceErrorType.Validation);
        }

        if (!PersonName.TryCreate(request.FullName, out var personName))
        {
            return ServiceResult<Collaborator>.Failure("O nome do colaborador é obrigatório.", ServiceErrorType.Validation);
        }

        if (collaborator.Email != emailAddress.Value)
        {
            var emailExists = await dbContext.Collaborators
                .AnyAsync(c => c.Email == emailAddress.Value && c.Id != id, cancellationToken);

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

        try
        {
            collaborator.UpdateProfile(
                personName.Value,
                emailAddress.Value,
                request.Role,
                request.LeaderId,
                collaborator.Id);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<Collaborator>.Failure(ex.Message, ServiceErrorType.Validation);
        }
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

        // Validação 3: Verificar se tem missões associadas
        var hasMissions = await dbContext.Missions.AnyAsync(m => m.CollaboratorId == id, cancellationToken);
        if (hasMissions)
        {
            return ServiceResult.Failure(
                "Não é possível excluir o colaborador porque existem missões associadas a ele.",
                ServiceErrorType.Conflict);
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

        query = new CollaboratorSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

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

    public async Task<ServiceResult<List<LeaderCollaborator>>> GetLeadersAsync(Guid? organizationId = null, CancellationToken cancellationToken = default)
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
            .Select(c => new LeaderCollaborator
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                TeamName = c.Team != null ? c.Team.Name : null,
                WorkspaceName = c.Team != null && c.Team.Workspace != null ? c.Team.Workspace.Name : null,
                OrganizationName = c.Organization.Name
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<List<LeaderCollaborator>>.Success(leaders);
    }

    public async Task<ServiceResult<List<CollaboratorHierarchyNode>>> GetSubordinatesAsync(Guid collaboratorId, int maxDepth = 5, CancellationToken cancellationToken = default)
    {
        if (!await CollaboratorExistsAsync(collaboratorId, cancellationToken))
        {
            return ServiceResult<List<CollaboratorHierarchyNode>>.NotFound("Colaborador não encontrado.");
        }

        var childrenByLeader = await LoadChildrenByLeaderAsync(cancellationToken);
        var tree = BuildSubordinateTree(childrenByLeader, collaboratorId, currentDepth: 0, maxDepth);

        return ServiceResult<List<CollaboratorHierarchyNode>>.Success(tree);
    }

    private async Task<bool> CollaboratorExistsAsync(Guid collaboratorId, CancellationToken cancellationToken)
    {
        return await dbContext.Collaborators
            .AsNoTracking()
            .AnyAsync(c => c.Id == collaboratorId, cancellationToken);
    }

    private async Task<Dictionary<Guid, List<Collaborator>>> LoadChildrenByLeaderAsync(CancellationToken cancellationToken)
    {
        var allSubordinates = await dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.LeaderId != null)
            .ToListAsync(cancellationToken);

        return allSubordinates
            .GroupBy(c => c.LeaderId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.FullName).ToList());
    }

    private static List<CollaboratorHierarchyNode> BuildSubordinateTree(
        Dictionary<Guid, List<Collaborator>> childrenByLeader,
        Guid parentId,
        int currentDepth,
        int maxDepth)
    {
        if (currentDepth >= maxDepth || !childrenByLeader.TryGetValue(parentId, out var children))
        {
            return [];
        }

        return children.Select(c => new CollaboratorHierarchyNode
        {
            Id = c.Id,
            FullName = c.FullName,
            Initials = GetInitials(c.FullName),
            Role = c.Role == CollaboratorRole.Leader ? "Líder" : "Contribuidor individual",
            Children = BuildSubordinateTree(childrenByLeader, c.Id, currentDepth + 1, maxDepth)
        }).ToList();
    }

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][..1].ToUpperInvariant();
        }

        return $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant();
    }

    public async Task<ServiceResult<List<TeamSummary>>> GetTeamsAsync(Guid collaboratorId, CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<List<TeamSummary>>.NotFound("Colaborador não encontrado.");
        }

        var teams = await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct => ct.CollaboratorId == collaboratorId)
            .Include(ct => ct.Team)
                .ThenInclude(t => t.Workspace)
            .Select(ct => new TeamSummary
            {
                Id = ct.Team.Id,
                Name = ct.Team.Name,
                WorkspaceName = ct.Team.Workspace.Name
            })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return ServiceResult<List<TeamSummary>>.Success(teams);
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

    public async Task<ServiceResult<List<CollaboratorSummary>>> GetSummariesAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Collaborators.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = new CollaboratorSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);
        }

        var collaborators = await query
            .Select(c => new CollaboratorSummary
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Role = c.Role
            })
            .OrderBy(c => c.FullName)
            .Take(50)
            .ToListAsync(cancellationToken);

        return ServiceResult<List<CollaboratorSummary>>.Success(collaborators);
    }

    public async Task<ServiceResult<List<TeamSummary>>> GetAvailableTeamsAsync(Guid collaboratorId, string? search = null, CancellationToken cancellationToken = default)
    {
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<List<TeamSummary>>.NotFound("Colaborador não encontrado.");
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
            query = new TeamSearchSpecification(search, dbContext.Database.IsNpgsql(), includeWorkspaceName: true).Apply(query);
        }

        var teams = await query
            .Select(t => new TeamSummary
            {
                Id = t.Id,
                Name = t.Name,
                WorkspaceName = t.Workspace.Name
            })
            .OrderBy(t => t.Name)
            .Take(50)
            .ToListAsync(cancellationToken);

        return ServiceResult<List<TeamSummary>>.Success(teams);
    }

}
