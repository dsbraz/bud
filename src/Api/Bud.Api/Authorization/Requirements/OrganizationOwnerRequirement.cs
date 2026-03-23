using Microsoft.AspNetCore.Authorization;

namespace Bud.Api.Authorization.Requirements;

public sealed class OrganizationOwnerRequirement : IAuthorizationRequirement
{
}
