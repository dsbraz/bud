#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<USAGE
Uso:
  ./scripts/gcp-deploy-web.sh [opcoes]

Opcoes:
  --env-file <path>             Arquivo de variaveis (default: .env.gcp, se existir)
  --project-id <id>             PROJECT_ID
  --region <region>             REGION
  --repo-name <nome>            REPO_NAME
  --service-name <nome>         SERVICE_NAME
  --sql-instance <nome>         SQL_INSTANCE
  --db-name <nome>              DB_NAME
  --db-user <nome>              DB_USER
  --service-account <nome>      SERVICE_ACCOUNT
  --secret-db-connection <n>    SECRET_DB_CONNECTION
  --secret-jwt-key <n>          SECRET_JWT_KEY
  --migration-job-name <nome>   MIGRATION_JOB_NAME
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
require_cmd docker

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
    --service-name) SERVICE_NAME="$2"; shift 2 ;;
    --sql-instance) SQL_INSTANCE="$2"; shift 2 ;;
    --db-name) DB_NAME="$2"; shift 2 ;;
    --db-user) DB_USER="$2"; shift 2 ;;
    --service-account) SERVICE_ACCOUNT="$2"; shift 2 ;;
    --secret-db-connection) SECRET_DB_CONNECTION="$2"; shift 2 ;;
    --secret-jwt-key) SECRET_JWT_KEY="$2"; shift 2 ;;
    --migration-job-name) MIGRATION_JOB_NAME="$2"; shift 2 ;;
    --image-tag) IMAGE_TAG="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Parametro invalido: $1" >&2; usage; exit 1 ;;
  esac
done

require_env PROJECT_ID
require_env REGION

REPO_NAME="${REPO_NAME:-bud}"
SERVICE_NAME="${SERVICE_NAME:-bud-web}"
SQL_INSTANCE="${SQL_INSTANCE:-bud-pg}"
DB_NAME="${DB_NAME:-bud}"
DB_USER="${DB_USER:-bud_app}"
SERVICE_ACCOUNT="${SERVICE_ACCOUNT:-bud-runner}"
SECRET_DB_CONNECTION="${SECRET_DB_CONNECTION:-bud-db-connection}"
SECRET_JWT_KEY="${SECRET_JWT_KEY:-bud-jwt-key}"
MIGRATION_JOB_NAME="${MIGRATION_JOB_NAME:-${SERVICE_NAME}-migrate}"
IMAGE_TAG="${IMAGE_TAG:-$(date +%Y%m%d-%H%M%S)}"

INSTANCE_CONNECTION_NAME="${PROJECT_ID}:${REGION}:${SQL_INSTANCE}"
SERVICE_ACCOUNT_EMAIL="${SERVICE_ACCOUNT}@${PROJECT_ID}.iam.gserviceaccount.com"
IMAGE_URI="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${SERVICE_NAME}:${IMAGE_TAG}"

echo "==> Configurando projeto"
gcloud config set project "$PROJECT_ID" >/dev/null

echo "==> Configurando Docker auth"
gcloud auth configure-docker "${REGION}-docker.pkg.dev" --quiet

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

echo "==> Buildando imagem (${IMAGE_URI})"
docker build --target dev-web -t "$IMAGE_URI" .

echo "==> Enviando imagem"
docker push "$IMAGE_URI"

echo "==> Garantindo Cloud Run Job de migracao"
MIGRATION_COMMAND='dotnet tool install --tool-path /tmp/tools dotnet-ef --version 10.0.2 >/dev/null && /tmp/tools/dotnet-ef database update --project src/Bud.Server'

if job_exists "$MIGRATION_JOB_NAME"; then
  gcloud run jobs update "$MIGRATION_JOB_NAME" \
    --project "$PROJECT_ID" \
    --region "$REGION" \
    --image "$IMAGE_URI" \
    --service-account "$SERVICE_ACCOUNT_EMAIL" \
    --set-cloudsql-instances "$INSTANCE_CONNECTION_NAME" \
    --set-secrets "ConnectionStrings__DefaultConnection=${SECRET_DB_CONNECTION}:latest" \
    --set-secrets "Jwt__Key=${SECRET_JWT_KEY}:latest" \
    --set-env-vars "DOTNET_ENVIRONMENT=Production,ASPNETCORE_ENVIRONMENT=Production,Jwt__Issuer=bud-api,Jwt__Audience=bud-api,GlobalAdminSettings__Email=admin@getbud.co,GlobalAdminSettings__OrganizationName=getbud.co" \
    --command "bash" \
    --args "-lc,${MIGRATION_COMMAND}" \
    --max-retries 1
else
  gcloud run jobs create "$MIGRATION_JOB_NAME" \
    --project "$PROJECT_ID" \
    --region "$REGION" \
    --image "$IMAGE_URI" \
    --service-account "$SERVICE_ACCOUNT_EMAIL" \
    --set-cloudsql-instances "$INSTANCE_CONNECTION_NAME" \
    --set-secrets "ConnectionStrings__DefaultConnection=${SECRET_DB_CONNECTION}:latest" \
    --set-secrets "Jwt__Key=${SECRET_JWT_KEY}:latest" \
    --set-env-vars "DOTNET_ENVIRONMENT=Production,ASPNETCORE_ENVIRONMENT=Production,Jwt__Issuer=bud-api,Jwt__Audience=bud-api,GlobalAdminSettings__Email=admin@getbud.co,GlobalAdminSettings__OrganizationName=getbud.co" \
    --command "bash" \
    --args "-lc,${MIGRATION_COMMAND}" \
    --max-retries 1
fi

echo "==> Executando migracao"
gcloud run jobs execute "$MIGRATION_JOB_NAME" \
  --project "$PROJECT_ID" \
  --region "$REGION" \
  --wait

echo "==> Deployando servico Cloud Run"
gcloud run deploy "$SERVICE_NAME" \
  --project "$PROJECT_ID" \
  --region "$REGION" \
  --platform managed \
  --image "$IMAGE_URI" \
  --service-account "$SERVICE_ACCOUNT_EMAIL" \
  --allow-unauthenticated \
  --port 8080 \
  --set-cloudsql-instances "$INSTANCE_CONNECTION_NAME" \
  --set-secrets "ConnectionStrings__DefaultConnection=${SECRET_DB_CONNECTION}:latest" \
  --set-secrets "Jwt__Key=${SECRET_JWT_KEY}:latest" \
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Production,DOTNET_ENVIRONMENT=Production,ASPNETCORE_URLS=http://0.0.0.0:8080,ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,Jwt__Issuer=bud-api,Jwt__Audience=bud-api,GlobalAdminSettings__Email=admin@getbud.co,GlobalAdminSettings__OrganizationName=getbud.co"

echo "==> Validando endpoints de health"
SERVICE_URL="$(gcloud run services describe "$SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"

curl --fail --silent --show-error "${SERVICE_URL}/health/live" >/dev/null
curl --fail --silent --show-error "${SERVICE_URL}/health/ready" >/dev/null

echo "==> Deploy concluido com sucesso"
echo "SERVICE_URL=${SERVICE_URL}"
