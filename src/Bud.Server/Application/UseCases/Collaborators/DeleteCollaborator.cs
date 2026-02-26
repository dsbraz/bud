using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Collaborators;

public sealed partial class DeleteCollaborator(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteCollaborator> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingCollaborator(logger, id);

        var collaborator = await collaboratorRepository.GetByIdAsync(id, cancellationToken);
        if (collaborator is null)
        {
            LogCollaboratorDeletionFailed(logger, id, "Not found");
            return Result.NotFound("Colaborador não encontrado.");
        }

        var canDelete = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogCollaboratorDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden("Apenas o proprietário da organização pode excluir colaboradores.");
        }

        if (await collaboratorRepository.IsOrganizationOwnerAsync(id, cancellationToken))
        {
            LogCollaboratorDeletionFailed(logger, id, "Is organization owner");
            return Result.Failure(
                "Não é possível excluir o colaborador. Ele é proprietário de uma organização.",
                ErrorType.Conflict);
        }

        if (await collaboratorRepository.HasSubordinatesAsync(id, cancellationToken))
        {
            LogCollaboratorDeletionFailed(logger, id, "Has subordinates");
            return Result.Failure(
                "Não é possível excluir o colaborador. Ele é líder de outros colaboradores.",
                ErrorType.Conflict);
        }

        if (await collaboratorRepository.HasMissionsAsync(id, cancellationToken))
        {
            LogCollaboratorDeletionFailed(logger, id, "Has missions");
            return Result.Failure(
                "Não é possível excluir o colaborador porque existem missões associadas a ele.",
                ErrorType.Conflict);
        }

        await collaboratorRepository.RemoveAsync(collaborator, cancellationToken);
        await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);

        LogCollaboratorDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4048, Level = LogLevel.Information, Message = "Deleting collaborator {CollaboratorId}")]
    private static partial void LogDeletingCollaborator(ILogger logger, Guid collaboratorId);

    [LoggerMessage(EventId = 4049, Level = LogLevel.Information, Message = "Collaborator deleted successfully: {CollaboratorId}")]
    private static partial void LogCollaboratorDeleted(ILogger logger, Guid collaboratorId);

    [LoggerMessage(EventId = 4050, Level = LogLevel.Warning, Message = "Collaborator deletion failed for {CollaboratorId}: {Reason}")]
    private static partial void LogCollaboratorDeletionFailed(ILogger logger, Guid collaboratorId, string reason);
}
