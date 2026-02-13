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

## Consequências

- Melhor separação de responsabilidades
- Maior testabilidade (mock de interfaces em UseCases)
- Menor acoplamento entre API e implementação
- Exige disciplina para evitar dependências cruzadas

## Alternativas consideradas

- Controllers chamando serviços diretamente: mais simples, porém mais acoplado
- MediatR em toda a aplicação: bom padrão, mas com custo adicional de introdução no momento
