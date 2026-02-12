using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public sealed class MissionTemplateServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .AddInterceptors(new TenantSaveChangesInterceptor(_tenantProvider))
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

    private static MissionTemplate CreateTestTemplate(Guid organizationId, string name = "Test Template")
    {
        return new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test Description",
            OrganizationId = organizationId,
            IsDefault = false,
            IsActive = true,
            Metrics = new List<MissionTemplateMetric>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Metric A",
                    Type = MetricType.Quantitative,
                    OrderIndex = 1,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MinValue = 0,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage,
                    OrganizationId = organizationId
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Metric B",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Deliver on time",
                    OrganizationId = organizationId
                }
            }
        };
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesTemplateWithMetrics()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = await CreateTestOrganization(context);
        var service = new MissionTemplateService(context);

        var request = new CreateMissionTemplateRequest
        {
            Name = "New Template",
            Description = "A description",
            MissionNamePattern = "Mission {n}",
            MissionDescriptionPattern = "Description {n}",
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "Revenue",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MinValue = 0,
                    MaxValue = 1000,
                    Unit = MetricUnit.Integer
                },
                new MissionTemplateMetricDto
                {
                    Name = "Quality",
                    Type = MetricType.Qualitative,
                    OrderIndex = 1,
                    TargetText = "High quality"
                }
            ]
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Template");
        result.Value.Description.Should().Be("A description");
        result.Value.MissionNamePattern.Should().Be("Mission {n}");
        result.Value.MissionDescriptionPattern.Should().Be("Description {n}");
        result.Value.IsDefault.Should().BeFalse();
        result.Value.IsActive.Should().BeTrue();
        result.Value.Metrics.Should().HaveCount(2);

        var savedTemplate = await context.MissionTemplates
            .Include(t => t.Metrics)
            .FirstOrDefaultAsync(t => t.Id == result.Value.Id);
        savedTemplate.Should().NotBeNull();
        savedTemplate!.Metrics.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_TrimsStringFields()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = await CreateTestOrganization(context);
        var service = new MissionTemplateService(context);

        var request = new CreateMissionTemplateRequest
        {
            Name = "  Padded Name  ",
            Description = "  Padded Description  ",
            MissionNamePattern = "  Pattern {n}  ",
            MissionDescriptionPattern = "  Desc Pattern  ",
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "  Metric Name  ",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "  Target Text  "
                }
            ]
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Padded Name");
        result.Value.Description.Should().Be("Padded Description");
        result.Value.MissionNamePattern.Should().Be("Pattern {n}");
        result.Value.MissionDescriptionPattern.Should().Be("Desc Pattern");
        result.Value.Metrics.Should().ContainSingle();
        result.Value.Metrics.First().Name.Should().Be("Metric Name");
        result.Value.Metrics.First().TargetText.Should().Be("Target Text");
    }

    [Fact]
    public async Task CreateAsync_WithQuantitativeMetricWithoutQuantitativeType_ReturnsValidationFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        _ = await CreateTestOrganization(context);
        var service = new MissionTemplateService(context);

        var request = new CreateMissionTemplateRequest
        {
            Name = "Template sem tipo quantitativo",
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "Receita",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = null,
                    MinValue = 0,
                    MaxValue = 100
                }
            ]
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Tipo quantitativo é obrigatório para métricas quantitativas.");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingTemplate_UpdatesSuccessfully()
    {
        // Arrange — use null tenant provider to bypass query filters (InMemory quirk)
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var seedContext = new ApplicationDbContext(options);
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        seedContext.Organizations.Add(org);
        var template = CreateTestTemplate(org.Id, "Original Name");
        seedContext.MissionTemplates.Add(template);
        await seedContext.SaveChangesAsync();
        var templateId = template.Id;
        seedContext.Dispose();

        using var context = new ApplicationDbContext(options);
        var service = new MissionTemplateService(context);

        var request = new UpdateMissionTemplateRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            MissionNamePattern = "Updated Pattern",
            MissionDescriptionPattern = "Updated Desc Pattern",
            IsActive = false,
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "New Metric",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = QuantitativeMetricType.KeepAbove,
                    MinValue = 10,
                    MaxValue = 200,
                    Unit = MetricUnit.Points
                }
            ]
        };

        // Act
        var result = await service.UpdateAsync(templateId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.Description.Should().Be("Updated Description");
        result.Value.MissionNamePattern.Should().Be("Updated Pattern");
        result.Value.MissionDescriptionPattern.Should().Be("Updated Desc Pattern");
        result.Value.IsActive.Should().BeFalse();
        result.Value.Metrics.Should().ContainSingle();
        result.Value.Metrics.First().Name.Should().Be("New Metric");
        result.Value.Metrics.First().QuantitativeType.Should().Be(QuantitativeMetricType.KeepAbove);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAllMetrics()
    {
        // Arrange — use null tenant provider to bypass query filters (InMemory quirk)
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var seedContext = new ApplicationDbContext(options);
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        seedContext.Organizations.Add(org);
        var template = CreateTestTemplate(org.Id, "Template With Metrics");
        seedContext.MissionTemplates.Add(template);
        await seedContext.SaveChangesAsync();
        var templateId = template.Id;
        var originalMetricIds = template.Metrics.Select(m => m.Id).ToList();
        seedContext.Dispose();

        using var context = new ApplicationDbContext(options);
        var service = new MissionTemplateService(context);

        var request = new UpdateMissionTemplateRequest
        {
            Name = "Template With Metrics",
            IsActive = true,
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "Completely New Metric",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "New target"
                }
            ]
        };

        // Act
        var result = await service.UpdateAsync(templateId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Metrics.Should().ContainSingle();
        result.Value.Metrics.First().Name.Should().Be("Completely New Metric");

        // Verify old metrics were removed
        var remainingOldMetrics = await context.MissionTemplateMetrics
            .Where(m => originalMetricIds.Contains(m.Id))
            .CountAsync();
        remainingOldMetrics.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionTemplateService(context);

        var request = new UpdateMissionTemplateRequest
        {
            Name = "Updated Name",
            IsActive = true,
            Metrics = []
        };

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Template de missão não encontrado.");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingTemplate_DeletesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = await CreateTestOrganization(context);
        var template = CreateTestTemplate(org.Id);
        context.MissionTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new MissionTemplateService(context);

        // Act
        var result = await service.DeleteAsync(template.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deletedTemplate = await context.MissionTemplates.FindAsync(template.Id);
        deletedTemplate.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionTemplateService(context);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Template de missão não encontrado.");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsTemplateWithOrderedMetrics()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = await CreateTestOrganization(context);
        var template = CreateTestTemplate(org.Id);
        context.MissionTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new MissionTemplateService(context);

        // Act
        var result = await service.GetByIdAsync(template.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(template.Id);
        result.Value.Name.Should().Be("Test Template");
        result.Value.Metrics.Should().HaveCount(2);

        // Metrics should be ordered by OrderIndex (Metric B=0, Metric A=1)
        var metrics = result.Value.Metrics.ToList();
        metrics[0].OrderIndex.Should().BeLessThanOrEqualTo(metrics[1].OrderIndex);
        metrics[0].Name.Should().Be("Metric B");
        metrics[1].Name.Should().Be("Metric A");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MissionTemplateService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Template de missão não encontrado.");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithMultipleTemplates_ReturnsPagedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = await CreateTestOrganization(context);

        for (var i = 0; i < 5; i++)
        {
            var t = CreateTestTemplate(org.Id, $"Template {i:D2}");
            context.MissionTemplates.Add(t);
        }
        await context.SaveChangesAsync();

        var service = new MissionTemplateService(context);

        // Act
        var result = await service.GetAllAsync(search: null, page: 1, pageSize: 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Total.Should().Be(5);
        result.Value.Items.Should().HaveCount(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchFilter_ReturnsMatchingTemplates()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = await CreateTestOrganization(context);

        context.MissionTemplates.AddRange(
            CreateTestTemplate(org.Id, "Alpha Template"),
            CreateTestTemplate(org.Id, "Beta Template"),
            CreateTestTemplate(org.Id, "Gamma Template"));
        await context.SaveChangesAsync();

        var service = new MissionTemplateService(context);

        // Act
        var result = await service.GetAllAsync(search: "beta", page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Name.Should().Be("Beta Template");
    }

    [Fact]
    public async Task GetAllAsync_WithNoMatches_ReturnsEmptyResult()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = await CreateTestOrganization(context);

        context.MissionTemplates.Add(CreateTestTemplate(org.Id, "Some Template"));
        await context.SaveChangesAsync();

        var service = new MissionTemplateService(context);

        // Act
        var result = await service.GetAllAsync(search: "nonexistent", page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithCaseInsensitiveSearch_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = await CreateTestOrganization(context);

        context.MissionTemplates.AddRange(
            CreateTestTemplate(org.Id, "UPPER Case"),
            CreateTestTemplate(org.Id, "lower case"),
            CreateTestTemplate(org.Id, "Other"));
        await context.SaveChangesAsync();

        var service = new MissionTemplateService(context);

        // Act
        var result = await service.GetAllAsync(search: "case", page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTemplatesOrderedByName()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var org = await CreateTestOrganization(context);

        context.MissionTemplates.AddRange(
            CreateTestTemplate(org.Id, "Charlie"),
            CreateTestTemplate(org.Id, "Alpha"),
            CreateTestTemplate(org.Id, "Bravo"));
        await context.SaveChangesAsync();

        var service = new MissionTemplateService(context);

        // Act
        var result = await service.GetAllAsync(search: null, page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items[0].Name.Should().Be("Alpha");
        result.Value.Items[1].Name.Should().Be("Bravo");
        result.Value.Items[2].Name.Should().Be("Charlie");
    }

    #endregion
}
