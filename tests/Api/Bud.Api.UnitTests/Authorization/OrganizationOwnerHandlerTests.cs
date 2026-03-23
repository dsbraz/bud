using System.Security.Claims;
using Bud.Api.Authorization.Handlers;
using Bud.Api.Authorization.Requirements;
using Bud.Api.Authorization.ResourceScopes;
using Bud.Api.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Bud.Api.UnitTests.Authorization;

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
