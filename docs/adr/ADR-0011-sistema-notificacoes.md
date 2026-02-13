# ADR-0013: Sistema de Notificações via Domain Events

## Status
Accepted

## Contexto

O sistema precisa notificar colaboradores sobre eventos relevantes (missões criadas/atualizadas/removidas, check-ins de métricas). Essas notificações devem ser persistentes, marcáveis como lidas, e acessíveis via API e frontend.

Até este ponto, os domain event subscribers do outbox apenas registravam logs. Agora, precisamos de subscribers que criem entidades persistentes (`Notification`) durante o processamento do outbox.

## Decisão

Reutilizar o padrão existente de Outbox + Domain Event Subscriber, adicionando handlers de notificação que:

1. Implementam `IDomainEventSubscriber<TDomainEvent>` (mesmo padrão de `MissionCreatedLogHandler`)
2. Resolvem destinatários via `INotificationRecipientResolver` (com base no escopo da missão: organização, workspace, time ou colaborador)
3. Criam notificações via `INotificationService.CreateForMultipleRecipientsAsync`

### Interação com tenant isolation

Os handlers rodam no scope do `OutboxProcessorBackgroundService`, que **não possui HTTP context nem tenant provider válido**. O `ApplicationDbContext` nesse scope tem `_applyTenantFilter = true` mas `_tenantId = null` e `_isGlobalAdmin = false`, resultando em queries que retornam vazio.

**Solução**: O `NotificationRecipientResolver` usa `IgnoreQueryFilters()` em todas as queries e recebe `OrganizationId` explicitamente do domain event (que já traz essa informação no payload serializado no outbox).

### Modelo de dados

- `Notification` implementa `ITenantEntity` com `OrganizationId` denormalizado
- FK para `Collaborator` (destinatário) com `DeleteBehavior.Cascade`
- FK para `Organization` com `DeleteBehavior.Restrict`
- Index composto em `(RecipientCollaboratorId, IsRead, CreatedAtUtc)` para queries eficientes

## Consequências

- Multiple subscribers por evento coexistem (log + notification) — o `DomainEventDispatcher` já suporta isso via `IEnumerable<IDomainEventSubscriber<T>>`
- Handlers que criam entidades no scope do outbox devem **sempre** usar `IgnoreQueryFilters()` e receber `OrganizationId` do evento
- A entidade `Notification` segue o mesmo padrão de tenant isolation das demais entidades
- O uso de `ExecuteUpdateAsync` para "marcar todas como lidas" garante eficiência sem materializar as entidades

## Alternativas consideradas

1. **Polling/SignalR em tempo real**: descartado por complexidade desnecessária nesta fase; o badge de contagem carrega no page load
2. **Notificações em tabela separada sem tenant isolation**: descartado para manter consistência com o modelo de segurança existente
3. **Criar notificações inline nos use cases**: descartado para manter separação de responsabilidades e reutilizar o padrão de domain events
