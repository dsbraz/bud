# ADR-0003: Agregados, Entidades, Value Objects e Invariantes

## Status
Accepted

## Contexto
Regras críticas de negócio precisam ser protegidas no ponto de verdade do domínio.

## Decisão
Modelar o domínio com:
- Agregados com raiz explícita.
- Entidades internas sem fuga de invariantes.
- Value Objects para conceitos semânticos.
- Invariantes validadas por comportamento de domínio.

Falhas de regra de negócio são representadas por exceções/inconsistências de domínio e traduzidas na aplicação.

## Consequências
- Integridade do modelo concentrada no domínio.
- Redução de lógica de negócio espalhada em controllers/repositórios.
- Necessidade de testes de invariantes por agregado.

## Alternativas consideradas
- Modelo anêmico com validações apenas em serviços.
- Regras distribuídas em camada de infraestrutura.
