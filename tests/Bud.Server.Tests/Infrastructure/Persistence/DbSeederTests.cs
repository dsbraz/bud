using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Persistence;

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
    public async Task SeedAsync_WhenDatabaseIsEmpty_ShouldCreateDefaultTemplatesAndDimensions()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        await DbSeeder.SeedAsync(context);

        // Assert
        var organization = await context.Organizations.IgnoreQueryFilters().SingleAsync(o => o.Name == "getbud.co");

        var templateNames = await context.MissionTemplates
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == organization.Id)
            .Select(t => t.Name)
            .OrderBy(name => name)
            .ToListAsync();

        templateNames.Should().BeEquivalentTo(
            ["BSC", "Mapa Estratégico", "OKR", "PDI", "Planejamento Estratégico Anual"]);

        var dimensionNames = await context.ObjectiveDimensions
            .IgnoreQueryFilters()
            .Where(d => d.OrganizationId == organization.Id)
            .Select(d => d.Name)
            .OrderBy(name => name)
            .ToListAsync();

        dimensionNames.Should().BeEquivalentTo(
            ["Aprendizado e Crescimento", "Clientes", "Financeira", "Processos Internos", "Produtos"]);
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

        context.MissionTemplates.Add(new MissionTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "OKR",
            Metrics =
            [
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    Name = "Resultado-chave 1",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                }
            ]
        });

        await context.SaveChangesAsync();

        // Act
        await DbSeeder.SeedAsync(context);

        // Assert
        var templateNames = await context.MissionTemplates
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == organization.Id)
            .Select(t => t.Name)
            .ToListAsync();

        templateNames.Should().Contain(["BSC", "Mapa Estratégico", "OKR", "PDI", "Planejamento Estratégico Anual"]);
        templateNames.Count(name => name == "OKR").Should().Be(1);

        var dimensionNames = await context.ObjectiveDimensions
            .IgnoreQueryFilters()
            .Where(d => d.OrganizationId == organization.Id)
            .Select(d => d.Name)
            .ToListAsync();

        dimensionNames.Should().Contain(["Aprendizado e Crescimento", "Clientes", "Financeira", "Processos Internos", "Produtos"]);
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

        var bscTemplate = await context.MissionTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Objectives)
            .Include(t => t.Metrics)
            .SingleAsync(t => t.OrganizationId == organization.Id && t.Name == "BSC");

        bscTemplate.Objectives.Should().NotBeEmpty();
        bscTemplate.Metrics.Should().NotBeEmpty();
        bscTemplate.Metrics.Should().OnlyContain(m => m.MissionTemplateObjectiveId.HasValue);
        bscTemplate.Objectives.Select(o => o.Id).Should().Contain(bscTemplate.Metrics.Select(m => m.MissionTemplateObjectiveId!.Value));
    }
}
