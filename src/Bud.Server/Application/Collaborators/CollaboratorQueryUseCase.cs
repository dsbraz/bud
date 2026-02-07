using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Collaborators;

public sealed class CollaboratorQueryUseCase(ICollaboratorService collaboratorService) : ICollaboratorQueryUseCase
{
    public Task<ServiceResult<Collaborator>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => collaboratorService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<List<LeaderCollaboratorResponse>>> GetLeadersAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default)
        => collaboratorService.GetLeadersAsync(organizationId, cancellationToken);

    public Task<ServiceResult<PagedResult<Collaborator>>> GetAllAsync(
        Guid? teamId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => collaboratorService.GetAllAsync(teamId, search, page, pageSize, cancellationToken);

    public Task<ServiceResult<List<TeamSummaryDto>>> GetTeamsAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
        => collaboratorService.GetTeamsAsync(collaboratorId, cancellationToken);

    public Task<ServiceResult<List<TeamSummaryDto>>> GetAvailableTeamsAsync(
        Guid collaboratorId,
        string? search,
        CancellationToken cancellationToken = default)
        => collaboratorService.GetAvailableTeamsAsync(collaboratorId, search, cancellationToken);
}
