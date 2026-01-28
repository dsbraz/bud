# Bud

Aplicação unificada em ASP.NET Core + Blazor WebAssembly (SPA),
utilizando PostgreSQL.

## Como rodar com Docker

```bash
docker compose up --build
```

- App (UI + API): `http://localhost:8080`
- Swagger (ambiente Development): `http://localhost:8080/swagger`

## Migrations (EF Core)

Já existe a migration inicial (`InitialCreate`). Para aplicar no banco com Docker rodando:

```bash
docker run --rm -v "$(pwd)/src":/src -w /src/Bud.Server --network bud_default \
  -e ConnectionStrings__DefaultConnection="Host=db;Port=5432;Database=bud;Username=postgres;Password=postgres" \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -lc "dotnet tool install --tool-path /tmp/tools dotnet-ef --version 8.0.12 && /tmp/tools/dotnet-ef database update"
```

No ambiente Development, a API tenta aplicar migrations automaticamente no startup.

## Endpoints básicos (criação)

- POST `/api/organizations`
- POST `/api/workspaces`
- POST `/api/teams`
- POST `/api/collaborators`

## Endpoints básicos (listagens)

- GET `/api/organizations?search=&page=1&pageSize=10`
- GET `/api/workspaces?organizationId=&search=&page=1&pageSize=10`
- GET `/api/teams?workspaceId=&parentTeamId=&search=&page=1&pageSize=10`
- GET `/api/collaborators?teamId=&search=&page=1&pageSize=10`

### Relacionamentos

- GET `/api/organizations/{id}/workspaces`
- GET `/api/workspaces/{id}/teams`
- GET `/api/teams/{id}/subteams`
- GET `/api/teams/{id}/collaborators`

### Exemplo de payloads

```json
{
  "name": "Acme"
}
```

```json
{
  "name": "Produto",
  "organizationId": "00000000-0000-0000-0000-000000000000"
}
```

```json
{
  "name": "Time A",
  "workspaceId": "00000000-0000-0000-0000-000000000000",
  "parentTeamId": null
}
```

```json
{
  "fullName": "Maria Silva",
  "email": "maria@acme.com",
  "teamId": "00000000-0000-0000-0000-000000000000"
}
```
