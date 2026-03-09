using System.Security.Claims;
using Bud.Api.Authorization.Handlers;
using Bud.Api.Authorization.Requirements;
using Bud.Api.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Bud.Api.UnitTests.Authorization;

public sealed class GlobalAdminHandlerTests
{
    [Fact]
    public async Task Handle_WhenGlobalAdmin_ShouldSucceed()
    {
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        var handler = new GlobalAdminHandler(tenantProvider);
        var requirement = new GlobalAdminRequirement();

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNotGlobalAdmin_ShouldFail()
    {
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = false };
        var handler = new GlobalAdminHandler(tenantProvider);
        var requirement = new GlobalAdminRequirement();

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
