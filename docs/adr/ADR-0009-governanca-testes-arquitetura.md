# ADR-0009: Governança por testes de arquitetura

## Status
Accepted

## Contexto

Regras arquiteturais críticas sofrem regressão quando não há proteção automatizada.
A solução exige validação contínua de fronteiras entre camadas, segurança e isolamento de tenant.

## Decisão

Manter testes de arquitetura automatizados em `tests/Bud.Server.Tests/Architecture` para impor:

### Fronteiras entre camadas

- Controllers não dependem diretamente de `ApplicationDbContext`.
- Controllers dependem de `Application` (Commands/Queries), não de repositories concretos.
- Commands/Queries dependem de abstrações de `Infrastructure` (interfaces), não de implementações concretas.
- `Application` não referencia tipos concretos de infraestrutura.
- `Domain` não depende de `Application` nem de `Infrastructure`.
- Repositories não expõem DTOs HTTP de `Bud.Shared.Contracts` em tipos de retorno.

### Segurança e tenant isolation

- Toda entidade `ITenantEntity` possui `HasQueryFilter`.
- Controllers (exceto autenticação) exigem autorização.
- Controllers herdam de `ApiControllerBase`.

## Consequências

- Regressões arquiteturais são detectadas cedo em CI.
- Revisão de código fica mais objetiva.
- Invariantes de segurança e isolamento de tenant ficam protegidos por automação.

## Alternativas consideradas

- Governança apenas via code review manual.
- Ferramentas externas dedicadas de validação arquitetural.
