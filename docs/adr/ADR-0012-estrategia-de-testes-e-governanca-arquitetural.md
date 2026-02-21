# ADR-0012: Estratégia de Testes e Governança Arquitetural

## Status
Accepted

## Contexto
A manutenção de DDD estrito depende de testes que protejam comportamento e fronteiras arquiteturais.

## Decisão
- Adotar TDD como fluxo padrão (Red -> Green -> Refactor).
- Cobrir domínio, casos de uso, repositórios, validações e endpoints.
- Manter testes de arquitetura como gate obrigatório para dependências e padrões.

## Consequências
- Regressões arquiteturais passam a ser detectadas cedo.
- Maior confiança em refatorações amplas.
- Investimento contínuo em suíte de testes e tempo de execução.

## Alternativas consideradas
- Testes apenas de integração.
- Governança arquitetural somente por revisão manual.
