using Microsoft.AspNetCore.Authorization;

namespace Bud.Server.Authorization.Requirements;

public sealed class OrganizationOwnerRequirement : IAuthorizationRequirement
{
}
