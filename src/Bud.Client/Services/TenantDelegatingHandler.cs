using System.Net.Http;
using System.Net.Http.Headers;

namespace Bud.Client.Services;

public sealed class TenantDelegatingHandler(
    AuthState authState,
    OrganizationContext orgContext) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await authState.EnsureInitializedAsync();

        if (authState.Session is not null)
        {
            // Add JWT Authorization header
            if (!string.IsNullOrEmpty(authState.Session.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.Session.Token);
            }

            // Use the selected organization from OrganizationContext
            // If null (TODOS selected), don't send X-Tenant-Id to see all orgs
            if (orgContext.SelectedOrganizationId.HasValue)
            {
                request.Headers.Add("X-Tenant-Id",
                    orgContext.SelectedOrganizationId.Value.ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
