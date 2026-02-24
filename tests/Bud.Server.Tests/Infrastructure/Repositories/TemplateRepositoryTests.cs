using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Repositories;
using Bud.Server.Tests.Helpers;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Repositories;

public sealed class TemplateRepositoryTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<Organization> CreateTestOrganization(ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();
        return org;
    }

    private static async Task<Template> CreateTestTemplate(
        ApplicationDbContext context,
        Guid organizationId,
        string name = "Test Template")
    {
        var template = new Template
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrganizationId = organizationId
        };
        context.Templates.Add(template);
        await context.SaveChangesAsync();
        return template;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenTemplateExists_ReturnsTemplate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);
        var template = await CreateTestTemplate(context, org.Id);

        // Act
        var result = await repository.GetByIdAsync(template.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Name.Should().Be("Test Template");
    }

    [Fact]
    public async Task GetByIdAsync_WhenTemplateNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByIdWithChildrenAsync Tests

    [Fact]
    public async Task GetByIdWithChildrenAsync_WhenExists_ReturnsTemplateWithObjectivesAndMetrics()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);
        var template = await CreateTestTemplate(context, org.Id);

        var objective = new TemplateObjective
        {
            Id = Guid.NewGuid(),
            Name = "Template Objective",
            TemplateId = template.Id,
            OrganizationId = org.Id,
            OrderIndex = 0
        };
        context.TemplateObjectives.Add(objective);

        var metric = new TemplateMetric
        {
            Id = Guid.NewGuid(),
            Name = "Template Metric",
            TemplateId = template.Id,
            OrganizationId = org.Id,
            Type = MetricType.Qualitative,
            OrderIndex = 0
        };
        context.TemplateMetrics.Add(metric);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdWithChildrenAsync(template.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Objectives.Should().HaveCount(1);
        result.Metrics.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdWithChildrenAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);

        // Act
        var result = await repository.GetByIdWithChildrenAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByIdReadOnlyAsync Tests

    [Fact]
    public async Task GetByIdReadOnlyAsync_WhenExists_ReturnsTemplateWithOrderedChildren()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);
        var template = await CreateTestTemplate(context, org.Id);

        context.TemplateObjectives.AddRange(
            new TemplateObjective
            {
                Id = Guid.NewGuid(),
                Name = "Second Objective",
                TemplateId = template.Id,
                OrganizationId = org.Id,
                OrderIndex = 1
            },
            new TemplateObjective
            {
                Id = Guid.NewGuid(),
                Name = "First Objective",
                TemplateId = template.Id,
                OrganizationId = org.Id,
                OrderIndex = 0
            });

        context.TemplateMetrics.AddRange(
            new TemplateMetric
            {
                Id = Guid.NewGuid(),
                Name = "Second Metric",
                TemplateId = template.Id,
                OrganizationId = org.Id,
                Type = MetricType.Qualitative,
                OrderIndex = 1
            },
            new TemplateMetric
            {
                Id = Guid.NewGuid(),
                Name = "First Metric",
                TemplateId = template.Id,
                OrganizationId = org.Id,
                Type = MetricType.Qualitative,
                OrderIndex = 0
            });
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdReadOnlyAsync(template.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Objectives.Should().HaveCount(2);
        result.Objectives.First().Name.Should().Be("First Objective");
        result.Metrics.Should().HaveCount(2);
        result.Metrics.First().Name.Should().Be("First Metric");
    }

    [Fact]
    public async Task GetByIdReadOnlyAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);

        // Act
        var result = await repository.GetByIdReadOnlyAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);

        await CreateTestTemplate(context, org.Id, "OKR Template");
        await CreateTestTemplate(context, org.Id, "KPI Template");

        // Act
        var result = await repository.GetAllAsync("okr", 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("OKR Template");
    }

    [Fact]
    public async Task GetAllAsync_WithoutSearch_ReturnsAll()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);

        await CreateTestTemplate(context, org.Id, "Template A");
        await CreateTestTemplate(context, org.Id, "Template B");

        // Act
        var result = await repository.GetAllAsync(null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);

        for (int i = 0; i < 5; i++)
        {
            await CreateTestTemplate(context, org.Id, $"Template {i:D2}");
        }

        // Act
        var result = await repository.GetAllAsync(null, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);

        await CreateTestTemplate(context, org.Id, "Charlie Template");
        await CreateTestTemplate(context, org.Id, "Alpha Template");
        await CreateTestTemplate(context, org.Id, "Bravo Template");

        // Act
        var result = await repository.GetAllAsync(null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Alpha Template");
        result.Items[1].Name.Should().Be("Bravo Template");
        result.Items[2].Name.Should().Be("Charlie Template");
    }

    #endregion

    #region AddAsync / RemoveAsync / SaveChangesAsync Tests

    [Fact]
    public async Task AddAsync_PersistsTemplate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);

        var template = Template.Create(
            Guid.NewGuid(),
            org.Id,
            "New Template",
            "A description",
            null,
            null);

        // Act
        await repository.AddAsync(template);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Templates.FindAsync(template.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("New Template");
    }

    [Fact]
    public async Task RemoveAsync_DeletesTemplate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);
        var template = await CreateTestTemplate(context, org.Id);

        // Act
        await repository.RemoveAsync(template);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await context.Templates.FindAsync(template.Id);
        persisted.Should().BeNull();
    }

    #endregion

    #region RemoveObjectivesAndMetricsAsync Tests

    [Fact]
    public async Task RemoveObjectivesAndMetricsAsync_RemovesBothObjectivesAndMetrics()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);
        var template = await CreateTestTemplate(context, org.Id);

        var objective = new TemplateObjective
        {
            Id = Guid.NewGuid(),
            Name = "To Remove",
            TemplateId = template.Id,
            OrganizationId = org.Id,
            OrderIndex = 0
        };
        context.TemplateObjectives.Add(objective);

        var metric = new TemplateMetric
        {
            Id = Guid.NewGuid(),
            Name = "To Remove",
            TemplateId = template.Id,
            OrganizationId = org.Id,
            Type = MetricType.Qualitative,
            OrderIndex = 0
        };
        context.TemplateMetrics.Add(metric);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveObjectivesAndMetricsAsync(new[] { objective }, new[] { metric });
        await repository.SaveChangesAsync();

        // Assert
        var objectives = await context.TemplateObjectives
            .Where(o => o.TemplateId == template.Id)
            .ToListAsync();
        objectives.Should().BeEmpty();

        var metrics = await context.TemplateMetrics
            .Where(m => m.TemplateId == template.Id)
            .ToListAsync();
        metrics.Should().BeEmpty();
    }

    #endregion

    #region AddObjectivesAndMetricsAsync Tests

    [Fact]
    public async Task AddObjectivesAndMetricsAsync_PersistsBothObjectivesAndMetrics()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TemplateRepository(context);
        var org = await CreateTestOrganization(context);
        var template = await CreateTestTemplate(context, org.Id);

        var objectives = new[]
        {
            new TemplateObjective
            {
                Id = Guid.NewGuid(),
                Name = "New Objective",
                TemplateId = template.Id,
                OrganizationId = org.Id,
                OrderIndex = 0
            }
        };

        var metrics = new[]
        {
            new TemplateMetric
            {
                Id = Guid.NewGuid(),
                Name = "New Metric",
                TemplateId = template.Id,
                OrganizationId = org.Id,
                Type = MetricType.Qualitative,
                OrderIndex = 0
            }
        };

        // Act
        await repository.AddObjectivesAndMetricsAsync(objectives, metrics);
        await repository.SaveChangesAsync();

        // Assert
        var persistedObjectives = await context.TemplateObjectives
            .Where(o => o.TemplateId == template.Id)
            .ToListAsync();
        persistedObjectives.Should().HaveCount(1);
        persistedObjectives[0].Name.Should().Be("New Objective");

        var persistedMetrics = await context.TemplateMetrics
            .Where(m => m.TemplateId == template.Id)
            .ToListAsync();
        persistedMetrics.Should().HaveCount(1);
        persistedMetrics[0].Name.Should().Be("New Metric");
    }

    #endregion
}
