# ADR-0003: Outbox com retry/backoff e dead-letter

## Status
Accepted

## Contexto

Eventos de domínio assíncronos precisam de confiabilidade operacional.
Sem mecanismo durável, falhas transitórias podem perder processamento.

## Decisão

Adotar Outbox no banco da aplicação:

- Persistir eventos em `OutboxMessages`
- Processar em background (`OutboxProcessorBackgroundService`)
- Retry com backoff exponencial
- Dead-letter após limite de tentativas
- Endpoints administrativos para inspeção e reprocessamento
- Configuração via `Outbox:Processing` e `Outbox:HealthCheck`

## Consequências

- Maior resiliência no processamento assíncrono
- Rastreabilidade operacional de falhas
- Complexidade adicional de operação e monitoramento

## Alternativas consideradas

- Publicação direta sem persistência: menor complexidade, menor confiabilidade
- Broker externo imediato (Kafka/Rabbit): escalável, porém custo operacional maior para a fase atual
