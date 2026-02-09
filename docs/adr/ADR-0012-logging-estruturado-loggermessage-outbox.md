# ADR-0012: Estratégia de logging estruturado com LoggerMessage e observabilidade de Outbox

## Status
Accepted

## Contexto

O projeto utiliza `TreatWarningsAsErrors=true` e passou a falhar com `CA1848`, que recomenda evitar chamadas diretas como `logger.LogInformation(...)` em favor de delegates de logging mais eficientes.

Além disso, o fluxo de Outbox é crítico para confiabilidade assíncrona. Precisamos de logs estáveis e estruturados para operação, troubleshooting e monitoramento (retry/dead-letter/processamento bem-sucedido), sem depender de mensagens ad-hoc dispersas.

## Decisão

Adotar logging source-generated com `[LoggerMessage]`, seguindo estas regras:

1. Definições de log ficam locais ao componente (`partial class`), evitando catálogo central global de logs.
2. Cada evento relevante usa `EventId` estável.
3. No Outbox, registrar ciclo completo com logs estruturados:
   - início de processamento da mensagem,
   - sucesso,
   - retry agendado,
   - dead-letter.
4. Campos mínimos no Outbox: `OutboxMessageId`, `EventType`, `Attempt/RetryCount`, `NextAttemptOnUtc`, `ElapsedMs`, `Error` (quando aplicável).
5. Pipeline de UseCase e handlers de eventos de domínio seguem o mesmo padrão (`[LoggerMessage]` local por classe).

## Consequências

### Positivas

- Conformidade com `CA1848` sem supressões.
- Menor overhead de logging em caminhos de alta frequência.
- Melhor observabilidade operacional do Outbox.
- Maior coesão: cada classe mantém seus próprios eventos de log.

### Negativas / trade-offs

- Mais código boilerplate por classe (`partial` + métodos de log).
- Necessidade de governar faixas de `EventId` para evitar colisões.

## Alternativas consideradas

1. **Manter chamadas diretas `LogInformation/LogError`**  
   Rejeitada por quebrar build com `CA1848` e piorar eficiência.

2. **Catálogo central único de logs compartilhado**  
   Rejeitada como padrão principal por reduzir coesão e dificultar evolução por componente.

3. **Desabilitar ou reduzir severidade de `CA1848`**  
   Rejeitada por conflitar com a política de qualidade warning-free do repositório.
