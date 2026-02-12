#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<USAGE
Uso:
  ./scripts/gcp-deploy-all.sh [opcoes]

Opcoes:
  --env-file <path>             Arquivo de variaveis (default: .env.gcp, se existir)
  --project-id <id>             PROJECT_ID
  --region <region>             REGION
  --web-api-url <url>           WEB_API_URL (opcional; usado no deploy do MCP)
USAGE
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Erro: comando '$1' nao encontrado." >&2
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

require_cmd bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

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
    --web-api-url) WEB_API_URL="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Parametro invalido: $1" >&2; usage; exit 1 ;;
  esac
done

if [[ -z "${PROJECT_ID:-}" || -z "${REGION:-}" ]]; then
  echo "Erro: informe PROJECT_ID e REGION via .env.gcp ou parametros --project-id/--region." >&2
  exit 1
fi

export PROJECT_ID REGION
if [[ -n "${WEB_API_URL:-}" ]]; then
  export WEB_API_URL
fi

echo "==> Etapa 1/2: deploy do Bud.Server (web)"
"${SCRIPT_DIR}/gcp-deploy-web.sh"

echo "==> Etapa 2/2: deploy do Bud.Mcp"
"${SCRIPT_DIR}/gcp-deploy-mcp.sh"

echo "==> Deploy completo finalizado com sucesso"
