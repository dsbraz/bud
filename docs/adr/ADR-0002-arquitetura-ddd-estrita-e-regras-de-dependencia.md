# ADR-0002: Arquitetura DDD Estrita e Regras de Dependência

## Status
Accepted

## Contexto
A evolução do sistema exige fronteiras arquiteturais rígidas para preservar o modelo de domínio.

## Decisão
Estabelecer dependências unidirecionais:
- `Controllers` -> casos de uso de aplicação.
- Casos de uso -> portas de repositório/serviço.
- `Domain` sem dependência de infraestrutura.
- Infraestrutura implementa portas do domínio/aplicação.
- `Bud.Shared` restrito à camada de borda (contratos).

## Consequências
- Maior isolamento do núcleo de domínio.
- Menor acoplamento entre HTTP, persistência e regra de negócio.
- Custos iniciais maiores para refatorações estruturais.

## Alternativas consideradas
- Arquitetura orientada por controllers e serviços genéricos.
- Dependências cruzadas entre domínio e infraestrutura.
