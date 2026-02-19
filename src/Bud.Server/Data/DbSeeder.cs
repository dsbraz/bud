using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Data;

public static class DbSeeder
{
    private const string DefaultOrganizationName = "getbud.co";
    private const string DefaultAdminEmail = "admin@getbud.co";

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var budOrg = await context.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Name == DefaultOrganizationName);

        if (budOrg is null)
        {
            budOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Name = DefaultOrganizationName,
                OwnerId = null
            };
            context.Organizations.Add(budOrg);
            await context.SaveChangesAsync();
        }

        var adminLeader = await context.Collaborators
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.OrganizationId == budOrg.Id &&
                c.Email == DefaultAdminEmail);

        if (adminLeader is null)
        {
            adminLeader = new Collaborator
            {
                Id = Guid.NewGuid(),
                FullName = "Administrador Global",
                Email = DefaultAdminEmail,
                Role = CollaboratorRole.Leader,
                OrganizationId = budOrg.Id,
                IsGlobalAdmin = true
            };
            context.Collaborators.Add(adminLeader);
            await context.SaveChangesAsync();
        }

        if (!adminLeader.IsGlobalAdmin)
        {
            adminLeader.IsGlobalAdmin = true;
            await context.SaveChangesAsync();
        }

        if (budOrg.OwnerId != adminLeader.Id)
        {
            budOrg.OwnerId = adminLeader.Id;
            await context.SaveChangesAsync();
        }

        var dimensionsByName = await SeedObjectiveDimensionsAsync(context, budOrg.Id);
        await SeedMissionTemplatesAsync(context, budOrg.Id, dimensionsByName);
    }

    private static async Task<Dictionary<string, Guid>> SeedObjectiveDimensionsAsync(ApplicationDbContext context, Guid organizationId)
    {
        var defaultDimensions = new[]
        {
            "Financeira",
            "Clientes",
            "Produtos",
            "Processos Internos",
            "Aprendizado e Crescimento"
        };

        var existingDimensions = await context.ObjectiveDimensions
            .IgnoreQueryFilters()
            .Where(d => d.OrganizationId == organizationId)
            .ToListAsync();

        var existingByName = existingDimensions.ToDictionary(d => d.Name, d => d.Id);
        var missingNames = defaultDimensions.Where(name => !existingByName.ContainsKey(name)).ToList();

        foreach (var dimensionName in missingNames)
        {
            var dimension = new ObjectiveDimension
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = dimensionName
            };

            context.ObjectiveDimensions.Add(dimension);
            existingByName[dimensionName] = dimension.Id;
        }

        if (missingNames.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        return existingByName;
    }

    private static async Task SeedMissionTemplatesAsync(
        ApplicationDbContext context,
        Guid organizationId,
        IReadOnlyDictionary<string, Guid> dimensionsByName)
    {
        var templates = new List<MissionTemplate>
        {
            BuildBscTemplate(organizationId, dimensionsByName),
            BuildStrategicMapTemplate(organizationId, dimensionsByName),
            BuildAnnualStrategicPlanningTemplate(organizationId, dimensionsByName),
            BuildOkrTemplate(organizationId, dimensionsByName),
            BuildPdiTemplate(organizationId, dimensionsByName)
        };

        var existingTemplateNames = await context.MissionTemplates
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == organizationId)
            .Select(t => t.Name)
            .ToHashSetAsync();

        var missingTemplates = templates
            .Where(template => !existingTemplateNames.Contains(template.Name))
            .ToList();

        if (missingTemplates.Count > 0)
        {
            context.MissionTemplates.AddRange(missingTemplates);
            await context.SaveChangesAsync();
        }
    }

    private static MissionTemplate BuildBscTemplate(Guid organizationId, IReadOnlyDictionary<string, Guid> dimensionsByName)
    {
        var financeiraObjectiveId = Guid.NewGuid();
        var clientesObjectiveId = Guid.NewGuid();
        var processosObjectiveId = Guid.NewGuid();
        var aprendizadoObjectiveId = Guid.NewGuid();

        return new MissionTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "BSC",
            Description = "Balanced Scorecard — framework para equilibrar execução estratégica entre finanças, clientes, processos e aprendizado.",
            MissionNamePattern = "BSC — ",
            MissionDescriptionPattern = "Missão estratégica baseada nas perspectivas do Balanced Scorecard.",
            Objectives =
            [
                new MissionTemplateObjective
                {
                    Id = financeiraObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Perspectiva Financeira",
                    Description = "Objetivos de desempenho econômico e sustentabilidade financeira.",
                    OrderIndex = 0,
                    ObjectiveDimensionId = dimensionsByName["Financeira"]
                },
                new MissionTemplateObjective
                {
                    Id = clientesObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Perspectiva de Clientes",
                    Description = "Objetivos relacionados à proposta de valor e satisfação do cliente.",
                    OrderIndex = 1,
                    ObjectiveDimensionId = dimensionsByName["Clientes"]
                },
                new MissionTemplateObjective
                {
                    Id = processosObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Perspectiva de Processos Internos",
                    Description = "Objetivos de eficiência e excelência operacional.",
                    OrderIndex = 2,
                    ObjectiveDimensionId = dimensionsByName["Processos Internos"]
                },
                new MissionTemplateObjective
                {
                    Id = aprendizadoObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Perspectiva de Aprendizado e Crescimento",
                    Description = "Objetivos de capacidade organizacional, pessoas e inovação.",
                    OrderIndex = 3,
                    ObjectiveDimensionId = dimensionsByName["Aprendizado e Crescimento"]
                }
            ],
            Metrics =
            [
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Resultado Financeiro",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    MissionTemplateObjectiveId = financeiraObjectiveId,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Satisfação de Clientes",
                    Type = MetricType.Quantitative,
                    OrderIndex = 1,
                    MissionTemplateObjectiveId = clientesObjectiveId,
                    QuantitativeType = QuantitativeMetricType.KeepAbove,
                    MinValue = 70,
                    MaxValue = 100,
                    Unit = MetricUnit.Points
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Eficiência de Processos Internos",
                    Type = MetricType.Quantitative,
                    OrderIndex = 2,
                    MissionTemplateObjectiveId = processosObjectiveId,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Capacitação e Aprendizado",
                    Type = MetricType.Quantitative,
                    OrderIndex = 3,
                    MissionTemplateObjectiveId = aprendizadoObjectiveId,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                }
            ]
        };
    }

    private static MissionTemplate BuildStrategicMapTemplate(Guid organizationId, IReadOnlyDictionary<string, Guid> dimensionsByName)
    {
        var crescimentoObjectiveId = Guid.NewGuid();
        var processosObjectiveId = Guid.NewGuid();
        var clientesObjectiveId = Guid.NewGuid();
        var financeiraObjectiveId = Guid.NewGuid();

        return new MissionTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "Mapa Estratégico",
            Description = "Mapa Estratégico — template para explicitar objetivos estratégicos e relações de causa e efeito.",
            MissionNamePattern = "Mapa Estratégico — ",
            MissionDescriptionPattern = "Missão para construção e acompanhamento do mapa estratégico.",
            Objectives =
            [
                new MissionTemplateObjective
                {
                    Id = crescimentoObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Capacidades Organizacionais",
                    Description = "Base de pessoas, cultura e inovação que viabiliza a estratégia.",
                    OrderIndex = 0,
                    ObjectiveDimensionId = dimensionsByName["Aprendizado e Crescimento"]
                },
                new MissionTemplateObjective
                {
                    Id = processosObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Excelência de Processos",
                    Description = "Processos críticos para entregar valor com previsibilidade.",
                    OrderIndex = 1,
                    ObjectiveDimensionId = dimensionsByName["Processos Internos"]
                },
                new MissionTemplateObjective
                {
                    Id = clientesObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Valor para Clientes",
                    Description = "Resultados percebidos pelos clientes e posicionamento competitivo.",
                    OrderIndex = 2,
                    ObjectiveDimensionId = dimensionsByName["Clientes"]
                },
                new MissionTemplateObjective
                {
                    Id = financeiraObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Resultados Financeiros",
                    Description = "Impacto econômico final esperado da estratégia.",
                    OrderIndex = 3,
                    ObjectiveDimensionId = dimensionsByName["Financeira"]
                }
            ],
            Metrics =
            [
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Objetivo Estratégico 1",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    MissionTemplateObjectiveId = crescimentoObjectiveId,
                    TargetText = "Descreva o objetivo e as relações de causa e efeito."
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Objetivo Estratégico 2",
                    Type = MetricType.Qualitative,
                    OrderIndex = 1,
                    MissionTemplateObjectiveId = processosObjectiveId,
                    TargetText = "Descreva o objetivo e as relações de causa e efeito."
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Objetivo Estratégico 3",
                    Type = MetricType.Qualitative,
                    OrderIndex = 2,
                    MissionTemplateObjectiveId = clientesObjectiveId,
                    TargetText = "Descreva o objetivo e as relações de causa e efeito."
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Objetivo Estratégico 4",
                    Type = MetricType.Qualitative,
                    OrderIndex = 3,
                    MissionTemplateObjectiveId = financeiraObjectiveId,
                    TargetText = "Descreva o objetivo e as relações de causa e efeito."
                }
            ]
        };
    }

    private static MissionTemplate BuildAnnualStrategicPlanningTemplate(Guid organizationId, IReadOnlyDictionary<string, Guid> dimensionsByName)
    {
        var portfolioObjectiveId = Guid.NewGuid();
        var executionObjectiveId = Guid.NewGuid();
        var productsObjectiveId = Guid.NewGuid();

        return new MissionTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "Planejamento Estratégico Anual",
            Description = "Template para consolidar prioridades, entregas e marcos estratégicos de um ciclo anual.",
            MissionNamePattern = "Plano Estratégico Anual — ",
            MissionDescriptionPattern = "Planejamento estratégico anual com marcos e prioridades do ciclo.",
            Objectives =
            [
                new MissionTemplateObjective
                {
                    Id = portfolioObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Priorização Estratégica",
                    Description = "Definição das frentes prioritárias do ano.",
                    OrderIndex = 0,
                    ObjectiveDimensionId = dimensionsByName["Financeira"]
                },
                new MissionTemplateObjective
                {
                    Id = executionObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Execução e Governança",
                    Description = "Ritmo e disciplina de execução do plano.",
                    OrderIndex = 1,
                    ObjectiveDimensionId = dimensionsByName["Processos Internos"]
                },
                new MissionTemplateObjective
                {
                    Id = productsObjectiveId,
                    OrganizationId = organizationId,
                    Name = "Evolução de Produtos",
                    Description = "Resultados estratégicos esperados para produtos no ciclo.",
                    OrderIndex = 2,
                    ObjectiveDimensionId = dimensionsByName["Produtos"]
                }
            ],
            Metrics =
            [
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Prioridade Estratégica 1",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    MissionTemplateObjectiveId = portfolioObjectiveId,
                    TargetText = "Descreva o objetivo e os entregáveis da prioridade."
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Prioridade Estratégica 2",
                    Type = MetricType.Qualitative,
                    OrderIndex = 1,
                    MissionTemplateObjectiveId = productsObjectiveId,
                    TargetText = "Descreva o objetivo e os entregáveis da prioridade."
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Execução do Plano no Ano",
                    Type = MetricType.Quantitative,
                    OrderIndex = 2,
                    MissionTemplateObjectiveId = executionObjectiveId,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                }
            ]
        };
    }

    private static MissionTemplate BuildOkrTemplate(Guid organizationId, IReadOnlyDictionary<string, Guid> dimensionsByName)
    {
        var objectiveId = Guid.NewGuid();

        return new MissionTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "OKR",
            Description = "Objectives and Key Results — framework para definir e acompanhar objetivos com resultados-chave mensuráveis.",
            MissionNamePattern = "OKR — ",
            MissionDescriptionPattern = "Missão seguindo o framework OKR com resultados-chave quantitativos.",
            Objectives =
            [
                new MissionTemplateObjective
                {
                    Id = objectiveId,
                    OrganizationId = organizationId,
                    Name = "Objetivo Principal",
                    Description = "Objetivo aspiracional do ciclo de OKR.",
                    OrderIndex = 0,
                    ObjectiveDimensionId = dimensionsByName["Clientes"]
                }
            ],
            Metrics =
            [
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Resultado-chave 1",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    MissionTemplateObjectiveId = objectiveId,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Resultado-chave 2",
                    Type = MetricType.Quantitative,
                    OrderIndex = 1,
                    MissionTemplateObjectiveId = objectiveId,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Resultado-chave 3",
                    Type = MetricType.Quantitative,
                    OrderIndex = 2,
                    MissionTemplateObjectiveId = objectiveId,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                }
            ]
        };
    }

    private static MissionTemplate BuildPdiTemplate(Guid organizationId, IReadOnlyDictionary<string, Guid> dimensionsByName)
    {
        var objectiveId = Guid.NewGuid();

        return new MissionTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "PDI",
            Description = "Plano de Desenvolvimento Individual — framework para acompanhar ações de desenvolvimento pessoal e profissional.",
            MissionNamePattern = "PDI — ",
            MissionDescriptionPattern = "Plano de desenvolvimento individual com ações qualitativas e acompanhamento de progresso.",
            Objectives =
            [
                new MissionTemplateObjective
                {
                    Id = objectiveId,
                    OrganizationId = organizationId,
                    Name = "Desenvolvimento Individual",
                    Description = "Capacidades e competências a desenvolver no ciclo.",
                    OrderIndex = 0,
                    ObjectiveDimensionId = dimensionsByName["Aprendizado e Crescimento"]
                }
            ],
            Metrics =
            [
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Ação de desenvolvimento 1",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    MissionTemplateObjectiveId = objectiveId,
                    TargetText = "Descreva a ação de desenvolvimento"
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Ação de desenvolvimento 2",
                    Type = MetricType.Qualitative,
                    OrderIndex = 1,
                    MissionTemplateObjectiveId = objectiveId,
                    TargetText = "Descreva a ação de desenvolvimento"
                },
                new MissionTemplateMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Progresso geral",
                    Type = MetricType.Quantitative,
                    OrderIndex = 2,
                    MissionTemplateObjectiveId = objectiveId,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                }
            ]
        };
    }
}
