using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionTemplates;

public sealed class MissionTemplateQueryUseCase(
    IMissionTemplateService templateService) : IMissionTemplateQueryUseCase
{
    public Task<ServiceResult<MissionTemplate>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => templateService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<MissionTemplate>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => templateService.GetAllAsync(search, page, pageSize, cancellationToken);
}
