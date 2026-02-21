# ADR-0012: Aggregate roots explícitas, value objects e invariantes de domínio

## Status
Accepted

## Contexto

Limites de aggregate root e enforcement de invariantes precisam estar explícitos para manter consistência de domínio e governança por testes.

## Decisão

1. Explicitar aggregate roots no domínio compartilhado.
2. Centralizar invariantes críticas nos agregados/value objects.
3. Usar `DomainInvariantException` para violação de invariantes.
4. Commands/Queries da camada `Application` orquestram persistência/autorização e traduzem falhas de domínio para `Result`.
5. Repositories permanecem focados em persistência e consulta.
6. Manter uma interface de repositório por aggregate root em `Infrastructure/Repositories`.

## Consequências

- Invariantes de domínio ficam próximas do modelo, reduzindo drift.
- Menor risco de mutações inconsistentes em fluxos críticos.
- Melhor testabilidade de regras de domínio e da orquestração de aplicação.

## Alternativas consideradas

- Regras concentradas apenas em validators/services de aplicação.
- Split formal de repositories por CQRS (`I*CommandRepository`/`I*QueryRepository`) em todos os domínios.
