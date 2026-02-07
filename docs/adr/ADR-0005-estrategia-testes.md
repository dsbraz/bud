# ADR-0005: Estratégia de testes (unit + integration)

## Status
Accepted

## Contexto

A aplicação possui regras de negócio críticas, multi-tenancy e fluxos assíncronos.
Era necessário equilibrar velocidade de feedback e confiança end-to-end.

## Decisão

Adotar estratégia híbrida:

- **Unit tests** para UseCases, Services, validações e componentes isolados
- **Integration tests** com `WebApplicationFactory` + PostgreSQL via Testcontainers
- TDD como fluxo padrão (`Red -> Green -> Refactor`)

## Consequências

- Alta confiança em regressões funcionais e arquiteturais
- Tempo de execução maior na suíte completa comparado a apenas unit tests
- Melhor cobertura dos cenários reais de infraestrutura

## Alternativas consideradas

- Somente unit tests: rápido, mas com risco de gaps de integração
- Somente integration tests: alta confiança end-to-end, porém ciclo de feedback mais lento
