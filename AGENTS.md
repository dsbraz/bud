# Repository Guidelines

This file provides guidance to coding agents when working with code in this repository.

## Scope and Precedence

- `AGENTS.md` is the authoritative runtime instruction set for coding agents in this repository.
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
  - UseCases -> `Application/Abstractions`
  - Services implement abstractions
- Respect the established design patterns in this file (pipeline, domain events, outbox, specification, policy-based auth).
- Apply TDD workflow (`Red -> Green -> Refactor`) for features, fixes, and behavior changes.
- Update tests together with production code changes.
- Keep OpenAPI semantic documentation aligned with implementation.
- Update or create ADR when architectural behavior changes.
- For `Bud.Mcp`, keep tool schemas explicit (`required`, field types/formats/enums) and propagate API validation details (`errors` by field) in tool errors.

### SHOULD

- Prefer composition/extensions over ad-hoc wiring in `Program.cs`.
- Prefer reusable specifications/policies/pipeline behaviors over duplicated conditionals.
- Prefer changing existing patterns consistently instead of introducing parallel alternatives.
- Keep AGENTS references up to date when structure or architectural contracts change.

## Agent Execution Flow (Recommended)

1. Identify affected domain and tenant/auth implications.
2. Confirm architectural path (Controller -> UseCase -> Abstractions -> Service).
3. Write/update tests first.
4. Implement minimal coherent change following existing patterns.
5. Validate API contract, error messages (`pt-BR`), and OpenAPI metadata.
6. Run tests and fix regressions.
7. If architecture changed, update ADR and AGENTS references.

## Agent Definition of Done (MUST)

Before finishing any task, agents MUST verify:

- `Code`: implementation follows Controller -> UseCase -> Abstractions -> Service boundaries.
- `Security`: tenant isolation and authorization policies are enforced for affected endpoints/use cases.
- `Language`: all user-facing messages are in `pt-BR`.
- `Tests`: required unit/integration tests were added/updated and executed.
- `API Contract`: HTTP mappings, `ProblemDetails`, and OpenAPI metadata are aligned with behavior.
- `Architecture Governance`: if structural decisions changed, ADR and AGENTS references were updated.
- `No drift`: no conflicting parallel pattern was introduced when an established pattern already exists.

## Language Requirements

**IMPORTANT: All user-facing messages, error messages, validation messages, and any text displayed to end users MUST be in Brazilian Portuguese (pt-BR).**

This includes:
- Error messages in controllers and services
- Validation error messages in FluentValidation validators
- API response messages (ProblemDetails, error responses)
- UI text in Blazor components
- Log messages that may be displayed to users
- Comments in code should remain in English for maintainability

## Design Principles

**IMPORTANT: All proposed solutions must follow industry best practices and reference architectures, not just the simplest or quickest approach.** This means:

- Prefer well-established design patterns and architectural standards over ad-hoc or shortcut implementations
- Follow SOLID principles, Clean Architecture, and Domain-Driven Design where applicable
- Prioritize maintainability, scalability, and correctness over development speed
- When multiple approaches exist, choose the one aligned with recognized reference architectures and community best practices
- Avoid "quick and dirty" solutions — every implementation should be production-grade and sustainable long-term

## Project Overview

Reference context (non-normative): helps understanding, but does not override the normative contract above.
Agents MAY skip this section during execution when the task does not require domain onboarding.

Bud is an ASP.NET Core 10 application with a Blazor WebAssembly frontend, using PostgreSQL as the database. The application manages organizational hierarchies and mission tracking.

## Project Structure

- **Bud.Server** (`src/Bud.Server`): ASP.NET Core API hosting both the API endpoints and the Blazor WebAssembly app
  - `Controllers/`: REST endpoints for organizations, workspaces, teams, collaborators, missions, outbox
  - `Application/`: use cases (`Command/Query`), abstractions (ports), pipeline behaviors, events
  - `Domain/`: domain events and domain-specific contracts
  - `Infrastructure/`: outbox processing, serialization, and background workers
  - `Data/`: `ApplicationDbContext`, EF Core configuration, and `DbSeeder`
  - `DependencyInjection/`: modular composition (`Bud*CompositionExtensions`)
  - `Migrations/`: database migrations
  - `Services/`: domain/application service implementations and supporting helpers
  - `Validators/`: FluentValidation validators
  - `Middleware/`: global exception handling and other middleware
  - `MultiTenancy/`: tenant isolation infrastructure (`ITenantProvider`, `JwtTenantProvider`, `TenantSaveChangesInterceptor`, `TenantRequiredMiddleware`)
  - `Settings/`: configuration POCOs (`GlobalAdminSettings`)

- **Bud.Client** (`src/Bud.Client`): Blazor WebAssembly SPA (compiled to static files served by Bud.Server)
  - `Pages/`: Blazor pages with routing
  - `Layout/`: Layout components (MainLayout, AuthLayout, NavMenu, ManagementMenu)
  - `Services/`: ApiClient, AuthState, and `TenantDelegatingHandler` (attaches tenant headers to HTTP requests)

- **Bud.Shared** (`src/Bud.Shared`): Shared models, contracts, and DTOs used by both Client and Server
  - `Models/`: Domain entities
  - `Contracts/`: Request/response DTOs

- **Bud.Mcp** (`src/Bud.Mcp`): MCP server (`stdio`) para integração com agentes
  - `Protocol/`: infraestrutura JSON-RPC/MCP over stdio
  - `Tools/`: definição e execução de ferramentas MCP (incluindo `help_action_schema` e `session_bootstrap` para descoberta orientada)
  - `Auth/`: sessão/autenticação e contexto de tenant (com login dinâmico por tool `auth_login`; `BUD_USER_EMAIL` opcional)
  - `Http/`: cliente para consumo dos endpoints do `Bud.Server`

- **Tests**:
  - `tests/Bud.Server.Tests/`: Unit tests (xUnit, Moq, FluentAssertions)
  - `tests/Bud.Server.IntegrationTests/`: Integration tests with WebApplicationFactory
  - `tests/Bud.Mcp.Tests/`: Unit tests do servidor MCP

- **Root**: `docker-compose.yml`, `README.md`, `AGENTS.md`

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
dotnet test tests/Bud.Server.Tests
dotnet test tests/Bud.Server.IntegrationTests

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### Migrations

Migrations are automatically applied on startup in Development mode.
Manual migration command (Docker network) when required:

```bash
docker run --rm -v "$(pwd)/src":/src -w /src/Bud.Server --network bud_default \
  -e ConnectionStrings__DefaultConnection="Host=db;Port=5432;Database=bud;Username=postgres;Password=postgres" \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -lc "dotnet tool install --tool-path /tmp/tools dotnet-ef --version 10.0.2 && /tmp/tools/dotnet-ef database update"
```

To create a new migration:
```bash
dotnet ef migrations add MigrationName --project src/Bud.Server
```

### Local Development without Docker

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the server (requires PostgreSQL connection string in appsettings)
dotnet run --project src/Bud.Server
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
          └── SubTeam(s) (recursive)

Mission (can be scoped to Organization, Workspace, Team, or Collaborator)
  └── MissionMetric(s)
```

**Critical cascade behaviors:**
- Organization deletion cascades to Workspaces, Teams, and Collaborators
- SubTeams have `DeleteBehavior.Restrict` on ParentTeam to prevent orphaned hierarchies
- All Mission relationships cascade delete

### Multi-Tenancy

The application uses **row-level tenant isolation** based on `OrganizationId`. Each organization (tenant) sees only its own data.

**Authentication:** The system uses JWT (JSON Web Tokens) for authentication without requiring passwords. Tokens are generated by `AuthService.LoginAsync` and validated by ASP.NET Core JWT Bearer middleware.

**How it works:**

1. **`ITenantEntity`** — marker interface implemented by all tenant-scoped entities (`Workspace`, `Team`, `Collaborator`, `Mission`, `MissionMetric`). Requires a `Guid OrganizationId` property.

2. **`ITenantProvider` / `JwtTenantProvider`** — scoped service that reads user information from validated JWT claims. Determines `TenantId` (from `X-Tenant-Id` header or `organization_id` claim), `CollaboratorId`, and `IsGlobalAdmin` (from `GlobalAdmin` role claim). Optionally accepts `X-Tenant-Id` header for multi-organization users.

3. **Authorization Services** — centralized authorization logic:
   - `TenantAuthorizationService` validates user access to specific tenants
   - `OrganizationAuthorizationService` validates organization ownership and write permissions

4. **EF Core Global Query Filters** — configured in `ApplicationDbContext.OnModelCreating()` on all tenant entities.
   - Filters are applied only when an `ITenantProvider` is available (normal runtime).
   - Global admin bypasses all filters.
   - If `TenantId` is `null` for a non-admin user, the filters return no data.

5. **`TenantSaveChangesInterceptor`** — EF Core `SaveChangesInterceptor` that auto-sets `OrganizationId` on new `ITenantEntity` entities if it's `Guid.Empty`.

6. **`TenantRequiredMiddleware`** — validates JWT authentication and tenant access for `/api/*` requests (except `/api/auth/login`, `/api/auth/logout`, `/api/auth/my-organizations`).
   - Returns 401 for unauthenticated requests.
   - Returns 403 if the user is authenticated but **does not have a tenant selected** and is not a global admin.
   - Returns 403 for unauthorized tenant access.

7. **`TenantDelegatingHandler`** (client) — `DelegatingHandler` that reads `AuthState` and attaches `Authorization: Bearer <token>` header and optional `X-Tenant-Id` header to every HTTP request from the Blazor client.

**Key design decisions:**

- `OrganizationId` is **denormalized** into `Team`, `Collaborator`, `Mission`, and `MissionMetric` for efficient query filtering without joins
- `Mission.OrganizationId` is **non-nullable** (always set as tenant discriminator). Mission scope level is determined by which of `WorkspaceId`/`TeamId`/`CollaboratorId` is set; if none are set, the mission is org-scoped
- Services must populate `OrganizationId` when creating entities (resolved from the parent entity in the hierarchy)

#### Multi-Tenancy Frontend (UI)

Frontend tenant context is implemented through `OrganizationContext` and applied by `TenantDelegatingHandler`.

Rules for agents:
- MUST ensure tenant-scoped pages react to organization changes (`OnOrganizationChanged`).
- MUST ensure requests include `X-Tenant-Id` when a specific organization is selected.
- MUST allow global-admin "all organizations" behavior by omitting `X-Tenant-Id` when selection is null.
- SHOULD keep tenant-selection behavior aligned with `MainLayout.razor` and `OrganizationContext.cs`.

### Application/UseCase Pattern

Controllers orchestrate requests through `UseCases` (`Command`/`Query`) and use `ServiceResult`/`ServiceResult<T>` from `Application.Common.Results`:

```csharp
public sealed class ServiceResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ServiceErrorType ErrorType { get; } // None, Validation, NotFound, Conflict
}
```

**Rules for agents:**
- MUST keep use case contracts in `Application/*` depending on ports from `Application/Abstractions`.
- MUST keep controllers depending on `UseCases` (not directly on `Services`).
- MUST map `result.ErrorType` to HTTP status codes consistently:
  - `NotFound` → 404
  - `Validation` → 400
  - `Conflict` → 409
- MUST keep all `ServiceResult.Error` messages in `pt-BR`.

### Architectural & Design Patterns in Use

This project intentionally uses the patterns below. New changes should follow the same direction:

- **Ports and Adapters (Hexagonal tendency):**
  - `Application/Abstractions` defines ports (`I*Service`, gateways, lookups)
  - `Services/*` provide concrete implementations
  - UseCases depend on abstractions, not concrete infrastructure details
- **UseCase Pipeline (Behavior Chain):**
  - Cross-cutting behavior is applied through `IUseCasePipeline` + `IUseCaseBehavior`
  - Current example: `LoggingUseCaseBehavior`
  - New cross-cutting concerns (metrics, tracing, idempotency) should prefer pipeline behaviors over duplicating logic in each use case
- **Domain Events + Subscriber pattern:**
  - Domain events in `Domain/*/Events`
  - Subscribers via `IDomainEventSubscriber<TEvent>` in `Application/*/Events`
  - Dispatching is centralized in `IDomainEventDispatcher`
- **Outbox Pattern (reliable async processing):**
  - Domain events are serialized/persisted in `OutboxMessages`
  - Background processing with retry/backoff and dead-letter
  - Reprocessing through admin endpoints/use cases
- **Specification Pattern (query composition):**
  - Query specifications in `Domain/Common/Specifications`
  - Prefer specifications for reusable filtering logic instead of duplicating LINQ predicates in multiple services
- **Policy-based Authorization + Handlers:**
  - Authorization rules modeled as requirements/handlers and policies
  - Avoid scattering permission `if` statements across services when a policy can express the rule
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
4. Map `ServiceResult` to HTTP status codes consistently.
5. Return `ProblemDetails`/`ValidationProblemDetails` with messages in `pt-BR`.

See [OrganizationsController.cs](src/Bud.Server/Controllers/OrganizationsController.cs) as the reference implementation.

### Authorization Pattern (recommended)

**Goal:** centralize authorization in policies and handlers, reducing scattered permission conditionals in services.

**Policies:**
- `TenantSelected` — requires authenticated user with a selected tenant (global admin always passes)
- `GlobalAdmin` — requires a global admin user
- `OrganizationOwner` — requires the collaborator to be organization owner
- `OrganizationWrite` — requires organization write permission

**Rules for agents:**
- MUST apply `[Authorize(Policy = ...)]` in controllers instead of ad-hoc logic.
- MUST use `GlobalAdmin` for administrative actions (e.g., `POST/PUT/DELETE` on organizations).
- MUST use `TenantSelected` for tenant-scoped endpoints.
- SHOULD avoid direct checks of `IsGlobalAdmin` and `TenantId` in services when a policy already models the rule.
- MUST keep error messages in `pt-BR`.
- MUST create a `Requirement` + `Handler` for new authorization rules and register them in `BudSecurityCompositionExtensions`.

### Validation

- MUST use **FluentValidation** for request validation.
- MUST place validators in `src/Bud.Server/Validators/`.
- MUST register validators in DI (see [BudApplicationCompositionExtensions.cs](src/Bud.Server/DependencyInjection/BudApplicationCompositionExtensions.cs)).
- MUST validate requests in controllers before calling use cases.
- MUST keep all validation messages in `pt-BR`.

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
- DbContext: [ApplicationDbContext.cs](src/Bud.Server/Data/ApplicationDbContext.cs)
- All entities are in `Bud.Shared.Models`
- All relationship configurations and **Global Query Filters** (multi-tenancy) are in `ApplicationDbContext.OnModelCreating()`
- The DbContext accepts an optional `ITenantProvider` for tenant-aware queries (nullable for migrations and tests)

### Client Architecture

- Blazor WebAssembly with pages in `src/Bud.Client/Pages/`
- API communication through [ApiClient.cs](src/Bud.Client/Services/ApiClient.cs)
- Auth state managed by [AuthState.cs](src/Bud.Client/Services/AuthState.cs)
- Tenant context managed by [OrganizationContext.cs](src/Bud.Client/Services/OrganizationContext.cs)
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
- All services must have unit tests.
- All use cases must have unit tests (command/query and authorization branches).
- Use case pipeline behaviors (e.g., logging/cross-cutting) must have unit tests.
- Domain event subscribers must have unit tests when they contain behavior beyond trivial logging.
- All validators must have unit tests.
- All API endpoints must have integration tests.
- All business logic must be tested.

### Unit Tests (`tests/Bud.Server.Tests`)

- Use **xUnit**, **Moq**, and **FluentAssertions**
- Test validators, services, and business logic in isolation
- **Database Strategy:**
  - **Validator tests**: No database needed, test FluentValidation logic directly
  - **Service tests**: Use `ApplicationDbContext` with **InMemoryDatabase provider** (EF Core)
    - Create context via `DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase()`
    - Each test uses a unique database name (e.g., `Guid.NewGuid().ToString()`)
    - Pass a `TestTenantProvider` (with `IsGlobalAdmin = true`) to the DbContext to bypass query filters
    - Always set `OrganizationId` on tenant entities in test data
- Every feature must include unit tests
- Unit tests must not access external resources (except InMemoryDatabase for service tests)
- Example validator test: [CreateOrganizationValidatorTests.cs](tests/Bud.Server.Tests/Validators/CreateOrganizationValidatorTests.cs)
- Example service test: [TeamServiceTests.cs](tests/Bud.Server.Tests/Services/TeamServiceTests.cs)
- Test tenant helper: [TestTenantProvider.cs](tests/Bud.Server.Tests/Helpers/TestTenantProvider.cs)

### Integration Tests (`tests/Bud.Server.IntegrationTests`)

- Use `WebApplicationFactory<Program>` to spin up the full API
- Test full request/response cycles (HTTP endpoints)
- **Database Strategy:**
  - Use **Testcontainers.PostgreSql** to spin up real PostgreSQL container
  - [CustomWebApplicationFactory.cs](tests/Bud.Server.IntegrationTests/CustomWebApplicationFactory.cs) configures the test container
  - PostgreSQL 16 image with automatic migrations on startup
  - Container lifecycle managed by xUnit's `IAsyncLifetime`
- **Multi-tenancy in integration tests:**
  - Use `factory.CreateAdminClient()` to create an `HttpClient` with admin headers (`X-User-Email: admin@getbud.co`), which bypasses `TenantRequiredMiddleware`
  - When creating entities directly via DbContext (e.g., in `GetOrCreateAdminLeader`), always set `OrganizationId` on `Team` and `Collaborator`
  - Use `IgnoreQueryFilters()` when looking up bootstrap data to avoid tenant filters hiding existing records
- Example: [OrganizationsEndpointsTests.cs](tests/Bud.Server.IntegrationTests/Endpoints/OrganizationsEndpointsTests.cs)

### E2E Tests

- Implement E2E tests when it makes sense for the feature

## Code Style & Naming Conventions

Enforced by `.editorconfig` and `Directory.Build.props`:

- **Indentation:** 4 spaces for C#/Razor, 2 spaces for XML/JSON
- **Line endings:** LF
- **Nullable reference types** enabled
- **Implicit usings** enabled
- Use **primary constructors** for controllers and services (C# 12 feature)
- Use **file-scoped namespaces** where possible
- Follow Microsoft C# naming conventions (PascalCase for public members, camelCase for locals)
- Apply Clean Code principles whenever possible
- Linting: default .NET analyzers

## Important Patterns to Follow

### Adding a New Entity

Follow this sequence as a MUST checklist:

1. Create the model in `src/Bud.Shared/Models/`
   - If the entity belongs to an organization, implement `ITenantEntity` and add `Guid OrganizationId` + `Organization` nav prop
2. Add `DbSet<TEntity>` to `ApplicationDbContext`
3. Configure relationships in `OnModelCreating()`
   - If tenant-scoped: add FK/index for `OrganizationId` (`DeleteBehavior.Restrict`) and add a `HasQueryFilter` following the existing pattern
4. Create a migration: `dotnet ef migrations add AddEntityName --project src/Bud.Server`
5. Create request/response contracts in `src/Bud.Shared/Contracts/`
6. Create FluentValidation validators in `src/Bud.Server/Validators/`
7. Create/adjust use case contracts in `src/Bud.Server/Application/*`
8. Create service interface in `src/Bud.Server/Application/Abstractions/` and implementation in `src/Bud.Server/Services/`
   - If tenant-scoped: resolve and set `OrganizationId` from the parent entity in `CreateAsync`
9. Register implementations and use cases in [BudApplicationCompositionExtensions.cs](src/Bud.Server/DependencyInjection/BudApplicationCompositionExtensions.cs)
10. Create controller in `src/Bud.Server/Controllers/`
11. Write unit tests in `tests/Bud.Server.Tests/`
12. Write integration tests in `tests/Bud.Server.IntegrationTests/`

### Adding a New Blazor Page

1. Create `.razor` file in `src/Bud.Client/Pages/`
2. Add route with `@page "/route"`
3. Add navigation link in [NavMenu.razor](src/Bud.Client/Layout/NavMenu.razor) or [ManagementMenu.razor](src/Bud.Client/Layout/ManagementMenu.razor)
4. Use `ApiClient` service for API calls
5. Handle loading states and errors in the UI

## Health Checks

The API exposes two health check endpoints:
- `/health/live` - Liveness probe (always returns healthy)
- `/health/ready` - Readiness probe (checks PostgreSQL + Outbox health)

Use these for Kubernetes probes or monitoring.

## Configuration

### appsettings

- Development settings: `src/Bud.Server/appsettings.Development.json`
- Connection string key: `ConnectionStrings:DefaultConnection`
- Outbox health and processing settings: `Outbox:HealthCheck:*`, `Outbox:Processing:*`
- Environment variables override appsettings (useful in Docker)

### Docker Compose

The `docker-compose.yml` configures:
- PostgreSQL on port 5432
- API + UI on port 8080
- Volume mounts for fast local rebuild during development (hot reload is not the default flow)
- Network for service communication

## Commit & Pull Request Guidelines

- Use clear, imperative commit messages (e.g., `Add team filters`, `Fix validation error`)
- PRs should include:
  - Summary of changes
  - Linked issues (if any)
  - Screenshots for UI changes
  - ADR reference when architecture is impacted (`docs/adr/ADR-XXXX-*.md`)

### ADR Governance

- Any architectural change must add or update an ADR in `docs/adr/`
- ADR status must be explicit (`Accepted`, `Proposed`, `Superseded`, `Deprecated`)
- PR description should include: "Architectural impact: yes/no"

## Key Files to Reference

- **Service pattern:** [OrganizationService.cs](src/Bud.Server/Services/OrganizationService.cs)
- **Use case pattern:** [MissionCommandUseCase.cs](src/Bud.Server/Application/Missions/MissionCommandUseCase.cs), [MissionQueryUseCase.cs](src/Bud.Server/Application/Missions/MissionQueryUseCase.cs)
- **Application ports:** [IOrganizationService.cs](src/Bud.Server/Application/Abstractions/IOrganizationService.cs)
- **Use case pipeline:** [IUseCasePipeline.cs](src/Bud.Server/Application/Common/Pipeline/IUseCasePipeline.cs), [UseCasePipeline.cs](src/Bud.Server/Application/Common/Pipeline/UseCasePipeline.cs), [LoggingUseCaseBehavior.cs](src/Bud.Server/Application/Common/Pipeline/LoggingUseCaseBehavior.cs)
- **Specification pattern:** [IQuerySpecification.cs](src/Bud.Server/Domain/Common/Specifications/IQuerySpecification.cs), [MissionSearchSpecification.cs](src/Bud.Server/Domain/Common/Specifications/MissionSearchSpecification.cs), [MissionScopeSpecification.cs](src/Bud.Server/Domain/Common/Specifications/MissionScopeSpecification.cs)
- **Domain events and subscribers:** [IDomainEvent.cs](src/Bud.Server/Domain/Common/Events/IDomainEvent.cs), [IDomainEventSubscriber.cs](src/Bud.Server/Application/Common/Events/IDomainEventSubscriber.cs), [DomainEventDispatcher.cs](src/Bud.Server/Application/Common/Events/DomainEventDispatcher.cs)
- **Controller pattern:** [OrganizationsController.cs](src/Bud.Server/Controllers/OrganizationsController.cs)
- **Base controller helpers:** [ApiControllerBase.cs](src/Bud.Server/Controllers/ApiControllerBase.cs)
- **Validation pattern:** [OrganizationValidators.cs](src/Bud.Server/Validators/OrganizationValidators.cs)
- **Error handling:** [GlobalExceptionHandler.cs](src/Bud.Server/Middleware/GlobalExceptionHandler.cs)
- **Outbox:** [OutboxEventProcessor.cs](src/Bud.Server/Infrastructure/Events/OutboxEventProcessor.cs), [OutboxProcessorBackgroundService.cs](src/Bud.Server/Infrastructure/Events/OutboxProcessorBackgroundService.cs), [OutboxController.cs](src/Bud.Server/Controllers/OutboxController.cs)
- **Architecture governance:** [ArchitectureTests.cs](tests/Bud.Server.Tests/Architecture/ArchitectureTests.cs)
- **ADR index:** [docs/adr/README.md](docs/adr/README.md)
- **Client API calls:** [ApiClient.cs](src/Bud.Client/Services/ApiClient.cs)
- **Multi-tenancy (backend):** [ITenantProvider.cs](src/Bud.Server/MultiTenancy/ITenantProvider.cs), [JwtTenantProvider.cs](src/Bud.Server/MultiTenancy/JwtTenantProvider.cs), [TenantRequiredMiddleware.cs](src/Bud.Server/MultiTenancy/TenantRequiredMiddleware.cs)
- **Authorization services:** [OrganizationAuthorizationService.cs](src/Bud.Server/Services/OrganizationAuthorizationService.cs), [TenantAuthorizationService.cs](src/Bud.Server/Services/TenantAuthorizationService.cs)
- **Authentication:** [AuthService.cs](src/Bud.Server/Services/AuthService.cs)
- **Multi-tenancy (frontend):** [OrganizationContext.cs](src/Bud.Client/Services/OrganizationContext.cs), [MainLayout.razor](src/Bud.Client/Layout/MainLayout.razor), [TenantDelegatingHandler.cs](src/Bud.Client/Services/TenantDelegatingHandler.cs)
- **Tenant entity marker:** [ITenantEntity.cs](src/Bud.Shared/Models/ITenantEntity.cs)
- **MCP entrypoint:** [Program.cs](src/Bud.Mcp/Program.cs)
- **MCP tools:** [McpToolService.cs](src/Bud.Mcp/Tools/McpToolService.cs)
