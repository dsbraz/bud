# Agent Note (non-normative)

Contract-Version: 2026-02-20

The canonical and normative rules start at `# Repository Guidelines` below.
This note is informational and does not add or override any requirement.

## Quick Reference (Top 5 Rules)

1. **pt-BR**: All user-facing messages in Brazilian Portuguese (see Language Requirements)
2. **Boundaries**: Controller -> UseCase -> Repository (via interfaces co-located in Infrastructure/) (never skip layers)
3. **TDD**: Write/update tests BEFORE production code (Red -> Green -> Refactor)
4. **Tenant isolation**: Every tenant-scoped entity needs `OrganizationId` + query filter + interceptor
5. **No warnings**: `TreatWarningsAsErrors=true` — zero build/test warnings allowed

# Repository Guidelines

This file provides guidance to coding agents when working with code in this repository.

## Scope and Precedence

- `CLAUDE.md` is the authoritative runtime instruction set for coding agents in this repository.
- `README.md` is human-oriented documentation and must not be treated as the source of mandatory agent behavior.
- Agents must rely on this file for implementation decisions in new features, bug fixes, and refactorings.

## Rule Priority (when in doubt)

1. Security, tenant isolation, and authorization rules
2. User-facing language rules (`pt-BR`)
3. Architectural boundaries and design patterns
4. Testing and validation requirements
5. Style and organizational conventions

## Agent Operating Contract (Normative)

### MUST

- Enforce tenant isolation, authentication, and authorization rules before implementing business changes.
- Keep all user-facing text in `pt-BR` (errors, validation messages, API problem responses, UI text).
- Preserve architectural boundaries:
  - Controllers -> UseCases
  - UseCases -> Repositories (via interfaces co-located in `Infrastructure/Repositories/` and `Infrastructure/Services/`)
  - Domain MUST NOT reference `Bud.Server.Infrastructure` (or any sub-namespace)
  - Repositories MUST NOT return HTTP DTO payloads from `Bud.Shared.Contracts`
  - Application (`UseCases`) MUST depend only on interfaces from `Infrastructure/`, not on concrete implementations
  - Domain MUST NOT depend on `Infrastructure/`
  - Repositories MUST return domain entities/read models; mapping to `Bud.Shared.Contracts` happens in `Application/`
- Respect the established design patterns in this file (specification, policy-based auth).
- Apply TDD workflow (`Red -> Green -> Refactor`) for features, fixes, and behavior changes.
- Update tests together with production code changes.
- Keep OpenAPI semantic documentation aligned with implementation.
- Update or create ADR when architectural behavior changes.
- For `Bud.Server` logging, use source-generated logging (`[LoggerMessage]`) local to each component (`partial` class); do not introduce centralized ad-hoc log catalogs.
- For `Bud.Mcp`, keep tool schemas explicit (`required`, field types/formats/enums) and propagate API validation details (`errors` by field) in tool errors.
- For `Bud.Mcp`, keep domain tools (`mission_*`, `mission_metric_*`, `metric_checkin_*`) sourced from `Tools/Generated/mcp-tool-catalog.json` (strict mode, no runtime fallback to ad-hoc schemas).
- For `Bud.Mcp` HTTP transport, use `MCP-Session-Id` as the session header.
- For `Bud.Mcp` protocol compatibility, keep `prompts/list` implemented (empty list when no prompts are published) to avoid client discovery regressions.
- Keep the solution warning-free (`TreatWarningsAsErrors=true`): code changes MUST not introduce build/test warnings.

### Documentation Update Rule (MUST)

When a feature is added, changed, or removed, agents MUST review and update, when applicable:

- `CLAUDE.md`
- `README.md`
- `DEPLOY.md`
- ADRs under `docs/adr/`

Minimum expected behavior:
- Update affected documents in the same change.
- If no documentation update is required, explicitly justify this in the final task summary.

### SHOULD

- Prefer composition/extensions over ad-hoc wiring in `Program.cs`.
- Prefer reusable specifications/policies over duplicated conditionals.
- Prefer changing existing patterns consistently instead of introducing parallel alternatives.
- Keep CLAUDE.md references up to date when structure or architectural contracts change.

## Agent Execution Flow (Recommended)

1. Identify affected domain and tenant/auth implications.
2. Confirm architectural path per MUST boundaries above.
3. Write/update tests first (TDD).
4. Implement minimal coherent change following existing patterns.
5. Validate API contract, Language Requirements, and OpenAPI metadata.
6. Run tests and fix regressions.
7. If architecture changed, apply Documentation Update Rule above.

## Agent Definition of Done (MUST)

Before finishing any task, agents MUST verify:

- `Code`: implementation follows Controller -> UseCase -> Repository boundaries.
- `Security`: tenant isolation and authorization policies are enforced for affected endpoints/use cases.
- `Language`: all user-facing messages are in `pt-BR`.
- `Tests`: required unit/integration tests were added/updated and executed.
- `API Contract`: HTTP mappings, `ProblemDetails`, and OpenAPI metadata are aligned with behavior.
- `Architecture Governance`: if structural decisions changed, ADR and CLAUDE.md references were updated.
- `No drift`: no conflicting parallel pattern was introduced when an established pattern already exists.

## Language Requirements

**IMPORTANT: All user-facing messages, error messages, validation messages, and any text displayed to end users MUST be in Brazilian Portuguese (pt-BR).**

This includes:
- Error messages in controllers and repositories
- Validation error messages in FluentValidation validators
- API response messages (ProblemDetails, error responses)
- UI text in Blazor components
- Log messages that may be displayed to users
- Comments in code should remain in English for maintainability

## Design Principles

All implementations must be production-grade: follow SOLID, Clean Architecture, and DDD where applicable. Prefer established design patterns over ad-hoc shortcuts. When multiple approaches exist, choose the one aligned with recognized reference architectures. See "Architectural & Design Patterns in Use" below for the specific patterns adopted in this project.

## Project Overview

Reference context (non-normative): helps understanding, but does not override the normative contract above.
Agents MAY skip this section during execution when the task does not require domain onboarding.

Bud is an ASP.NET Core 10 application with a Blazor WebAssembly frontend, using PostgreSQL as the database. The application manages organizational hierarchies and mission tracking.

## Project Structure

- **Bud.Server** (`src/Bud.Server`): ASP.NET Core API hosting both the API endpoints and the Blazor WebAssembly app
  - `Controllers/`: REST endpoints for auth, organizations, workspaces, teams, collaborators, missions, mission-objectives, mission-metrics, metric-checkins, mission-templates, objective-dimensions, dashboard, notifications
  - `Application/`: use cases organized by domain (Auth, Collaborators, Dashboard, MetricCheckins, MissionMetrics, MissionObjectives, MissionTemplates, Missions, Notifications, Organizations, Teams, Workspaces)
    - `Application/Common/`: `Result`, `ErrorType`, `PaginationNormalizer`
    - `Application/Notifications/`: `INotificationOrchestrator`, `NotificationOrchestrator`, `INotificationRecipientResolver`
  - `Authorization/`: policy-based authorization (requirements, handlers, `IApplicationAuthorizationGateway`, `TenantAuthorizationService`, `OrganizationAuthorizationService`)
  - `Domain/`: specifications (`Domain/Specifications/`), querying (`Domain/Querying/`), value objects, and domain-specific contracts
  - `Infrastructure/`: infrastructure layer
    - `Infrastructure/Persistence/`: `ApplicationDbContext`, `DbSeeder`, `Configurations/` (EF Core entity configurations), `Migrations/`
    - `Infrastructure/Repositories/`: repository interfaces (`I*Repository`) and implementations, co-located
    - `Infrastructure/Services/`: service interfaces (`IAuthService`, `IMissionScopeResolver`, `IMissionProgressService`) and implementations (`AuthService`, `MissionScopeResolver`, `MissionProgressService`, `NotificationRecipientResolver`), co-located
    - `Infrastructure/Serialization/`: JSON converters (`LenientEnumJsonConverterFactory`)
  - `DependencyInjection/`: modular composition (`Bud*CompositionExtensions`)
  - *(No migrations — database is created from model via `EnsureCreated()` during development)*
  - `Validators/`: FluentValidation validators
  - `Middleware/`: global exception handling, security headers (`SecurityHeadersMiddleware`), request telemetry, and other middleware
  - `MultiTenancy/`: tenant isolation infrastructure (`ITenantProvider`, `JwtTenantProvider`, `TenantSaveChangesInterceptor`, `TenantRequiredMiddleware`)
  - `Settings/`: configuration POCOs (`GlobalAdminSettings` — retained for seed and org protection only, `JwtSettings` — JWT config with fail-fast in non-Development, `RateLimitSettings` — login rate limiting config)

- **Bud.Client** (`src/Bud.Client`): Blazor WebAssembly SPA (compiled to static files served by Bud.Server)
  - `Pages/`: Blazor pages with routing
  - `Layout/`: Layout components (MainLayout, AuthLayout, NavMenu, ManagementMenu)
  - `Shared/`: Reusable Razor components (Modal, TransferList, PagedTableSection, CrudRowActions, Toast/ToastContainer, summary cards, form fields, ConfirmDeleteButton, NotificationDropdown, etc.)
  - `Services/`: ApiClient, AuthState, OrganizationContext, TenantDelegatingHandler, ToastService, UiOperationService, EnumParsingHelper, MissionMetricDisplayHelper, MissionProgressDisplayHelper

- **Bud.Shared** (`src/Bud.Shared`): Shared models, contracts, and DTOs used by both Client and Server
  - `Domain/`: Domain entities, value objects, `IAggregateRoot`, `ITenantEntity`, `DomainInvariantException`
  - `Contracts/`: Request/response DTOs

- **Bud.Mcp** (`src/Bud.Mcp`): MCP server over HTTP for agent integration
  - `Protocol/`: JSON-RPC/MCP over HTTP infrastructure (`IMcpRequestProcessor`, `McpRequestProcessor`, `McpJsonRpcDispatcher`)
  - `Tools/`: MCP tool definitions and execution (including `help_action_schema` and `session_bootstrap` for guided discovery)
  - `Tools/Generation/`: tool catalog generation from OpenAPI (`generate-tool-catalog` / `check-tool-catalog`)
  - `Auth/`: session/authentication and tenant context (dynamic login via `auth_login` tool; `BUD_USER_EMAIL` optional)
  - `Http/`: HTTP client for consuming `Bud.Server` endpoints
  - `Configuration/`: options POCOs (`BudMcpOptions`)

- **Tests**:
  - `tests/Bud.Server.Tests/`: Unit tests (xUnit, Moq, FluentAssertions)
    - `Application/<Domain>/`: use case tests (e.g., `Application/Missions/`)
    - `Infrastructure/Repositories/`: repository tests
    - `Infrastructure/Services/`: infrastructure service tests
    - `Infrastructure/Persistence/`: DbSeeder tests
    - `Authorization/`: authorization service tests
    - `Validators/`: validator tests
    - `Architecture/`: architecture governance tests
  - `tests/Bud.Server.IntegrationTests/`: Integration tests with WebApplicationFactory + Testcontainers
  - `tests/Bud.Client.Tests/`: Client-side unit tests (Services, Shared components)
  - `tests/Bud.Mcp.Tests/`: MCP server unit tests (protocol, tools, auth, generation)

- **Root**: `docker-compose.yml`, `README.md`, `DEPLOY.md`, `CLAUDE.md`

## Build and Development Commands

### Running with Docker (Recommended)

Recommended local flow uses Docker Compose.

```bash
# Start all services (API, UI, PostgreSQL)
docker compose up --build

# Stop all services
docker compose down
```

The application runs at `http://localhost:8080` with Swagger available at `http://localhost:8080/swagger` in Development mode.

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Bud.Mcp.Tests
dotnet test tests/Bud.Server.Tests
dotnet test tests/Bud.Server.IntegrationTests
dotnet test tests/Bud.Client.Tests

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### MCP Tool Catalog Sync

When endpoint contracts used by MCP tools change (`/api/missions`, `/api/mission-metrics`, `/api/metric-checkins`), agents MUST sync the catalog:

```bash
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- generate-tool-catalog
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- check-tool-catalog --fail-on-diff
```

Execution notes:
- In `docker compose`, the `mcp` service uses `BUD_API_BASE_URL=http://web:8080` by default.
- When running on the host, set `BUD_API_BASE_URL=http://localhost:8080` to avoid connection errors.
- `check-tool-catalog --fail-on-diff` also validates minimum `required` field contracts; fails if the catalog is missing required fields per tool.

### Database Setup (Development)

The database schema is created automatically from the EF Core model on startup via `EnsureCreated()`. No migrations are used during development — the schema is always derived from the current model in `ApplicationDbContext`.

When the model changes, drop and recreate the database (automatic on next startup if the database doesn't exist, or manually via `docker compose down -v && docker compose up --build`).

When the project approaches production, reintroduce migrations with:
```bash
dotnet ef migrations add Initial --project src/Bud.Server
```

## Architecture

Reference context (non-normative): use for system understanding; normative implementation rules are defined in the sections above and below.
Agents SHOULD consult this section only when the task touches domain modeling, tenant behavior, or cross-cutting architecture.

### Domain Model Hierarchy

The application follows a strict organizational hierarchy:

```
Organization
  └── Workspace(s)
      └── Team(s)
          ├── Collaborator(s)
          ├── SubTeam(s) (recursive)
          └── CollaboratorTeam (many-to-many join)

Mission (can be scoped to Organization, Workspace, Team, or Collaborator)
  ├── MissionObjective(s) (hierarchical via ParentObjectiveId)
  │   ├── ObjectiveDimension (optional classification via ObjectiveDimensionId)
  │   └── MissionMetric(s) (metrics linked to objectives)
  │       └── MetricCheckin(s)
  └── MissionMetric(s) (direct metrics, MissionObjectiveId = null)
      └── MetricCheckin(s)

MissionTemplate
  └── MissionTemplateMetric(s)

Notification (tenant-scoped, with NotificationType, RelatedEntityId, RelatedEntityType)
CollaboratorAccessLog (tenant-scoped, audit trail)
```

**Critical cascade behaviors:**
- Organization → Workspace = `Cascade`; Organization → Team and Organization → Collaborator = `Restrict` (Teams are reached indirectly via Workspace → Team = `Cascade`; Collaborators get `TeamId = null` via `SetNull`)
- SubTeams have `DeleteBehavior.Restrict` on ParentTeam to prevent orphaned hierarchies
- MissionObjective → SubObjectives = `Restrict` (must delete children first, same pattern as SubTeams)
- Mission → MissionObjective = `Cascade`; MissionObjective → MissionMetric = `Cascade`
- Mission → Organization/Workspace/Team/Collaborator = `Restrict` (repositories validate and return Conflict before deletion)
- MissionTemplate → Organization = `Restrict`; MissionTemplate → Metrics/Objectives = `Cascade`
- ObjectiveDimension uses `Restrict` on both MissionObjective and MissionTemplateObjective (repositories validate before deletion)

### Multi-Tenancy

The application uses **row-level tenant isolation** based on `OrganizationId`. Each organization (tenant) sees only its own data.

**Authentication:** The system uses JWT (JSON Web Tokens) for authentication without requiring passwords. Tokens are generated by `AuthService.LoginAsync` and validated by ASP.NET Core JWT Bearer middleware. Global admin status is determined by the `IsGlobalAdmin` flag on the `Collaborator` entity in the database (not by config/appsettings). The `DbSeeder` creates the initial admin and sets this flag. `GlobalAdminSettings` is retained only for seed (admin email) and org protection (org name).

**Components** (read source files for implementation details):

| Component | Role |
|-----------|------|
| `ITenantEntity` | Marker interface on all tenant-scoped entities; requires `Guid OrganizationId` |
| `ITenantProvider` / `JwtTenantProvider` | Resolves `TenantId`, `CollaboratorId`, `IsGlobalAdmin` from JWT claims + `X-Tenant-Id` header |
| `TenantAuthorizationService` | Validates user access to specific tenants |
| `OrganizationAuthorizationService` | Validates organization ownership and write permissions |
| EF Core Global Query Filters | Auto-filter all tenant entities; global admin bypasses; null tenant = no data |
| `TenantSaveChangesInterceptor` | Auto-sets `OrganizationId` on new entities if `Guid.Empty` |
| `TenantRequiredMiddleware` | Enforces auth + tenant for `/api/*` (401/403); excludes auth endpoints |
| `TenantDelegatingHandler` (client) | Attaches Bearer token + `X-Tenant-Id` header to client HTTP requests |

**Key design decisions:**

- `OrganizationId` is **denormalized** into all tenant entities for efficient query filtering without joins
- `Mission.OrganizationId` is **non-nullable**; scope level is determined by which of `WorkspaceId`/`TeamId`/`CollaboratorId` is set (none = org-scoped)
- Repositories must populate `OrganizationId` when creating entities (resolved from the parent entity in the hierarchy)

#### Multi-Tenancy Frontend (UI)

Frontend tenant context is implemented through `OrganizationContext` and applied by `TenantDelegatingHandler`.

Rules for agents:
- MUST ensure tenant-scoped pages react to organization changes (`OnOrganizationChanged`).
- MUST ensure requests include `X-Tenant-Id` when a specific organization is selected.
- MUST allow global-admin "all organizations" behavior by omitting `X-Tenant-Id` when selection is null.
- SHOULD keep tenant-selection behavior aligned with `MainLayout.razor` and `OrganizationContext.cs`.

### Application/UseCase Pattern

Controllers orchestrate requests through `UseCases` (`Command`/`Query`) and use `Result`/`Result<T>` from `Application/Common/Result.cs`:

```csharp
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType ErrorType { get; } // None, Validation, NotFound, Conflict, Forbidden
}
```

**Rules for agents:**
- MUST keep use case contracts in `Application/*` depending on interfaces from `Infrastructure/Repositories/` and `Infrastructure/Services/`.
- MUST keep controllers depending on `UseCases` (not directly on repositories).
- MUST keep `Application/*` free of direct dependencies on `Infrastructure/` concrete types (interfaces are allowed).
- MUST map `result.ErrorType` to HTTP status codes consistently:
  - `NotFound` → 404
  - `Validation` → 400
  - `Conflict` → 409
  - `Forbidden` → 403
- `Result.Error` messages follow Language Requirements (see above).

### Architectural & Design Patterns in Use

This project intentionally uses the patterns below. New changes should follow the same direction:

- **Repository interfaces and implementations (co-located):**
  - `Infrastructure/Repositories/` contains repository interfaces (`I*Repository`) and their implementations, co-located
  - `Infrastructure/Services/` contains service interfaces (`IAuthService`, `IMissionScopeResolver`, `IMissionProgressService`) and their implementations, co-located
  - UseCases depend on interfaces, not concrete implementations
  - Each domain exposes a single `I*Repository` interface with all command and query methods
- **Specification Pattern (query composition):**
  - Query specifications in `Domain/Specifications`
  - Prefer specifications for reusable filtering logic instead of duplicating LINQ predicates in multiple repositories
- **Aggregate Root boundaries (explicit markers):**
  - Domain roots are marked with `IAggregateRoot` in `Bud.Shared.Domain`
  - Child entities (internal to aggregates) MUST NOT implement `IAggregateRoot`
- **Domain invariants in aggregates:**
  - Critical business invariants SHOULD be enforced by aggregate/entity methods in `Bud.Shared.Domain`
  - Repositories SHOULD orchestrate persistence and map `DomainInvariantException` to application results
  - Prefer Value Objects for semantic concepts (e.g., `PersonName`, `MissionScope`, `ConfidenceLevel`, `MetricRange`) instead of primitive strings/guids
- **Policy-based Authorization + Handlers:**
  - Authorization rules modeled as requirements/handlers and policies
  - Avoid scattering permission `if` statements across repositories when a policy can express the rule
- **Composition Root modularization:**
  - Service registration is split into `Bud*CompositionExtensions` modules
  - New modules should be wired through composition extensions, not directly inside `Program.cs`
- **Base API behavior centralization:**
  - Common controller behavior should be centralized in `ApiControllerBase`
  - Avoid duplicating mapping/authorization helpers across controllers

### Controller Pattern

Controllers MUST follow this sequence:

1. Inject use case interfaces and FluentValidation validators via primary constructor.
2. Validate request payloads before calling use cases.
3. Call use case method(s) only after successful validation.
4. Map `Result` to HTTP status codes consistently.
5. Return `ProblemDetails`/`ValidationProblemDetails` (Language Requirements apply).

See [OrganizationsController.cs](src/Bud.Server/Controllers/OrganizationsController.cs) as the reference implementation.

### Authorization Pattern (recommended)

**Goal:** centralize authorization in policies and handlers, reducing scattered permission conditionals in services.

**Policies:**
- `TenantSelected` — requires authenticated user with a selected tenant (global admin always passes)
- `GlobalAdmin` — requires a global admin user
- `OrganizationOwner` — requires the collaborator to be organization owner
- `OrganizationWrite` — requires organization write permission
- `TenantOrganizationMatch` — validates that the target organization matches the current tenant context
- `MissionScopeAccess` — validates that the user has access to the mission's scope (workspace/team/collaborator)

**Rules for agents:**
- MUST apply `[Authorize(Policy = ...)]` in controllers instead of ad-hoc logic.
- MUST use `GlobalAdmin` for administrative actions (e.g., `POST/PUT/DELETE` on organizations).
- MUST use `TenantSelected` for tenant-scoped endpoints.
- SHOULD avoid direct checks of `IsGlobalAdmin` and `TenantId` in repositories when a policy already models the rule.
- MUST create a `Requirement` + `Handler` for new authorization rules and register them in `BudSecurityCompositionExtensions`.
- Error messages follow Language Requirements (see above).

### Validation

- MUST use **FluentValidation** for request validation.
- MUST place validators in `src/Bud.Server/Validators/`.
- MUST register validators in DI (see [BudApiCompositionExtensions.cs](src/Bud.Server/DependencyInjection/BudApiCompositionExtensions.cs)).
- MUST validate requests in controllers before calling use cases.
- MUST NOT access `ApplicationDbContext` directly from validators; validators must depend on abstractions/services for data-dependent checks.
- Validation messages follow Language Requirements (see above).

### API Documentation (OpenAPI)

- MUST expose OpenAPI/Swagger documentation in Development.
- MUST keep OpenAPI endpoints available at `/swagger` and `/openapi/v1.json`.
- MUST include semantic documentation (summary/description/responses) via XML comments and attributes.
- MUST keep `ProducesResponseType`, `Consumes`, and `Produces` aligned with controller behavior.
- MUST document key fields in `Bud.Shared/Contracts` with XML comments.
- Minimum semantic quality gate per endpoint (MUST):
  - operation summary/description
  - documented success and error status codes
  - payload examples for critical flows (create/update/reprocess)

### Data Access

- **Entity Framework Core** with PostgreSQL (Npgsql provider)
- DbContext: [ApplicationDbContext.cs](src/Bud.Server/Infrastructure/Persistence/ApplicationDbContext.cs)
- All entities are in `Bud.Shared/Domain`
- Entity configurations (`IEntityTypeConfiguration<T>`) are in `Infrastructure/Persistence/Configurations/`, loaded via `ApplyConfigurationsFromAssembly`
- **Global Query Filters** (multi-tenancy) are applied via configurations
- The DbContext accepts an optional `ITenantProvider` for tenant-aware queries (nullable for schema creation and tests)

### Client Architecture

- Blazor WebAssembly with pages in `src/Bud.Client/Pages/`
- Reusable components in `src/Bud.Client/Shared/` (Modal, TransferList, PagedTableSection, CrudRowActions, Toast, summary cards, form fields, etc.)
- API communication through [ApiClient.cs](src/Bud.Client/Services/ApiClient.cs)
- Auth state managed by [AuthState.cs](src/Bud.Client/Services/AuthState.cs)
- Tenant context managed by [OrganizationContext.cs](src/Bud.Client/Services/OrganizationContext.cs)
- UI helpers: `UiOperationService` (operation wrapping and error handling), `ToastService` (notifications)
- Display helpers: `MissionMetricDisplayHelper`, `MissionProgressDisplayHelper`, `EnumParsingHelper`
- Tenant selection UI in sidebar ([MainLayout.razor](src/Bud.Client/Layout/MainLayout.razor))
- Layouts in `src/Bud.Client/Layout/` (MainLayout, AuthLayout)

## Testing Guidelines

### Test-Driven Development (TDD) - MANDATORY

**This project follows TDD as the standard development approach.**

**CRITICAL RULES:**

1. **Write tests BEFORE implementing or changing code** - This is non-negotiable
2. **Every code change requires test adjustments** - Either modify existing tests or create new ones
3. **No code changes without corresponding tests** - Production code and test code must evolve together

**TDD Workflow:**

```
1. Write/Update Test (Red) → 2. Implement/Change Code (Green) → 3. Refactor (if needed)
```

**When making changes:**
- **New feature?** Write new tests first, then implement
- **Bug fix?** Write a failing test that reproduces the bug, then fix it
- **Refactoring?** Ensure existing tests pass, add tests for edge cases if needed
- **Changing behavior?** Update tests to reflect new expected behavior, then change code

**Test coverage expectations (MUST):**
- All repositories must have unit tests.
- All use cases must have unit tests (command/query and authorization branches).
- All validators must have unit tests.
- All API endpoints must have integration tests.
- All business logic must be tested.

### Unit Tests (`tests/Bud.Server.Tests`)

- Use **xUnit**, **Moq**, and **FluentAssertions**
- Test validators, repositories, and business logic in isolation
- **Database Strategy:**
  - **Validator tests**: No database needed, test FluentValidation logic directly
  - **Repository tests**: Use `ApplicationDbContext` with **InMemoryDatabase provider** (EF Core)
    - Create context via `DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase()`
    - Each test uses a unique database name (e.g., `Guid.NewGuid().ToString()`)
    - Pass a `TestTenantProvider` (with `IsGlobalAdmin = true`) to the DbContext to bypass query filters
    - Always set `OrganizationId` on tenant entities in test data
- Every feature must include unit tests
- Unit tests must not access external resources (except InMemoryDatabase for repository tests)
- Example validator test: [CreateOrganizationValidatorTests.cs](tests/Bud.Server.Tests/Validators/CreateOrganizationValidatorTests.cs)
- Example repository test: [TeamRepositoryTests.cs](tests/Bud.Server.Tests/Infrastructure/Repositories/TeamRepositoryTests.cs)
- Test tenant helper: [TestTenantProvider.cs](tests/Bud.Server.Tests/Helpers/TestTenantProvider.cs)

### Integration Tests (`tests/Bud.Server.IntegrationTests`)

- Use `WebApplicationFactory<Program>` to spin up the full API
- Test full request/response cycles (HTTP endpoints)
- **Database Strategy:**
  - Use **Testcontainers.PostgreSql** to spin up real PostgreSQL container
  - [CustomWebApplicationFactory.cs](tests/Bud.Server.IntegrationTests/CustomWebApplicationFactory.cs) configures the test container
  - PostgreSQL 16 image with automatic schema creation on startup
  - Container lifecycle managed by xUnit's `IAsyncLifetime`
- **Multi-tenancy in integration tests:**
  - `factory.CreateGlobalAdminClient()` — `HttpClient` with global admin JWT, bypasses `TenantRequiredMiddleware`
  - `factory.CreateTenantClient(tenantId, email, collaboratorId)` — `HttpClient` with `X-Tenant-Id` header for tenant-scoped tests
  - `factory.CreateUserClientWithoutTenant(email)` — authenticated `HttpClient` without tenant context
  - When creating entities directly via DbContext, always set `OrganizationId` on `Team` and `Collaborator`
  - Use `IgnoreQueryFilters()` when looking up bootstrap data to avoid tenant filters hiding existing records
- Example: [OrganizationsEndpointsTests.cs](tests/Bud.Server.IntegrationTests/Endpoints/OrganizationsEndpointsTests.cs)

## Code Style & Naming Conventions

Enforced by `.editorconfig` and `Directory.Build.props`:

- **Indentation:** 4 spaces for C#/Razor, 2 spaces for XML/JSON
- **Line endings:** LF
- **Nullable reference types** enabled
- **Implicit usings** enabled
- Use **primary constructors** for controllers, use cases, and repositories (C# 12 feature)
- Use **file-scoped namespaces** where possible
- Follow Microsoft C# naming conventions (PascalCase for public members, camelCase for locals)
- Apply Clean Code principles whenever possible
- Linting: default .NET analyzers

## Important Patterns to Follow

### Adding a New Entity

Follow this sequence as a MUST checklist:

1. Create the model in `src/Bud.Shared/Domain/`
   - If the entity belongs to an organization, implement `ITenantEntity` and add `Guid OrganizationId` + `Organization` nav prop
2. Add `DbSet<TEntity>` to `ApplicationDbContext`
3. Create `IEntityTypeConfiguration<TEntity>` in `src/Bud.Server/Infrastructure/Persistence/Configurations/`
   - If tenant-scoped: add FK/index for `OrganizationId` (`DeleteBehavior.Restrict`) and add a `HasQueryFilter` following the existing pattern
4. Create request/response contracts in `src/Bud.Shared/Contracts/`
5. Create FluentValidation validators in `src/Bud.Server/Validators/`
6. Create repository interface and implementation in `src/Bud.Server/Infrastructure/Repositories/`
   - If tenant-scoped: resolve and set `OrganizationId` from the parent entity in `CreateAsync`
7. Create/adjust use case contracts in `src/Bud.Server/Application/{Domain}/`
8. Register implementations and use cases in [BudApplicationCompositionExtensions.cs](src/Bud.Server/DependencyInjection/BudApplicationCompositionExtensions.cs)
9. Create controller in `src/Bud.Server/Controllers/`
10. Write unit tests in `tests/Bud.Server.Tests/`
11. Write integration tests in `tests/Bud.Server.IntegrationTests/`

### Adding a New Blazor Page

1. Create `.razor` file in `src/Bud.Client/Pages/`
2. Add route with `@page "/route"`
3. Add navigation link in [NavMenu.razor](src/Bud.Client/Layout/NavMenu.razor) or [ManagementMenu.razor](src/Bud.Client/Layout/ManagementMenu.razor)
4. Use `ApiClient` service for API calls
5. Handle loading states and errors in the UI

## Operational Reference

Quick reference:
- Health: `/health/live`, `/health/ready`
- Config: `ConnectionStrings:DefaultConnection`
- Docker: PostgreSQL :5432, API :8080, MCP :8081
- Configuration files: `appsettings.json` (common defaults), `appsettings.Development.json` (dev/Docker), `appsettings.Production.json` (GCP/production — no secrets)
- Secrets (connection string, JWT key) come from env vars (Docker Compose) or Secret Manager (GCP)
- Security: `SecurityHeadersMiddleware` (CSP, X-Frame-Options, etc.), rate limiting policy `"auth-login"` on login endpoint, JWT fail-fast in non-Development
- Rate limiting: configurable via `RateLimitSettings` section (default: 10 req/60s on login)

## Key Files to Reference

### Architecture & Patterns
- **Controller:** [OrganizationsController.cs](src/Bud.Server/Controllers/OrganizationsController.cs), [ApiControllerBase.cs](src/Bud.Server/Controllers/ApiControllerBase.cs)
- **Repository:** [OrganizationRepository.cs](src/Bud.Server/Infrastructure/Repositories/OrganizationRepository.cs)
- **Repository Interfaces:** [IOrganizationRepository.cs](src/Bud.Server/Infrastructure/Repositories/IOrganizationRepository.cs), [IMissionRepository.cs](src/Bud.Server/Infrastructure/Repositories/IMissionRepository.cs)
- **Use cases:** [MissionCommandUseCase.cs](src/Bud.Server/Application/Missions/MissionCommandUseCase.cs), [MissionQueryUseCase.cs](src/Bud.Server/Application/Missions/MissionQueryUseCase.cs)
- **Application Common:** [Result.cs](src/Bud.Server/Application/Common/Result.cs), [PaginationNormalizer.cs](src/Bud.Server/Application/Common/PaginationNormalizer.cs)
- **Specifications:** [IQuerySpecification.cs](src/Bud.Server/Domain/Specifications/IQuerySpecification.cs), [MissionSearchSpecification.cs](src/Bud.Server/Domain/Specifications/MissionSearchSpecification.cs), [MissionScopeSpecification.cs](src/Bud.Server/Domain/Specifications/MissionScopeSpecification.cs)
- **Querying:** [QuerySearchHelper.cs](src/Bud.Server/Domain/Querying/QuerySearchHelper.cs)
- **Validators:** [OrganizationValidators.cs](src/Bud.Server/Validators/OrganizationValidators.cs)
- **Error handling:** [GlobalExceptionHandler.cs](src/Bud.Server/Middleware/GlobalExceptionHandler.cs)
- **Security middleware:** [SecurityHeadersMiddleware.cs](src/Bud.Server/Middleware/SecurityHeadersMiddleware.cs)

### Authorization & Security
- **Gateway:** [IApplicationAuthorizationGateway.cs](src/Bud.Server/Authorization/IApplicationAuthorizationGateway.cs), [ApplicationAuthorizationGateway.cs](src/Bud.Server/Authorization/ApplicationAuthorizationGateway.cs)
- **Policies:** [AuthorizationPolicies.cs](src/Bud.Server/Authorization/AuthorizationPolicies.cs)
- **Composition:** [BudSecurityCompositionExtensions.cs](src/Bud.Server/DependencyInjection/BudSecurityCompositionExtensions.cs)
- **Auth:** [IAuthService.cs](src/Bud.Server/Infrastructure/Services/IAuthService.cs), [AuthService.cs](src/Bud.Server/Infrastructure/Services/AuthService.cs)
- **DbSeeder:** [DbSeeder.cs](src/Bud.Server/Infrastructure/Persistence/DbSeeder.cs)
- **Tenant backend:** [ITenantProvider.cs](src/Bud.Server/MultiTenancy/ITenantProvider.cs), [JwtTenantProvider.cs](src/Bud.Server/MultiTenancy/JwtTenantProvider.cs), [TenantRequiredMiddleware.cs](src/Bud.Server/MultiTenancy/TenantRequiredMiddleware.cs)
- **Tenant entity:** [ITenantEntity.cs](src/Bud.Shared/Domain/ITenantEntity.cs)
- **Tenant authorization:** [OrganizationAuthorizationService.cs](src/Bud.Server/Authorization/OrganizationAuthorizationService.cs), [TenantAuthorizationService.cs](src/Bud.Server/Authorization/TenantAuthorizationService.cs)

### Infrastructure Services
- **Mission:** [MissionProgressService.cs](src/Bud.Server/Infrastructure/Services/MissionProgressService.cs), [MissionScopeResolver.cs](src/Bud.Server/Infrastructure/Services/MissionScopeResolver.cs)
- **Value Objects:** [EmailAddress.cs](src/Bud.Server/Domain/ValueObjects/EmailAddress.cs), [EntityName.cs](src/Bud.Shared/Domain/EntityName.cs), [PersonName.cs](src/Bud.Shared/Domain/PersonName.cs), [MissionScope.cs](src/Bud.Shared/Domain/MissionScope.cs), [ConfidenceLevel.cs](src/Bud.Shared/Domain/ConfidenceLevel.cs), [MetricRange.cs](src/Bud.Shared/Domain/MetricRange.cs), [NotificationTitle.cs](src/Bud.Shared/Domain/NotificationTitle.cs), [NotificationMessage.cs](src/Bud.Shared/Domain/NotificationMessage.cs)
- **Notifications:** [NotificationsController.cs](src/Bud.Server/Controllers/NotificationsController.cs), [INotificationOrchestrator.cs](src/Bud.Server/Application/Notifications/INotificationOrchestrator.cs), [NotificationRecipientResolver.cs](src/Bud.Server/Infrastructure/Services/NotificationRecipientResolver.cs)

### Client
- **API:** [ApiClient.cs](src/Bud.Client/Services/ApiClient.cs)
- **Tenant:** [OrganizationContext.cs](src/Bud.Client/Services/OrganizationContext.cs), [MainLayout.razor](src/Bud.Client/Layout/MainLayout.razor), [TenantDelegatingHandler.cs](src/Bud.Client/Services/TenantDelegatingHandler.cs)

### MCP
- **Entry:** [Program.cs](src/Bud.Mcp/Program.cs)
- **Protocol:** [IMcpRequestProcessor.cs](src/Bud.Mcp/Protocol/IMcpRequestProcessor.cs), [McpRequestProcessor.cs](src/Bud.Mcp/Protocol/McpRequestProcessor.cs)
- **Tools:** [McpToolService.cs](src/Bud.Mcp/Tools/McpToolService.cs)

### Governance
- **Architecture tests:** [ArchitectureTests.cs](tests/Bud.Server.Tests/Architecture/ArchitectureTests.cs)
  - Every entity exposed by `ApplicationDbContext` MUST have a dedicated `IEntityTypeConfiguration<T>` in `src/Bud.Server/Infrastructure/Persistence/Configurations/`
  - Tenant isolation, authorization, and aggregate root boundary tests
- **ADR index:** [docs/adr/README.md](docs/adr/README.md)
