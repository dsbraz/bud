# ADR-0016: CQRS por portas formais e Aggregate Roots explícitas

## Status
Accepted

## Contexto

Mesmo com UseCases separados em comando/consulta, alguns contratos de aplicação ainda expunham leitura e escrita em uma única porta (`I*Service`), reduzindo a clareza de fronteiras CQRS.

Além disso, os limites de aggregate root não estavam explicitados no modelo compartilhado, dificultando governança arquitetural por testes.

## Decisão

1. Formalizar CQRS no nível de portas de aplicação para os domínios de equipes, workspaces, colaboradores, missões, métricas de missão e check-ins:
   - `ITeamCommandService` / `ITeamQueryService`
   - `IWorkspaceCommandService` / `IWorkspaceQueryService`
   - `ICollaboratorCommandService` / `ICollaboratorQueryService`
   - `IMissionCommandService` / `IMissionQueryService`
   - `IMissionMetricCommandService` / `IMissionMetricQueryService`
   - `IMetricCheckinCommandService` / `IMetricCheckinQueryService`
   - contratos legados (`ITeamService`, `IWorkspaceService`, `ICollaboratorService`, `IMissionService`, `IMissionMetricService`, `IMetricCheckinService`) permanecem como contratos compostos para compatibilidade progressiva.
   - UseCases de comando dependem apenas das portas de comando.
   - UseCases de consulta dependem apenas das portas de consulta.

2. Explicitar aggregate roots no domínio compartilhado:
   - Introduzir `IAggregateRoot` em `Bud.Shared.Models`.
   - Marcar como roots: `Organization`, `Workspace`, `Team`, `Collaborator`, `Mission`, `MissionTemplate`.
   - Manter entidades internas (ex.: `MissionMetric`, `MetricCheckin`, `MissionTemplateMetric`, `CollaboratorTeam`) fora desse marcador.

3. Expandir invariantes de domínio e comportamento nos agregados:
   - Agregados passam a expor métodos explícitos de domínio (`Create`, `Rename`, `UpdateDetails`, `SetScope`, `ApplyTarget`, etc.).
   - Introduzir Value Objects formais para conceitos semânticos (`PersonName`, `MissionScope`, `ConfidenceLevel`, `MetricRange`, `EntityName`, `NotificationTitle`, `NotificationMessage`).
   - Violações de regra são representadas por `DomainInvariantException`.
   - Services de aplicação orquestram persistência/autorização e traduzem violações de domínio para `ServiceResult` de validação.

4. Completar padronização de mapeamento de dados:
   - Todas as configurações de entidades são extraídas para `IEntityTypeConfiguration<T>` em `Data/Configurations`.
   - `ApplicationDbContext` mantém `DbSet<>`, `ApplyConfigurationsFromAssembly` e query filters multi-tenant.

5. Proteger as decisões por testes de arquitetura:
   - Testes garantindo quais entidades são roots e quais não são.

6. Fechar boundaries de entidades sensíveis com API explícita de domínio:
   - `MissionTemplate` passa a compor métricas via `ReplaceMetrics(...)` + factory em `MissionTemplateMetric`.
   - `Notification` passa a usar `Create(...)` e `MarkAsRead(...)`.
   - `CollaboratorAccessLog` passa a usar `Create(...)` no fluxo de autenticação.
   - `MissionMetric` passa a centralizar consistência de payload de check-in via `CreateCheckin(...)` e `UpdateCheckin(...)`, com `MetricCheckinService` delegando validação de domínio.

## Consequências

- Ganho de clareza arquitetural e redução de acoplamento entre fluxos de leitura/escrita.
- Base para evolução incremental para CQRS mais completo nos demais domínios.
- Fronteiras de aggregate passam a ser verificáveis automaticamente em CI.
- Guardrails de CQRS passam a ser validados por testes de arquitetura (evitando dependências cruzadas e uso indevido de contratos compostos quando split ports existem).
- Novos contratos compostos (`I*Service`) passam por allowlist explícita em testes de arquitetura, forçando atualização de ADR quando houver exceção aprovada.
- Invariantes passam a ter enforcement no modelo de domínio (não apenas em validators/services), reduzindo drift entre regras de negócio e persistência.
- Menor risco de mutações inconsistentes por atribuição direta em services de aplicação para fluxos críticos de template/notificação/auditoria.
- Mudança backward-compatible via manutenção dos contratos compostos (`ITeamService`, `IWorkspaceService`, `ICollaboratorService`).

## Status de Execução

As fases de execução incremental associadas a esta decisão foram concluídas em `2026-02-12`, incluindo:
- formalização de VOs (`EntityName`, `NotificationTitle`, `NotificationMessage`);
- fechamento de boundaries em `MissionTemplate`, `Notification`, `CollaboratorAccessLog`;
- centralização de consistência de check-in em `MissionMetric` (`CreateCheckin`/`UpdateCheckin`);
- atualização dos guardrails arquiteturais e suíte de testes.

## Alternativas consideradas

- Migrar todos os domínios para portas CQRS no mesmo PR:
  - Rejeitado por risco alto e baixa incrementabilidade.
- Não manter os contratos compostos:
  - Rejeitado para evitar quebra ampla e permitir migração progressiva.
