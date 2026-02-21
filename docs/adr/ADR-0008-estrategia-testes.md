# ADR-0008: Estratégia de testes (unit + integration)

## Status
Accepted

## Contexto

A aplicação possui regras críticas de negócio, multi-tenancy e fluxos assíncronos.
É necessário equilibrar velocidade de feedback com confiança end-to-end.

## Decisão

Adotar estratégia híbrida:

- **Unit tests** para `Application` Commands/Queries, repositories, serviços de domínio, validações e componentes isolados.
- **Integration tests** com `WebApplicationFactory` + PostgreSQL via Testcontainers.
- TDD como fluxo padrão (`Red -> Green -> Refactor`).

## Consequências

- Alta confiança contra regressões funcionais e arquiteturais.
- Suíte completa mais lenta do que apenas unit tests.
- Melhor cobertura dos cenários reais de infraestrutura.

## Alternativas consideradas

- Somente unit tests.
- Somente integration tests.
