using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Services;

public interface IMissionObjectiveService
{
    Task<ServiceResult<MissionObjective>> CreateAsync(CreateMissionObjectiveRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<MissionObjective>> UpdateAsync(Guid id, UpdateMissionObjectiveRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<MissionObjective>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<MissionObjective>>> GetByMissionAsync(Guid missionId, Guid? parentObjectiveId, int page, int pageSize, CancellationToken cancellationToken = default);
}
