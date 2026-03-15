# ADR-0002: Arquitetura e Regras de Dependência

## Status
Accepted

## Contexto
A evolução do sistema exige fronteiras arquiteturais rígidas para preservar o modelo de domínio e maximizar coesão por feature.

## Decisão
Estabelecer dependências unidirecionais e organização física por feature:

**Regras de dependência:**
- `Bud.Api` → `Bud.Application` + `Bud.Infrastructure` + `Bud.Shared`.
- `Controllers` → casos de uso de aplicação.
- Casos de uso → interfaces de repositório/serviço (definidas em `Bud.Application`).
- `Bud.Application` → `Bud.Domain` + `Bud.Shared`.
- `Bud.Domain` sem dependência de Application, Infrastructure ou ASP.NET.
- `Bud.Infrastructure` → `Bud.Application` + `Bud.Domain` + `Bud.Shared`.
- `Bud.Api` não referencia `Bud.Domain` diretamente.

**Organização física por feature:**
- `Bud.Domain/<Feature>/` — entidades, value objects e eventos de domínio.
- `Bud.Application/Features/<Feature>/` — casos de uso, interfaces de repositório, ports da feature, mappers, read models e policies por feature.
- `Bud.Application/Ports/` — apenas ports transversais (ex: tenant/auth).
- `Bud.Infrastructure/Features/<Feature>/` — implementações de repositório e search specs.
- `Bud.Api/Features/<Feature>/` — controller e validators por feature.
- `Bud.Shared.Contracts/Features/<Feature>/` — DTOs de request e response por feature.

## Consequências
- Maior isolamento do núcleo de domínio.
- Navegação por feature: todos os artefatos de uma capacidade de negócio estão co-localizados.
- Custos iniciais maiores para refatorações estruturais.

## Alternativas consideradas
- Organização por camada técnica (Controllers/, Repositories/, UseCases/ flat).
- Dependências cruzadas entre domínio e infraestrutura.
