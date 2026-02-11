using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Collaborators;

public interface ICollaboratorQueryUseCase
{
    Task<ServiceResult<Collaborator>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<List<LeaderCollaboratorResponse>>> GetLeadersAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<Collaborator>>> GetAllAsync(
        Guid? teamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<List<CollaboratorHierarchyNodeDto>>> GetSubordinatesAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<List<TeamSummaryDto>>> GetTeamsAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<List<TeamSummaryDto>>> GetAvailableTeamsAsync(
        Guid collaboratorId,
        string? search,
        CancellationToken cancellationToken = default);
}
