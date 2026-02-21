# ADR-0005: Persistência com EF Core e Mapeamento de Infraestrutura

## Status
Accepted

## Contexto
O sistema precisa de mapeamento estável entre modelo de domínio e banco relacional com baixa fricção operacional.

## Decisão
- Usar EF Core com configurações explícitas por entidade.
- Manter mapeamentos na infraestrutura.
- Garantir que regras de banco (índices, chaves, relacionamentos) não vazem para a borda.

## Consequências
- Persistência padronizada e previsível.
- Clareza entre modelo de domínio e representação relacional.
- Exige manutenção disciplinada de configurações por entidade.

## Alternativas consideradas
- Mapeamento implícito por convenção sem configurações dedicadas.
- SQL ad-hoc como abordagem principal.
