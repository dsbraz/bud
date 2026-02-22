using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Workspaces;

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

