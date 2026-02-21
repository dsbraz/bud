# ADR-0010: Contratos HTTP, Tratamento de Erros e Camada de Borda

## Status
Accepted

## Contexto
Clientes HTTP e MCP precisam de contratos estáveis, independentes do modelo interno de domínio.

## Decisão
- Endpoints retornam contratos de borda (`Bud.Shared.Contracts`).
- Controllers atuam como adaptadores HTTP puros.
- Erros padronizados com `ProblemDetails`/`ValidationProblemDetails`.

## Consequências
- Menor acoplamento de clientes ao modelo interno.
- Evolução de domínio com menor impacto externo.
- Exige mapeamento explícito entre domínio e contratos de borda.

## Alternativas consideradas
- Expor entidades de domínio diretamente pela API.
- Mapeamento de erro ad-hoc por endpoint.
