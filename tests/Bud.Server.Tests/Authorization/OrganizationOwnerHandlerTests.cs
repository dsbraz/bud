using Bud.Server.Authorization.Handlers;
using Bud.Server.Authorization.Requirements;
using Bud.Server.Authorization.ResourceScopes;
using Bud.Server.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace Bud.Server.Tests.Authorization;

public sealed class OrganizationOwnerHandlerTests
{
    [Fact]
    public async Task Handle_WhenOwnerAllowed_ShouldSucceed()
    {
        var orgAuth = new TestOrganizationAuthorizationService
        {
            ShouldAllowOwnerAccess = true
        };

        var handler = new OrganizationOwnerHandler(orgAuth);
        var requirement = new OrganizationOwnerRequirement();
        var resource = new OrganizationResource(Guid.NewGuid());

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenOwnerDenied_ShouldFail()
    {
        var orgAuth = new TestOrganizationAuthorizationService
        {
            ShouldAllowOwnerAccess = false
        };

        var handler = new OrganizationOwnerHandler(orgAuth);
        var requirement = new OrganizationOwnerRequirement();
        var resource = new OrganizationResource(Guid.NewGuid());

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
