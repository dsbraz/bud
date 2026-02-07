# ADRs (Architecture Decision Records)

Este diretório registra decisões arquiteturais relevantes do projeto.

## Convenção

- Arquivos no formato `ADR-XXXX-titulo-curto.md`
- Numeração sequencial com 4 dígitos (ex.: `ADR-0001`)
- Idioma: pt-BR

## Ordem de leitura recomendada

Para onboarding, use a sequência lógica abaixo (independente da numeração histórica):

1. `ADR-0006` - Stack tecnológica
2. `ADR-0001` - Arquitetura por UseCases + Abstractions + Services
3. `ADR-0007` - Estilo de API e padronização de erros
4. `ADR-0008` - Autenticação, autorização e políticas
5. `ADR-0009` - Persistência, EF Core e migrations
6. `ADR-0002` - Multi-tenancy por OrganizationId
7. `ADR-0010` - Estratégia OpenAPI semântica
8. `ADR-0003` - Outbox com retry/backoff/dead-letter
9. `ADR-0005` - Estratégia de testes
10. `ADR-0004` - Governança por testes de arquitetura

## Status possíveis

- `Accepted`: decisão ativa
- `Superseded`: substituída por outra ADR
- `Deprecated`: não recomendada para novas evoluções
- `Proposed`: em discussão

## Template

Use a estrutura abaixo em novas ADRs:

```md
# ADR-XXXX: Título

## Status
Accepted | Proposed | Superseded | Deprecated

## Contexto
Problema, restrições e motivação.

## Decisão
O que foi decidido.

## Consequências
Impactos positivos e negativos.

## Alternativas consideradas
Opções descartadas e trade-offs.
```

## Regra de governança

Mudanças arquiteturais (camadas, contratos, integração, resiliência, padrões estruturais)
devem incluir ADR nova ou atualização explícita de ADR existente.
