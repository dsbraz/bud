using System.Security.Claims;
using Bud.Api.Authorization.Handlers;
using Bud.Api.Authorization.Requirements;
using Bud.Api.Authorization.ResourceScopes;
using Bud.Api.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Bud.Api.UnitTests.Authorization;

public sealed class OrganizationWriteHandlerTests
{
    [Fact]
    public async Task Handle_WhenWriteAllowed_ShouldSucceed()
    {
        var orgAuth = new TestOrganizationAuthorizationService
        {
            ShouldAllowWriteAccess = true
        };

        var handler = new OrganizationWriteHandler(orgAuth);
        var requirement = new OrganizationWriteRequirement();
        var resource = new OrganizationResource(Guid.NewGuid());

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWriteDenied_ShouldFail()
    {
        var orgAuth = new TestOrganizationAuthorizationService
        {
            ShouldAllowWriteAccess = false
        };

        var handler = new OrganizationWriteHandler(orgAuth);
        var requirement = new OrganizationWriteRequirement();
        var resource = new OrganizationResource(Guid.NewGuid());

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
