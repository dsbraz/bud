# ADR-0006: Stack tecnológica da aplicação

## Status
Accepted

## Contexto

Era necessário definir uma stack única para acelerar entrega, reduzir fricção de integração
entre frontend e backend e manter operação simples no ambiente atual.

## Decisão

Adotar:

- Backend: ASP.NET Core 10
- Frontend: Blazor WebAssembly (SPA)
- Persistência: EF Core 10 + PostgreSQL
- Testes de integração: WebApplicationFactory + Testcontainers PostgreSQL
- Containerização: Docker Compose para desenvolvimento local

## Consequências

- Stack consistente e homogênea no ecossistema .NET
- Boa produtividade de equipe com contratos compartilhados (`Bud.Shared`)
- Operação simplificada para ambiente atual
- Acoplamento maior ao ecossistema .NET (trade-off aceito)

## Alternativas consideradas

- Frontend separado em framework JS (React/Vue): maior flexibilidade, maior complexidade de integração
- Banco relacional alternativo: possível, porém sem ganho claro frente ao PostgreSQL no contexto atual
