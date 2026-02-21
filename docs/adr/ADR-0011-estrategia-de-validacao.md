# ADR-0011: Estratégia de Validação

## Status
Accepted

## Contexto
Validação de entrada e validação de regra de negócio possuem naturezas diferentes e precisam de separação clara.

## Decisão
- `FluentValidation` na borda para validação sintática/estrutural de requests.
- Invariantes e regra de negócio no domínio e nos casos de uso.
- Mensagens de erro ao usuário em pt-BR.

## Consequências
- Redução de duplicidade de regra entre validator e domínio.
- Fronteira clara entre erro de entrada e erro de negócio.

## Alternativas consideradas
- Concentrar toda validação em validators.
- Concentrar toda validação apenas no domínio.
