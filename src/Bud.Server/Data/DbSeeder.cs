using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Check if seed already exists
        // Must ignore query filters since we're seeding without tenant context
        if (await context.Organizations.IgnoreQueryFilters().AnyAsync(o => o.Name == "getbud.co"))
        {
            return;
        }

        // 1. Create Organization "Bud" (without owner initially)
        var budOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "getbud.co",
            OwnerId = null
        };
        context.Organizations.Add(budOrg);

        // 2. Create Global Admin Leader
        var adminLeader = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Administrador Global",
            Email = "admin@getbud.co",
            Role = CollaboratorRole.Leader,
            OrganizationId = budOrg.Id
        };
        context.Collaborators.Add(adminLeader);

        await context.SaveChangesAsync();

        // 3. Update Organization with Owner
        budOrg.OwnerId = adminLeader.Id;
        await context.SaveChangesAsync();

        // 4. Seed default Mission Templates
        await SeedMissionTemplatesAsync(context, budOrg.Id);
    }

    private static async Task SeedMissionTemplatesAsync(ApplicationDbContext context, Guid organizationId)
    {
        if (await context.MissionTemplates.IgnoreQueryFilters().AnyAsync(t => t.OrganizationId == organizationId))
        {
            return;
        }

        // OKR Template
        var okrTemplate = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "OKR",
            Description = "Objectives and Key Results — framework para definir e acompanhar objetivos com resultados-chave mensuráveis.",
            MissionNamePattern = "OKR — ",
            MissionDescriptionPattern = "Missão seguindo o framework OKR com resultados-chave quantitativos.",
            IsDefault = true,
            IsActive = true,
            Metrics = new List<MissionTemplateMetric>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Key Result 1",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Key Result 2",
                    Type = MetricType.Quantitative,
                    OrderIndex = 1,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Key Result 3",
                    Type = MetricType.Quantitative,
                    OrderIndex = 2,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                }
            }
        };
        context.MissionTemplates.Add(okrTemplate);

        // PDI Template
        var pdiTemplate = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "PDI",
            Description = "Plano de Desenvolvimento Individual — framework para acompanhar ações de desenvolvimento pessoal e profissional.",
            MissionNamePattern = "PDI — ",
            MissionDescriptionPattern = "Plano de desenvolvimento individual com ações qualitativas e acompanhamento de progresso.",
            IsDefault = true,
            IsActive = true,
            Metrics = new List<MissionTemplateMetric>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Ação de desenvolvimento 1",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Descreva a ação de desenvolvimento"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Ação de desenvolvimento 2",
                    Type = MetricType.Qualitative,
                    OrderIndex = 1,
                    TargetText = "Descreva a ação de desenvolvimento"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Progresso geral",
                    Type = MetricType.Quantitative,
                    OrderIndex = 2,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                }
            }
        };
        context.MissionTemplates.Add(pdiTemplate);

        await context.SaveChangesAsync();
    }
}
