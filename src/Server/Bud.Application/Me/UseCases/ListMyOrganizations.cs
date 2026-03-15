using Bud.Application.Common;

namespace Bud.Application.Me;

public sealed class ListMyOrganizations(IMyOrganizationsReadStore myOrganizationsReadStore)
{
    public async Task<Result<List<MyOrganizationResponse>>> ExecuteAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var result = await myOrganizationsReadStore.GetMyOrganizationsAsync(email, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<MyOrganizationResponse>>.Failure(result.Error ?? UserErrorMessages.ListOrganizationsFailed, result.ErrorType);
        }

        return Result<List<MyOrganizationResponse>>.Success(result.Value!.Select(o => o.ToResponse()).ToList());
    }
}
