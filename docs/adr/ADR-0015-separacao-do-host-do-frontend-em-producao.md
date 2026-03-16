# ADR-0015: Separação do Host do Frontend em Produção

## Status
Deprecated — conteudo movido para `DEPLOY.md` (secao "Topologia de producao"). Decisao de hospedagem, nao arquitetural.

## Contexto

O Bud vinha publicando o frontend Blazor WebAssembly junto com o `Bud.Api`. Isso simplificava o bootstrap inicial, mas acoplava o deploy público da interface ao ciclo de release da API e impedia a evolução do frontend como serviço separado sem trocar a URL pública do ambiente produtivo.

Em produção, a URL pública existente é a URL do Cloud Run do serviço `bud-web`, e o fluxo operacional já estava automatizado com `Cloud Build` + `Cloud Run` + job de migração EF Core.

## Decisão

Adotar separação de hosting em produção com três serviços:

- `bud-web`: frontend público Blazor WASM em container estático.
- `bud-api`: API ASP.NET Core dedicada.
- `bud-mcp`: servidor MCP HTTP.

O `bud-web` passa a servir apenas os assets do frontend e a fazer proxy reverso para `bud-api` em `/api/*`, `/health/*`, `/swagger/*` e `/openapi/*`, preservando a URL pública existente e o modelo same-origin para o browser.

O `Bud.Api` deixa de referenciar `Bud.BlazorWasm` e deixa de ser responsável por `UseBlazorFrameworkFiles`, `UseStaticFiles` e `MapFallbackToFile("index.html")`.

## Consequências

- Deploy e rollback de frontend e API passam a ser independentes.
- O browser continua sem depender de CORS em produção, porque a origem pública permanece a mesma.
- O `Bud.Mcp` passa a depender explicitamente da URL do `bud-api`, não do serviço público `bud-web`.
- O fluxo de deploy continua automatizado por scripts, agora com `gcp-deploy-api.sh`, `gcp-deploy-web.sh` e `gcp-deploy-all.sh`.
- O ambiente local passa a tratar frontend, API e MCP como processos/serviços distintos.

## Alternativas consideradas

- Manter frontend e API no mesmo processo ASP.NET Core.
- Separar frontend e API com hosts públicos distintos e habilitar CORS no browser.
- Introduzir load balancer externo apenas para preservar a URL pública existente.
