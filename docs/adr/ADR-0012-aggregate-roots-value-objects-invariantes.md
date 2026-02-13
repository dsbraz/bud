# ADR-0012: Aggregate Roots explícitas, Value Objects e invariantes de domínio

## Status
Accepted

## Contexto

Os limites de aggregate root não estavam explicitados no modelo compartilhado, dificultando governança arquitetural por testes. Além disso, invariantes de domínio estavam dispersas em services/validators sem enforcement formal no modelo.

## Decisão

1. Explicitar aggregate roots no domínio compartilhado:
   - `IAggregateRoot` em `Bud.Shared.Domain` marca entidades raiz.
   - Roots: `Organization`, `Workspace`, `Team`, `Collaborator`, `Mission`, `MissionTemplate`.
   - Entidades internas (`MissionMetric`, `MetricCheckin`, `MissionTemplateMetric`, `CollaboratorTeam`) não implementam `IAggregateRoot`.

2. Invariantes de domínio e comportamento nos agregados:
   - Agregados expõem métodos explícitos de domínio (`Create`, `Rename`, `UpdateDetails`, `SetScope`, `ApplyTarget`, etc.).
   - Value Objects formais para conceitos semânticos (`PersonName`, `MissionScope`, `ConfidenceLevel`, `MetricRange`, `EntityName`, `NotificationTitle`, `NotificationMessage`).
   - Violações de regra representadas por `DomainInvariantException`.
   - Services de aplicação orquestram persistência/autorização e traduzem violações de domínio para `ServiceResult` de validação.

3. Padronização de mapeamento de dados:
   - Todas as configurações de entidades extraídas para `IEntityTypeConfiguration<T>` em `Data/Configurations`.
   - `ApplicationDbContext` mantém `DbSet<>`, `ApplyConfigurationsFromAssembly` e query filters multi-tenant.

4. Proteção por testes de arquitetura:
   - Testes garantindo quais entidades são roots e quais não são.

5. Boundaries de entidades sensíveis com API explícita de domínio:
   - `MissionTemplate` compõe métricas via `ReplaceMetrics(...)` + factory em `MissionTemplateMetric`.
   - `Notification` usa `Create(...)` e `MarkAsRead(...)`.
   - `CollaboratorAccessLog` usa `Create(...)` no fluxo de autenticação.
   - `MissionMetric` centraliza consistência de payload de check-in via `CreateCheckin(...)` e `UpdateCheckin(...)`, com `MetricCheckinService` delegando validação de domínio.

6. Interface de serviço única por domínio:
   - Cada domínio expõe uma única interface (`ITeamService`, `IWorkspaceService`, `ICollaboratorService`, `IMissionService`, `IMissionMetricService`, `IMetricCheckinService`) com todos os métodos de comando e consulta.
   - UseCases de comando e consulta dependem da mesma interface.

## Consequências

- Fronteiras de aggregate verificáveis automaticamente em CI.
- Invariantes com enforcement no modelo de domínio (não apenas em validators/services), reduzindo drift entre regras de negócio e persistência.
- Menor risco de mutações inconsistentes por atribuição direta em services de aplicação para fluxos críticos de template/notificação/auditoria.
- Interface única por domínio simplifica DI e reduz número de abstrações.

## Alternativas consideradas

- **CQRS split por portas formais** (`I*CommandService`/`I*QueryService`): descartado por complexidade desproporcional ao estágio atual (CRUD hierárquico com ~9 entidades).
