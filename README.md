# Bud

Aplicação unificada em ASP.NET Core + Blazor WebAssembly (SPA),
utilizando PostgreSQL.

## Como rodar com Docker

```bash
docker compose up --build
```

- App (UI + API): `http://localhost:8080`
- Swagger (ambiente Development): `http://localhost:8080/swagger`

### Padrão de desenvolvimento (sem hot reload)

- O hot reload do Blazor WASM está desativado por padrão.
- O build usa caches de NuGet e de compilação via volumes nomeados para acelerar o ciclo local.

Se você encontrar assets antigos no browser, limpe os volumes e recompile:

```bash
docker compose down -v
docker compose up --build
```

## Design System & Tokens

Bud 2.0 uses a comprehensive design token system based on the [Figma Style Guide](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide).

### Brand Colors

- **Primary**: Orange (#FF6B35) - CTAs, active states, primary actions
- **Secondary**: Wine (#E838A3) - Accents, highlights, secondary actions

### Typography

- **Crimson Pro**: Serif font for headings and display text
- **Plus Jakarta Sans**: Sans-serif for body text and UI components

### Design Tokens

All design values (colors, typography, spacing, shadows) are defined as CSS custom properties in [`src/Bud.Client/wwwroot/css/tokens.css`](src/Bud.Client/wwwroot/css/tokens.css).

**Usage example:**
```css
.button {
    background: var(--color-brand-primary);
    padding: var(--spacing-3) var(--spacing-4);
    border-radius: var(--radius-md);
    font-size: var(--font-size-base);
}
```

### Updating Design Tokens

See [DESIGN_TOKENS.md](DESIGN_TOKENS.md) for:
- Complete token reference
- How to update tokens from Figma
- Token naming conventions
- Best practices

## Migrations (EF Core)

Já existe a migration inicial (`InitialCreate`). Para aplicar no banco com Docker rodando:

```bash
docker run --rm -v "$(pwd)/src":/src -w /src/Bud.Server --network bud_default \
  -e ConnectionStrings__DefaultConnection="Host=db;Port=5432;Database=bud;Username=postgres;Password=postgres" \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -lc "dotnet tool install --tool-path /tmp/tools dotnet-ef --version 10.0.2 && /tmp/tools/dotnet-ef database update"
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
