# Deploy no Google Cloud (Bud.Server + Bud.Mcp)

## INTRODUCAO

Este documento descreve o fluxo recomendado para publicar o ambiente GCP.

Fluxo principal (3 etapas):

```bash
# 1. Pre-requisitos manuais (primeira vez)
# 2. Bootstrap da infraestrutura
./scripts/gcp-bootstrap.sh
# 3. Deploy das aplicacoes
./scripts/gcp-deploy-all.sh
```

Resumo:

- `gcp-bootstrap.sh`: prepara infraestrutura base e atualiza `.env.gcp`
- `gcp-deploy-all.sh`: publica `bud-web` (com migracao) e `bud-mcp`

## PRE-REQUISITOS (primeira vez)

Antes de rodar o bootstrap pela primeira vez, garanta:

### 1. Projeto GCP criado

```bash
gcloud projects create <PROJECT_ID> --name="<PROJECT_ID>"
```

### 2. Billing vinculado

Vincule uma conta de billing ao projeto pelo Console ou CLI:

```bash
gcloud beta billing projects link <PROJECT_ID> --billing-account="<BILLING_ACCOUNT_ID>"
```

### 3. Usuario com role Owner

O usuario que executara o bootstrap precisa de permissao Owner no projeto:

```bash
gcloud projects add-iam-policy-binding <PROJECT_ID> \
  --member="user:<email>" \
  --role="roles/owner"
```

## CRIANDO O AMBIENTE

### 1. Criar `.env.gcp`

```bash
cp .env.example .env.gcp
```

Valores principais:

```bash
PROJECT_ID="bud2-spike"
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

- habilita APIs necessarias
- cria Artifact Registry
- cria Cloud SQL, database e usuario
- cria service account e permissoes (runtime + Cloud Build)
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

1. build e push da imagem de migracao (`prod-migrate`)
2. build e push da imagem web (`prod-web`)
3. execucao da migracao via Cloud Run Job
4. deploy do `bud-web` no Cloud Run
5. build, push e deploy do `bud-mcp` no Cloud Run

### Pular migracao

Se nao houve mudanca no schema do banco:

```bash
./scripts/gcp-deploy-all.sh --skip-migration
```

### Deploy por servico

```bash
./scripts/gcp-deploy-web.sh
./scripts/gcp-deploy-web.sh --skip-migration
./scripts/gcp-deploy-mcp.sh
```

Forcar URL da API no MCP:

```bash
./scripts/gcp-deploy-mcp.sh --web-api-url "<url-do-web>"
```

### Sem `.env.gcp` (somente parametros)

```bash
./scripts/gcp-bootstrap.sh \
  --project-id "bud2-spike" \
  --region "us-central1"

./scripts/gcp-deploy-all.sh \
  --project-id "bud2-spike" \
  --region "us-central1"
```

## MIGRACOES

O deploy web executa migracoes EF Core automaticamente via Cloud Run Job.

- A imagem de migracao usa `dotnet-ef migrations bundle` (executavel standalone, sem SDK em runtime)
- O target `prod-migrate` no `Dockerfile.gcp` gera o bundle
- O seed (`DbSeeder`) roda no startup da aplicacao em todos os ambientes (idempotente)
- `EnsureCreated()` roda apenas em Development (local/Docker Compose)

Para criar novas migracoes localmente:

```bash
dotnet ef migrations add <NomeDaMigracao> --project src/Bud.Server --output-dir Data/Migrations
```

## POS-DEPLOY

### Liberar acesso publico

Se a organizacao GCP tiver policy `iam.allowedPolicyMemberDomains`, o `--allow-unauthenticated` via CLI nao funciona. Nesse caso, libere manualmente:

1. Acesse o Console do Cloud Run
2. Selecione o servico (`bud-web` ou `bud-mcp`)
3. Va em **Seguranca**
4. Marque **Permitir invocacoes nao autenticadas**
5. Repita para cada servico

## OUTRAS CONSIDERACOES

- `bud-web` e `bud-mcp` rodam em Cloud Run com porta interna `8080`.
- Scripts oficiais:
  - `scripts/gcp-bootstrap.sh`
  - `scripts/gcp-deploy-web.sh`
  - `scripts/gcp-deploy-mcp.sh`
  - `scripts/gcp-deploy-all.sh`

Troubleshooting rapido:

- MCP sem acesso a API:
  - valide URL do `bud-web`
  - rode `./scripts/gcp-deploy-mcp.sh --web-api-url "<url-do-web>"`
- Migracao falha com OOM:
  - a imagem de migracao deve usar `migrations bundle` (target `prod-migrate`), nao `dotnet-ef` em runtime
- Health retorna 403:
  - acesso publico nao foi liberado (ver secao Pos-Deploy)

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
WEB_URL="$(gcloud run services describe bud-web --region us-central1 --project bud2-spike --format='value(status.url)')"
MCP_URL="$(gcloud run services describe bud-mcp --region us-central1 --project bud2-spike --format='value(status.url)')"

curl -i "${WEB_URL}/health/live"
curl -i "${WEB_URL}/health/ready"
curl -i "${MCP_URL}/health/live"
curl -i "${MCP_URL}/health/ready"

curl -i "${MCP_URL}/" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}'
```
