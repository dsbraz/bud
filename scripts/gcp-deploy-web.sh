#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<USAGE
Uso:
  ./scripts/gcp-deploy-web.sh [opcoes]

Opcoes:
  --env-file <path>                  Arquivo de variaveis (default: .env.gcp, se existir)
  --project-id <id>                  PROJECT_ID
  --region <region>                  REGION
  --repo-name <nome>                 REPO_NAME
  --web-service-name <nome>          WEB_SERVICE_NAME
  --api-service-name <nome>          API_SERVICE_NAME
  --api-url <url>                    API_URL (opcional)
  --web-api-url <url>                Alias legado para API_URL
  --image-tag <tag>                  IMAGE_TAG
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
    --web-service-name) WEB_SERVICE_NAME="$2"; shift 2 ;;
    --api-service-name) API_SERVICE_NAME="$2"; shift 2 ;;
    --api-url) API_URL="$2"; shift 2 ;;
    --web-api-url) API_URL="$2"; shift 2 ;;
    --image-tag) IMAGE_TAG="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Parametro invalido: $1" >&2; usage; exit 1 ;;
  esac
done

require_env PROJECT_ID
require_env REGION

REPO_NAME="${REPO_NAME:-bud}"
WEB_SERVICE_NAME="${WEB_SERVICE_NAME:-bud-web}"
API_SERVICE_NAME="${API_SERVICE_NAME:-bud-api}"
IMAGE_TAG="${IMAGE_TAG:-$(date +%Y%m%d-%H%M%S)}"
API_URL="${API_URL:-}"

IMAGE_URI="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${WEB_SERVICE_NAME}:${IMAGE_TAG}"

echo "==> Configurando projeto"
gcloud config set project "$PROJECT_ID" >/dev/null

if [[ -z "$API_URL" ]]; then
  echo "==> Obtendo URL da API ($API_SERVICE_NAME)"
  API_URL="$(gcloud run services describe "$API_SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"
fi

if [[ -z "$API_URL" ]]; then
  echo "Erro: nao foi possivel resolver API_URL. Defina --api-url manualmente." >&2
  exit 1
fi

echo "==> Buildando imagem do web (Next.js) no Cloud Build (${IMAGE_URI})"
gcloud builds submit \
  --project "$PROJECT_ID" \
  --config "scripts/cloudbuild-frontend.yaml" \
  --substitutions "_IMAGE_URI=${IMAGE_URI}" \
  ./frontend

echo "==> Deployando web (Next.js) no Cloud Run"
gcloud run deploy "$WEB_SERVICE_NAME" \
  --project "$PROJECT_ID" \
  --region "$REGION" \
  --platform managed \
  --image "$IMAGE_URI" \
  --port 3000 \
  --set-env-vars "NEXT_PUBLIC_API_URL=${API_URL}"

echo "==> Validando web"
WEB_URL="$(gcloud run services describe "$WEB_SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"

curl --fail --silent --show-error "${WEB_URL}/" >/dev/null

echo "==> Deploy do web concluido com sucesso"
echo "WEB_URL=${WEB_URL}"
echo "API_URL=${API_URL}"
