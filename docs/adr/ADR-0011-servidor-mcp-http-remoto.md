# ADR-0011: Servidor MCP HTTP remoto para operações de missões

## Status
Accepted

## Contexto

Precisamos expor operações de missão, métrica e check-in para agentes/assistentes via protocolo MCP, sem duplicar regras de negócio já existentes no `Bud.Server`.

Também há necessidade de isolamento operacional (deploy e ciclo de vida independentes) e de conectividade remota para clientes que não executam MCP via processo local (`stdio`).

## Decisão

Adotar o projeto `src/Bud.Mcp` como servidor MCP HTTP remoto, com endpoint `POST /` e health checks (`/health/live`, `/health/ready`).

Diretrizes da decisão:

- O MCP não implementa regras de domínio próprias; ele orquestra chamadas para os endpoints REST existentes do `Bud.Server`.
- A autenticação do MCP é por sessão, via tool `auth_login` (email -> `/api/auth/login`); `BUD_USER_EMAIL` pode ser usado opcionalmente para auto-login no boot.
- O tenant é selecionado na sessão MCP por ferramenta dedicada (`tenant_set_current`) e propagado em `X-Tenant-Id`.
- Sessões MCP remotas são identificadas por `MCP-Session-Id`.
- O servidor MCP expõe `prompts/list` para compatibilidade de discovery em clientes MCP, retornando lista vazia quando não houver prompts publicados.
- As tools de domínio do MCP (`mission_*`, `mission_metric_*`, `metric_checkin_*`) usam catálogo versionado em `Tools/Generated/mcp-tool-catalog.json` como fonte única de schema (modo estrito).
- A inicialização do MCP falha quando o catálogo está ausente, inválido, vazio ou sem tools/campos obrigatórios mínimos.
- O escopo inicial inclui CRUD completo de:
  - missões
  - métricas de missão
  - check-ins de métricas

## Consequências

- Reuso total de validações/autorização/multi-tenancy já consolidados na API.
- Menor risco de divergência de regra entre consumo humano (HTTP/Blazor) e consumo por agentes (MCP).
- Novo boundary operacional para monitorar e manter (`Bud.Mcp`), com testes e documentação próprios.
- Fluxo de autorização passa a depender da identidade autenticada na sessão MCP (dinâmica por usuário), preservando níveis de acesso distintos.
- Catálogo MCP passa a ser artefato crítico de release: alterações de contrato OpenAPI exigem sincronização (`generate-tool-catalog` + `check-tool-catalog --fail-on-diff`).

## Alternativas consideradas

- Manter apenas `stdio`/sidecar local: simples para uso local, mas não atende clientes que exigem endpoint remoto.
- Implementar MCP dentro do `Bud.Server`: simplifica número de projetos, mas aumenta acoplamento de deploy/runtime.
- Expor somente wrappers no cliente Blazor: não atende uso server-to-server/agent tooling.
- Implementar regras de missão diretamente no MCP: aumenta risco de inconsistência e viola separação de responsabilidades.
