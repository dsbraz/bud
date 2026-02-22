using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using ApiMissionScopeType = Bud.Shared.Contracts.MissionScopeType;
using DomainMissionScopeType = Bud.Server.Domain.Model.MissionScopeType;

namespace Bud.Server.Application.UseCases.Missions;

public sealed class ListMissionsByScope(IMissionRepository missionRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Mission>>> ExecuteAsync(
        ApiMissionScopeType? scopeType,
        Guid? scopeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        DomainMissionScopeType? domainScopeType = scopeType.HasValue ? scopeType.Value.ToDomain() : null;

        var result = await missionRepository.GetAllAsync(
            domainScopeType,
            scopeId,
            search,
            page,
            pageSize,
            cancellationToken);

        return Result<Bud.Shared.Contracts.Common.PagedResult<Mission>>.Success(result.MapPaged(x => x));
    }
}
