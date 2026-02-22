using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.Ports;

namespace Bud.Server.Application.UseCases.Me;

public sealed class ListMyOrganizations(IAuthService authService)
{
    public async Task<Result<List<OrganizationSummaryResponse>>> ExecuteAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.GetMyOrganizationsAsync(email, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<OrganizationSummaryResponse>>.Failure(result.Error ?? "Falha ao carregar organizações.", result.ErrorType);
        }

        return Result<List<OrganizationSummaryResponse>>.Success(result.Value!.Select(o => o.ToResponse()).ToList());
    }
}
