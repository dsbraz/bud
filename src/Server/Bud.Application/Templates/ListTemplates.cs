using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Application.Templates;

public sealed class ListTemplates(ITemplateRepository templateRepository)
{
    public async Task<Result<PagedResult<Template>>> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await templateRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<PagedResult<Template>>.Success(result.MapPaged(x => x));
    }
}
