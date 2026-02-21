# ADR-0004: Repositórios por Agregado e Unit of Work

## Status
Accepted

## Contexto
Operações de escrita precisam de consistência transacional e fronteira clara de persistência.

## Decisão
- Definir repositório por agregado com contratos orientados a intenção de negócio.
- Publicar contratos de repositório em `Bud.Server/Domain/Repositories` e implementações em `Bud.Server/Infrastructure/Repositories`.
- Evitar retorno de payload HTTP em repositórios.
- Introduzir `IUnitOfWork` para commit explícito e coordenação transacional.

## Consequências
- Escritas coordenadas de forma explícita.
- Menor acoplamento entre persistência e borda.
- Adoção disciplinada de transação por caso de uso.

## Alternativas consideradas
- `SaveChanges` distribuído em múltiplos serviços.
- Repositórios genéricos sem fronteira de agregado.
