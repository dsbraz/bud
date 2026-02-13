using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Services;

public sealed class CollaboratorValidationServiceTests
{
    [Fact]
    public async Task IsEmailUniqueAsync_WithExistingEmail_ReturnsFalse()
    {
        var context = CreateContext();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.TenantId).Returns(Guid.NewGuid());

        context.Collaborators.Add(new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "User A",
            Email = "user@example.com",
            OrganizationId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var service = new CollaboratorValidationService(context, tenantProvider.Object);
        var isUnique = await service.IsEmailUniqueAsync("user@example.com");

        isUnique.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidLeaderForCreateAsync_WithLeaderInSameTenant_ReturnsTrue()
    {
        var organizationId = Guid.NewGuid();
        var context = CreateContext();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.TenantId).Returns(organizationId);

        var leader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Leader",
            Email = "leader@example.com",
            Role = CollaboratorRole.Leader,
            OrganizationId = organizationId
        };

        context.Collaborators.Add(leader);
        await context.SaveChangesAsync();

        var service = new CollaboratorValidationService(context, tenantProvider.Object);
        var isValid = await service.IsValidLeaderForCreateAsync(leader.Id);

        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidLeaderForUpdateAsync_WithIndividualContributor_ReturnsFalse()
    {
        var context = CreateContext();
        var tenantProvider = new Mock<ITenantProvider>();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Contributor",
            Email = "ic@example.com",
            Role = CollaboratorRole.IndividualContributor,
            OrganizationId = Guid.NewGuid()
        };

        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        var service = new CollaboratorValidationService(context, tenantProvider.Object);
        var isValid = await service.IsValidLeaderForUpdateAsync(collaborator.Id);

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task IsEmailUniqueAsync_InParallel_ShouldBeConsistent()
    {
        var context = CreateContext();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(x => x.TenantId).Returns(Guid.NewGuid());

        context.Collaborators.Add(new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Parallel User",
            Email = "parallel@example.com",
            OrganizationId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var service = new CollaboratorValidationService(context, tenantProvider.Object);
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => service.IsEmailUniqueAsync("parallel@example.com"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(x => x == false);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new TestTenantProvider { IsGlobalAdmin = true });
    }
}
