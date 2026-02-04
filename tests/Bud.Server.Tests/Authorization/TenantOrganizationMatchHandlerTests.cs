using Bud.Server.Authorization.Handlers;
using Bud.Server.Authorization.Requirements;
using Bud.Server.Authorization.ResourceScopes;
using Bud.Server.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace Bud.Server.Tests.Authorization;

public sealed class TenantOrganizationMatchHandlerTests
{
    [Fact]
    public async Task Handle_WhenGlobalAdmin_ShouldSucceed()
    {
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        var handler = new TenantOrganizationMatchHandler(tenantProvider);
        var requirement = new TenantOrganizationMatchRequirement();
        var resource = new OrganizationResource(Guid.NewGuid());

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTenantMatchesOrganization_ShouldSucceed()
    {
        var orgId = Guid.NewGuid();
        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            TenantId = orgId
        };
        var handler = new TenantOrganizationMatchHandler(tenantProvider);
        var requirement = new TenantOrganizationMatchRequirement();
        var resource = new OrganizationResource(orgId);

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTenantDoesNotMatchOrganization_ShouldFail()
    {
        var tenantProvider = new TestTenantProvider
        {
            IsGlobalAdmin = false,
            TenantId = Guid.NewGuid()
        };
        var handler = new TenantOrganizationMatchHandler(tenantProvider);
        var requirement = new TenantOrganizationMatchRequirement();
        var resource = new OrganizationResource(Guid.NewGuid());

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
