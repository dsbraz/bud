using System.Security.Claims;
using Bud.Api.Authorization.Handlers;
using Bud.Api.Authorization.Requirements;
using Bud.Api.Authorization.ResourceScopes;
using Bud.Api.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Bud.Api.UnitTests.Authorization;

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
