# ADR-0002: Arquitetura por UseCases e Repositories

## Status
Accepted (atualizado — substitui versão anterior "UseCases e Services")

## Contexto

O backend precisava reduzir acoplamento entre controllers, regras de aplicação e detalhes de implementação.
Também era necessário facilitar testes e evolução por domínio.

A versão anterior desta ADR descrevia uma camada `Services/` que concentrava interfaces, implementações, regras de negócio e persistência. Com a evolução do projeto, essa camada foi eliminada em favor de uma separação mais clara entre orquestração (UseCases) e implementações de infraestrutura (`Infrastructure/Repositories/`, `Infrastructure/Services/`). As interfaces (ports) são co-localizadas com suas implementações na camada de infraestrutura (ver ADR-0014).

## Decisão

Adotar estrutura em camadas alinhada com Clean Architecture:

- **Controllers** dependem de **UseCases** (`Command`/`Query`)
- **UseCases** contêm a lógica de orquestração completa (não são thin delegators)
- **UseCases** dependem de interfaces (ports) co-localizadas com implementações em `Infrastructure/` (`I*Repository` em `Repositories/`, `IAuthService` etc. em `Services/`)
- **Implementações** dos ports ficam no mesmo diretório que suas interfaces
- **`Result`** (em `Application/Common/`) padroniza retorno funcional das operações (substitui o antigo `ServiceResult`)
- **Domain Services** (ex.: `MissionProgressCalculator`) encapsulam lógica de domínio complexa que não pertence a um único aggregate
- `Application` importa namespaces de `Infrastructure/` apenas para referenciar interfaces (ports); não depende de tipos concretos (ex.: `ApplicationDbContext` ou lookups de infraestrutura)
- `Domain` não depende de `Application/` nem de `Infrastructure/`
- `Infrastructure/Repositories` não retornam DTOs HTTP de `Bud.Shared.Contracts`; retornam entidades de domínio ou read models de domínio
- Mapeamento para contratos HTTP (`Bud.Shared.Contracts`) ocorre na camada `Application` (UseCases)

### Diagrama de dependências

```
Controller → UseCase → Port (interface) → Repository (implementação) → DbContext
                ↓
         Domain Service (quando necessário)
```

### Responsabilidades por camada

| Camada | Responsabilidade |
|--------|------------------|
| `Controllers/` | Validação de request, mapeamento HTTP, delegação para UseCases |
| `Application/UseCases/` | Orquestração completa: autorização, lógica de aplicação, coordenação de ports |
| `Infrastructure/Repositories/` | Interfaces (`I*Repository`) e implementações de persistência com EF Core |
| `Infrastructure/Services/` | Interfaces e implementações de serviços de infraestrutura |
| `Application/Common/` | `Result`/`Result<T>`, tipos compartilhados da camada de aplicação |
| `Domain/` | Entities, Value Objects, Domain Services, Specifications |

## Consequências

- Melhor separação de responsabilidades — UseCases são o ponto central de orquestração
- Maior testabilidade (mock de interfaces de repositório em UseCases)
- Menor acoplamento entre API e implementação de persistência
- Repositories focados exclusivamente em persistência, sem lógica de negócio
- Exige disciplina para evitar dependências cruzadas
- Requer mapeamento explícito entre read models de domínio e DTOs de contrato na camada `Application`

## Alternativas consideradas

- **Camada Services unificada** (versão anterior): concentrava interfaces, implementações e regras de negócio; substituída por co-localização de interfaces com implementações em Infrastructure
- Controllers chamando repositórios diretamente: mais simples, porém sem camada de orquestração
- MediatR em toda a aplicação: bom padrão, mas com custo adicional de introdução no momento
