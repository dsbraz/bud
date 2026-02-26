# Agent Note (non-normative)

Contract-Version: 2026-02-23

This is the canonical agent contract for this repository.
`CLAUDE.md` should point to this file and must not diverge.

## Quick Reference (Top 5 Rules)

1. **pt-BR**: All user-facing messages must be in Brazilian Portuguese.
2. **Boundaries**: Controller -> Use Case -> Repository (via interfaces).
3. **TDD**: Write/update tests BEFORE production code (`Red -> Green -> Refactor`).
4. **Tenant isolation**: Tenant-scoped entities require `OrganizationId` + query filter + interceptor.
5. **No warnings**: `TreatWarningsAsErrors=true` (zero warning build/test).

# Repository Guidelines

## Scope and Precedence

- This file is the normative runtime instruction set for coding agents.
- `README.md` is human-oriented documentation.
- `DEPLOY.md` is deployment-oriented documentation.
- ADRs under `docs/adr/` are the architecture decision history.

## Rule Priority (when in doubt)

1. Security, tenant isolation, and authorization.
2. User-facing language rules (`pt-BR`).
3. Architectural boundaries and design patterns.
4. Testing and validation requirements.
5. Style and organizational conventions.

## Agent Operating Contract (Normative)

### MUST

- Enforce tenant isolation, authentication, and authorization before business changes.
- Keep all user-facing text in `pt-BR` (errors, validation, API problem responses, UI text).
- Preserve architectural boundaries:
  - Controllers -> Use Cases.
  - Use Cases -> Repositories (interfaces in `Domain/Repositories`) and Ports (`Application/Ports`).
  - Domain MUST NOT reference `Bud.Server.Infrastructure` (or sub-namespaces).
  - Domain MUST NOT depend on `Infrastructure/`.
  - Application MUST depend only on abstractions (`Domain/Repositories`, `Application/Ports`).
  - Repositories MUST NOT return HTTP DTOs from `Bud.Shared.Contracts`.
  - Repositories MUST return domain entities/read models; mapping to `Bud.Shared.Contracts` happens in Application.
- Respect established patterns (specification, policy-based auth, composition root modules).
- Apply TDD workflow (`Red -> Green -> Refactor`) for features, fixes, and behavior changes.
- Update tests together with production code changes.
- Keep OpenAPI semantic documentation aligned with implementation.
- Update or create ADR when architectural behavior changes.
- Keep solution warning-free (`TreatWarningsAsErrors=true`).
- For `Bud.Server` logging, use source-generated logging (`[LoggerMessage]`) local to each component (`partial` class); do not introduce centralized ad-hoc log catalogs.
- For observability, register via `AddBudObservability()` in `BudObservabilityCompositionExtensions` (structured logging + OpenTelemetry). OTel config is **config-as-environment**: all export/resource/service-name settings come from standard OTel env vars, never hardcoded in code or appsettings.
- Reserve EventId ranges per domain: 3100–3199 RequestTelemetryMiddleware; 4000–4009 Mission; 4010–4019 Organization; 4020–4029 Workspace; 4030–4039 Team; 4040–4049 Collaborator; 4050–4059 Metric; 4060–4069 MetricCheckin; 4070–4079 Template; 4080–4089 Objective; 4090–4099 Session/Notification; 5000–5009 McpRequestLoggingMiddleware.
- For `Bud.Mcp`, keep tool schemas explicit (`required`, field types/formats/enums) and propagate API validation details (`errors` by field).
- For `Bud.Mcp`, keep domain tools (`mission_*`, `mission_metric_*`, `metric_checkin_*`) sourced from `Tools/Generated/mcp-tool-catalog.json` (strict mode, no runtime ad-hoc fallback).
- For `Bud.Mcp` HTTP transport, use `MCP-Session-Id` as the session header.
- For `Bud.Mcp` protocol compatibility, keep `prompts/list` implemented (empty list when no prompts are published).

### Documentation Update Rule (MUST)

When a feature is added, changed, or removed, review and update when applicable:

- `AGENTS.md`
- `README.md`
- `DEPLOY.md`
- ADRs under `docs/adr/`

Minimum expected behavior:

- Update affected docs in the same change.
- If no documentation update is required, explicitly justify this in the final task summary.

### SHOULD

- Prefer composition/extensions over ad-hoc wiring in `Program.cs`.
- Prefer reusable specifications/policies over duplicated conditionals.
- Prefer changing existing patterns consistently instead of introducing parallel alternatives.

## Agent Execution Flow (Recommended)

1. Identify domain and tenant/auth implications.
2. Confirm architectural path per boundaries above.
3. Write/update tests first (TDD).
4. Implement minimal coherent change following existing patterns.
5. Validate API contract, Language Requirements, and OpenAPI metadata.
6. Run tests and fix regressions.
7. Apply Documentation Update Rule.

## Agent Definition of Done (MUST)

Before finishing, verify:

- `Code`: implementation follows Controller -> Use Case -> Repository boundaries.
- `Security`: tenant isolation and authorization policies are enforced.
- `Language`: all user-facing messages are in `pt-BR`.
- `Tests`: required unit/integration tests were added/updated and executed.
- `API Contract`: HTTP mappings, `ProblemDetails`, and OpenAPI metadata are aligned.
- `Architecture Governance`: structural changes are reflected in docs/ADRs.
- `No drift`: no conflicting parallel pattern was introduced.

## Language Requirements (MUST)

All user-facing text MUST be in Brazilian Portuguese (`pt-BR`).

This includes:

- Error messages in controllers/repositories.
- FluentValidation messages.
- API `ProblemDetails` / `ValidationProblemDetails`.
- Blazor UI text.
- Log messages that may be displayed to users.

Comments in code should remain in English for maintainability.

## Architecture Essentials

## System Overview

Bud is an ASP.NET Core 10 solution with Blazor WebAssembly frontend and PostgreSQL backend.

Main projects:

- `src/Bud.Server`: API + static hosting for client.
- `src/Bud.Client`: Blazor WebAssembly UI.
- `src/Bud.Shared`: shared contracts.
- `src/Bud.Mcp`: MCP server over HTTP.
- `tests/*`: server, integration, client, and MCP tests.

## Layering and Dependencies (MUST)

- Controllers orchestrate validators + use cases.
- Use cases depend on abstractions only (`Domain/Repositories`, `Application/Ports`).
- Repository interfaces stay in Domain; implementations in Infrastructure.
- Domain stays infrastructure-agnostic.
- Mapping to shared HTTP contracts happens in Application.

## Multi-Tenancy (MUST)

- Row-level isolation by `OrganizationId`.
- Tenant entities implement `ITenantEntity` with `Guid OrganizationId`.
- Tenant entities must have query filters and tenant-aware persistence via `TenantSaveChangesInterceptor`.
- `TenantRequiredMiddleware` enforces tenant/auth rules for API endpoints, preserving configured exclusions.
- Frontend tenant rules:
  - Include `X-Tenant-Id` when a specific organization is selected.
  - Omit `X-Tenant-Id` for global-admin all-organizations context.
  - Tenant-scoped pages must react to organization changes.

## Authorization Pattern (MUST)

- Prefer `[Authorize(Policy = ...)]` in controllers.
- Reuse existing policies (`TenantSelected`, `GlobalAdmin`, `OrganizationOwner`, `OrganizationWrite`, `TenantOrganizationMatch`, `MissionScopeAccess`).
- New authorization rule -> new Requirement + Handler registered in `BudSecurityCompositionExtensions`.

## Controller Pattern (MUST)

Controllers must:

1. Validate payloads first.
2. Call use case only after validation success.
3. Map `Result.ErrorType` consistently:
   - `NotFound` -> 404
   - `Validation` -> 400
   - `Conflict` -> 409
   - `Forbidden` -> 403
4. Return `ProblemDetails` / `ValidationProblemDetails` in `pt-BR`.

## Validation and OpenAPI (MUST)

- Use FluentValidation in `src/Bud.Server/Validators/`, registered in DI.
- Validators must not access `ApplicationDbContext` directly.
- Keep OpenAPI available at `/swagger` and `/openapi/v1.json` in Development.
- Keep `ProducesResponseType`, `Consumes`, and `Produces` aligned with behavior.
- Document operation summary/description, success/error status codes, and key DTO fields.

## Data Access Essentials

- EF Core with Npgsql.
- DbContext: `ApplicationDbContext`.
- Every entity in `ApplicationDbContext` must have `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/`.
- Tenant query filters are configured at EF entity configuration level.

## Database Setup

- **Development:** schema created via `EnsureCreated()`. On model changes, recreate DB (e.g., `docker compose down -v && docker compose up --build`).
- **Production:** EF Core migrations are used (`dotnet-ef migrations bundle`) with migrations in `Infrastructure/Persistence/Migrations/`.

To add migration:

```bash
dotnet ef migrations add <Name> --project src/Bud.Server
```

## Testing Guidelines (Normative)

## TDD Workflow (MUST)

- Follow `Red -> Green -> Refactor`.
- New feature: write tests first.
- Bug fix: write failing test first.
- Behavior change: update tests first, then code.

## Coverage Expectations (MUST)

- Repositories: unit tests.
- Use cases: unit tests (including authorization branches when applicable).
- Validators: unit tests.
- API endpoints: integration tests.
- Business rules/invariants: automated tests.

## Test Strategy Essentials

- Unit tests: xUnit + Moq + FluentAssertions.
- Repository unit tests: EF InMemory + unique DB per test + tenant-aware setup.
- Integration tests: `WebApplicationFactory<Program>` + Testcontainers PostgreSQL.
- Include tenant scenarios: global admin, tenant-scoped requests, and no-tenant authenticated requests when relevant.

## MCP Tool Catalog Sync (MUST)

When contracts used by MCP tools change (`/api/missions`, `/api/objectives`, `/api/metrics`, `/api/metrics/{id}/checkins`), run:

```bash
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- generate-tool-catalog
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- check-tool-catalog --fail-on-diff
```

## Operational References

Operational command details are maintained in:

- `README.md`
- `DEPLOY.md`

## Key Files (Essential)

- `src/Bud.Server/Controllers/ApiControllerBase.cs`
- `src/Bud.Server/Controllers/OrganizationsController.cs`
- `src/Bud.Server/Application/Common/Result.cs`
- `src/Bud.Server/DependencyInjection/BudApplicationCompositionExtensions.cs`
- `src/Bud.Server/DependencyInjection/BudObservabilityCompositionExtensions.cs`
- `src/Bud.Server/DependencyInjection/BudSecurityCompositionExtensions.cs`
- `src/Bud.Server/Infrastructure/Persistence/ApplicationDbContext.cs`
- `src/Bud.Server/MultiTenancy/ITenantProvider.cs`
- `src/Bud.Server/MultiTenancy/TenantRequiredMiddleware.cs`
- `src/Bud.Client/Services/OrganizationContext.cs`
- `tests/Bud.Server.Tests/Architecture/ArchitectureTests.cs`
- `docs/adr/README.md`
