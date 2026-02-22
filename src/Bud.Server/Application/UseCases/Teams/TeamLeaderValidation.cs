using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Teams;

internal static class TeamLeaderValidation
{
    public static async Task<Result<Team>?> ValidateAsync(
        ICollaboratorRepository collaboratorRepository,
        Guid leaderId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var leader = await collaboratorRepository.GetByIdAsync(leaderId, cancellationToken);
        if (leader is null)
        {
            return Result<Team>.NotFound("Líder não encontrado.");
        }

        if (leader.Role != CollaboratorRole.Leader)
        {
            return Result<Team>.Failure(
                "O colaborador selecionado como líder deve ter o perfil de Líder.",
                ErrorType.Validation);
        }

        if (leader.OrganizationId != organizationId)
        {
            return Result<Team>.Failure(
                "O líder deve pertencer à mesma organização do time.",
                ErrorType.Validation);
        }

        return null;
    }
}
