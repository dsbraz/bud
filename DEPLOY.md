# Deploy no Google Cloud (Bud.Server + Cloud SQL)

Este documento descreve o fluxo de deploy automatizado para o projeto Bud e os passos manuais que ainda sao obrigatorios.

## Escopo deste deploy

- Publica **Bud.Server** no Cloud Run (API + frontend Blazor WASM hosted).
- Publica banco PostgreSQL no Cloud SQL.
- Executa migrations por Cloud Run Job.
- Mantem **Bud.Mcp fora da nuvem** (rodando local/sidecar) e apontando para a API publicada.

## Scripts adicionados

- `scripts/gcp-bootstrap.sh`: prepara infraestrutura base (rodar 1 vez por ambiente).
- `scripts/gcp-deploy.sh`: build, push, migration e deploy do servico.
- `scripts/mcp-env.sh`: resolve URL da API publicada e mostra comando para iniciar MCP local.

## O que cada script faz

### 1) `scripts/gcp-bootstrap.sh`

Automatiza:

1. Valida prerequisitos (`gcloud`) e variaveis obrigatorias.
2. Configura `gcloud` no projeto.
3. Habilita APIs: Cloud Run, Cloud SQL Admin, Artifact Registry, Secret Manager, IAM.
4. Cria (se nao existir):
   - repositorio Docker no Artifact Registry
   - service account de runtime para Cloud Run
   - instancia Cloud SQL PostgreSQL 16
   - database
   - usuario de banco
   - secrets no Secret Manager
5. Aplica IAM na service account:
   - `roles/cloudsql.client`
   - `roles/secretmanager.secretAccessor`
6. Se `DB_PASS` for informado, publica versao do secret com `ConnectionStrings__DefaultConnection` pronta para Cloud Run.
7. Se `JWT_KEY` for informado, publica versao do secret da chave JWT (com validacao minima de 32 caracteres).
8. Se `JWT_KEY` nao for informado e o secret JWT ainda estiver vazio, gera automaticamente uma chave forte e publica uma versao.

Variaveis obrigatorias:

- `PROJECT_ID`
- `REGION`

Variaveis opcionais (com default):

- `REPO_NAME=bud`
- `SQL_INSTANCE=bud-pg`
- `DB_NAME=bud`
- `DB_USER=bud_app`
- `SERVICE_ACCOUNT=bud-runner`
- `SECRET_DB_CONNECTION=bud-db-connection`
- `SECRET_JWT_KEY=bud-jwt-key`
- `DB_TIER=db-custom-1-3840`
- `DB_EDITION=ENTERPRISE`
- `DB_PASS` (recomendado)
- `JWT_KEY` (opcional; se ausente, o script gera chave segura automaticamente quando necessario)

Exemplo:

```bash
export PROJECT_ID="seu-projeto"
export REGION="us-central1"
export DB_PASS="senha-forte"
export JWT_KEY="chave-jwt-com-32-ou-mais-caracteres"

./scripts/gcp-bootstrap.sh
```

### 2) `scripts/gcp-deploy.sh`

Automatiza:

1. Valida prerequisitos (`gcloud`, `docker`) e variaveis obrigatorias.
2. Configura auth Docker para Artifact Registry.
3. Build da imagem com `docker build --target dev-web`.
4. Push da imagem para Artifact Registry.
5. Cria/atualiza Cloud Run Job de migration (`<service>-migrate`) com:
   - Cloud SQL attachment
   - secrets para connection string e JWT
6. Executa o job de migration e aguarda terminar.
7. Deploy/atualizacao do servico Cloud Run com:
   - `--allow-unauthenticated`
   - `--port 8080`
   - Cloud SQL attachment
   - secrets (`ConnectionStrings__DefaultConnection`, `Jwt__Key`)
   - env vars de runtime (`ASPNETCORE_ENVIRONMENT=Production`, etc)
8. Verifica `GET /health/live` e `GET /health/ready`.
9. Falha cedo se os secrets obrigatorios (`SECRET_DB_CONNECTION` e `SECRET_JWT_KEY`) estiverem sem versao.

Variaveis obrigatorias:

- `PROJECT_ID`
- `REGION`

Variaveis opcionais (com default):

- `REPO_NAME=bud`
- `SERVICE_NAME=bud-web`
- `SQL_INSTANCE=bud-pg`
- `DB_NAME=bud`
- `DB_USER=bud_app`
- `SERVICE_ACCOUNT=bud-runner`
- `SECRET_DB_CONNECTION=bud-db-connection`
- `SECRET_JWT_KEY=bud-jwt-key`
- `MIGRATION_JOB_NAME=<SERVICE_NAME>-migrate`
- `IMAGE_TAG=<timestamp>`

Exemplo:

```bash
export PROJECT_ID="seu-projeto"
export REGION="us-central1"

./scripts/gcp-deploy.sh
```

### 3) `scripts/mcp-env.sh`

Automatiza:

1. Recebe URL da API diretamente (`--api-url`) **ou** descobre URL do Cloud Run (`--project --region --service`).
2. Exibe `BUD_API_BASE_URL` pronta para uso.
3. Exibe comando para iniciar MCP local apontando para a API publicada.

Exemplos:

```bash
./scripts/mcp-env.sh --api-url https://bud-web-xxxxx-uc.a.run.app
```

```bash
./scripts/mcp-env.sh --project seu-projeto --region us-central1 --service bud-web
```

Comando sugerido pelo script:

```bash
BUD_API_BASE_URL=https://... dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj
```

## Passos manuais que ainda sao necessarios

Mesmo com os scripts, estes passos continuam manuais por seguranca/governanca:

1. **Conta/projeto/billing**
   - Criar projeto GCP (se ainda nao existir).
   - Vincular billing ao projeto.
   - Fazer login no `gcloud` com permissao adequada.

2. **Decisoes de acesso publico**
   - O script usa `--allow-unauthenticated` no Cloud Run.
   - Se ambiente exigir privado, ajuste esse parametro no script.

3. **Gestao de segredos sensiveis**
   - Definir `DB_PASS` forte.
   - Opcionalmente definir `JWT_KEY` forte (minimo 32 chars) no bootstrap; se nao definir, o script gera uma automaticamente.
   - Rotacao periodica de secrets (nova versao no Secret Manager).

4. **Dominio customizado e TLS gerenciado**
   - Mapear dominio no Cloud Run.
   - Configurar DNS no provedor.

5. **Politicas de producao**
   - Definir limites de escala, CPU/memoria e concorrencia conforme carga real.
   - Configurar alertas/monitoramento no Cloud Monitoring.
   - Restringir IAM (principio do menor privilegio).

6. **Aprovacoes organizacionais**
   - Revisao de seguranca/compliance antes de abrir para trafego externo.

## Fluxo recomendado (resumo)

1. Executar bootstrap uma vez:

```bash
./scripts/gcp-bootstrap.sh
```

2. Executar deploy sempre que houver release:

```bash
./scripts/gcp-deploy.sh
```

3. Rodar MCP local apontando para API publicada:

```bash
./scripts/mcp-env.sh --project <project> --region <region> --service bud-web
BUD_API_BASE_URL=<url> dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj
```

## Observacoes importantes

- O `Dockerfile` atual e orientado a desenvolvimento; por isso o deploy usa explicitamente `--target dev-web`.
- O frontend esta no projeto `Bud.Client`, mas no runtime ele e servido por `Bud.Server` (modelo hosted WASM).
- O `Bud.Mcp` atual usa transporte `stdio`; por isso ele nao entra neste deploy Cloud Run como endpoint publico HTTP.
