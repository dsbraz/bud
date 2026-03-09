# ADR-0002: Arquitetura DDD Estrita e Regras de Dependência

## Status
Accepted

## Contexto
A evolução do sistema exige fronteiras arquiteturais rígidas para preservar o modelo de domínio.

## Decisão
Estabelecer dependências unidirecionais:
- `Bud.Api` -> `Bud.Application` + `Bud.Infrastructure` + `Bud.Shared`.
- `Controllers` -> casos de uso de aplicação.
- Casos de uso -> portas de repositório/serviço.
- `Bud.Application` -> `Bud.Domain` + `Bud.Shared`.
- `Bud.Domain` sem dependência de infraestrutura ou ASP.NET.
- `Bud.Infrastructure` -> `Bud.Application` + `Bud.Domain` + `Bud.Shared`.
- Infraestrutura implementa portas do domínio/aplicação.
- `Bud.Api` não referencia `Bud.Domain` diretamente.
- `Bud.Shared` permanece como projeto compartilhado nesta etapa, sem desmontar contratos existentes.

## Consequências
- Maior isolamento do núcleo de domínio.
- Menor acoplamento entre HTTP, persistência e regra de negócio.
- Custos iniciais maiores para refatorações estruturais.

## Alternativas consideradas
- Arquitetura orientada por controllers e serviços genéricos.
- Dependências cruzadas entre domínio e infraestrutura.
