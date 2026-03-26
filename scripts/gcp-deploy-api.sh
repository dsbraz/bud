#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<USAGE
Uso:
  ./scripts/gcp-deploy-api.sh [opcoes]

Opcoes:
  --env-file <path>             Arquivo de variaveis (default: .env.gcp, se existir)
  --project-id <id>             PROJECT_ID
  --region <region>             REGION
  --repo-name <nome>            REPO_NAME
  --api-service-name <nome>     API_SERVICE_NAME
  --service-name <nome>         Alias legado para API_SERVICE_NAME
  --sql-instance <nome>         SQL_INSTANCE
  --db-name <nome>              DB_NAME
  --db-user <nome>              DB_USER
  --service-account <nome>      SERVICE_ACCOUNT
  --secret-db-connection <n>    SECRET_DB_CONNECTION
  --secret-jwt-key <n>          SECRET_JWT_KEY
  --migration-job-name <nome>   MIGRATION_JOB_NAME
  --skip-migration              Pular etapa de migracao (EF migrations)
  --image-tag <tag>             IMAGE_TAG
USAGE
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Erro: comando '$1' nao encontrado." >&2
    exit 1
  fi
}

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "Erro: variavel obrigatoria nao definida: $name" >&2
    exit 1
  fi
}

load_env_file() {
  local env_file="$1"
  if [[ -f "$env_file" ]]; then
    set -a
    # shellcheck disable=SC1090
    source "$env_file"
    set +a
  fi
}

job_exists() {
  local name="$1"
  gcloud run jobs describe "$name" --region "$REGION" --project "$PROJECT_ID" >/dev/null 2>&1
}

secret_has_versions() {
  local name="$1"
  [[ -n "$(gcloud secrets versions list "$name" --project "$PROJECT_ID" --limit=1 --format='value(name)' 2>/dev/null)" ]]
}

require_cmd gcloud

ENV_FILE=".env.gcp"
args=("$@")
for ((i = 0; i < ${#args[@]}; i++)); do
  if [[ "${args[$i]}" == "--env-file" ]]; then
    if (( i + 1 >= ${#args[@]} )); then
      echo "Erro: --env-file requer um valor." >&2
      exit 1
    fi
    ENV_FILE="${args[$((i + 1))]}"
  fi
done

load_env_file "$ENV_FILE"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env-file) ENV_FILE="$2"; shift 2 ;;
    --project-id) PROJECT_ID="$2"; shift 2 ;;
    --region) REGION="$2"; shift 2 ;;
    --repo-name) REPO_NAME="$2"; shift 2 ;;
    --api-service-name) API_SERVICE_NAME="$2"; shift 2 ;;
    --service-name) API_SERVICE_NAME="$2"; shift 2 ;;
    --sql-instance) SQL_INSTANCE="$2"; shift 2 ;;
    --db-name) DB_NAME="$2"; shift 2 ;;
    --db-user) DB_USER="$2"; shift 2 ;;
    --service-account) SERVICE_ACCOUNT="$2"; shift 2 ;;
    --secret-db-connection) SECRET_DB_CONNECTION="$2"; shift 2 ;;
    --secret-jwt-key) SECRET_JWT_KEY="$2"; shift 2 ;;
    --migration-job-name) MIGRATION_JOB_NAME="$2"; shift 2 ;;
    --skip-migration) SKIP_MIGRATION="true"; shift ;;
    --image-tag) IMAGE_TAG="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Parametro invalido: $1" >&2; usage; exit 1 ;;
  esac
done

require_env PROJECT_ID
require_env REGION

REPO_NAME="${REPO_NAME:-bud}"
API_SERVICE_NAME="${API_SERVICE_NAME:-bud-api}"
SQL_INSTANCE="${SQL_INSTANCE:-bud-pg}"
DB_NAME="${DB_NAME:-bud}"
DB_USER="${DB_USER:-bud_app}"
SERVICE_ACCOUNT="${SERVICE_ACCOUNT:-bud-runner}"
SECRET_DB_CONNECTION="${SECRET_DB_CONNECTION:-bud-db-connection}"
SECRET_JWT_KEY="${SECRET_JWT_KEY:-bud-jwt-key}"
MIGRATION_JOB_NAME="${MIGRATION_JOB_NAME:-${API_SERVICE_NAME}-migrate}"
SKIP_MIGRATION="${SKIP_MIGRATION:-false}"
IMAGE_TAG="${IMAGE_TAG:-$(date +%Y%m%d-%H%M%S)}"

INSTANCE_CONNECTION_NAME="${PROJECT_ID}:${REGION}:${SQL_INSTANCE}"
SERVICE_ACCOUNT_EMAIL="${SERVICE_ACCOUNT}@${PROJECT_ID}.iam.gserviceaccount.com"
IMAGE_URI="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${API_SERVICE_NAME}:${IMAGE_TAG}"

echo "==> Configurando projeto"
gcloud config set project "$PROJECT_ID" >/dev/null

echo "==> Validando secrets obrigatorios"
if ! secret_has_versions "$SECRET_DB_CONNECTION"; then
  echo "Erro: secret '$SECRET_DB_CONNECTION' nao possui versao." >&2
  echo "Execute bootstrap com DB_PASS ou publique manualmente uma versao da connection string." >&2
  exit 1
fi

if ! secret_has_versions "$SECRET_JWT_KEY"; then
  echo "Erro: secret '$SECRET_JWT_KEY' nao possui versao." >&2
  echo "Execute bootstrap novamente ou publique manualmente uma versao da chave JWT." >&2
  exit 1
fi

MIGRATE_IMAGE_URI="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${API_SERVICE_NAME}-migrate:${IMAGE_TAG}"

if [[ "$SKIP_MIGRATION" == "true" ]]; then
  echo "==> Buildando imagem da API no Cloud Build (${IMAGE_URI})"
  gcloud builds submit \
    --project "$PROJECT_ID" \
    --config "scripts/cloudbuild-backend.yaml" \
    --substitutions "_IMAGE_URI=${IMAGE_URI},_DOCKER_TARGET=prod-api" \
    ./backend
  echo "==> Migracao pulada (--skip-migration)"
else
  echo "==> Buildando imagem de migracao (${MIGRATE_IMAGE_URI})"
  gcloud builds submit \
    --project "$PROJECT_ID" \
    --config "scripts/cloudbuild-backend.yaml" \
    --substitutions "_IMAGE_URI=${MIGRATE_IMAGE_URI},_DOCKER_TARGET=prod-migrate" \
    ./backend

  echo "==> Buildando imagem da API (${IMAGE_URI})"
  gcloud builds submit \
    --project "$PROJECT_ID" \
    --config "scripts/cloudbuild-backend.yaml" \
    --substitutions "_IMAGE_URI=${IMAGE_URI},_DOCKER_TARGET=prod-api" \
    ./backend

  echo "==> Garantindo Cloud Run Job de migracao"
  if job_exists "$MIGRATION_JOB_NAME"; then
    gcloud run jobs update "$MIGRATION_JOB_NAME" \
      --project "$PROJECT_ID" \
      --region "$REGION" \
      --image "$MIGRATE_IMAGE_URI" \
      --service-account "$SERVICE_ACCOUNT_EMAIL" \
      --set-cloudsql-instances "$INSTANCE_CONNECTION_NAME" \
      --set-secrets "ConnectionStrings__DefaultConnection=${SECRET_DB_CONNECTION}:latest" \
      --set-secrets "Jwt__Key=${SECRET_JWT_KEY}:latest" \
      --set-env-vars "DOTNET_ENVIRONMENT=Production,ASPNETCORE_ENVIRONMENT=Production" \
      --max-retries 1
  else
    gcloud run jobs create "$MIGRATION_JOB_NAME" \
      --project "$PROJECT_ID" \
      --region "$REGION" \
      --image "$MIGRATE_IMAGE_URI" \
      --service-account "$SERVICE_ACCOUNT_EMAIL" \
      --set-cloudsql-instances "$INSTANCE_CONNECTION_NAME" \
      --set-secrets "ConnectionStrings__DefaultConnection=${SECRET_DB_CONNECTION}:latest" \
      --set-secrets "Jwt__Key=${SECRET_JWT_KEY}:latest" \
      --set-env-vars "DOTNET_ENVIRONMENT=Production,ASPNETCORE_ENVIRONMENT=Production" \
      --max-retries 1
  fi

  echo "==> Executando migracao"
  gcloud run jobs execute "$MIGRATION_JOB_NAME" \
    --project "$PROJECT_ID" \
    --region "$REGION" \
    --wait
fi

echo "==> Deployando API no Cloud Run"
gcloud run deploy "$API_SERVICE_NAME" \
  --project "$PROJECT_ID" \
  --region "$REGION" \
  --platform managed \
  --image "$IMAGE_URI" \
  --service-account "$SERVICE_ACCOUNT_EMAIL" \
  --port 8080 \
  --set-cloudsql-instances "$INSTANCE_CONNECTION_NAME" \
  --set-secrets "ConnectionStrings__DefaultConnection=${SECRET_DB_CONNECTION}:latest" \
  --set-secrets "Jwt__Key=${SECRET_JWT_KEY}:latest" \
  --set-env-vars "^|^ASPNETCORE_ENVIRONMENT=Production|DOTNET_ENVIRONMENT=Production|ASPNETCORE_URLS=http://0.0.0.0:8080|ASPNETCORE_FORWARDEDHEADERS_ENABLED=true|OTEL_SERVICE_NAME=Bud.Api|OTEL_RESOURCE_ATTRIBUTES=cloud.provider=gcp,cloud.platform=gcp_cloud_run|OTEL_EXPORTER_OTLP_ENDPOINT=https://telemetry.googleapis.com|GCP_PROJECT_ID=${PROJECT_ID}"

echo "==> Validando endpoints de health da API"
API_URL="$(gcloud run services describe "$API_SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"

curl --fail --silent --show-error "${API_URL}/health/live" >/dev/null
curl --fail --silent --show-error "${API_URL}/health/ready" >/dev/null

echo "==> Deploy da API concluido com sucesso"
echo "API_URL=${API_URL}"
