using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionTemplates;

public sealed class MissionTemplateQueryUseCase(
    IMissionTemplateRepository templateRepository) : IMissionTemplateQueryUseCase
{
    public async Task<Result<MissionTemplate>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await templateRepository.GetByIdReadOnlyAsync(id, cancellationToken);
        return template is null
            ? Result<MissionTemplate>.NotFound("Template de missão não encontrado.")
            : Result<MissionTemplate>.Success(template);
    }

    public async Task<Result<PagedResult<MissionTemplate>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await templateRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<PagedResult<MissionTemplate>>.Success(result);
    }
}
