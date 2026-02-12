#!/usr/bin/env bash
set -euo pipefail

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Erro: comando '$1' nao encontrado." >&2
    exit 1
  fi
}

usage() {
  cat <<USAGE
Uso:
  ./scripts/mcp-env.sh --api-url <url>
  ./scripts/mcp-env.sh --project <id> --region <region> --service <cloud-run-service>

Exemplos:
  ./scripts/mcp-env.sh --api-url https://bud-web-xxxxx-uc.a.run.app
  ./scripts/mcp-env.sh --project meu-projeto --region us-central1 --service bud-web
USAGE
}

API_URL=""
PROJECT_ID=""
REGION=""
SERVICE_NAME="bud-web"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --api-url)
      API_URL="$2"
      shift 2
      ;;
    --project)
      PROJECT_ID="$2"
      shift 2
      ;;
    --region)
      REGION="$2"
      shift 2
      ;;
    --service)
      SERVICE_NAME="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Parametro invalido: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ -z "$API_URL" ]]; then
  require_cmd gcloud
  if [[ -z "$PROJECT_ID" || -z "$REGION" ]]; then
    echo "Erro: informe --api-url ou (--project e --region)." >&2
    exit 1
  fi

  API_URL="$(gcloud run services describe "$SERVICE_NAME" --project "$PROJECT_ID" --region "$REGION" --format='value(status.url)')"
  if [[ -z "$API_URL" ]]; then
    echo "Erro: nao foi possivel obter URL do service '$SERVICE_NAME'." >&2
    exit 1
  fi
fi

echo "BUD_API_BASE_URL=$API_URL"
echo "Para iniciar o MCP local com essa API:"
echo "BUD_API_BASE_URL=$API_URL dotnet run --project src/Bud.Mcp/Bud.Mcp.csproj"
