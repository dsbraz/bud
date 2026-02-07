# ADR-0007: Estilo de API e padronização de erros

## Status
Accepted

## Contexto

A API precisava de contrato consistente para sucesso/erro, reduzindo divergência entre endpoints
e facilitando consumo no client.

## Decisão

Adotar padrão REST com:

- Controllers finos delegando para UseCases
- Retorno funcional via `ServiceResult` / `ServiceResult<T>`
- Mapeamento padronizado para HTTP status code
- Erros no formato `ProblemDetails`/`ValidationProblemDetails`
- Mensagens de erro e validação em pt-BR

## Consequências

- Comportamento previsível para client e integrações
- Menos lógica duplicada de tratamento de erro
- Contrato mais fácil de documentar em OpenAPI

## Alternativas consideradas

- Exceções como fluxo de controle para regras de negócio: menos explícito, difícil padronização
- Resposta customizada por endpoint: mais flexível, menor consistência global
