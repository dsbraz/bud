# ADR-0011: Sistema de Notificações em Use Cases

## Status
Accepted

## Contexto

O sistema precisa notificar colaboradores sobre eventos relevantes (missões criadas/atualizadas/removidas, check-ins de métricas). Essas notificações devem ser persistentes, marcáveis como lidas, e acessíveis via API e frontend.

## Decisão

Criar notificações diretamente nos use cases de comando que geram eventos relevantes:

1. `MissionCommandUseCase` — após criar/atualizar/deletar missão com sucesso, resolve destinatários e cria notificações
2. `MetricCheckinCommandUseCase` — após criar check-in com sucesso, resolve destinatários (excluindo o autor) e cria notificações

Os use cases utilizam:
- `INotificationRecipientResolver` para resolver destinatários com base no escopo da missão (organização, workspace, time ou colaborador)
- `INotificationRepository.CreateForMultipleRecipientsAsync` para persistir notificações
- A orquestração de criação de notificações (resolução de destinatários + persistência) é coordenada diretamente pelos UseCases

### Modelo de dados

- `Notification` implementa `ITenantEntity` com `OrganizationId` denormalizado
- FK para `Collaborator` (destinatário) com `DeleteBehavior.Cascade`
- FK para `Organization` com `DeleteBehavior.Restrict`
- Index composto em `(RecipientCollaboratorId, IsRead, CreatedAtUtc)` para queries eficientes

## Consequências

- A entidade `Notification` segue o mesmo padrão de tenant isolation das demais entidades
- O uso de `ExecuteUpdateAsync` para "marcar todas como lidas" garante eficiência sem materializar as entidades
- Notificações são criadas de forma síncrona no mesmo request, garantindo entrega imediata (ideal para SignalR futuro)
- Falhas na criação de notificações não afetam o resultado da operação principal (fire-and-forget dentro do mesmo request)

## Alternativas consideradas

1. **Polling/SignalR em tempo real**: descartado por complexidade desnecessária nesta fase; o badge de contagem carrega no page load
2. **Notificações em tabela separada sem tenant isolation**: descartado para manter consistência com o modelo de segurança existente
3. **Criar notificações via domain events + outbox**: descartado por adicionar indireção desnecessária (use case → outbox → background worker → handler → notification service) quando a criação direta é mais simples e imediata
