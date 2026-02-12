#!/usr/bin/env bash
set -euo pipefail

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

require_cmd gcloud
require_cmd docker

require_env PROJECT_ID
require_env REGION

REPO_NAME="${REPO_NAME:-bud}"
MCP_SERVICE_NAME="${MCP_SERVICE_NAME:-bud-mcp-dev}"
WEB_SERVICE_NAME="${WEB_SERVICE_NAME:-bud-web}"
IMAGE_TAG="${IMAGE_TAG:-$(date +%Y%m%d-%H%M%S)}"
WEB_API_URL="${WEB_API_URL:-}"

IMAGE_URI="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${MCP_SERVICE_NAME}:${IMAGE_TAG}"

echo "==> Configurando projeto"
gcloud config set project "$PROJECT_ID" >/dev/null

echo "==> Configurando Docker auth"
gcloud auth configure-docker "${REGION}-docker.pkg.dev" --quiet

if [[ -z "$WEB_API_URL" ]]; then
  echo "==> Obtendo URL da API web ($WEB_SERVICE_NAME)"
  WEB_API_URL="$(gcloud run services describe "$WEB_SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"
fi

if [[ -z "$WEB_API_URL" ]]; then
  echo "Erro: nao foi possivel resolver WEB_API_URL. Defina WEB_API_URL manualmente." >&2
  exit 1
fi

echo "==> Buildando imagem MCP (${IMAGE_URI})"
docker build --target dev-mcp-web -t "$IMAGE_URI" .

echo "==> Enviando imagem MCP"
docker push "$IMAGE_URI"

echo "==> Deployando MCP no Cloud Run"
gcloud run deploy "$MCP_SERVICE_NAME" \
  --project "$PROJECT_ID" \
  --region "$REGION" \
  --platform managed \
  --image "$IMAGE_URI" \
  --allow-unauthenticated \
  --port 8080 \
  --set-env-vars "DOTNET_ENVIRONMENT=Development,ASPNETCORE_ENVIRONMENT=Development,ASPNETCORE_URLS=http://0.0.0.0:8080,BUD_API_BASE_URL=${WEB_API_URL}"

echo "==> Validando MCP"
MCP_URL="$(gcloud run services describe "$MCP_SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"

curl --fail --silent --show-error "${MCP_URL}/health/live" >/dev/null
curl --fail --silent --show-error "${MCP_URL}/health/ready" >/dev/null

echo "==> Deploy MCP concluido com sucesso"
echo "MCP_URL=${MCP_URL}"
echo "WEB_API_URL=${WEB_API_URL}"
