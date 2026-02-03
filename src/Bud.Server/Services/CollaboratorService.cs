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
        var team = await dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken);

        if (team is null)
        {
            return ServiceResult<Collaborator>.NotFound("Time não encontrado.");
        }

        if (!tenantProvider.IsAdmin)
        {
            var isOwner = await IsOrgOwnerAsync(team.OrganizationId, cancellationToken);
            if (!isOwner)
            {
                return ServiceResult<Collaborator>.Forbidden("Apenas o proprietário da organização pode criar colaboradores.");
            }
        }

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = request.Role,
            OrganizationId = team.OrganizationId,
            TeamId = request.TeamId,
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
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var query = dbContext.Collaborators.AsNoTracking();

        if (teamId.HasValue)
        {
            query = query.Where(c => c.TeamId == teamId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.FullName.Contains(term) || c.Email.Contains(term));
        }

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

    public async Task<ServiceResult<List<LeaderCollaboratorResponse>>> GetLeadersAsync(CancellationToken cancellationToken = default)
    {
        var leaders = await dbContext.Collaborators
            .Include(c => c.Team)
                .ThenInclude(t => t.Workspace)
                    .ThenInclude(w => w.Organization)
            .Where(c => c.Role == CollaboratorRole.Leader)
            .OrderBy(c => c.FullName)
            .Select(c => new LeaderCollaboratorResponse
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                TeamName = c.Team.Name,
                WorkspaceName = c.Team.Workspace.Name,
                OrganizationName = c.Team.Workspace.Organization.Name
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<List<LeaderCollaboratorResponse>>.Success(leaders);
    }

    private async Task<bool> IsOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (tenantProvider.CollaboratorId is null)
            return false;

        return await dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(o =>
                o.Id == organizationId &&
                o.OwnerId == tenantProvider.CollaboratorId.Value,
                cancellationToken);
    }
}
