# ADR-0009: Governanca por testes de arquitetura

## Status
Accepted (atualizado — regras de fronteira atualizadas para refletir migração de Services para Infrastructure/Repositories)

## Contexto

Mesmo com boa arquitetura inicial, regressoes acontecem quando novas features introduzem dependencias inadequadas entre camadas.

Com a migracão de `Services/` para `Application/Ports/` + `Infrastructure/Repositories/`, as regras de fronteira precisaram ser atualizadas para refletir a nova estrutura.

## Decisão

Manter testes de arquitetura automatizados em `tests/Bud.Server.Tests/Architecture` para impor regras como:

### Fronteiras entre camadas

- Controllers não dependem diretamente de `ApplicationDbContext`
- Controllers dependem apenas de UseCases, nunca de Repositories diretamente
- UseCases dependem de interfaces (ports) em `Application/Ports/`, nunca de implementações em `Infrastructure/`
- Camada `Application` não depende da camada `Infrastructure`
- Camada `Domain` não depende da camada `Application` nem de `Infrastructure`
- `Infrastructure/Repositories` não expõem DTOs/Responses de `Bud.Shared.Contracts` em tipos de retorno
- **Repositories NÃO contêm lógica de negócio** — responsabilidade exclusiva de persistência e queries
- **UseCases DEVEM conter a orquestração completa** — autorização, coordenação de ports, lógica de aplicação

### Segurança e tenant isolation

- Toda entidade `ITenantEntity` deve ter `HasQueryFilter` configurado no `ApplicationDbContext`
- Todo controller (exceto `AuthController`) deve ter `[Authorize]` no nível de classe
- Todo controller deve herdar de `ApiControllerBase`

## Consequências

- Regressões arquiteturais detectadas cedo
- Revisões de PR mais objetivas
- Invariantes de segurança (tenant isolation, authorization) protegidos por testes automatizados
- Necessidade de manutenção dos testes conforme evolução da arquitetura
- Fronteiras mais claras entre orquestração (UseCases) e persistência (Repositories)

## Alternativas consideradas

- Confiar apenas em code review manual: menor custo inicial, menor consistência
- Ferramentas externas de arquitetura: poderosas, porém custo de adoção adicional
