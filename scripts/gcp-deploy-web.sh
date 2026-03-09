#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
echo "Aviso: gcp-deploy-web.sh agora delega para gcp-deploy-api.sh." >&2
exec "${SCRIPT_DIR}/gcp-deploy-api.sh" "$@"
