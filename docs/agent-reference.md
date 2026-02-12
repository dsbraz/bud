# Agent Reference (Non-Normative)

This document is optional context for coding agents.
It does not override `AGENTS.md`.

## Project Snapshot

- Stack: ASP.NET Core 10 + Blazor WebAssembly + PostgreSQL.
- Bounded domains: organizations, workspaces, teams, collaborators, missions, mission metrics, metric checkins, templates, notifications, outbox, MCP tools.

## Structure

- `src/Bud.Server`: API, app layer, domain, infrastructure, multi-tenancy, authorization.
- `src/Bud.Client`: Blazor SPA.
- `src/Bud.Shared`: contracts/models shared by server/client.
- `src/Bud.Mcp`: MCP server and tool transport.
- `tests/*`: unit and integration tests.

## Development Commands

```bash
# Docker
docker compose up --build
docker compose down

# Tests
dotnet test
dotnet test tests/Bud.Mcp.Tests
dotnet test tests/Bud.Server.Tests
dotnet test tests/Bud.Server.IntegrationTests

# Coverage
dotnet test /p:CollectCoverage=true

# Local host
dotnet restore
dotnet build
dotnet run --project src/Bud.Server
```

## MCP Catalog Sync

```bash
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- generate-tool-catalog
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- check-tool-catalog --fail-on-diff
```

- Host execution may require `BUD_API_BASE_URL=http://localhost:8080`.

## Core Tenant/Auth Components

- `ITenantEntity`
- `ITenantProvider` / `JwtTenantProvider`
- `TenantRequiredMiddleware`
- `TenantSaveChangesInterceptor`
- `TenantAuthorizationService`
- `OrganizationAuthorizationService`
- `TenantDelegatingHandler` (client)

## Health Endpoints

- `/health/live`
- `/health/ready`

## Config Keys

- `ConnectionStrings:DefaultConnection`
- `Outbox:HealthCheck:*`
- `Outbox:Processing:*`

## Key File Map

- Controller pattern: `src/Bud.Server/Controllers/OrganizationsController.cs`
- Base API behavior: `src/Bud.Server/Controllers/ApiControllerBase.cs`
- Service pattern: `src/Bud.Server/Services/OrganizationService.cs`
- UseCases: `src/Bud.Server/Application/Missions/MissionCommandUseCase.cs`, `src/Bud.Server/Application/Missions/MissionQueryUseCase.cs`
- DbContext: `src/Bud.Server/Data/ApplicationDbContext.cs`
- Validators: `src/Bud.Server/Validators/OrganizationValidators.cs`
- Exception handling: `src/Bud.Server/Middleware/GlobalExceptionHandler.cs`
- Authorization services: `src/Bud.Server/Services/OrganizationAuthorizationService.cs`, `src/Bud.Server/Services/TenantAuthorizationService.cs`
- Auth service: `src/Bud.Server/Services/AuthService.cs`
- Tenant provider/middleware: `src/Bud.Server/MultiTenancy/ITenantProvider.cs`, `src/Bud.Server/MultiTenancy/JwtTenantProvider.cs`, `src/Bud.Server/MultiTenancy/TenantRequiredMiddleware.cs`
- Outbox runtime: `src/Bud.Server/Infrastructure/Events/OutboxEventProcessor.cs`, `src/Bud.Server/Infrastructure/Events/OutboxProcessorBackgroundService.cs`, `src/Bud.Server/Controllers/OutboxController.cs`
- Domain events/pipeline/specification:
  - `src/Bud.Server/Domain/Common/Events/IDomainEvent.cs`
  - `src/Bud.Server/Application/Common/Events/IDomainEventSubscriber.cs`
  - `src/Bud.Server/Application/Common/Events/DomainEventDispatcher.cs`
  - `src/Bud.Server/Infrastructure/Events/OutboxDomainEventDispatcher.cs`
  - `src/Bud.Server/Application/Common/Pipeline/IUseCasePipeline.cs`
  - `src/Bud.Server/Application/Common/Pipeline/UseCasePipeline.cs`
  - `src/Bud.Server/Application/Common/Pipeline/LoggingUseCaseBehavior.cs`
  - `src/Bud.Server/Domain/Common/Specifications/IQuerySpecification.cs`
  - `src/Bud.Server/Domain/Common/Specifications/MissionSearchSpecification.cs`
  - `src/Bud.Server/Domain/Common/Specifications/MissionScopeSpecification.cs`
- Client tenant flow: `src/Bud.Client/Services/OrganizationContext.cs`, `src/Bud.Client/Layout/MainLayout.razor`, `src/Bud.Client/Services/TenantDelegatingHandler.cs`
- MCP entry/tools: `src/Bud.Mcp/Program.cs`, `src/Bud.Mcp/Tools/McpToolService.cs`
- Architecture tests: `tests/Bud.Server.Tests/Architecture/ArchitectureTests.cs`
- Test helpers/examples:
  - `tests/Bud.Server.Tests/Helpers/TestTenantProvider.cs`
  - `tests/Bud.Server.Tests/Validators/CreateOrganizationValidatorTests.cs`
  - `tests/Bud.Server.Tests/Services/TeamServiceTests.cs`
  - `tests/Bud.Server.IntegrationTests/CustomWebApplicationFactory.cs`
  - `tests/Bud.Server.IntegrationTests/Endpoints/OrganizationsEndpointsTests.cs`
- ADR index: `docs/adr/README.md`

## Conventions

- Style is enforced by `.editorconfig` and `Directory.Build.props`.
- Nullable reference types enabled.
- Implicit usings enabled.
- Prefer primary constructors and file-scoped namespaces where applicable.
- Use clear imperative commit messages.
- Any architecture-impacting change should include ADR update under `docs/adr/`.
