using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Mapping;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Application.UseCases.Templates;

public sealed class ListTemplates(ITemplateRepository templateRepository)
{
    public async Task<Result<Bud.Shared.Contracts.Common.PagedResult<Template>>> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await templateRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<Bud.Shared.Contracts.Common.PagedResult<Template>>.Success(result.MapPaged(x => x));
    }
}
