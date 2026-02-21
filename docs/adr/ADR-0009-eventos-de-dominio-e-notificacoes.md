# ADR-0009: Eventos de Domínio e Notificações

## Status
Accepted

## Contexto
Efeitos colaterais como notificações precisam ser desacoplados do fluxo principal de escrita.

## Decisão
- Tornar eventos de domínio explícitos para fatos relevantes.
- Processar efeitos colaterais por handlers de aplicação.
- Publicação coordenada com `IUnitOfWork` para consistência transacional.

## Consequências
- Fluxo principal mais limpo e orientado ao domínio.
- Melhor extensibilidade para integrações internas.
- Necessidade de governança de idempotência em handlers.

## Alternativas consideradas
- Chamada direta de orquestrador de notificação em cada fluxo de escrita.
- Acoplamento de integrações dentro de agregados.
