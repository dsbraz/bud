# Repository Guidelines

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Bud is an ASP.NET Core 10 application with a Blazor WebAssembly frontend, using PostgreSQL as the database. The application manages organizational hierarchies and mission tracking.

## Project Structure

- **Bud.Server** (`src/Bud.Server`): ASP.NET Core API hosting both the API endpoints and the Blazor WebAssembly app
  - `Controllers/`: REST endpoints for organizations, workspaces, teams, collaborators
  - `Models/`: EF Core entities
  - `Data/`: `ApplicationDbContext` and EF Core configuration
  - `Migrations/`: database migrations
  - `Services/`: business logic layer
  - `Validators/`: FluentValidation validators
  - `Middleware/`: global exception handling and other middleware

- **Bud.Client** (`src/Bud.Client`): Blazor WebAssembly SPA (compiled to static files served by Bud.Server)
  - `Pages/`: Blazor pages with routing
  - `Layout/`: Layout components (MainLayout, AuthLayout, NavMenu, ManagementMenu)
  - `Services/`: ApiClient and AuthState

- **Bud.Shared** (`src/Bud.Shared`): Shared models, contracts, and DTOs used by both Client and Server
  - `Models/`: Domain entities
  - `Contracts/`: Request/response DTOs

- **Tests**:
  - `tests/Bud.Server.Tests/`: Unit tests (xUnit, Moq, FluentAssertions)
  - `tests/Bud.Server.IntegrationTests/`: Integration tests with WebApplicationFactory

- **Root**: `docker-compose.yml`, `README.md`, `AGENTS.md`

## Build and Development Commands

### Running with Docker (Recommended)

Development is expected to run via Docker Desktop on macOS. All services (API, UI, PostgreSQL) are containerized.

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

Migrations are automatically applied on startup in Development mode. To manually apply migrations with Docker running:

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

### Service Layer Pattern

All business logic lives in services that return `ServiceResult` or `ServiceResult<T>`:

```csharp
public sealed class ServiceResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ServiceErrorType ErrorType { get; } // None, Validation, NotFound, Conflict
}
```

**Important:** Controllers must check `result.ErrorType` to return appropriate HTTP status codes:
- `NotFound` → 404
- `Validation` → 400
- `Conflict` → 409

### Controller Pattern

Controllers use primary constructors and follow this pattern:

1. Inject the service interface and FluentValidation validators via primary constructor
2. Validate the request using FluentValidation
3. Call the service method
4. Map `ServiceResult` to appropriate HTTP responses

See [OrganizationsController.cs](src/Bud.Server/Controllers/OrganizationsController.cs) as the reference implementation.

### Validation

- **FluentValidation** is used for all request validation
- Validators are in `src/Bud.Server/Validators/`
- Each validator must be registered in DI (see [Program.cs](src/Bud.Server/Program.cs))
- Controllers validate requests before calling services

### Data Access

- **Entity Framework Core** with PostgreSQL (Npgsql provider)
- DbContext: [ApplicationDbContext.cs](src/Bud.Server/Data/ApplicationDbContext.cs)
- All entities are in `Bud.Shared.Models`
- All relationship configurations are in `ApplicationDbContext.OnModelCreating()`

### Client Architecture

- Blazor WebAssembly with pages in `src/Bud.Client/Pages/`
- API communication through [ApiClient.cs](src/Bud.Client/Services/ApiClient.cs)
- Auth state managed by [AuthState.cs](src/Bud.Client/Services/AuthState.cs)
- Layouts in `src/Bud.Client/Layout/` (MainLayout, AuthLayout)

## Testing Guidelines

### Unit Tests (`tests/Bud.Server.Tests`)

- Use **xUnit**, **Moq**, and **FluentAssertions**
- Test validators, services, and business logic in isolation
- **Database Strategy:**
  - **Validator tests**: No database needed, test FluentValidation logic directly
  - **Service tests**: Use `ApplicationDbContext` with **InMemoryDatabase provider** (EF Core)
    - Create context via `DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase()`
    - Each test uses a unique database name (e.g., `Guid.NewGuid().ToString()`)
- Every feature must include unit tests
- Unit tests must not access external resources (except InMemoryDatabase for service tests)
- Example validator test: [CreateOrganizationValidatorTests.cs](tests/Bud.Server.Tests/Validators/CreateOrganizationValidatorTests.cs)
- Example service test: [TeamServiceTests.cs](tests/Bud.Server.Tests/Services/TeamServiceTests.cs)

### Integration Tests (`tests/Bud.Server.IntegrationTests`)

- Use `WebApplicationFactory<Program>` to spin up the full API
- Test full request/response cycles (HTTP endpoints)
- **Database Strategy:**
  - Use **Testcontainers.PostgreSql** to spin up real PostgreSQL container
  - [CustomWebApplicationFactory.cs](tests/Bud.Server.IntegrationTests/CustomWebApplicationFactory.cs) configures the test container
  - PostgreSQL 16 image with automatic migrations on startup
  - Container lifecycle managed by xUnit's `IAsyncLifetime`
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

1. Create the model in `src/Bud.Shared/Models/`
2. Add `DbSet<TEntity>` to `ApplicationDbContext`
3. Configure relationships in `OnModelCreating()`
4. Create a migration: `dotnet ef migrations add AddEntityName --project src/Bud.Server`
5. Create request/response contracts in `src/Bud.Shared/Contracts/`
6. Create FluentValidation validators in `src/Bud.Server/Validators/`
7. Create service interface and implementation in `src/Bud.Server/Services/`
8. Register service in [Program.cs](src/Bud.Server/Program.cs) DI configuration
9. Create controller in `src/Bud.Server/Controllers/`
10. Write unit tests in `tests/Bud.Server.Tests/`
11. Write integration tests in `tests/Bud.Server.IntegrationTests/`

### Adding a New Blazor Page

1. Create `.razor` file in `src/Bud.Client/Pages/`
2. Add route with `@page "/route"`
3. Add navigation link in [NavMenu.razor](src/Bud.Client/Layout/NavMenu.razor) or [ManagementMenu.razor](src/Bud.Client/Layout/ManagementMenu.razor)
4. Use `ApiClient` service for API calls
5. Handle loading states and errors in the UI

## Health Checks

The API exposes two health check endpoints:
- `/health/live` - Liveness probe (always returns healthy)
- `/health/ready` - Readiness probe (checks PostgreSQL connection)

Use these for Kubernetes probes or monitoring.

## Configuration

### appsettings

- Development settings: `src/Bud.Server/appsettings.Development.json`
- Connection string key: `ConnectionStrings:DefaultConnection`
- Environment variables override appsettings (useful in Docker)

### Docker Compose

The `docker-compose.yml` configures:
- PostgreSQL on port 5432
- API + UI on port 8080
- Volume mounts for hot reload during development
- Network for service communication

## Commit & Pull Request Guidelines

- Use clear, imperative commit messages (e.g., `Add team filters`, `Fix validation error`)
- PRs should include:
  - Summary of changes
  - Linked issues (if any)
  - Screenshots for UI changes

## Key Files to Reference

- **Service pattern:** [OrganizationService.cs](src/Bud.Server/Services/OrganizationService.cs)
- **Controller pattern:** [OrganizationsController.cs](src/Bud.Server/Controllers/OrganizationsController.cs)
- **Validation pattern:** [OrganizationValidators.cs](src/Bud.Server/Validators/OrganizationValidators.cs)
- **Error handling:** [GlobalExceptionHandler.cs](src/Bud.Server/Middleware/GlobalExceptionHandler.cs)
- **Client API calls:** [ApiClient.cs](src/Bud.Client/Services/ApiClient.cs)
