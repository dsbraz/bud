#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<USAGE
Uso:
  ./scripts/gcp-bootstrap.sh [opcoes]

Opcoes:
  --env-file <path>            Arquivo de variaveis (default: .env.gcp, se existir)
  --project-id <id>            PROJECT_ID
  --region <region>            REGION
  --db-pass <senha>            DB_PASS
  --jwt-key <chave>            JWT_KEY
  --repo-name <nome>           REPO_NAME
  --sql-instance <nome>        SQL_INSTANCE
  --db-name <nome>             DB_NAME
  --db-user <nome>             DB_USER
  --service-account <nome>     SERVICE_ACCOUNT
  --secret-db-connection <n>   SECRET_DB_CONNECTION
  --secret-jwt-key <n>         SECRET_JWT_KEY
  --db-tier <tier>             DB_TIER
  --db-edition <ed>            DB_EDITION
  --billing-account-id <id>    BILLING_ACCOUNT_ID (opcional; para vincular billing via CLI)
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

escape_env_value() {
  local value="$1"
  value="${value//\\/\\\\}"
  value="${value//\"/\\\"}"
  printf '%s' "$value"
}

upsert_env_var() {
  local file="$1"
  local key="$2"
  local value="$3"
  local escaped
  escaped="$(escape_env_value "$value")"

  if [[ ! -f "$file" ]]; then
    printf '%s="%s"\n' "$key" "$escaped" >"$file"
    return
  fi

  local tmp
  tmp="$(mktemp)"
  awk -v key="$key" -v value="$escaped" '
    BEGIN { found = 0 }
    $0 ~ "^" key "=" {
      print key "=\"" value "\""
      found = 1
      next
    }
    { print }
    END {
      if (!found) {
        print key "=\"" value "\""
      }
    }
  ' "$file" >"$tmp"
  mv "$tmp" "$file"
}

prompt_if_empty() {
  local var_name="$1"
  local prompt_text="$2"
  local default_value="${3:-}"
  if [[ -n "${!var_name:-}" ]]; then
    return
  fi

  local value
  if [[ -n "$default_value" ]]; then
    read -r -p "$prompt_text [$default_value]: " value
    value="${value:-$default_value}"
  else
    read -r -p "$prompt_text: " value
  fi

  if [[ -z "$value" ]]; then
    echo "Erro: valor obrigatorio nao informado para $var_name." >&2
    exit 1
  fi

  printf -v "$var_name" '%s' "$value"
}

generate_secure_password() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -base64 32 | tr -d '\n'
    return
  fi

  LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 48
}

ensure_project_exists() {
  if gcloud projects describe "$PROJECT_ID" >/dev/null 2>&1; then
    return
  fi

  echo "Projeto '$PROJECT_ID' nao encontrado."
  read -r -p "Deseja criar o projeto agora via CLI? [y/N]: " create_project
  if [[ "$create_project" =~ ^[Yy]$ ]]; then
    local project_name="${PROJECT_NAME:-$PROJECT_ID}"
    gcloud projects create "$PROJECT_ID" --name="$project_name"
  else
    echo "Erro: projeto inexistente. Crie o projeto e rode novamente." >&2
    exit 1
  fi
}

ensure_billing_linked() {
  local billing_enabled
  billing_enabled="$(gcloud beta billing projects describe "$PROJECT_ID" --format='value(billingEnabled)' 2>/dev/null || true)"

  if [[ "$billing_enabled" == "True" ]]; then
    return
  fi

  if [[ -z "${BILLING_ACCOUNT_ID:-}" ]]; then
    echo "Billing nao vinculado ao projeto '$PROJECT_ID'."
    echo "Informe BILLING_ACCOUNT_ID para vincular automaticamente."
    echo "Contas disponiveis:"
    gcloud beta billing accounts list --format='table(name,displayName,open)' || true
    read -r -p "BILLING_ACCOUNT_ID (ou Enter para pular): " BILLING_ACCOUNT_ID
  fi

  if [[ -n "${BILLING_ACCOUNT_ID:-}" ]]; then
    gcloud beta billing projects link "$PROJECT_ID" --billing-account="$BILLING_ACCOUNT_ID"
  else
    echo "Aviso: billing nao foi vinculado automaticamente. Alguns recursos podem falhar na criacao." >&2
  fi
}

persist_effective_env() {
  local file="$1"
  upsert_env_var "$file" "PROJECT_ID" "$PROJECT_ID"
  upsert_env_var "$file" "REGION" "$REGION"
  upsert_env_var "$file" "DB_PASS" "$DB_PASS"
  upsert_env_var "$file" "JWT_KEY" "$JWT_KEY"
  upsert_env_var "$file" "REPO_NAME" "$REPO_NAME"
  upsert_env_var "$file" "SERVICE_NAME" "${SERVICE_NAME:-bud-web}"
  upsert_env_var "$file" "MCP_SERVICE_NAME" "${MCP_SERVICE_NAME:-bud-mcp}"
  upsert_env_var "$file" "WEB_SERVICE_NAME" "${WEB_SERVICE_NAME:-bud-web}"
  upsert_env_var "$file" "SQL_INSTANCE" "$SQL_INSTANCE"
  upsert_env_var "$file" "DB_NAME" "$DB_NAME"
  upsert_env_var "$file" "DB_USER" "$DB_USER"
  upsert_env_var "$file" "SERVICE_ACCOUNT" "$SERVICE_ACCOUNT"
  upsert_env_var "$file" "SECRET_DB_CONNECTION" "$SECRET_DB_CONNECTION"
  upsert_env_var "$file" "SECRET_JWT_KEY" "$SECRET_JWT_KEY"
  upsert_env_var "$file" "DB_TIER" "$DB_TIER"
  upsert_env_var "$file" "DB_EDITION" "$DB_EDITION"
  if [[ -n "${BILLING_ACCOUNT_ID:-}" ]]; then
    upsert_env_var "$file" "BILLING_ACCOUNT_ID" "$BILLING_ACCOUNT_ID"
  fi
}

secret_exists() {
  local name="$1"
  gcloud secrets describe "$name" --project "$PROJECT_ID" >/dev/null 2>&1
}

ensure_secret() {
  local name="$1"
  if secret_exists "$name"; then
    echo "Secret ja existe: $name"
  else
    gcloud secrets create "$name" --replication-policy="automatic" --project "$PROJECT_ID"
    echo "Secret criado: $name"
  fi
}

ensure_secret_value() {
  local name="$1"
  local value="$2"
  printf '%s' "$value" | gcloud secrets versions add "$name" --data-file=- --project "$PROJECT_ID" >/dev/null
  echo "Nova versao publicada para secret: $name"
}

generate_secure_jwt_key() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -base64 48 | tr -d '\n'
    return
  fi

  LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 64
}

normalize_secret_name() {
  local value="$1"
  value="$(printf '%s' "$value" | tr '[:upper:]' '[:lower:]')"
  value="$(printf '%s' "$value" | sed -E 's/[^a-z0-9-]+/-/g; s/^-+//; s/-+$//; s/-{2,}/-/g')"
  printf '%s' "$value"
}

generate_secret_name() {
  local suffix="$1"
  local base
  base="$(normalize_secret_name "$PROJECT_ID")"
  if [[ -z "$base" ]]; then
    base="bud"
  fi
  printf '%s-%s' "$base" "$suffix"
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
    --db-pass) DB_PASS="$2"; shift 2 ;;
    --jwt-key) JWT_KEY="$2"; shift 2 ;;
    --repo-name) REPO_NAME="$2"; shift 2 ;;
    --sql-instance) SQL_INSTANCE="$2"; shift 2 ;;
    --db-name) DB_NAME="$2"; shift 2 ;;
    --db-user) DB_USER="$2"; shift 2 ;;
    --service-account) SERVICE_ACCOUNT="$2"; shift 2 ;;
    --secret-db-connection) SECRET_DB_CONNECTION="$2"; shift 2 ;;
    --secret-jwt-key) SECRET_JWT_KEY="$2"; shift 2 ;;
    --db-tier) DB_TIER="$2"; shift 2 ;;
    --db-edition) DB_EDITION="$2"; shift 2 ;;
    --billing-account-id) BILLING_ACCOUNT_ID="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Parametro invalido: $1" >&2; usage; exit 1 ;;
  esac
done

prompt_if_empty PROJECT_ID "PROJECT_ID" "getbud-co-dev"
prompt_if_empty REGION "REGION" "us-central1"

REPO_NAME="${REPO_NAME:-bud}"
SQL_INSTANCE="${SQL_INSTANCE:-bud-pg}"
DB_NAME="${DB_NAME:-bud}"
DB_USER="${DB_USER:-bud_app}"
SERVICE_ACCOUNT="${SERVICE_ACCOUNT:-bud-runner}"
DB_TIER="${DB_TIER:-db-custom-1-3840}"
DB_EDITION="${DB_EDITION:-ENTERPRISE}"

if [[ -z "${SECRET_DB_CONNECTION:-}" ]]; then
  SECRET_DB_CONNECTION="$(generate_secret_name "db-connection")"
  echo "SECRET_DB_CONNECTION nao informado. Nome gerado automaticamente: $SECRET_DB_CONNECTION"
fi

if [[ -z "${SECRET_JWT_KEY:-}" ]]; then
  SECRET_JWT_KEY="$(generate_secret_name "jwt-key")"
  echo "SECRET_JWT_KEY nao informado. Nome gerado automaticamente: $SECRET_JWT_KEY"
fi

if [[ -z "${DB_PASS:-}" ]]; then
  DB_PASS="$(generate_secure_password)"
  echo "DB_PASS nao informado. Senha forte gerada automaticamente."
fi

if [[ -z "${JWT_KEY:-}" ]]; then
  JWT_KEY="$(generate_secure_jwt_key)"
  echo "JWT_KEY nao informado. Chave forte gerada automaticamente."
fi

if [[ "${#JWT_KEY}" -lt 32 ]]; then
  echo "Erro: JWT_KEY deve ter no minimo 32 caracteres." >&2
  exit 1
fi

persist_effective_env "$ENV_FILE"
echo "Variaveis efetivas gravadas em '$ENV_FILE'."

INSTANCE_CONNECTION_NAME="${PROJECT_ID}:${REGION}:${SQL_INSTANCE}"
SERVICE_ACCOUNT_EMAIL="${SERVICE_ACCOUNT}@${PROJECT_ID}.iam.gserviceaccount.com"

ensure_project_exists

echo "==> Configurando projeto"
gcloud config set project "$PROJECT_ID" >/dev/null

ensure_billing_linked

echo "==> Habilitando APIs"
gcloud services enable \
  cloudbuild.googleapis.com \
  run.googleapis.com \
  sqladmin.googleapis.com \
  artifactregistry.googleapis.com \
  secretmanager.googleapis.com \
  iam.googleapis.com

echo "==> Garantindo Artifact Registry"
if gcloud artifacts repositories describe "$REPO_NAME" --location "$REGION" >/dev/null 2>&1; then
  echo "Repositorio ja existe: $REPO_NAME"
else
  gcloud artifacts repositories create "$REPO_NAME" \
    --repository-format=docker \
    --location="$REGION" \
    --description="Docker repository for Bud"
fi

echo "==> Garantindo service account"
if gcloud iam service-accounts describe "$SERVICE_ACCOUNT_EMAIL" >/dev/null 2>&1; then
  echo "Service account ja existe: $SERVICE_ACCOUNT_EMAIL"
else
  gcloud iam service-accounts create "$SERVICE_ACCOUNT" \
    --display-name="Bud Cloud Run runtime"
fi

echo "==> Aplicando papeis na service account"
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${SERVICE_ACCOUNT_EMAIL}" \
  --role="roles/cloudsql.client" >/dev/null

gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${SERVICE_ACCOUNT_EMAIL}" \
  --role="roles/secretmanager.secretAccessor" >/dev/null

echo "==> Garantindo Cloud SQL"
if gcloud sql instances describe "$SQL_INSTANCE" >/dev/null 2>&1; then
  echo "Cloud SQL instance ja existe: $SQL_INSTANCE"
else
  gcloud sql instances create "$SQL_INSTANCE" \
    --database-version=POSTGRES_16 \
    --tier="$DB_TIER" \
    --edition="$DB_EDITION" \
    --region="$REGION" \
    --storage-type=SSD \
    --storage-size=20GB \
    --backup-start-time=03:00
fi

echo "==> Garantindo database"
if gcloud sql databases describe "$DB_NAME" --instance "$SQL_INSTANCE" >/dev/null 2>&1; then
  echo "Database ja existe: $DB_NAME"
else
  gcloud sql databases create "$DB_NAME" --instance "$SQL_INSTANCE"
fi

echo "==> Garantindo usuario de banco"
if gcloud sql users list --instance "$SQL_INSTANCE" --format='value(name)' | grep -Fxq "$DB_USER"; then
  echo "Usuario de banco ja existe: $DB_USER"
else
  gcloud sql users create "$DB_USER" --instance "$SQL_INSTANCE" --password "$DB_PASS"
fi

echo "==> Garantindo secrets"
ensure_secret "$SECRET_DB_CONNECTION"
ensure_secret "$SECRET_JWT_KEY"

DB_CONNECTION="Host=/cloudsql/${INSTANCE_CONNECTION_NAME};Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS};SSL Mode=Disable"
ensure_secret_value "$SECRET_DB_CONNECTION" "$DB_CONNECTION"
ensure_secret_value "$SECRET_JWT_KEY" "$JWT_KEY"

echo "==> Bootstrap concluido"
echo "PROJECT_ID=$PROJECT_ID"
echo "REGION=$REGION"
echo "REPO_NAME=$REPO_NAME"
echo "SQL_INSTANCE=$SQL_INSTANCE"
echo "INSTANCE_CONNECTION_NAME=$INSTANCE_CONNECTION_NAME"
echo "SERVICE_ACCOUNT_EMAIL=$SERVICE_ACCOUNT_EMAIL"
echo "SECRET_DB_CONNECTION=$SECRET_DB_CONNECTION"
echo "SECRET_JWT_KEY=$SECRET_JWT_KEY"
