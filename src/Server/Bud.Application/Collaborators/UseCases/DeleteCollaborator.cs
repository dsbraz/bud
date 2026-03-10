using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Collaborators;

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
            return Result.NotFound(UserErrorMessages.CollaboratorNotFound);
        }

        var canDelete = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogCollaboratorDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden(UserErrorMessages.CollaboratorDeleteForbidden);
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

        if (await collaboratorRepository.HasGoalsAsync(id, cancellationToken))
        {
            LogCollaboratorDeletionFailed(logger, id, "Has goals");
            return Result.Failure(
                "Não é possível excluir o colaborador porque existem metas associadas a ele.",
                ErrorType.Conflict);
        }

        await collaboratorRepository.RemoveAsync(collaborator, cancellationToken);
        await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);

        LogCollaboratorDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4046, Level = LogLevel.Information, Message = "Deleting collaborator {CollaboratorId}")]
    private static partial void LogDeletingCollaborator(ILogger logger, Guid collaboratorId);

    [LoggerMessage(EventId = 4047, Level = LogLevel.Information, Message = "Collaborator deleted successfully: {CollaboratorId}")]
    private static partial void LogCollaboratorDeleted(ILogger logger, Guid collaboratorId);

    [LoggerMessage(EventId = 4048, Level = LogLevel.Warning, Message = "Collaborator deletion failed for {CollaboratorId}: {Reason}")]
    private static partial void LogCollaboratorDeletionFailed(ILogger logger, Guid collaboratorId, string reason);
}
