namespace Bud.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string TenantSelected = "TenantSelected";
    public const string GlobalAdmin = "GlobalAdmin";
    public const string OrganizationOwner = "OrganizationOwner";
    public const string OrganizationWrite = "OrganizationWrite";
}
