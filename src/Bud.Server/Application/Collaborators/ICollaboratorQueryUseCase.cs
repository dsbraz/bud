using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Collaborators;

public interface ICollaboratorQueryUseCase
{
    Task<Result<Collaborator>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<List<LeaderCollaboratorResponse>>> GetLeadersAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Collaborator>>> GetAllAsync(
        Guid? teamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<List<CollaboratorHierarchyNodeDto>>> GetSubordinatesAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default);

    Task<Result<List<TeamSummaryDto>>> GetTeamsAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default);

    Task<Result<List<TeamSummaryDto>>> GetAvailableTeamsAsync(
        Guid collaboratorId,
        string? search,
        CancellationToken cancellationToken = default);

    Task<Result<List<CollaboratorSummaryDto>>> GetSummariesAsync(
        string? search,
        CancellationToken cancellationToken = default);
}
