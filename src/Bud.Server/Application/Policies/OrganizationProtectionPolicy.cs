namespace Bud.Server.Application.Policies;

internal static class OrganizationProtectionPolicy
{
    public static bool IsProtectedOrganization(string organizationName, string globalAdminOrganizationName)
    {
        return !string.IsNullOrWhiteSpace(globalAdminOrganizationName)
            && organizationName.Equals(globalAdminOrganizationName, StringComparison.OrdinalIgnoreCase);
    }
}
