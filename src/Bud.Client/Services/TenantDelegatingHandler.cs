using System.Net.Http;

namespace Bud.Client.Services;

public sealed class TenantDelegatingHandler(AuthState authState) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await authState.EnsureInitializedAsync();

        if (authState.Session is not null)
        {
            request.Headers.Add("X-User-Email", authState.Session.Email);

            if (authState.Session.OrganizationId.HasValue)
            {
                request.Headers.Add("X-Tenant-Id",
                    authState.Session.OrganizationId.Value.ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
