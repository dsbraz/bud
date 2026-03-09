using Bud.Infrastructure.Persistence;
using Bud.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Infrastructure.UnitTests.Infrastructure.Persistence;

public sealed class DbSeederTests
{
    private static ApplicationDbContext CreateInMemoryContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_WhenDatabaseIsEmpty_ShouldCreateDefaultTemplates()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        await DbSeeder.SeedAsync(context);

        // Assert
        var organization = await context.Organizations.IgnoreQueryFilters().SingleAsync(o => o.Name == "getbud.co");

        var templateNames = await context.Templates
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == organization.Id)
            .Select(t => t.Name)
            .OrderBy(name => name)
            .ToListAsync();

        templateNames.Should().BeEquivalentTo(
            ["BSC", "Mapa Estratégico", "OKR", "PDI", "Planejamento Estratégico Anual"]);

        var dimensions = await context.TemplateGoals
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == organization.Id)
            .Select(o => o.Dimension)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .ToListAsync();

        dimensions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SeedAsync_WhenOrganizationAlreadyExists_ShouldAddMissingTemplatesWithoutDuplicates()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "getbud.co"
        };

        context.Organizations.Add(organization);
        context.Collaborators.Add(new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador Global",
            Email = "admin@getbud.co",
            Role = CollaboratorRole.Leader,
            OrganizationId = organization.Id
        });

        context.Templates.Add(new Template
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "OKR",
            Indicators =
            [
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    Name = "Resultado-chave 1",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Percentage
                }
            ]
        });

        await context.SaveChangesAsync();

        // Act
        await DbSeeder.SeedAsync(context);

        // Assert
        var templateNames = await context.Templates
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == organization.Id)
            .Select(t => t.Name)
            .ToListAsync();

        templateNames.Should().Contain(["BSC", "Mapa Estratégico", "OKR", "PDI", "Planejamento Estratégico Anual"]);
        templateNames.Count(name => name == "OKR").Should().Be(1);

        var dimensions = await context.TemplateGoals
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == organization.Id)
            .Select(o => o.Dimension)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .ToListAsync();

        dimensions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateTemplatesWithObjectivesLinkedToMetrics()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        await DbSeeder.SeedAsync(context);

        // Assert
        var organization = await context.Organizations.IgnoreQueryFilters().SingleAsync(o => o.Name == "getbud.co");

        var bscTemplate = await context.Templates
            .IgnoreQueryFilters()
            .Include(t => t.Goals)
            .Include(t => t.Indicators)
            .SingleAsync(t => t.OrganizationId == organization.Id && t.Name == "BSC");

        bscTemplate.Goals.Should().NotBeEmpty();
        bscTemplate.Indicators.Should().NotBeEmpty();
        bscTemplate.Indicators.Should().OnlyContain(m => m.TemplateGoalId.HasValue);
        bscTemplate.Goals.Select(o => o.Id).Should().Contain(bscTemplate.Indicators.Select(m => m.TemplateGoalId!.Value));
    }
}
