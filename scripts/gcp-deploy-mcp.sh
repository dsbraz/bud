#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<USAGE
Uso:
  ./scripts/gcp-deploy-mcp.sh [opcoes]

Opcoes:
  --env-file <path>             Arquivo de variaveis (default: .env.gcp, se existir)
  --project-id <id>             PROJECT_ID
  --region <region>             REGION
  --repo-name <nome>            REPO_NAME
  --mcp-service-name <nome>     MCP_SERVICE_NAME
  --web-service-name <nome>     WEB_SERVICE_NAME
  --image-tag <tag>             IMAGE_TAG
  --web-api-url <url>           WEB_API_URL (opcional)
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
    --mcp-service-name) MCP_SERVICE_NAME="$2"; shift 2 ;;
    --web-service-name) WEB_SERVICE_NAME="$2"; shift 2 ;;
    --image-tag) IMAGE_TAG="$2"; shift 2 ;;
    --web-api-url) WEB_API_URL="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Parametro invalido: $1" >&2; usage; exit 1 ;;
  esac
done

require_env PROJECT_ID
require_env REGION

REPO_NAME="${REPO_NAME:-bud}"
MCP_SERVICE_NAME="${MCP_SERVICE_NAME:-bud-mcp}"
WEB_SERVICE_NAME="${WEB_SERVICE_NAME:-bud-web}"
IMAGE_TAG="${IMAGE_TAG:-$(date +%Y%m%d-%H%M%S)}"
WEB_API_URL="${WEB_API_URL:-}"

IMAGE_URI="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${MCP_SERVICE_NAME}:${IMAGE_TAG}"

echo "==> Configurando projeto"
gcloud config set project "$PROJECT_ID" >/dev/null

if [[ -z "$WEB_API_URL" ]]; then
  echo "==> Obtendo URL da API web ($WEB_SERVICE_NAME)"
  WEB_API_URL="$(gcloud run services describe "$WEB_SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"
fi

if [[ -z "$WEB_API_URL" ]]; then
  echo "Erro: nao foi possivel resolver WEB_API_URL. Defina WEB_API_URL manualmente." >&2
  exit 1
fi

echo "==> Buildando imagem MCP no Cloud Build (${IMAGE_URI})"
gcloud builds submit \
  --project "$PROJECT_ID" \
  --config "scripts/cloudbuild-image.yaml" \
  --substitutions "_IMAGE_URI=${IMAGE_URI},_DOCKER_TARGET=prod-mcp" \
  .

echo "==> Deployando MCP no Cloud Run"
gcloud run deploy "$MCP_SERVICE_NAME" \
  --project "$PROJECT_ID" \
  --region "$REGION" \
  --platform managed \
  --image "$IMAGE_URI" \
  --allow-unauthenticated \
  --port 8080 \
  --set-env-vars "DOTNET_ENVIRONMENT=Production,ASPNETCORE_ENVIRONMENT=Production,ASPNETCORE_URLS=http://0.0.0.0:8080,BUD_API_BASE_URL=${WEB_API_URL}"

echo "==> Validando MCP"
MCP_URL="$(gcloud run services describe "$MCP_SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"

curl --fail --silent --show-error "${MCP_URL}/health/live" >/dev/null
curl --fail --silent --show-error "${MCP_URL}/health/ready" >/dev/null

echo "==> Deploy MCP concluido com sucesso"
echo "MCP_URL=${MCP_URL}"
echo "WEB_API_URL=${WEB_API_URL}"
