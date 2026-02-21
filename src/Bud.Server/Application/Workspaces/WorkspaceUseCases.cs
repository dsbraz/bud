using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Workspaces;

public sealed class CreateWorkspace(
    IWorkspaceRepository workspaceRepository,
    IOrganizationRepository organizationRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Workspace>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, request.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return Result<Workspace>.Forbidden("Apenas o proprietário da organização pode criar workspaces.");
        }

        if (!await organizationRepository.ExistsAsync(request.OrganizationId, cancellationToken))
        {
            return Result<Workspace>.NotFound("Organização não encontrada.");
        }

        if (!await workspaceRepository.IsNameUniqueAsync(request.OrganizationId, request.Name, ct: cancellationToken))
        {
            return Result<Workspace>.Failure("Já existe um workspace com este nome nesta organização.", ErrorType.Conflict);
        }

        try
        {
            var workspace = Workspace.Create(Guid.NewGuid(), request.OrganizationId, request.Name);

            await workspaceRepository.AddAsync(workspace, cancellationToken);
            await unitOfWork.CommitAsync(workspaceRepository.SaveChangesAsync, cancellationToken);

            return Result<Workspace>.Success(workspace);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Workspace>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class RenameWorkspace(
    IWorkspaceRepository workspaceRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Workspace>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        if (workspace is null)
        {
            return Result<Workspace>.NotFound("Workspace não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<Workspace>.Forbidden("Você não tem permissão para atualizar este workspace.");
        }

        if (!await workspaceRepository.IsNameUniqueAsync(workspace.OrganizationId, request.Name, excludeId: id, ct: cancellationToken))
        {
            return Result<Workspace>.Failure("Já existe um workspace com este nome nesta organização.", ErrorType.Conflict);
        }

        try
        {
            workspace.Rename(request.Name);
            await unitOfWork.CommitAsync(workspaceRepository.SaveChangesAsync, cancellationToken);

            return Result<Workspace>.Success(workspace);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Workspace>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class DeleteWorkspace(
    IWorkspaceRepository workspaceRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        if (workspace is null)
        {
            return Result.NotFound("Workspace não encontrado.");
        }

        var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir este workspace.");
        }

        if (await workspaceRepository.HasMissionsAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir o workspace porque existem missões associadas a ele.",
                ErrorType.Conflict);
        }

        await workspaceRepository.RemoveAsync(workspace, cancellationToken);
        await unitOfWork.CommitAsync(workspaceRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

public sealed class ViewWorkspaceDetails(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<Workspace>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(id, cancellationToken);
        return workspace is null
            ? Result<Workspace>.NotFound("Workspace não encontrado.")
            : Result<Workspace>.Success(workspace);
    }
}

public sealed class ListWorkspaces(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<PagedResult<Workspace>>> ExecuteAsync(
        Guid? organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await workspaceRepository.GetAllAsync(organizationId, search, page, pageSize, cancellationToken);
        return Result<PagedResult<Workspace>>.Success(result);
    }
}

public sealed class ListWorkspaceTeams(IWorkspaceRepository workspaceRepository)
{
    public async Task<Result<PagedResult<Team>>> ExecuteAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        if (!await workspaceRepository.ExistsAsync(id, cancellationToken))
        {
            return Result<PagedResult<Team>>.NotFound("Workspace não encontrado.");
        }

        var result = await workspaceRepository.GetTeamsAsync(id, page, pageSize, cancellationToken);
        return Result<PagedResult<Team>>.Success(result);
    }
}
