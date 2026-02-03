using System.Net.Http;

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
            request.Headers.Add("X-User-Email", authState.Session.Email);

            // Use the selected organization from OrganizationContext
            // If null (TODOS selected), don't send X-Tenant-Id to see all orgs
            if (orgContext.SelectedOrganizationId.HasValue)
            {
                request.Headers.Add("X-Tenant-Id",
                    orgContext.SelectedOrganizationId.Value.ToString());
            }

            if (authState.Session.CollaboratorId.HasValue)
            {
                request.Headers.Add("X-Collaborator-Id",
                    authState.Session.CollaboratorId.Value.ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
