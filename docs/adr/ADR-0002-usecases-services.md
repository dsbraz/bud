# ADR-0002: Arquitetura por Commands/Queries e Repositories

## Status
Accepted

## Contexto

O backend precisa manter fronteiras claras entre transporte HTTP, orquestração de aplicação e persistência.
Também é necessário suportar evolução por domínio com alta testabilidade e baixo acoplamento.

## Decisão

Adotar arquitetura em camadas com comandos/queries na camada `Application`:

- **Controllers** dependem de **Application Commands/Queries**.
- **Commands/Queries** concentram orquestração de aplicação (autorização, coordenação de ports, fluxo de negócio de aplicação).
- **Commands/Queries** dependem de interfaces (ports) de `Infrastructure/Repositories` e `Infrastructure/Services`.
- **Repositories/Services** implementam os ports e encapsulam detalhes de persistência/infraestrutura.
- **Domain** permanece independente de `Application` e `Infrastructure`.
- Repositories retornam entidades/read models de domínio; mapeamento para `Bud.Shared.Contracts` ocorre em `Application`.
- `Result`/`Result<T>` em `Application/Common` padroniza retornos funcionais.

### Diagrama de dependências

```
Controller -> Application Command/Query -> Port (interface) -> Repository/Service -> DbContext
```

### Responsabilidades por camada

| Camada | Responsabilidade |
|--------|------------------|
| `Controllers/` | Validação de request, mapeamento HTTP, delegação para Commands/Queries |
| `Application/` | Orquestração de aplicação, autorização, composição de fluxo |
| `Infrastructure/Repositories/` | Interfaces e implementações de persistência |
| `Infrastructure/Services/` | Interfaces e implementações de serviços de infraestrutura |
| `Application/Common/` | `Result`/`Result<T>` e utilitários da aplicação |
| `Domain/` | Entidades, value objects, invariantes e specifications |

## Consequências

- Fronteiras explícitas entre API, aplicação e persistência.
- Menor acoplamento entre controllers e detalhes de infraestrutura.
- Maior testabilidade da camada `Application` via mocks de ports.
- Requer disciplina para evitar lógica de negócio em repositories.

## Alternativas consideradas

- Controllers chamando repositórios diretamente.
- Introdução de pipeline com MediatR para todos os fluxos.
