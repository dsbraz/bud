using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionTemplates;

public interface IMissionTemplateQueryUseCase
{
    Task<ServiceResult<MissionTemplate>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<MissionTemplate>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
