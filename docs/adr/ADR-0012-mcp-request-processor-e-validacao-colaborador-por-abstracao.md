# ADR-0014: MCP Request Processor e Validação de Colaborador por Abstração

## Status
Accepted

## Contexto

O endpoint HTTP do `Bud.Mcp` estava com lógica concentrada em `Program.cs` (parsing JSON-RPC, sessão, headers e dispatch), reduzindo coesão e dificultando evolução/testes.

No `Bud.Server`, validators de colaborador acessavam `ApplicationDbContext` diretamente, gerando acoplamento entre validação e data access.

## Decisão

1. Extrair o fluxo HTTP do MCP para `IMcpRequestProcessor`/`McpRequestProcessor`, mantendo `Program.cs` apenas como composição/roteamento.
2. Introduzir `ICollaboratorValidationService` para regras de validação dependentes de dados (unicidade de e-mail e validação de líder), removendo acesso direto ao `DbContext` dos validators.

## Consequências

- Melhor separação de responsabilidades no `Bud.Mcp` e aderência à diretriz de evitar lógica ad-hoc no `Program.cs`.
- Validators ficam focados em regras de validação e delegam checks de dados para abstrações testáveis.
- A composição DI passa a registrar `ICollaboratorValidationService`.

## Alternativas consideradas

1. Manter lógica MCP no `Program.cs`: descartada por baixa manutenibilidade.
2. Manter queries nos validators: descartada por acoplamento arquitetural e menor testabilidade.
