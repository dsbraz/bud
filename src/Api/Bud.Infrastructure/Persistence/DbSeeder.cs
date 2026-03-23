using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Persistence;

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

        await SeedTemplatesAsync(context, budOrg.Id);
    }

    private static async Task SeedTemplatesAsync(
        ApplicationDbContext context,
        Guid organizationId)
    {
        var templates = new List<Template>
        {
            BuildBscTemplate(organizationId),
            BuildStrategicMapTemplate(organizationId),
            BuildAnnualStrategicPlanningTemplate(organizationId),
            BuildOkrTemplate(organizationId),
            BuildPdiTemplate(organizationId)
        };

        var existingTemplateNames = await context.Templates
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == organizationId)
            .Select(t => t.Name)
            .ToHashSetAsync();

        var missingTemplates = templates
            .Where(template => !existingTemplateNames.Contains(template.Name))
            .ToList();

        if (missingTemplates.Count > 0)
        {
            context.Templates.AddRange(missingTemplates);
            await context.SaveChangesAsync();
        }
    }

    private static Template BuildBscTemplate(Guid organizationId)
    {
        var financeiraGoalId = Guid.NewGuid();
        var clientesGoalId = Guid.NewGuid();
        var processosGoalId = Guid.NewGuid();
        var aprendizadoGoalId = Guid.NewGuid();

        return new Template
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "BSC",
            Description = "Balanced Scorecard — framework para equilibrar execução estratégica entre finanças, clientes, processos e aprendizado.",
            GoalNamePattern = "BSC — ",
            GoalDescriptionPattern = "Meta estratégica baseada nas perspectivas do Balanced Scorecard.",
            Goals =
            [
                new TemplateGoal
                {
                    Id = financeiraGoalId,
                    OrganizationId = organizationId,
                    Name = "Perspectiva Financeira",
                    Description = "Objetivos de desempenho econômico e sustentabilidade financeira.",
                    OrderIndex = 0,
                    Dimension = "Financeira"
                },
                new TemplateGoal
                {
                    Id = clientesGoalId,
                    OrganizationId = organizationId,
                    Name = "Perspectiva de Clientes",
                    Description = "Objetivos relacionados à proposta de valor e satisfação do cliente.",
                    OrderIndex = 1,
                    Dimension = "Clientes"
                },
                new TemplateGoal
                {
                    Id = processosGoalId,
                    OrganizationId = organizationId,
                    Name = "Perspectiva de Processos Internos",
                    Description = "Objetivos de eficiência e excelência operacional.",
                    OrderIndex = 2,
                    Dimension = "Processos Internos"
                },
                new TemplateGoal
                {
                    Id = aprendizadoGoalId,
                    OrganizationId = organizationId,
                    Name = "Perspectiva de Aprendizado e Crescimento",
                    Description = "Objetivos de capacidade organizacional, pessoas e inovação.",
                    OrderIndex = 3,
                    Dimension = "Aprendizado e Crescimento"
                }
            ],
            Indicators =
            [
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Resultado Financeiro",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 0,
                    TemplateGoalId = financeiraGoalId,
                    QuantitativeType = QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Percentage
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Satisfação de Clientes",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 1,
                    TemplateGoalId = clientesGoalId,
                    QuantitativeType = QuantitativeIndicatorType.KeepAbove,
                    MinValue = 70,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Points
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Eficiência de Processos Internos",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 2,
                    TemplateGoalId = processosGoalId,
                    QuantitativeType = QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Percentage
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Capacitação e Aprendizado",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 3,
                    TemplateGoalId = aprendizadoGoalId,
                    QuantitativeType = QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Percentage
                }
            ]
        };
    }

    private static Template BuildStrategicMapTemplate(Guid organizationId)
    {
        var crescimentoGoalId = Guid.NewGuid();
        var processosGoalId = Guid.NewGuid();
        var clientesGoalId = Guid.NewGuid();
        var financeiraGoalId = Guid.NewGuid();

        return new Template
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "Mapa Estratégico",
            Description = "Mapa Estratégico — template para explicitar objetivos estratégicos e relações de causa e efeito.",
            GoalNamePattern = "Mapa Estratégico — ",
            GoalDescriptionPattern = "Meta para construção e acompanhamento do mapa estratégico.",
            Goals =
            [
                new TemplateGoal
                {
                    Id = crescimentoGoalId,
                    OrganizationId = organizationId,
                    Name = "Capacidades Organizacionais",
                    Description = "Base de pessoas, cultura e inovação que viabiliza a estratégia.",
                    OrderIndex = 0,
                    Dimension = "Aprendizado e Crescimento"
                },
                new TemplateGoal
                {
                    Id = processosGoalId,
                    OrganizationId = organizationId,
                    Name = "Excelência de Processos",
                    Description = "Processos críticos para entregar valor com previsibilidade.",
                    OrderIndex = 1,
                    Dimension = "Processos Internos"
                },
                new TemplateGoal
                {
                    Id = clientesGoalId,
                    OrganizationId = organizationId,
                    Name = "Valor para Clientes",
                    Description = "Resultados percebidos pelos clientes e posicionamento competitivo.",
                    OrderIndex = 2,
                    Dimension = "Clientes"
                },
                new TemplateGoal
                {
                    Id = financeiraGoalId,
                    OrganizationId = organizationId,
                    Name = "Resultados Financeiros",
                    Description = "Impacto econômico final esperado da estratégia.",
                    OrderIndex = 3,
                    Dimension = "Financeira"
                }
            ],
            Indicators =
            [
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Objetivo Estratégico 1",
                    Type = IndicatorType.Qualitative,
                    OrderIndex = 0,
                    TemplateGoalId = crescimentoGoalId,
                    TargetText = "Descreva o objetivo e as relações de causa e efeito."
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Objetivo Estratégico 2",
                    Type = IndicatorType.Qualitative,
                    OrderIndex = 1,
                    TemplateGoalId = processosGoalId,
                    TargetText = "Descreva o objetivo e as relações de causa e efeito."
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Objetivo Estratégico 3",
                    Type = IndicatorType.Qualitative,
                    OrderIndex = 2,
                    TemplateGoalId = clientesGoalId,
                    TargetText = "Descreva o objetivo e as relações de causa e efeito."
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Objetivo Estratégico 4",
                    Type = IndicatorType.Qualitative,
                    OrderIndex = 3,
                    TemplateGoalId = financeiraGoalId,
                    TargetText = "Descreva o objetivo e as relações de causa e efeito."
                }
            ]
        };
    }

    private static Template BuildAnnualStrategicPlanningTemplate(Guid organizationId)
    {
        var portfolioGoalId = Guid.NewGuid();
        var executionGoalId = Guid.NewGuid();
        var productsGoalId = Guid.NewGuid();

        return new Template
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "Planejamento Estratégico Anual",
            Description = "Template para consolidar prioridades, entregas e marcos estratégicos de um ciclo anual.",
            GoalNamePattern = "Plano Estratégico Anual — ",
            GoalDescriptionPattern = "Planejamento estratégico anual com marcos e prioridades do ciclo.",
            Goals =
            [
                new TemplateGoal
                {
                    Id = portfolioGoalId,
                    OrganizationId = organizationId,
                    Name = "Priorização Estratégica",
                    Description = "Definição das frentes prioritárias do ano.",
                    OrderIndex = 0,
                    Dimension = "Financeira"
                },
                new TemplateGoal
                {
                    Id = executionGoalId,
                    OrganizationId = organizationId,
                    Name = "Execução e Governança",
                    Description = "Ritmo e disciplina de execução do plano.",
                    OrderIndex = 1,
                    Dimension = "Processos Internos"
                },
                new TemplateGoal
                {
                    Id = productsGoalId,
                    OrganizationId = organizationId,
                    Name = "Evolução de Produtos",
                    Description = "Resultados estratégicos esperados para produtos no ciclo.",
                    OrderIndex = 2,
                    Dimension = "Produtos"
                }
            ],
            Indicators =
            [
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Prioridade Estratégica 1",
                    Type = IndicatorType.Qualitative,
                    OrderIndex = 0,
                    TemplateGoalId = portfolioGoalId,
                    TargetText = "Descreva o objetivo e os entregáveis da prioridade."
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Prioridade Estratégica 2",
                    Type = IndicatorType.Qualitative,
                    OrderIndex = 1,
                    TemplateGoalId = productsGoalId,
                    TargetText = "Descreva o objetivo e os entregáveis da prioridade."
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Execução do Plano no Ano",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 2,
                    TemplateGoalId = executionGoalId,
                    QuantitativeType = QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Percentage
                }
            ]
        };
    }

    private static Template BuildOkrTemplate(Guid organizationId)
    {
        var goalId = Guid.NewGuid();

        return new Template
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "OKR",
            Description = "Objectives and Key Results — framework para definir e acompanhar objetivos com resultados-chave mensuráveis.",
            GoalNamePattern = "OKR — ",
            GoalDescriptionPattern = "Meta seguindo o framework OKR com resultados-chave quantitativos.",
            Goals =
            [
                new TemplateGoal
                {
                    Id = goalId,
                    OrganizationId = organizationId,
                    Name = "Objetivo Principal",
                    Description = "Objetivo aspiracional do ciclo de OKR.",
                    OrderIndex = 0,
                    Dimension = "Clientes"
                }
            ],
            Indicators =
            [
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Resultado-chave 1",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 0,
                    TemplateGoalId = goalId,
                    QuantitativeType = QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Percentage
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Resultado-chave 2",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 1,
                    TemplateGoalId = goalId,
                    QuantitativeType = QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Percentage
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Resultado-chave 3",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 2,
                    TemplateGoalId = goalId,
                    QuantitativeType = QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Percentage
                }
            ]
        };
    }

    private static Template BuildPdiTemplate(Guid organizationId)
    {
        var goalId = Guid.NewGuid();

        return new Template
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "PDI",
            Description = "Plano de Desenvolvimento Individual — framework para acompanhar ações de desenvolvimento pessoal e profissional.",
            GoalNamePattern = "PDI — ",
            GoalDescriptionPattern = "Plano de desenvolvimento individual com ações qualitativas e acompanhamento de progresso.",
            Goals =
            [
                new TemplateGoal
                {
                    Id = goalId,
                    OrganizationId = organizationId,
                    Name = "Desenvolvimento Individual",
                    Description = "Capacidades e competências a desenvolver no ciclo.",
                    OrderIndex = 0,
                    Dimension = "Aprendizado e Crescimento"
                }
            ],
            Indicators =
            [
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Ação de desenvolvimento 1",
                    Type = IndicatorType.Qualitative,
                    OrderIndex = 0,
                    TemplateGoalId = goalId,
                    TargetText = "Descreva a ação de desenvolvimento"
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Ação de desenvolvimento 2",
                    Type = IndicatorType.Qualitative,
                    OrderIndex = 1,
                    TemplateGoalId = goalId,
                    TargetText = "Descreva a ação de desenvolvimento"
                },
                new TemplateIndicator
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = "Progresso geral",
                    Type = IndicatorType.Quantitative,
                    OrderIndex = 2,
                    TemplateGoalId = goalId,
                    QuantitativeType = QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = IndicatorUnit.Percentage
                }
            ]
        };
    }
}
