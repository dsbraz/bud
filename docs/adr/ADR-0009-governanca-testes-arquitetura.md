# ADR-0009: Governança por testes de arquitetura

## Status
Accepted

## Contexto

Mesmo com boa arquitetura inicial, regressões acontecem quando novas features introduzem dependências inadequadas entre camadas.

## Decisão

Manter testes de arquitetura automatizados em `tests/Bud.Server.Tests/Architecture` para impor regras como:

### Fronteiras entre camadas

- Controllers não dependem diretamente de `ApplicationDbContext`
- UseCases não dependem de contratos legados de `Services`
- Camada `Application` não expõe tipos da camada `Services`
- Camada `Application` não depende da camada `Data`
- Camada `Domain` não depende da camada `Services`
- Camada `Services` não expõe DTOs/Responses de `Bud.Shared.Contracts` em tipos de retorno

### Segurança e tenant isolation

- Toda entidade `ITenantEntity` deve ter `HasQueryFilter` configurado no `ApplicationDbContext`
- Todo controller (exceto `AuthController`) deve ter `[Authorize]` no nível de classe
- Todo controller deve herdar de `ApiControllerBase`

## Consequências

- Regressões arquiteturais detectadas cedo
- Revisões de PR mais objetivas
- Invariantes de segurança (tenant isolation, authorization) protegidos por testes automatizados
- Necessidade de manutenção dos testes conforme evolução da arquitetura

## Alternativas consideradas

- Confiar apenas em code review manual: menor custo inicial, menor consistência
- Ferramentas externas de arquitetura: poderosas, porém custo de adoção adicional
