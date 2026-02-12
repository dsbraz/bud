#!/usr/bin/env bash
set -euo pipefail

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Erro: comando '$1' nao encontrado." >&2
    exit 1
  fi
}

require_cmd bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "==> Etapa 1/2: deploy do Bud.Server (web)"
"${SCRIPT_DIR}/gcp-deploy-web.sh"

echo "==> Etapa 2/2: deploy do Bud.Mcp"
"${SCRIPT_DIR}/gcp-deploy-mcp.sh"

echo "==> Deploy completo finalizado com sucesso"
