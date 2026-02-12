using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface ITeamCommandService
{
    Task<ServiceResult<Team>> CreateAsync(CreateTeamRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Team>> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateCollaboratorsAsync(Guid teamId, UpdateTeamCollaboratorsRequest request, CancellationToken cancellationToken = default);
}
