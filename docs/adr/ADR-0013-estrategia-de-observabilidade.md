# ADR-0013: Estratégia de Observabilidade

## Status
Accepted

## Contexto
O sistema Bud roda em produção no Google Cloud Run. A observabilidade inicial era mínima: logs não-estruturados para stdout (sem metadados compatíveis com Cloud Logging), zero distributed tracing, métricas HTTP customizadas sem exportador configurado, e o Bud.Mcp era um ponto cego total. A falta de contexto estruturado nas entradas de log tornava diagnóstico de incidentes ineficiente.

## Decisão

### 1. Structured Logging (Cloud Logging JSON)
- Implementar `ConsoleFormatter` customizado (`CloudLoggingJsonFormatter`) que emite JSON single-line compatível com o Cloud Logging do GCP.
- Campos obrigatórios: `severity` (mapeado de `LogLevel`), `message`, `time` (ISO 8601), `logging.googleapis.com/trace`, `logging.googleapis.com/spanId`, `eventId`, `category`.
- Ativo apenas em ambientes não-Development/Testing; em desenvolvimento, mantém o formatter padrão do console.
- GCP Project ID configurável via variável de ambiente `GCP_PROJECT_ID`.

### 2. OpenTelemetry SDK (Traces + Metrics)
- Adotar OpenTelemetry para distributed tracing e métricas via `OpenTelemetry.Extensions.Hosting`.
- Instrumentação: ASP.NET Core, HttpClient e EF Core (`Bud.Api`/`Bud.Infrastructure`); ASP.NET Core e HttpClient (`Bud.Mcp`).
- **Princípio config-as-environment**: o código registra apenas *o que instrumentar*. Toda configuração de exportação, recursos e service name é externalizada via variáveis de ambiente padrão do OTel spec (sem hardcode em código ou appsettings).
- `UseOtlpExporter()` lê automaticamente `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`, etc.
- Health checks excluídos dos traces (`Filter` na instrumentação ASP.NET Core).
- Métricas customizadas do `RequestTelemetryMiddleware` removidas em favor das métricas automáticas do OTel ASP.NET Core (`http.server.request.duration`, `http.server.active_requests`).

### 3. Log Enrichment Middleware (Bud.Api)
- `LogEnrichmentMiddleware` cria `ILogger.BeginScope()` com `TraceId`, `SpanId` (de `Activity.Current`) e `CorrelationId` (de `context.TraceIdentifier`).
- Posicionado imediatamente após `UseExceptionHandler()` para que todos os middlewares e use cases downstream emitam logs com contexto de trace.

### 4. Request Logging Middleware (Bud.Mcp)
- `McpRequestLoggingMiddleware` registra cada request MCP com método, path, status code, elapsed time e correlation ID.
- EventId estável: 5000.
- Header `X-Correlation-Id` adicionado à resposta (sem sobrescrever header existente).

### 5. Source-Generated Logging nos Use Cases
- Todos os use cases de escrita (Create/Patch/Delete) recebem `ILogger<T>` via construtor.
- `[LoggerMessage]` source-generated com EventIds estáveis por domínio.
- Ranges de EventId alocados:
  - 3100–3199: RequestTelemetryMiddleware
  - 4000–4009: Goal
  - 4010–4019: Organization
  - 4020–4029: Workspace
  - 4030–4039: Team
  - 4040–4049: Collaborator
  - 4050–4059: Indicator
  - 4060–4069: Checkin
  - 4070–4079: Template
  - 4080–4089: GoalTask
  - 4090–4099: Session / Notification
  - 5000–5009: McpRequestLoggingMiddleware (Bud.Mcp)

### 6. Módulo de Composição (Bud.Api)
- `AddBudObservability(services, configuration, environment)` em `BudObservabilityCompositionExtensions`.
- Primeiro item chamado em `AddBudPlatform`, garantindo que logging e tracing estejam disponíveis para todos os outros módulos.

### 7. Duplicação do Formatter no Bud.Mcp
- `Bud.Mcp` não referencia `Bud.Api`, portanto `CloudLoggingJsonFormatter` é duplicado em `src/Bud.Mcp/Observability/`.
- Aceito como trade-off de isolamento; consolidar em pacote compartilhado é deferido.

## Consequências
- Logs em produção são indexáveis por `severity`, `traceId`, `correlationId` e `eventId` no Cloud Logging.
- Distributed traces correlacionam requests do Bud.Client → Bud.Api → PostgreSQL e do agente → Bud.Mcp → Bud.Api.
- Métricas HTTP padrão disponíveis via OTel sem manutenção de instrumentação customizada.
- EventIds estáveis permitem alertas e dashboards baseados em IDs específicos.
- Custo de instrumentação: ~zero em dev local (OTLP exporter falha silenciosamente sem `OTEL_EXPORTER_OTLP_ENDPOINT`).

## Alternativas consideradas
- **Serilog + Seq**: logging mais rico, mas adiciona dependência externa e não integra nativamente ao Cloud Logging.
- **Google.Cloud.OpenTelemetry**: SDK específico do GCP para OTel, descartado em favor do OTLP padrão com env vars para evitar acoplamento ao vendor no código.
- **Centralizar formatter num pacote shared**: deferido; custo de manutenção de pacote interno não justificado pela duplicação de dois arquivos pequenos.
- **Manter métricas customizadas**: removidas porque as métricas automáticas do OTel cobrem os casos de uso com maior padronização e sem manutenção.
