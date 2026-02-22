using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Infrastructure.Services;
using Bud.Server.Application.Ports;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;

namespace Bud.Server.Tests.Infrastructure.Services;

public sealed class MissionScopeResolverTests
{
    private static ApplicationDbContext CreateInMemoryContext(TestTenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, tenantProvider);
    }

    [Fact]
    public async Task ResolveScopeOrganizationIdAsync_WithAnotherTenant_WithoutIgnoringFilters_ReturnsNotFound()
    {
        // Arrange
        var selectedTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var tenantProvider = new TestTenantProvider { TenantId = selectedTenantId };
        using var context = CreateInMemoryContext(tenantProvider);

        context.Organizations.AddRange(
            new Organization { Id = selectedTenantId, Name = "selected.org" },
            new Organization { Id = otherTenantId, Name = "other.org" });
        await context.SaveChangesAsync();

        var resolver = new MissionScopeResolver(context);

        // Act
        var result = await resolver.ResolveScopeOrganizationIdAsync(
            MissionScopeType.Organization,
            otherTenantId,
            ignoreQueryFilters: false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task ResolveScopeOrganizationIdAsync_WithAnotherTenant_IgnoringFilters_ReturnsOrganizationId()
    {
        // Arrange
        var selectedTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var tenantProvider = new TestTenantProvider { TenantId = selectedTenantId };
        using var context = CreateInMemoryContext(tenantProvider);

        context.Organizations.AddRange(
            new Organization { Id = selectedTenantId, Name = "selected.org" },
            new Organization { Id = otherTenantId, Name = "other.org" });
        await context.SaveChangesAsync();

        var resolver = new MissionScopeResolver(context);

        // Act
        var result = await resolver.ResolveScopeOrganizationIdAsync(
            MissionScopeType.Organization,
            otherTenantId,
            ignoreQueryFilters: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(otherTenantId);
    }

    [Fact]
    public async Task ResolveScopeOrganizationIdAsync_WithWorkspaceScope_ReturnsWorkspaceOrganizationId()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);

        context.Organizations.Add(new Organization { Id = orgId, Name = "org" });
        context.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "workspace",
            OrganizationId = orgId
        });
        await context.SaveChangesAsync();

        var resolver = new MissionScopeResolver(context);

        // Act
        var result = await resolver.ResolveScopeOrganizationIdAsync(MissionScopeType.Workspace, workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(orgId);
    }

    [Fact]
    public async Task ResolveScopeOrganizationIdAsync_WithInvalidWorkspaceScope_ReturnsNotFound()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var resolver = new MissionScopeResolver(context);

        // Act
        var result = await resolver.ResolveScopeOrganizationIdAsync(MissionScopeType.Workspace, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.Should().Be("Workspace não encontrado.");
    }
}
