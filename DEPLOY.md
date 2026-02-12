# Deploy da aplicacao no Google Cloud

Este documento e um guia pratico para publicar e operar a aplicacao no Google Cloud.

Escopo deste runbook:

- `Bud.Server` (API + frontend Blazor hosted)
- `Bud.Mcp` (endpoint MCP HTTP remoto)

## 1. Resultado esperado

Ao final do deploy, voce deve ter:

- `bud-web` ativo no Cloud Run
- `bud-mcp` ativo no Cloud Run
- banco PostgreSQL no Cloud SQL
- migrations aplicadas no banco
- endpoints de health respondendo com sucesso

## 2. Arquitetura de runtime

### Local (desenvolvimento com Docker)

- `Bud.Server`: `http://localhost:8080`
- `Bud.Mcp`: `http://localhost:8081`
- PostgreSQL: `localhost:5432`

### Google Cloud

- `bud-web`: URL publica do Cloud Run (porta interna `8080`)
- `bud-mcp`: URL publica do Cloud Run (porta interna `8080`)
- Cloud SQL PostgreSQL

Observacao: no Cloud Run nao ha conflito de porta entre servicos.

## 3. Pre-requisitos

1. Projeto GCP criado e com billing ativo.
2. `gcloud` instalado e autenticado.
3. Docker instalado.
4. Permissoes no projeto para Cloud Run, Cloud SQL, Artifact Registry, IAM e Secret Manager.

Comandos de verificacao:

```bash
gcloud auth list
gcloud config list project
docker --version
```

## 4. Variaveis de ambiente

Defina ao menos:

```bash
export PROJECT_ID="seu-projeto"
export REGION="us-central1"
```

Na primeira publicacao, defina tambem:

```bash
export DB_PASS="senha-forte"
export JWT_KEY="chave-jwt-com-32-ou-mais-caracteres"
```

## 5. Primeira publicacao (novo ambiente)

### Passo 1: bootstrap de infraestrutura

```bash
./scripts/gcp-bootstrap.sh
```

Esse passo prepara:

- APIs necessarias no GCP
- Artifact Registry
- Cloud SQL + database + usuario
- service account
- secrets de conexao e JWT

### Passo 2: deploy completo da aplicacao

```bash
./scripts/gcp-deploy-all.sh
```

Esse comando executa em sequencia:

1. deploy do `Bud.Server` (inclui migration)
2. deploy do `Bud.Mcp` apontando para a URL do `Bud.Server`

## 6. Publicacoes seguintes (release)

### Opcao A: deploy completo

```bash
./scripts/gcp-deploy-all.sh
```

### Opcao B: deploy por componente

Deploy somente web:

```bash
./scripts/gcp-deploy-web.sh
```

Deploy somente MCP:

```bash
./scripts/gcp-deploy-mcp.sh
```

Se precisar forcar a URL da API usada pelo MCP:

```bash
export WEB_API_URL="https://bud-web-xxxx.a.run.app"
./scripts/gcp-deploy-mcp.sh
```

## 7. Validacao pos-deploy

### Health checks

```bash
curl -i "https://<url-bud-web>/health/live"
curl -i "https://<url-bud-web>/health/ready"
curl -i "https://<url-bud-mcp>/health/live"
curl -i "https://<url-bud-mcp>/health/ready"
```

### Smoke do MCP

```bash
curl -i "https://<url-bud-mcp>/" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}'
```

Esperado:

- resposta `200`
- header `X-Mcp-Session-Id`

## 8. Rollback operacional

### Rollback do Bud.Server

1. listar revisoes do servico `bud-web`
2. promover revisao anterior no Cloud Run

### Rollback do Bud.Mcp

1. listar revisoes do servico `bud-mcp`
2. promover revisao anterior no Cloud Run

Observacao: se o rollback envolver schema, valide compatibilidade de migrations antes de promover trafego.

## 9. Troubleshooting rapido

### Falha em migration no deploy do web

- Verifique secret `ConnectionStrings__DefaultConnection`.
- Verifique conectividade Cloud Run -> Cloud SQL.
- Execute novamente `./scripts/gcp-deploy-web.sh` apos ajustar.

### MCP sobe mas nao acessa API

- Verifique `BUD_API_BASE_URL` no `bud-mcp`.
- Se necessario, rode `gcp-deploy-mcp.sh` com `WEB_API_URL` explicito.

### Health `/ready` falhando

- Verifique logs do Cloud Run.
- Verifique estado do Cloud SQL.
- Verifique variaveis de ambiente e secrets no servico.

## 10. Scripts e responsabilidades

- `scripts/gcp-bootstrap.sh`: preparar infraestrutura base (uso inicial).
- `scripts/gcp-deploy-web.sh`: publicar `Bud.Server` + migration.
- `scripts/gcp-deploy-mcp.sh`: publicar `Bud.Mcp`.
- `scripts/gcp-deploy-all.sh`: publicar ambos em sequencia.

## 11. Regras MCP que impactam deploy

- O catalogo MCP e obrigatorio em `src/Bud.Mcp/Tools/Generated/mcp-tool-catalog.json`.
- Quando houver mudanca nos contratos de dominio MCP, sincronize o catalogo:

```bash
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- generate-tool-catalog
dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj -- check-tool-catalog --fail-on-diff
```
