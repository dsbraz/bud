# ADR-0004: Governança por testes de arquitetura

## Status
Accepted

## Contexto

Mesmo com boa arquitetura inicial, regressões acontecem quando novas features introduzem dependências inadequadas entre camadas.

## Decisão

Manter testes de arquitetura automatizados em `tests/Bud.Server.Tests/Architecture` para impor regras como:

- Controllers não dependem diretamente de `ApplicationDbContext`
- UseCases não dependem de contratos legados de `Services`
- Camada `Application` não expõe tipos da camada `Services`

## Consequências

- Regressões arquiteturais detectadas cedo
- Revisões de PR mais objetivas
- Necessidade de manutenção dos testes conforme evolução da arquitetura

## Alternativas consideradas

- Confiar apenas em code review manual: menor custo inicial, menor consistência
- Ferramentas externas de arquitetura: poderosas, porém custo de adoção adicional
