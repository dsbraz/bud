using System.Security.Claims;
using Bud.Server.Authorization;
using Bud.Server.Authorization.Handlers;
using Bud.Server.Authorization.Requirements;
using Bud.Server.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Bud.Server.Tests.Authorization;

public sealed class TenantSelectedHandlerTests
{
    [Fact]
    public async Task Handle_WhenGlobalAdmin_ShouldSucceed()
    {
        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = true,
            TenantId = null
        };

        var handler = new TenantSelectedHandler(tenantProvider);
        var requirement = new TenantSelectedRequirement();

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTenantSelected_ShouldSucceed()
    {
        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            TenantId = Guid.NewGuid()
        };

        var handler = new TenantSelectedHandler(tenantProvider);
        var requirement = new TenantSelectedRequirement();

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTenantMissing_ShouldFail()
    {
        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            TenantId = null
        };

        var handler = new TenantSelectedHandler(tenantProvider);
        var requirement = new TenantSelectedRequirement();

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeFalse();
    }
}
