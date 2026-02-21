# ADR-0011: Sistema de notificações em comandos da aplicação

## Status
Accepted

## Contexto

O sistema precisa notificar colaboradores sobre eventos relevantes de missão e check-ins.
As notificações devem ser persistentes, marcáveis como lidas e acessíveis por API/frontend.

## Decisão

Criar notificações diretamente nos comandos que geram eventos relevantes:

1. `MissionCommand`: após criar/atualizar/deletar missão com sucesso.
2. `MetricCheckinCommand`: após criar check-in com sucesso.

Os comandos utilizam:

- `INotificationRecipientResolver` para resolver destinatários por escopo.
- `INotificationRepository.CreateForMultipleRecipientsAsync` para persistência.
- `INotificationOrchestrator` para encapsular coordenação de envio/persistência.

### Modelo de dados

- `Notification` implementa `ITenantEntity` com `OrganizationId` denormalizado.
- FK para `Collaborator` com `DeleteBehavior.Cascade`.
- FK para `Organization` com `DeleteBehavior.Restrict`.
- Índice `(RecipientCollaboratorId, IsRead, CreatedAtUtc)` para leitura eficiente.

## Consequências

- Fluxo síncrono de criação de notificações dentro do request principal.
- Operação principal permanece resiliente mesmo com falha de notificação (tratamento controlado na orquestração).
- Consistência com o modelo multi-tenant existente.

## Alternativas consideradas

1. Polling/SignalR em tempo real desde o início.
2. Notificações sem tenant isolation.
3. Domain events + outbox para todo o fluxo.
