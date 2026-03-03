namespace Bud.Server.Authorization;

public static class AuthorizationPolicies
{
    public const string TenantSelected = "TenantSelected";
    public const string GlobalAdmin = "GlobalAdmin";
    public const string TenantOrganizationMatch = "TenantOrganizationMatch";
    public const string OrganizationOwner = "OrganizationOwner";
    public const string OrganizationWrite = "OrganizationWrite";
}
