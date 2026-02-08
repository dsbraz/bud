# ADRs (Architecture Decision Records)

Esta pasta concentra as decisões arquiteturais relevantes do projeto.
Cada ADR registra contexto, decisão, trade-offs e alternativas.

## Convenção

- Arquivos no formato `ADR-XXXX-titulo-curto.md`
- Numeração sequencial com 4 dígitos (ex.: `ADR-0001`)
- Um ADR por decisão arquitetural relevante

## Ordem de leitura recomendada

A ordem abaixo vai das decisões mais abrangentes para as mais específicas:

1. `ADR-0001` - Stack tecnológica
2. `ADR-0002` - Arquitetura por UseCases + Abstractions + Services
3. `ADR-0003` - Persistência, EF Core e migrations
4. `ADR-0004` - Autenticação, autorização e políticas
5. `ADR-0005` - Multi-tenancy por OrganizationId
6. `ADR-0006` - Estilo de API e padronização de erros
7. `ADR-0007` - Estratégia OpenAPI semântica
8. `ADR-0008` - Outbox com retry/backoff/dead-letter
9. `ADR-0009` - Estratégia de testes
10. `ADR-0010` - Governança por testes de arquitetura

## Status possíveis

- `Proposed`: em discussão
- `Accepted`: adotada e em uso
- `Superseded`: substituída por outra ADR
- `Deprecated`: ainda existente, mas não recomendada para novas evoluções

## Template

Use a estrutura abaixo em novas ADRs:

```md
# ADR-XXXX: Título

## Status
Accepted | Proposed | Superseded | Deprecated

## Contexto
...

## Decisão
...

## Consequências
...

## Alternativas consideradas
...
```

## Regra de governança

Mudanças arquiteturais em PRs devem incluir ADR nova ou atualização explícita de ADR existente.
