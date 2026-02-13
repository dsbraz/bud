using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Settings;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bud.Server.Tests.Services;

public class OrganizationServiceTests
{
    private static ApplicationDbContext CreateInMemoryContext(TestTenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, tenantProvider);
    }

    private static OrganizationService CreateService(ApplicationDbContext context)
    {
        var settings = Options.Create(new GlobalAdminSettings
        {
            Email = "admin@getbud.co",
            OrganizationName = "getbud.co"
        });

        return new OrganizationService(context, settings);
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ReturnsNotFoundInPortuguese()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task GetWorkspaces_WithNonExistingOrganization_ReturnsNotFoundInPortuguese()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        // Act
        var result = await service.GetWorkspacesAsync(Guid.NewGuid(), 1, 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_ReturnsFilteredOrganizations()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        var owner = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = "owner@test.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = Guid.NewGuid()
        };
        var org1 = new Organization { Id = Guid.NewGuid(), Name = "ALPHA Corp", OwnerId = owner.Id };
        var org2 = new Organization { Id = Guid.NewGuid(), Name = "Beta Corp", OwnerId = owner.Id };
        context.Collaborators.Add(owner);
        context.Organizations.AddRange(org1, org2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync("alpha", 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("ALPHA Corp");
    }

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithNonExistentOrganization_ReturnsNotFound()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), new Bud.Shared.Contracts.UpdateOrganizationRequest
        {
            Name = "New Name"
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentOwner_ReturnsNotFound()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateAsync(org.Id, new Bud.Shared.Contracts.UpdateOrganizationRequest
        {
            Name = "New Name",
            OwnerId = Guid.NewGuid() // Non-existent
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("O líder selecionado não foi encontrado.");
    }

    [Fact]
    public async Task UpdateAsync_WithOwnerNotLeader_ReturnsValidationError()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);

        var nonLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Non Leader",
            Email = "nonleader@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = org.Id
        };
        context.Collaborators.Add(nonLeader);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateAsync(org.Id, new Bud.Shared.Contracts.UpdateOrganizationRequest
        {
            Name = "New Name",
            OwnerId = nonLeader.Id
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("O proprietário da organização deve ter a função de Líder.");
    }

    [Fact]
    public async Task UpdateAsync_ProtectedOrganization_ReturnsValidationError()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        // Create protected org with name "getbud.co"
        var protectedOrg = new Organization { Id = Guid.NewGuid(), Name = "getbud.co" };
        context.Organizations.Add(protectedOrg);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateAsync(protectedOrg.Id, new Bud.Shared.Contracts.UpdateOrganizationRequest
        {
            Name = "New Name"
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Esta organização está protegida e não pode ser alterada.");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithNonExistentOrganization_ReturnsNotFound()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Organização não encontrada.");
    }

    [Fact]
    public async Task DeleteAsync_ProtectedOrganization_ReturnsValidationError()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        var protectedOrg = new Organization { Id = Guid.NewGuid(), Name = "getbud.co" };
        context.Organizations.Add(protectedOrg);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(protectedOrg.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Esta organização está protegida e não pode ser excluída.");
    }

    [Fact]
    public async Task DeleteAsync_WithWorkspaces_ReturnsConflict()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Workspace",
            OrganizationId = org.Id
        };
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(org.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Error.Should().Be("Não é possível excluir a organização porque ela possui workspaces associados. Exclua os workspaces primeiro.");
    }

    [Fact]
    public async Task DeleteAsync_WithCollaborators_ReturnsConflict()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "user@example.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(org.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Error.Should().Be("Não é possível excluir a organização porque ela possui colaboradores associados. Remova os colaboradores primeiro.");
    }

    [Fact]
    public async Task DeleteAsync_WithValidOrganization_Succeeds()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider { IsGlobalAdmin = true };
        using var context = CreateInMemoryContext(tenantProvider);
        var service = CreateService(context);

        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(org.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deletedOrg = await context.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == org.Id);
        deletedOrg.Should().BeNull();
    }

    #endregion
}
