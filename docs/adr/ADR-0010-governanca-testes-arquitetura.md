# ADR-0010: Governança por testes de arquitetura

## Status
Accepted

## Contexto

Mesmo com boa arquitetura inicial, regressões acontecem quando novas features introduzem dependências inadequadas entre camadas.

## Decisão

Manter testes de arquitetura automatizados em `tests/Bud.Server.Tests/Architecture` para impor regras como:

### Fronteiras entre camadas (originais)

- Controllers não dependem diretamente de `ApplicationDbContext`
- UseCases não dependem de contratos legados de `Services`
- Camada `Application` não expõe tipos da camada `Services`

### Segurança e tenant isolation

- Toda entidade `ITenantEntity` deve ter `HasQueryFilter` configurado no `ApplicationDbContext`
- Todo controller (exceto `AuthController`) deve ter `[Authorize]` no nível de classe
- Todo controller deve herdar de `ApiControllerBase`

### Domain events e observabilidade

- Todo `IDomainEvent` concreto deve ter ao menos um `IDomainEventSubscriber<TEvent>`
- Todo `IDomainEventSubscriber<>` concreto deve estar registrado no DI (`AddBudApplication()`)
- Todo `IDomainEvent` deve ser `sealed record` (imutabilidade + serialização outbox)

## Consequências

- Regressões arquiteturais detectadas cedo
- Revisões de PR mais objetivas
- Invariantes de segurança (tenant isolation, authorization) protegidos por testes automatizados
- Contratos de domain events (subscriber coverage, DI registration, sealed record convention) validados em build time
- Necessidade de manutenção dos testes conforme evolução da arquitetura

## Alternativas consideradas

- Confiar apenas em code review manual: menor custo inicial, menor consistência
- Ferramentas externas de arquitetura: poderosas, porém custo de adoção adicional
