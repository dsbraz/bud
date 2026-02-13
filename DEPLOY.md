# Deploy no Google Cloud (Bud.Server + Bud.Mcp)

## INTRODUCAO

Este documento descreve o fluxo recomendado para publicar o ambiente remoto de dev no GCP.

Fluxo principal (2 comandos):

```bash
./scripts/gcp-bootstrap.sh
./scripts/gcp-deploy-all.sh
```

Resumo:

- `gcp-bootstrap.sh`: prepara infraestrutura base e atualiza `.env.gcp`
- `gcp-deploy-all.sh`: publica `bud-web` e `bud-mcp`

## CRIANDO O AMBIENTE

### Pre-requisitos

```bash
gcloud auth list
gcloud config list project
```

### 1. Criar `.env.gcp`

```bash
cp .env.example .env.gcp
```

Valores principais:

```bash
PROJECT_ID="getbud-co-dev"
REGION="us-central1"
DB_PASS=""
JWT_KEY=""
SECRET_DB_CONNECTION=""
SECRET_JWT_KEY=""
```

### 2. Executar bootstrap

```bash
./scripts/gcp-bootstrap.sh
```

O que o bootstrap faz automaticamente:

- cria projeto (se nao existir e voce confirmar)
- tenta vincular billing
- habilita APIs necessarias
- cria Artifact Registry
- cria Cloud SQL, database e usuario
- cria service account e permissoes
- cria/atualiza secrets no Secret Manager
- gera automaticamente (quando vazios): `DB_PASS`, `JWT_KEY`, `SECRET_DB_CONNECTION`, `SECRET_JWT_KEY`
- persiste valores efetivos no `.env.gcp`

Opcional com arquivo explicito:

```bash
./scripts/gcp-bootstrap.sh --env-file .env.gcp
```

## PUBLICANDO AS APLICACOES

### Deploy completo

```bash
./scripts/gcp-deploy-all.sh
```

Esse comando executa:

1. deploy do `bud-web`
2. deploy do `bud-mcp` apontando para a URL do `bud-web`

### Deploy por servico

```bash
./scripts/gcp-deploy-web.sh
./scripts/gcp-deploy-mcp.sh
```

Forcar URL da API no MCP:

```bash
./scripts/gcp-deploy-mcp.sh --web-api-url "<url-do-web>"
```

### Sem `.env.gcp` (somente parametros)

```bash
./scripts/gcp-bootstrap.sh \
  --project-id "getbud-co-dev" \
  --region "us-central1"

./scripts/gcp-deploy-all.sh \
  --project-id "getbud-co-dev" \
  --region "us-central1"
```

## OUTRAS CONSIDERACOES

- `bud-web` e `bud-mcp` rodam em Cloud Run com porta interna `8080`.
- Script oficiais:
  - `scripts/gcp-bootstrap.sh`
  - `scripts/gcp-deploy-web.sh`
  - `scripts/gcp-deploy-mcp.sh`
  - `scripts/gcp-deploy-all.sh`

Troubleshooting rapido:

- MCP sem acesso a API:
  - valide URL do `bud-web`
  - rode `./scripts/gcp-deploy-mcp.sh --web-api-url "<url-do-web>"`

## CHECKLIST

Antes de considerar o deploy concluido:

1. `bootstrap` executado sem erro.
2. `deploy-all` executado sem erro.
3. health do web:
   - `GET /health/live` retorna `200`
   - `GET /health/ready` retorna `200`
4. health do mcp:
   - `GET /health/live` retorna `200`
   - `GET /health/ready` retorna `200`
5. smoke MCP:
   - `POST /` com `initialize` retorna `200`
   - resposta inclui header `MCP-Session-Id`

Comandos de validacao:

```bash
WEB_URL="$(gcloud run services describe bud-web --region us-central1 --project getbud-co-dev --format='value(status.url)')"
MCP_URL="$(gcloud run services describe bud-mcp --region us-central1 --project getbud-co-dev --format='value(status.url)')"

curl -i "${WEB_URL}/health/live"
curl -i "${WEB_URL}/health/ready"
curl -i "${MCP_URL}/health/live"
curl -i "${MCP_URL}/health/ready"

curl -i "${MCP_URL}/" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}'
```
