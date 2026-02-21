# Bud

Aplicação unificada em ASP.NET Core + Blazor WebAssembly (SPA),
utilizando PostgreSQL.

## Para quem é este README

Este documento é voltado para devs que precisam:
- entender rapidamente a arquitetura e os padrões do Bud,
- subir o ambiente local,
- executar fluxos principais de desenvolvimento com segurança.

## Índice

- [Arquitetura da aplicação](#arquitetura-da-aplicação)
- [Padrões arquiteturais adotados](#padrões-arquiteturais-adotados)
- [Como contribuir](#como-contribuir)
- [Como rodar](#como-rodar-com-docker)
- [Como rodar sem Docker](#como-rodar-sem-docker)
- [Servidor MCP (Missões e Métricas)](#servidor-mcp-missões-e-métricas)
- [Deploy no Google Cloud](#deploy-no-google-cloud)
- [Onboarding rápido (30 min)](#onboarding-rápido-30-min)
- [Testes](#testes)
- [Health checks](#health-checks)
- [Endpoints principais](#endpoints-principais)
- [Sistema de Design e Tokens](#sistema-de-design-e-tokens)

## Arquitetura da aplicação

### Visão geral

O Bud segue uma arquitetura em camadas com separação explícita de responsabilidades:

- **API/Host (`Bud.Server`)**: exposição HTTP, autenticação/autorização, middleware e composição de dependências.
- **Application**: casos de uso (`Command/Query`), orquestração de notificações e resultado unificado (`Result`).
- **Domain**: conceitos de domínio, value objects e specifications.
- **Infrastructure**: EF Core (`ApplicationDbContext`), repositórios (`Repositories/`) e serviços de infraestrutura (`Services/`).
- **Client (`Bud.Client`)**: SPA Blazor WASM com consumo da API.
- **Shared (`Bud.Shared`)**: contratos de borda compartilhados entre cliente, servidor e MCP.

### Organização do backend (Bud.Server)

- **Controllers** recebem requests, validam payloads (FluentValidation) e delegam para Commands/Queries.
  Validações dependentes de dados devem passar por abstrações/repositórios, não por acesso direto de validator ao `DbContext`.
- **Commands/Queries** centralizam o fluxo completo da aplicação (orquestração, autorização, notificações) e retornam `Result`/`Result<T>` (em `Application/Common/`).
- **Infrastructure** (`Infrastructure/`) contém interfaces e implementações concretas:
  - `Repositories/`: interfaces (`I*Repository`) e implementações dos repositórios.
  - `Services/`: interfaces (`IAuthService`, `IMissionScopeResolver`, `IMissionProgressService`) e implementações (`AuthService`, `MissionScopeResolver`, `MissionProgressService`, `NotificationRecipientResolver`).
  - `ApplicationDbContext`, `Configurations/` e `DbSeeder`.
- **Authorization** (`Authorization/`) contém serviços de autorização (`TenantAuthorizationService`, `OrganizationAuthorizationService`), policies, requirements e handlers.
- **DependencyInjection** modulariza bootstrap (`BudApi`, `BudSecurity`, `BudData`, `BudApplication`).

### Padrões arquiteturais adotados

- **Commands/Queries + Repositories (Clean Architecture)**
  Controllers delegam para Commands/Queries, que dependem de interfaces (portas) co-localizadas com suas implementações em `Infrastructure/`.
  Repositórios e serviços definem interfaces e implementações no mesmo diretório. `Domain` não depende de `Application` nem de `Infrastructure`.
  Referências: `docs/adr/ADR-0002-arquitetura-ddd-estrita-e-regras-de-dependencia.md`.
- **Policy-based Authorization (Requirement/Handler)**
  Regras de autorização centralizadas em policies e handlers, reduzindo condicionais espalhadas.
  Referências: `docs/adr/ADR-0007-autenticacao-e-autorizacao-por-politicas.md`.
- **Specification Pattern (consultas reutilizáveis)**
  Filtros de domínio encapsulados em specifications para evitar duplicação de predicados.
  Referências: `src/Bud.Server/Infrastructure/Querying/`.
- **Structured Logging (source-generated)**
  Logs com `[LoggerMessage]` definidos localmente por componente (`partial`), com `EventId` estável e sem catálogo central global.
- **Governança arquitetural por testes + ADRs**  
  Decisões versionadas (ADR) e proteção contra regressão de fronteiras via testes de arquitetura.
  Referências: `docs/adr/README.md` e `tests/Bud.Server.Tests/Architecture/ArchitectureTests.cs`.
- **Aggregate Roots explícitas**
  Entidades raiz de agregado são marcadas com `IAggregateRoot` para tornar boundaries verificáveis por testes.
  Referências: `docs/adr/ADR-0003-agregados-entidades-value-objects-e-invariantes.md`.
- **Invariantes no domínio (modelo rico)**  
  Regras centrais de negócio são aplicadas por métodos de agregado/entidade (`Create`, `Rename`, `SetScope`, etc.) com tradução para `Result` na camada de aplicação (Commands/Queries).
  Inclui Value Objects formais (`PersonName`, `MissionScope`, `ConfidenceLevel`, `MetricRange`) para reduzir primitive obsession.
- **Notification Orchestration (Application)**
  Orquestração de notificações centralizada em `Application/Notifications/` (`NotificationOrchestrator`), desacoplada dos repositórios.

### Multi-tenancy

Isolamento por organização (`OrganizationId`) com:

- `ITenantProvider` para resolver tenant do contexto autenticado.
- Query filters globais do EF Core.
- `TenantRequiredMiddleware` para reforçar seleção/autorização de tenant em `/api/*`.
- Cabeçalho `X-Tenant-Id` enviado pelo client quando uma organização específica está selecionada.

### Fluxo de requisição (resumo)

1. Request chega no controller.
2. Payload é validado.
3. Controller chama o Command/Query correspondente.
4. Command/Query aplica regras de autorização/orquestração e delega para repositórios (via interfaces definidas em `Infrastructure/`).
5. Repositório persiste/consulta via `ApplicationDbContext`.
6. Command/Query orquestra notificações quando aplicável (via `NotificationOrchestrator`).
7. Resultado (`Result`) é mapeado para resposta HTTP.

### Testes e governança arquitetural

- **Unit tests**: regras de negócio, validações, commands/queries e componentes de infraestrutura.
- **Integration tests**: ciclo HTTP completo com PostgreSQL em container.
- **Architecture tests**: evitam regressão de fronteira entre camadas (ex.: Command/Query depender de Infrastructure diretamente), validam tenant isolation, autorização e boundaries de aggregate roots.
  A camada de infraestrutura exige `IEntityTypeConfiguration<T>` dedicada para todas as entidades do `ApplicationDbContext`.
- **ADRs**: decisões arquiteturais versionadas em `docs/adr/`.

### ADRs e fluxo de PR

- Toda mudança arquitetural deve criar/atualizar ADR.
- ADRs seguem convenção `docs/adr/ADR-XXXX-*.md`.
- Índice e convenções: `docs/adr/README.md`.
- No PR, inclua explicitamente:
  - `Architectural impact: yes/no`
  - `ADR: ADR-XXXX` (quando aplicável)

Para lista atualizada de ADRs e ordem recomendada de leitura, consulte:
`docs/adr/README.md`.

### Diagramas

#### Arquitetura e fluxo principal

```mermaid
flowchart LR
    A[Bud.Client<br/>Blazor WASM] -->|HTTP + JWT + X-Tenant-Id| B[Bud.Server Controllers]
    B --> C[Application Commands/Queries<br/>Command/Query]
    C --> R[Infrastructure<br/>I*Repository + Repositories]
    R --> E[(PostgreSQL<br/>ApplicationDbContext)]
    C --> N[NotificationOrchestrator]
    N --> P
```

#### Sequência de request com notificações

```mermaid
sequenceDiagram
    participant U as Usuário/Client
    participant C as Controller
    participant UC as Command/Query
    participant R as Repository
    participant NO as NotificationOrchestrator
    participant DB as PostgreSQL

    U->>C: Requisição de comando (create/update/delete)
    C->>UC: Executa caso de uso
    UC->>R: Persiste entidade
    R->>DB: Consulta/persistência
    UC->>NO: Orquestra notificações (quando aplicável)
    NO->>R: Persiste notificações
    C-->>U: Resposta HTTP (sucesso)
```

#### Modelo de domínio e hierarquia organizacional

```mermaid
flowchart TD
    O[Organization]
    W[Workspace]
    T[Team]
    ST[SubTeam]
    C[Collaborator]
    M[Mission]
    MO[MissionObjective]
    MM[MissionMetric]
    MC[MetricCheckin]

    O --> W
    W --> T
    T --> ST
    T --> C

    O --> M
    W -. escopo .-> M
    T -. escopo .-> M
    C -. escopo .-> M

    M --> MO
    MO --> MO
    MO --> MM
    M --> MM
    MM --> MC

    O --> N[Notification]
    C --> N
```

#### Fluxo de autenticação, tenant e autorização

```mermaid
sequenceDiagram
    participant UI as Bud.Client
    participant API as Bud.Server
    participant AUTH as AuthController/AuthCommand
    participant TENANT as TenantRequiredMiddleware
    participant CTRL as Controller

    UI->>API: POST /api/auth/login
    API->>AUTH: Valida e autentica por e-mail
    AUTH-->>UI: JWT + dados básicos da sessão

    UI->>API: GET /api/auth/my-organizations (X-User-Email)
    API-->>UI: Lista de organizações disponíveis

    UI->>UI: Usuário seleciona organização (ou "TODOS")
    UI->>API: Request com Authorization: Bearer + X-Tenant-Id (opcional)
    API->>TENANT: Valida autenticação e tenant selecionado
    TENANT->>CTRL: Libera acesso conforme policies
    CTRL-->>UI: Resposta filtrada por tenant
```

#### Fluxo completo de request (fim a fim)

```mermaid
sequenceDiagram
    participant UI as Bud.Client / Caller
    participant API as ASP.NET Core Pipeline
    participant AUTH as AuthN/AuthZ
    participant TENANT as TenantRequiredMiddleware
    participant CTRL as Controller
    participant VAL as FluentValidation
    participant UC as Command/Query
    participant REPO as Repository
    participant DB as ApplicationDbContext/PostgreSQL

    UI->>API: HTTP Request
    API->>AUTH: Valida JWT/policies
    AUTH->>TENANT: Valida contexto de tenant (quando aplicável)
    TENANT->>CTRL: Encaminha request autorizado
    CTRL->>VAL: Valida payload

    alt Payload inválido
        VAL-->>CTRL: Erros de validação
        CTRL-->>UI: 400 ValidationProblemDetails (pt-BR)
    else Payload válido
        CTRL->>UC: Executa caso de uso
        UC->>REPO: Consulta/persistência
        REPO->>DB: EF Core
        UC-->>CTRL: Result
        CTRL-->>UI: 2xx/4xx/5xx + ProblemDetails quando aplicável
    end
```

#### Fluxo de request (falhas comuns)

```mermaid
flowchart TD
    A[Request recebido] --> B{JWT válido?}
    B -- Não --> C[401 Não autenticado]
    B -- Sim --> D{Tenant exigido e selecionado?}
    D -- Não --> E[403 Tenant obrigatório]
    D -- Sim --> F{Payload válido?}
    F -- Não --> G[400 ValidationProblemDetails]
    F -- Sim --> H{Entidade existe?}
    H -- Não --> I[404 NotFound]
    H -- Sim --> J{Conflito de regra?}
    J -- Sim --> K[409 Conflict]
    J -- Não --> L[2xx Sucesso]
```

#### Módulos do backend e dependências

```mermaid
flowchart TB
    subgraph Host["Bud.Server Host"]
      Controllers
      Middleware
      DependencyInjection
    end
    subgraph App["Application"]
      Commands/Queries
    end
    subgraph Domain["Domain"]
      Specifications
      ValueObjects
    end
    subgraph Infra["Infrastructure"]
      Interfaces["Interfaces (I*Repository)"]
      Repositories
      DbContext
    end

    Controllers --> Commands/Queries
    Commands/Queries --> Interfaces
    Repositories -.-> Interfaces
    Repositories --> DbContext
    Repositories --> Specifications
    DependencyInjection --> Controllers
    DependencyInjection --> Commands/Queries
    DependencyInjection --> Repositories
```

#### Fronteira de responsabilidades (Client x API x Dados)

```mermaid
flowchart LR
    subgraph Client["Bud.Client (Blazor WASM)"]
      UI["Pages + Layout"]
      ApiClient["ApiClient + TenantDelegatingHandler"]
    end

    subgraph Api["Bud.Server (API/Aplicação)"]
      Controllers["Controllers"]
      Commands/Queries["Commands/Queries"]
      Authz["AuthN/AuthZ + Policies"]
    end

    subgraph Infra["Infrastructure"]
      Ports["Interfaces (I*Repository)"]
      Repos["Repositories"]
      Db["ApplicationDbContext"]
      Pg["PostgreSQL"]
    end

    UI --> ApiClient
    ApiClient --> Controllers
    Controllers --> Commands/Queries
    Commands/Queries --> Ports
    Repos -.-> Ports
    Controllers --> Authz
    Repos --> Db
    Db --> Pg
```

## Como contribuir

Fluxo recomendado de contribuição para manter qualidade arquitetural e consistência:

1. Crie uma branch curta e focada no objetivo da mudança.
2. Escreva/atualize testes antes da implementação (TDD: Red -> Green -> Refactor).
3. Implemente seguindo os padrões do projeto:
   - Controllers -> Commands/Queries -> Repositories (via interfaces em Infrastructure)
   - autorização por policies/handlers
   - mensagens de erro/validação em pt-BR
4. Atualize documentação OpenAPI (summary/description/status/errors) quando alterar contratos.
5. Se houver mudança arquitetural, atualize/crie ADR em `docs/adr/`.
6. Rode a suíte de testes aplicável e valide Swagger/health checks.
7. Abra PR com impacto arquitetural explícito e referência de ADR (quando aplicável).

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

## Como rodar sem Docker

Pré-requisitos:

- .NET SDK 10
- PostgreSQL 16+

Comandos:

```bash
dotnet restore
dotnet build
dotnet run --project src/Bud.Server
```

## Servidor MCP (Missões e Métricas)

O repositório inclui um servidor MCP HTTP em `src/Bud.Mcp`, que consome a API do `Bud.Server`.

No transporte HTTP do MCP, o endpoint raiz delega o processamento para `IMcpRequestProcessor`/`McpRequestProcessor`,
mantendo `Program.cs` focado em composição e roteamento.

No ambiente local via Docker Compose:
- API + frontend: `http://localhost:8080`
- MCP: `http://localhost:8081`

### Configuração (`appsettings` + override por ambiente)

O `Bud.Mcp` lê configuração na seguinte ordem:
1. `appsettings.json`
2. `appsettings.{DOTNET_ENVIRONMENT}.json`
3. variáveis de ambiente (override)

Chaves suportadas:
- `BudMcp:ApiBaseUrl` (ou `BUD_API_BASE_URL`)
- `BudMcp:UserEmail` (ou `BUD_USER_EMAIL`) opcional, para login automático no boot
- `BudMcp:DefaultTenantId` (ou `BUD_DEFAULT_TENANT_ID`)
- `BudMcp:HttpTimeoutSeconds` (ou `BUD_HTTP_TIMEOUT_SECONDS`)
- `BudMcp:SessionIdleTtlMinutes` (ou `BUD_SESSION_IDLE_TTL_MINUTES`)

### Subindo via Docker Compose

```bash
docker compose up --build
```

O serviço `mcp` é criado no compose usando:
- `Dockerfile` (target `dev-mcp-web`)
- `DOTNET_ENVIRONMENT=Development` (usa `src/Bud.Mcp/appsettings.Development.json`)
- `BUD_API_BASE_URL=http://web:8080` para chamadas internas ao `Bud.Server`
- mapeamento de porta `8081:8080` para evitar conflito com o `web`

Health checks do MCP local:

```bash
curl -i http://localhost:8081/health/live
curl -i http://localhost:8081/health/ready
```

Exemplo de `initialize` no endpoint MCP:

```bash
curl -s http://localhost:8081/ \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}'
```

A resposta inclui o header `MCP-Session-Id`, que deve ser enviado nas chamadas seguintes (`tools/list`, `tools/call`, etc.).

Fluxo obrigatório para atualizar catálogo MCP:

```bash
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- generate-tool-catalog
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- check-tool-catalog --fail-on-diff
```

Regras importantes do catálogo:
- As ferramentas de domínio (`mission_*`, `mission_objective_*`, `mission_metric_*`, `metric_checkin_*`) são carregadas exclusivamente do arquivo `src/Bud.Mcp/Tools/Generated/mcp-tool-catalog.json`.
- O `Bud.Mcp` falha na inicialização se o catálogo estiver ausente, inválido, vazio ou sem ferramentas de domínio obrigatórias.
- O comando `check-tool-catalog --fail-on-diff` também valida o contrato mínimo de campos `required` por ferramenta e retorna erro quando houver quebra de contrato.

Se estiver rodando local com `DOTNET_ENVIRONMENT=Development`, defina:
`BUD_API_BASE_URL=http://localhost:8080`.

### Ferramentas MCP disponíveis

- `auth_login`
- `auth_whoami`
- `tenant_list_available`
- `tenant_set_current`
- `session_bootstrap`
- `help_list_actions`
- `help_action_schema`
- `mission_create`, `mission_get`, `mission_list`, `mission_update`, `mission_delete`
- `mission_objective_create`, `mission_objective_get`, `mission_objective_list`, `mission_objective_update`, `mission_objective_delete`
- `mission_metric_create`, `mission_metric_get`, `mission_metric_list`, `mission_metric_update`, `mission_metric_delete`
- `metric_checkin_create`, `metric_checkin_get`, `metric_checkin_list`, `metric_checkin_update`, `metric_checkin_delete`

### Descoberta de parâmetros e bootstrap de sessão no MCP

- `prompts/list` é suportado para compatibilidade de clientes MCP e retorna lista vazia quando não há prompts publicados.
- `auth_login` retorna `whoami`, `requiresTenantForDomainTools` e `nextSteps` para orientar o agente nos próximos passos.
- `session_bootstrap` retorna snapshot de sessão (`whoami`, `availableTenants`, tenant atual) e `starterSchemas` para fluxos de criação.
- `help_action_schema` retorna `required`, `inputSchema` e `example` para uma ação específica (ou todas as ações, quando `action` não é informado).
- Em erro de validação da API, o MCP retorna `statusCode`, `title`, `detail` e `errors` por campo quando disponível.

## Deploy no Google Cloud

Scripts disponíveis:

- `scripts/gcp-bootstrap.sh`: prepara infraestrutura base (APIs, Artifact Registry, Cloud SQL, service account, secrets, permissões do Cloud Build).
- `scripts/gcp-deploy-web.sh`: deploy do `Bud.Server` (com migração EF Core via Cloud Run Job).
- `scripts/gcp-deploy-mcp.sh`: deploy do `Bud.Mcp` HTTP.
- `scripts/gcp-deploy-all.sh`: executa deploy completo (`Bud.Server` + `Bud.Mcp`).

Observação: os scripts de deploy remoto usam **Cloud Build** para gerar imagens no GCP (não dependem de `docker build` local).

Pré-requisitos (primeira vez): projeto GCP criado, billing vinculado, usuário com role Owner. Ver `DEPLOY.md` para detalhes.

Fluxo recomendado:

```bash
cp .env.example .env.gcp   # ajustar PROJECT_ID
./scripts/gcp-bootstrap.sh
./scripts/gcp-deploy-all.sh
```

Deploy sem migração (quando o schema não mudou):

```bash
./scripts/gcp-deploy-all.sh --skip-migration
```

Deploy por serviço:

```bash
./scripts/gcp-deploy-web.sh
./scripts/gcp-deploy-web.sh --skip-migration
./scripts/gcp-deploy-mcp.sh
```

Para detalhes operacionais, pré-requisitos e troubleshooting, consulte `DEPLOY.md`.

## Onboarding rápido (30 min)

Objetivo: validar ponta a ponta (auth, tenant, CRUD básico e leitura) em ambiente local.

### 1) Subir a aplicação

Opção A (recomendada):

```bash
docker compose up --build
```

Opção B (sem Docker):

```bash
dotnet restore
dotnet build
dotnet run --project src/Bud.Server
```

Defina a URL base conforme o modo de execução:

```bash
# Com Docker Compose
export BUD_BASE_URL="http://localhost:8080"

# Sem Docker (dotnet run, profile padrão)
export BUD_BASE_URL="http://localhost:5096"
```

### 2) Login e captura do token

```bash
curl -s -X POST "$BUD_BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@getbud.co"}'
```

Copie o `token` da resposta e exporte:

```bash
export BUD_TOKEN="<jwt>"
```

### 3) Descobrir organizações disponíveis

```bash
curl -s "$BUD_BASE_URL/api/auth/my-organizations" \
  -H "X-User-Email: admin@getbud.co"
```

Copie um `id` e exporte:

```bash
export BUD_ORG_ID="<organization-id>"
```

### 4) Smoke test de leitura tenant-scoped

```bash
curl -s "$BUD_BASE_URL/api/missions?page=1&pageSize=10" \
  -H "Authorization: Bearer $BUD_TOKEN" \
  -H "X-Tenant-Id: $BUD_ORG_ID"
```

### 5) Smoke test de criação de missão

```bash
curl -s -X POST "$BUD_BASE_URL/api/missions" \
  -H "Authorization: Bearer $BUD_TOKEN" \
  -H "X-Tenant-Id: $BUD_ORG_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Onboarding - Missão",
    "description":"Validação ponta a ponta",
    "startDate":"2026-01-01T00:00:00Z",
    "endDate":"2026-12-31T23:59:59Z",
    "status":"Planned",
    "scopeType":"Organization",
    "scopeId":"'"$BUD_ORG_ID"'"
  }'
```

### 6) Diagrama rápido do onboarding HTTP

```mermaid
sequenceDiagram
    participant Dev as Dev
    participant API as Bud.Server

    Dev->>API: POST /api/auth/login (email)
    API-->>Dev: token JWT
    Dev->>API: GET /api/auth/my-organizations (X-User-Email)
    API-->>Dev: organizations[]
    Dev->>Dev: define BUD_ORG_ID
    Dev->>API: GET /api/missions (Bearer + X-Tenant-Id)
    API-->>Dev: lista paginada
    Dev->>API: POST /api/missions (Bearer + X-Tenant-Id)
    API-->>Dev: missão criada
```

### 7) Debug rápido no backend

- Acesse `$BUD_BASE_URL/swagger` para executar os mesmos fluxos pela UI.
- Consulte `GET /health/ready` para validar PostgreSQL.
- Em caso de erro 403, valide se o header `X-Tenant-Id` foi enviado (exceto cenário global admin em "TODOS").

### 8) Troubleshooting rápido (dev local)

- **Porta 8080 ocupada**  
  Sintoma: erro ao subir `docker compose up --build` com bind em `8080`.  
  Ação: pare o processo/serviço que está usando a porta ou altere o mapeamento no `docker-compose.yml`.

- **Falha de conexão com PostgreSQL**  
  Sintoma: `/health/ready` retorna unhealthy para banco.  
  Ação: confirme se o container `db` subiu e se a connection string está correta (`ConnectionStrings:DefaultConnection`).

- **401/403 em endpoints protegidos**  
  Sintoma: chamadas autenticadas falham mesmo após login.  
  Ação: verifique `Authorization: Bearer <token>` e, para endpoints tenant-scoped, envie `X-Tenant-Id`.

- **Dados/artefatos antigos no browser**  
  Sintoma: UI não reflete mudanças recentes.  
  Ação: execute `docker compose down -v && docker compose up --build` e force reload no navegador.

#### Fluxo de diagnóstico rápido (401/403)

```mermaid
flowchart TD
    A[Erro 401/403] --> B{Tem Bearer token?}
    B -- Não --> C[Refaça login em /api/auth/login]
    B -- Sim --> D{Endpoint é tenant-scoped?}
    D -- Não --> E[Validar payload/headers específicos]
    D -- Sim --> F{X-Tenant-Id foi enviado?}
    F -- Não --> G[Enviar X-Tenant-Id]
    F -- Sim --> H{Usuário pertence ao tenant?}
    H -- Não --> I[Selecionar tenant válido em /api/auth/my-organizations]
    H -- Sim --> J[Validar policy/regra de autorização do endpoint]
```

## Testes

```bash
# suíte completa
dotnet test

# testes MCP
dotnet test tests/Bud.Mcp.Tests

# apenas unitários
dotnet test tests/Bud.Server.Tests

# apenas integração
dotnet test tests/Bud.Server.IntegrationTests
```

Observação:

- `dotnet test` usa `Bud.slnx` e executa também `tests/Bud.Mcp.Tests`.
- Testes de integração usam PostgreSQL via Testcontainers.
- Use `dotnet test --nologo` para saída mais limpa no terminal.
- A solução usa `TreatWarningsAsErrors=true`; avisos quebram build/test.

## Documentação da API (OpenAPI/Swagger)

- UI interativa (Development): `http://localhost:8080/swagger`
- Documento OpenAPI bruto: `http://localhost:8080/openapi/v1.json`
- A documentação é enriquecida com:
  - `ProducesResponseType` por endpoint
  - comentários XML (`summary`, `response`, `remarks`)
  - metadados de conteúdo (`Consumes`/`Produces`) quando aplicável
- Para campos enum em payload JSON, a API aceita tanto `string` (case-insensitive) quanto `number` (compatibilidade retroativa).

## Sistema de Design e Tokens

O Bud 2.0 usa um sistema de tokens de design baseado no [Figma Style Guide](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide).

### Cores de marca

- **Primária**: Orange (#FF6B35) - CTAs, estados ativos e ações principais
- **Secundária**: Wine (#E838A3) - acentos, destaques e ações secundárias

### Tipografia

- **Crimson Pro**: fonte serifada para títulos e destaques
- **Plus Jakarta Sans**: fonte sem serifa para texto e componentes de interface

### Tokens de design

Todos os valores de design (cores, tipografia, espaçamento e sombras) são definidos como propriedades CSS em [`src/Bud.Client/wwwroot/css/tokens.css`](src/Bud.Client/wwwroot/css/tokens.css).

**Exemplo de uso:**
```css
.button {
    background: var(--color-brand-primary);
    padding: var(--spacing-3) var(--spacing-4);
    border-radius: var(--radius-md);
    font-size: var(--font-size-base);
}
```

### Atualização de tokens

Veja [DESIGN_TOKENS.md](DESIGN_TOKENS.md) para:
- Referência completa de tokens
- Processo de atualização a partir do Figma
- Convenções de nomenclatura
- Boas práticas

## Banco de Dados (Desenvolvimento)

O schema do banco é criado automaticamente a partir do modelo EF Core via `EnsureCreated()` no startup em Development. Não são usadas migrations durante o desenvolvimento.

Para recriar o banco após mudanças no modelo:
```bash
docker compose down -v && docker compose up --build
```

Quando o projeto se aproximar de produção, migrations serão reintroduzidas.

## Health checks

- `GET /health/live`: liveness (sempre saudável).
- `GET /health/ready`: readiness (PostgreSQL).

## Endpoints principais

Referência completa com exemplos interativos disponível em `/swagger` (ambiente Development).

### Auth

- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/my-organizations`

### Organizations (CRUD + relacionamentos)

- `POST /api/organizations`
- `GET /api/organizations` — listagem paginada (`?search=&page=1&pageSize=10`)
- `GET /api/organizations/{id}`
- `PUT /api/organizations/{id}`
- `DELETE /api/organizations/{id}`
- `GET /api/organizations/{id}/workspaces`
- `GET /api/organizations/{id}/collaborators`

### Workspaces (CRUD + relacionamentos)

- `POST /api/workspaces`
- `GET /api/workspaces` — listagem paginada (`?organizationId=&search=&page=1&pageSize=10`)
- `GET /api/workspaces/{id}`
- `PUT /api/workspaces/{id}`
- `DELETE /api/workspaces/{id}`
- `GET /api/workspaces/{id}/teams`

### Teams (CRUD + relacionamentos)

- `POST /api/teams`
- `GET /api/teams` — listagem paginada (`?workspaceId=&parentTeamId=&search=&page=1&pageSize=10`)
- `GET /api/teams/{id}`
- `PUT /api/teams/{id}`
- `DELETE /api/teams/{id}`
- `GET /api/teams/{id}/subteams`
- `GET /api/teams/{id}/collaborators`
- `GET /api/teams/{id}/collaborators-summary`
- `PUT /api/teams/{id}/collaborators`
- `GET /api/teams/{id}/available-collaborators`

### Collaborators (CRUD + relacionamentos)

- `POST /api/collaborators`
- `GET /api/collaborators` — listagem paginada (`?teamId=&search=&page=1&pageSize=10`)
- `GET /api/collaborators/{id}`
- `PUT /api/collaborators/{id}`
- `DELETE /api/collaborators/{id}`
- `GET /api/collaborators/leaders`
- `GET /api/collaborators/{id}/subordinates`
- `GET /api/collaborators/{id}/teams`
- `PUT /api/collaborators/{id}/teams`
- `GET /api/collaborators/{id}/available-teams`

### Missions (CRUD + consultas)

- `POST /api/missions`
- `GET /api/missions` — listagem paginada
- `GET /api/missions/{id}`
- `PUT /api/missions/{id}`
- `DELETE /api/missions/{id}`
- `GET /api/missions/progress`
- `GET /api/missions/my-missions/{collaboratorId}`
- `GET /api/missions/{id}/metrics`
- `GET /api/missions/{id}/objectives`

### Mission Objectives (CRUD)

- `POST /api/mission-objectives`
- `GET /api/mission-objectives` — listagem paginada (filtro por `missionId`)
- `GET /api/mission-objectives/{id}`
- `PUT /api/mission-objectives/{id}`
- `DELETE /api/mission-objectives/{id}`
- `GET /api/mission-objectives/progress`
- Campo opcional nos payloads de criação/atualização: `objectiveDimensionId` (GUID de `/api/objective-dimensions`).

### Objective Dimensions (CRUD)

- `POST /api/objective-dimensions`
- `GET /api/objective-dimensions` — listagem paginada
- `GET /api/objective-dimensions/{id}`
- `PUT /api/objective-dimensions/{id}`
- `DELETE /api/objective-dimensions/{id}`

### Mission Metrics (CRUD)

- `POST /api/mission-metrics`
- `GET /api/mission-metrics` — listagem paginada
- `GET /api/mission-metrics/{id}`
- `PUT /api/mission-metrics/{id}`
- `DELETE /api/mission-metrics/{id}`
- `GET /api/mission-metrics/progress`

### Metric Checkins (CRUD)

- `POST /api/metric-checkins`
- `GET /api/metric-checkins` — listagem paginada
- `GET /api/metric-checkins/{id}`
- `PUT /api/metric-checkins/{id}`
- `DELETE /api/metric-checkins/{id}`

### Mission Templates (CRUD)

- `POST /api/mission-templates`
- `GET /api/mission-templates` — listagem paginada
- `GET /api/mission-templates/{id}`
- `PUT /api/mission-templates/{id}`
- `DELETE /api/mission-templates/{id}`
- Modelos padrão semeados para a organização bootstrap (`getbud.co`): `BSC`, `Mapa Estratégico`, `Planejamento Estratégico Anual`, `OKR` e `PDI` (todos com objetivos e métricas vinculadas).
- Payloads de criação/atualização também aceitam `objectives` (objetivos do template) e `metrics[].missionTemplateObjectiveId` para vincular métricas a objetivos.

### Notifications

- `GET /api/notifications` — listagem paginada
- `GET /api/notifications/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`

### Dashboard

- `GET /api/dashboard/my-dashboard`

### Exemplos de payloads de criação

**Organization:**
```json
{
  "name": "acme.com.br",
  "ownerId": "00000000-0000-0000-0000-000000000000"
}
```

**Workspace:**
```json
{
  "name": "Produto",
  "organizationId": "00000000-0000-0000-0000-000000000000"
}
```

**Team:**
```json
{
  "name": "Time A",
  "workspaceId": "00000000-0000-0000-0000-000000000000",
  "parentTeamId": null
}
```

**Collaborator:**
```json
{
  "fullName": "Maria Silva",
  "email": "maria@acme.com",
  "teamId": "00000000-0000-0000-0000-000000000000",
  "leaderId": null
}
```

**Mission:**
```json
{
  "name": "Aumentar NPS",
  "description": "Melhorar satisfação do cliente",
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": "2026-03-31T23:59:59Z",
  "status": "Planned",
  "scopeType": "Workspace",
  "scopeId": "00000000-0000-0000-0000-000000000000"
}
```

**Metric Checkin:**
```json
{
  "missionMetricId": "00000000-0000-0000-0000-000000000000",
  "value": 42.5,
  "text": null,
  "checkinDate": "2026-02-07T00:00:00Z",
  "note": "Evolução semanal",
  "confidenceLevel": 4
}
```
