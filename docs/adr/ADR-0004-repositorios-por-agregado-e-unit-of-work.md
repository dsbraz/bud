# ADR-0004: Repositórios por Agregado e Unit of Work

## Status
Accepted

## Contexto
Operações de escrita precisam de consistência transacional e fronteira clara de persistência. As interfaces de repositório definem contratos orientados à intenção de negócio e são consumidas exclusivamente pela camada de aplicação (casos de uso).

## Decisão
- Definir um repositório por agregado com contratos orientados a intenção de negócio.
- Publicar contratos de repositório em `src/Server/Bud.Application/Features/<Feature>/` (ex: `IGoalRepository` em `Bud.Application.Features.Goals`), pois são os casos de uso quem definem o que precisam da persistência.
- Implementações residem em `src/Server/Bud.Infrastructure/Features/<Feature>/`.
- Repositórios retornam entidades de domínio ou read models; nunca DTOs HTTP de `Bud.Shared.Contracts`.
- Introduzir `IUnitOfWork` para commit explícito e coordenação transacional.

## Consequências
- Escritas coordenadas de forma explícita.
- Menor acoplamento entre persistência e borda.
- Adoção disciplinada de transação por caso de uso.
- Domain permanece livre de contratos de persistência — contém apenas entidades, value objects e eventos.

## Alternativas consideradas
- Interfaces em `Bud.Domain` (Evans/Vernon): válido em domínios ricos com domain services que precisam de repositório. No bud, entidades de domínio não chamam repositórios, tornando Application a posição mais coerente.
- `SaveChanges` distribuído em múltiplos serviços.
- Repositórios genéricos sem fronteira de agregado.
