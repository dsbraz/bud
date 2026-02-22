using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace Bud.Client.Services;

public sealed class TenantDelegatingHandler(
    AuthState authState,
    OrganizationContext orgContext,
    NavigationManager navigationManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await authState.EnsureInitializedAsync();

        if (authState.SessionResponse is not null)
        {
            // Add JWT Authorization header
            if (!string.IsNullOrEmpty(authState.SessionResponse.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.SessionResponse.Token);
            }

            // Add user email header for organization lookup
            if (!string.IsNullOrEmpty(authState.SessionResponse.Email))
            {
                request.Headers.Add("X-User-Email", authState.SessionResponse.Email);
            }

            // Use the selected organization from OrganizationContext
            // If null (TODOS selected), don't send X-Tenant-Id to see all orgs
            if (orgContext.SelectedOrganizationId.HasValue)
            {
                request.Headers.Add("X-Tenant-Id",
                    orgContext.SelectedOrganizationId.Value.ToString());
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If 401 Unauthorized, clear session and redirect to login
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await authState.ClearAsync();
            navigationManager.NavigateTo("/login", forceLoad: true);
        }

        return response;
    }
}
