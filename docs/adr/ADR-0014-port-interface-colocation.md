# ADR-0014: Co-localização de ports com implementações em Infrastructure

## Status
Accepted

## Contexto

Interfaces de repositórios e serviços de infraestrutura precisam estar organizadas junto das implementações para reduzir fricção de manutenção e melhorar navegação.

## Decisão

- Manter interfaces de repositório em `Infrastructure/Repositories` junto com implementações.
- Manter interfaces de serviços em `Infrastructure/Services` junto com implementações.
- Commands/Queries da `Application` dependem desses ports (interfaces), nunca de tipos concretos.

A inversão de dependência é preservada pelo contrato em interface, mesmo com co-localização física na infraestrutura.

## Consequências

- Navegação mais direta entre contrato e implementação.
- Menor custo de evolução por domínio.
- Necessidade de testes de arquitetura para impedir dependência de concretos na `Application`.

## Alternativas consideradas

- Centralizar interfaces em pasta única de ports na aplicação.
- Criar subpastas adicionais de abstrações dentro de `Infrastructure`.
