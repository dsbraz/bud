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
2. `ADR-0002` - Arquitetura por UseCases e Repositories
3. `ADR-0003` - Persistência, EF Core e PostgreSQL
4. `ADR-0004` - Autenticação, autorização e políticas
5. `ADR-0005` - Multi-tenancy por OrganizationId
6. `ADR-0006` - Estilo de API e padronização de erros
7. `ADR-0007` - Estratégia OpenAPI semântica
8. `ADR-0008` - Estratégia de testes
9. `ADR-0009` - Governança por testes de arquitetura
10. `ADR-0010` - Servidor MCP HTTP remoto para missões, métricas e check-ins
11. `ADR-0011` - Sistema de notificações em use cases
12. `ADR-0012` - Aggregate roots explícitas, value objects e invariantes de domínio
13. `ADR-0013` - Hardening de produção (JWT fail-fast, security headers, rate limiting, forwarded headers)
14. `ADR-0014` - Co-localização de interfaces (ports) com implementações

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
