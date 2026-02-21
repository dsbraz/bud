# ADR-0006: Estilo de API e padronização de erros

## Status
Accepted

## Contexto

A API precisa de contrato consistente para sucesso/erro, reduzindo divergências entre endpoints e simplificando consumo no client.

## Decisão

Adotar padrão REST com:

- Controllers finos delegando para `Application` Commands/Queries.
- Retorno funcional via `Result` / `Result<T>`.
- Mapeamento consistente para HTTP status code.
- Erros em `ProblemDetails` / `ValidationProblemDetails`.
- Mensagens de erro e validação em pt-BR.

## Consequências

- Comportamento previsível para client e integrações.
- Menos lógica duplicada de tratamento de erro.
- Contrato mais simples de documentar em OpenAPI.

## Alternativas consideradas

- Exceções como fluxo principal de controle.
- Resposta de erro customizada por endpoint.
