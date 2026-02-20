# ADR-0002: Arquitetura por UseCases e Services

## Status
Accepted

## Contexto

O backend precisava reduzir acoplamento entre controllers, regras de aplicação e detalhes de implementação.
Também era necessário facilitar testes e evolução por domínio.

## Decisão

Adotar estrutura em camadas:

- Controllers dependem de UseCases (`Command/Query`)
- UseCases dependem de interfaces de serviço em `Services/` (`I*Service`)
- `Services/` contém interfaces e implementações com regras de negócio e persistência
- `ServiceResult` padroniza retorno funcional das operações
- `Application` não depende diretamente de tipos em `Data/` (ex.: `ApplicationDbContext` ou lookups da camada `Data`)
- `Domain` não depende de `Services/`
- `Services` não retornam DTOs HTTP de `Bud.Shared.Contracts`; retornam entidades de domínio ou read models de domínio
- Mapeamento para contratos HTTP (`Bud.Shared.Contracts`) ocorre na camada `Application`

## Consequências

- Melhor separação de responsabilidades
- Maior testabilidade (mock de interfaces em UseCases)
- Menor acoplamento entre API e implementação
- Exige disciplina para evitar dependências cruzadas
- Requer mapeamento explícito entre read models de domínio e DTOs de contrato na camada `Application`

## Alternativas consideradas

- Controllers chamando serviços diretamente: mais simples, porém mais acoplado
- MediatR em toda a aplicação: bom padrão, mas com custo adicional de introdução no momento
