# ADR-0007: Estratégia de documentação OpenAPI semântica

## Status
Accepted

## Contexto

A documentação da API tinha cobertura de status/tipos, mas precisava de semântica
(descrição de operações, erros e exemplos) para melhorar onboarding e integração.

## Decisão

Padronizar OpenAPI com:

- `ProducesResponseType` em endpoints
- XML comments em controllers e contratos (`summary`, `remarks`, `response`)
- `Consumes`/`Produces` quando aplicável
- Exemplos de payload nos endpoints críticos (missões, métricas, check-ins, outbox)
- Swagger configurado para incluir XML comments

## Consequências

- API mais autoexplicativa no Swagger UI
- Menor necessidade de documentação paralela para casos comuns
- Requer disciplina para manter comentários atualizados em cada mudança de endpoint

## Alternativas consideradas

- Apenas documentação externa (README/wiki): risco de desatualização maior
- Somente `ProducesResponseType`: cobertura técnica parcial sem contexto funcional
