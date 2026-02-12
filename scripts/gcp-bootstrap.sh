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

secret_has_versions() {
  local name="$1"
  [[ -n "$(gcloud secrets versions list "$name" --project "$PROJECT_ID" --limit=1 --format='value(name)' 2>/dev/null)" ]]
}

generate_secure_jwt_key() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -base64 48 | tr -d '\n'
    return
  fi

  LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 64
}

require_cmd gcloud

require_env PROJECT_ID
require_env REGION

REPO_NAME="${REPO_NAME:-bud}"
SQL_INSTANCE="${SQL_INSTANCE:-bud-pg}"
DB_NAME="${DB_NAME:-bud}"
DB_USER="${DB_USER:-bud_app}"
SERVICE_ACCOUNT="${SERVICE_ACCOUNT:-bud-runner}"
SECRET_DB_CONNECTION="${SECRET_DB_CONNECTION:-bud-db-connection}"
SECRET_JWT_KEY="${SECRET_JWT_KEY:-bud-jwt-key}"
DB_TIER="${DB_TIER:-db-custom-1-3840}"
DB_EDITION="${DB_EDITION:-ENTERPRISE}"

INSTANCE_CONNECTION_NAME="${PROJECT_ID}:${REGION}:${SQL_INSTANCE}"
SERVICE_ACCOUNT_EMAIL="${SERVICE_ACCOUNT}@${PROJECT_ID}.iam.gserviceaccount.com"

echo "==> Configurando projeto"
gcloud config set project "$PROJECT_ID" >/dev/null

echo "==> Habilitando APIs"
gcloud services enable \
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
  if [[ -z "${DB_PASS:-}" ]]; then
    echo "Erro: DB_PASS nao informado para criar usuario de banco '$DB_USER'." >&2
    echo "Defina DB_PASS e rode novamente." >&2
    exit 1
  fi
  gcloud sql users create "$DB_USER" --instance "$SQL_INSTANCE" --password "$DB_PASS"
fi

echo "==> Garantindo secrets"
ensure_secret "$SECRET_DB_CONNECTION"
ensure_secret "$SECRET_JWT_KEY"

if [[ -n "${DB_PASS:-}" ]]; then
  DB_CONNECTION="Host=/cloudsql/${INSTANCE_CONNECTION_NAME};Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS};SSL Mode=Disable"
  ensure_secret_value "$SECRET_DB_CONNECTION" "$DB_CONNECTION"
else
  echo "Aviso: DB_PASS nao definido. Secret '$SECRET_DB_CONNECTION' foi criado sem versao." >&2
  echo "Manual: adicione uma versao com a connection string completa." >&2
fi

if [[ -n "${JWT_KEY:-}" ]]; then
  if [[ "${#JWT_KEY}" -lt 32 ]]; then
    echo "Erro: JWT_KEY deve ter no minimo 32 caracteres." >&2
    exit 1
  fi
  ensure_secret_value "$SECRET_JWT_KEY" "$JWT_KEY"
else
  if secret_has_versions "$SECRET_JWT_KEY"; then
    echo "Secret JWT ja possui versao. Mantendo valor atual."
  else
    GENERATED_JWT_KEY="$(generate_secure_jwt_key)"
    ensure_secret_value "$SECRET_JWT_KEY" "$GENERATED_JWT_KEY"
    echo "JWT_KEY nao informado. Foi gerada chave segura automaticamente."
  fi
fi

echo "==> Bootstrap concluido"
echo "PROJECT_ID=$PROJECT_ID"
echo "REGION=$REGION"
echo "REPO_NAME=$REPO_NAME"
echo "SQL_INSTANCE=$SQL_INSTANCE"
echo "INSTANCE_CONNECTION_NAME=$INSTANCE_CONNECTION_NAME"
echo "SERVICE_ACCOUNT_EMAIL=$SERVICE_ACCOUNT_EMAIL"
echo "SECRET_DB_CONNECTION=$SECRET_DB_CONNECTION"
echo "SECRET_JWT_KEY=$SECRET_JWT_KEY"
