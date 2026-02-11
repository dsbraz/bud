using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IMissionTemplateService
{
    Task<ServiceResult<MissionTemplate>> CreateAsync(CreateMissionTemplateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<MissionTemplate>> UpdateAsync(Guid id, UpdateMissionTemplateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<MissionTemplate>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<MissionTemplate>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
