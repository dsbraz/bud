/**
 * Integrations Store
 * Local-first storage for integration connections and configurations
 */

// ─── Date Helpers ───

/**
 * Generate ISO timestamp for X minutes ago (for seed data)
 */
function minutesAgo(minutes: number): string {
  const date = new Date();
  date.setMinutes(date.getMinutes() - minutes);
  return date.toISOString();
}

/**
 * Generate ISO timestamp for X hours ago (for seed data)
 */
function hoursAgo(hours: number): string {
  const date = new Date();
  date.setHours(date.getHours() - hours);
  return date.toISOString();
}

// ─── Types ───

export type IntegrationStatus = "connected" | "disconnected" | "error";
export type IntegrationCategory = "communication" | "calendar" | "hris" | "pm" | "auth" | "api";

export interface IntegrationRecord {
  id: string;
  name: string;
  description: string;
  category: IntegrationCategory;
  iconId: string;
  iconBg: string;
  iconColor: string;
  status: IntegrationStatus;
  enabled: boolean;
  popular: boolean;
  lastSync: string | null;
  features: string[];
  connectedAt: string | null;
  config: Record<string, unknown>;
}

export interface IntegrationsStoreSnapshot {
  schemaVersion: number;
  updatedAt: string;
  integrations: IntegrationRecord[];
}

const STORAGE_KEY_PREFIX = "bud.saas.integrations-store";
const STORE_SCHEMA_VERSION = 2;
const DEFAULT_ORG_ID = "org-1";

function getStorageKey(orgId: string): string {
  return `${STORAGE_KEY_PREFIX}:${orgId}`;
}

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") {
    return structuredClone(value);
  }
  return JSON.parse(JSON.stringify(value)) as T;
}

/**
 * Seed integrations - all available integrations with their default state
 */
const SEED_INTEGRATIONS: IntegrationRecord[] = [
  {
    id: "slack",
    name: "Slack",
    description: "Envie nudges, feedbacks, check-ins e reconhecimentos diretamente nos canais do Slack. Comandos como /bud feedback e /bud okr permitem acoes rapidas sem sair do Slack.",
    category: "communication",
    iconId: "SlackLogo",
    iconBg: "var(--color-yellow-100)",
    iconColor: "var(--color-yellow-700)",
    status: "connected",
    enabled: true,
    popular: true,
    lastSync: minutesAgo(2),
    features: ["Slash commands", "Notificacoes", "Nudges automaticos", "Check-ins"],
    connectedAt: "2026-01-15T10:30:00Z",
    config: {},
  },
  {
    id: "teams",
    name: "Microsoft Teams",
    description: "Integre notificacoes, lembretes e comandos rapidos com o Microsoft Teams. Bot nativo para acoes diretas no chat.",
    category: "communication",
    iconId: "MicrosoftTeamsLogo",
    iconBg: "var(--color-neutral-100)",
    iconColor: "var(--color-neutral-700)",
    status: "disconnected",
    enabled: false,
    popular: true,
    lastSync: null,
    features: ["Bot nativo", "Notificacoes", "Nudges automaticos", "Check-ins"],
    connectedAt: null,
    config: {},
  },
  {
    id: "whatsapp",
    name: "WhatsApp Business",
    description: "Envie lembretes e nudges via WhatsApp para gestores. Ideal para equipes que priorizam mobile.",
    category: "communication",
    iconId: "WhatsappLogo",
    iconBg: "var(--color-green-100)",
    iconColor: "var(--color-green-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Lembretes", "Nudges", "Notificacoes"],
    connectedAt: null,
    config: {},
  },
  {
    id: "email",
    name: "E-mail (SMTP)",
    description: "Configure o servidor de e-mail para notificacoes personalizadas, digests semanais e resumos para executivos.",
    category: "communication",
    iconId: "Envelope",
    iconBg: "var(--color-caramel-100)",
    iconColor: "var(--color-caramel-700)",
    status: "connected",
    enabled: true,
    popular: false,
    lastSync: minutesAgo(15),
    features: ["Digest semanal", "Notificacoes", "Relatorios"],
    connectedAt: "2026-01-10T08:00:00Z",
    config: {},
  },
  {
    id: "google-calendar",
    name: "Google Calendar",
    description: "Detecte automaticamente 1:1s agendadas e envie briefings 30 minutos antes da reuniao com contexto completo do liderado.",
    category: "calendar",
    iconId: "CalendarBlank",
    iconBg: "var(--color-green-100)",
    iconColor: "var(--color-green-700)",
    status: "connected",
    enabled: true,
    popular: true,
    lastSync: minutesAgo(5),
    features: ["Deteccao de 1:1s", "Briefings automaticos", "Sync bidirecional"],
    connectedAt: "2026-01-12T14:00:00Z",
    config: {},
  },
  {
    id: "outlook",
    name: "Microsoft Outlook",
    description: "Sincronize agendas do Outlook para briefings automaticos de 1:1 e deteccao de reunioes recorrentes.",
    category: "calendar",
    iconId: "MicrosoftOutlookLogo",
    iconBg: "var(--color-neutral-100)",
    iconColor: "var(--color-neutral-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Deteccao de 1:1s", "Briefings automaticos", "Sync bidirecional"],
    connectedAt: null,
    config: {},
  },
  {
    id: "gupy",
    name: "Gupy",
    description: "Importe dados de colaboradores e estrutura organizacional diretamente do Gupy para configuracao rapida.",
    category: "hris",
    iconId: "Database",
    iconBg: "var(--color-orange-100)",
    iconColor: "var(--color-orange-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Importar colaboradores", "Estrutura hierarquica", "Sync automatico"],
    connectedAt: null,
    config: {},
  },
  {
    id: "totvs",
    name: "TOTVS RM / Protheus",
    description: "Conecte com TOTVS RM ou Protheus para sincronizar dados de colaboradores, cargos e departamentos.",
    category: "hris",
    iconId: "Database",
    iconBg: "var(--color-wine-100)",
    iconColor: "var(--color-wine-700)",
    status: "disconnected",
    enabled: false,
    popular: true,
    lastSync: null,
    features: ["Importar colaboradores", "Cargos e departamentos", "Sync automatico"],
    connectedAt: null,
    config: {},
  },
  {
    id: "sap",
    name: "SAP SuccessFactors",
    description: "Sincronize dados de colaboradores com o SAP SuccessFactors. Ideal para empresas enterprise.",
    category: "hris",
    iconId: "Database",
    iconBg: "var(--color-neutral-100)",
    iconColor: "var(--color-neutral-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Importar colaboradores", "Estrutura hierarquica", "Sync automatico"],
    connectedAt: null,
    config: {},
  },
  {
    id: "sheets",
    name: "Google Sheets",
    description: "Importe e exporte dados de performance, OKRs e relatorios para planilhas do Google Sheets.",
    category: "hris",
    iconId: "GoogleLogo",
    iconBg: "var(--color-green-100)",
    iconColor: "var(--color-green-700)",
    status: "connected",
    enabled: true,
    popular: false,
    lastSync: hoursAgo(1),
    features: ["Exportar relatorios", "Importar dados", "Sync automatico"],
    connectedAt: "2026-02-01T09:00:00Z",
    config: {},
  },
  {
    id: "csv",
    name: "Importacao CSV",
    description: "Importe dados de colaboradores via planilha CSV. Baixe o template, preencha e faca o upload para configuracao rapida.",
    category: "hris",
    iconId: "Upload",
    iconBg: "var(--color-caramel-100)",
    iconColor: "var(--color-caramel-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Template pronto", "Validacao automatica", "Importacao em lote"],
    connectedAt: null,
    config: {},
  },
  {
    id: "jira",
    name: "Jira",
    description: "Atualize o progresso de OKRs automaticamente quando tarefas sao concluidas no Jira. Correlacione delivery com execucao estrategica.",
    category: "pm",
    iconId: "GitBranch",
    iconBg: "var(--color-neutral-100)",
    iconColor: "var(--color-neutral-700)",
    status: "disconnected",
    enabled: false,
    popular: true,
    lastSync: null,
    features: ["Sync de progresso", "OKR automatico", "Correlacao de tarefas"],
    connectedAt: null,
    config: {},
  },
  {
    id: "asana",
    name: "Asana",
    description: "Conecte projetos do Asana para acompanhar progresso de OKRs com base na conclusao de tarefas.",
    category: "pm",
    iconId: "GitBranch",
    iconBg: "var(--color-orange-100)",
    iconColor: "var(--color-orange-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Sync de progresso", "OKR automatico", "Projetos vinculados"],
    connectedAt: null,
    config: {},
  },
  {
    id: "powerbi",
    name: "Power BI",
    description: "Conecte dashboards do Power BI com dados de performance, engajamento e progresso de OKRs da plataforma.",
    category: "pm",
    iconId: "ChartBar",
    iconBg: "var(--color-yellow-100)",
    iconColor: "var(--color-yellow-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Dashboards", "Dados em tempo real", "Relatorios customizados"],
    connectedAt: null,
    config: {},
  },
  {
    id: "google-sso",
    name: "Google SSO",
    description: "Login unico com contas Google corporativas. Simplifique o acesso para toda a organizacao com um clique.",
    category: "auth",
    iconId: "GoogleLogo",
    iconBg: "var(--color-green-100)",
    iconColor: "var(--color-green-700)",
    status: "connected",
    enabled: true,
    popular: true,
    lastSync: "always_active",
    features: ["Login com Google", "Provisionamento automatico", "MFA"],
    connectedAt: "2026-01-01T00:00:00Z",
    config: {},
  },
  {
    id: "microsoft-sso",
    name: "Microsoft SSO / Azure AD",
    description: "Login unico com contas Microsoft corporativas e Azure Active Directory. Suporte a SAML 2.0.",
    category: "auth",
    iconId: "ShieldCheck",
    iconBg: "var(--color-neutral-100)",
    iconColor: "var(--color-neutral-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Login com Microsoft", "SAML 2.0", "Provisionamento automatico"],
    connectedAt: null,
    config: {},
  },
  {
    id: "saml",
    name: "SAML 2.0 Customizado",
    description: "Configure qualquer provedor de identidade compativel com SAML 2.0 para login unico corporativo.",
    category: "auth",
    iconId: "ShieldCheck",
    iconBg: "var(--color-wine-100)",
    iconColor: "var(--color-wine-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Provedor customizado", "SSO corporativo", "Certificado X.509"],
    connectedAt: null,
    config: {},
  },
  {
    id: "api",
    name: "REST API",
    description: "Acesse a API da plataforma para integracoes customizadas. Documentacao completa com exemplos e SDKs.",
    category: "api",
    iconId: "Key",
    iconBg: "var(--color-orange-100)",
    iconColor: "var(--color-orange-700)",
    status: "connected",
    enabled: true,
    popular: false,
    lastSync: "always_active",
    features: ["API RESTful", "Documentacao", "Rate limiting", "SDKs"],
    connectedAt: "2026-01-01T00:00:00Z",
    config: {},
  },
  {
    id: "webhooks",
    name: "Webhooks",
    description: "Receba notificacoes em tempo real quando eventos ocorrerem na plataforma. Configure endpoints customizados.",
    category: "api",
    iconId: "WebhooksLogo",
    iconBg: "var(--color-caramel-100)",
    iconColor: "var(--color-caramel-700)",
    status: "disconnected",
    enabled: false,
    popular: false,
    lastSync: null,
    features: ["Eventos em tempo real", "Retry automatico", "Logs de entrega"],
    connectedAt: null,
    config: {},
  },
];

function getSeedSnapshot(): IntegrationsStoreSnapshot {
  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: new Date().toISOString(),
    integrations: cloneDeep(SEED_INTEGRATIONS),
  };
}

function sanitizeIntegrations(rawIntegrations: unknown): IntegrationRecord[] {
  if (!Array.isArray(rawIntegrations)) return [];

  return rawIntegrations
    .filter((item) => !!item && typeof item === "object")
    .map((item) => {
      const record = item as Partial<IntegrationRecord>;
      if (!record.id || !record.name || !record.category) {
        return null;
      }

      return {
        id: String(record.id),
        name: String(record.name),
        description: String(record.description ?? ""),
        category: record.category as IntegrationCategory,
        iconId: String(record.iconId ?? "Plugs"),
        iconBg: String(record.iconBg ?? "var(--color-neutral-100)"),
        iconColor: String(record.iconColor ?? "var(--color-neutral-700)"),
        status: (record.status ?? "disconnected") as IntegrationStatus,
        enabled: Boolean(record.enabled),
        popular: Boolean(record.popular),
        lastSync: record.lastSync ?? null,
        features: Array.isArray(record.features) ? record.features.map(String) : [],
        connectedAt: record.connectedAt ?? null,
        config: record.config && typeof record.config === "object" ? record.config : {},
      };
    })
    .filter((record): record is IntegrationRecord => !!record);
}

function mergeWithSeed(rawIntegrations: IntegrationRecord[]): IntegrationRecord[] {
  const seedIntegrations = cloneDeep(SEED_INTEGRATIONS);
  const rawById = new Map(rawIntegrations.map((i) => [i.id, i]));

  // Merge: keep user state for existing integrations, add any missing seed integrations
  return seedIntegrations.map((seed) => {
    const existing = rawById.get(seed.id);
    if (!existing) return seed;

    // Preserve user's connection state but use seed's static data
    return {
      ...seed,
      status: existing.status,
      enabled: existing.enabled,
      lastSync: existing.lastSync,
      connectedAt: existing.connectedAt,
      config: existing.config,
    };
  });
}

function migrateSnapshot(raw: Partial<IntegrationsStoreSnapshot> | null): IntegrationsStoreSnapshot {
  const seed = getSeedSnapshot();
  if (!raw || typeof raw !== "object") {
    return seed;
  }

  const integrations = mergeWithSeed(sanitizeIntegrations(raw.integrations));

  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: typeof raw.updatedAt === "string" ? raw.updatedAt : seed.updatedAt,
    integrations,
  };
}

export function loadIntegrationsSnapshot(orgId = DEFAULT_ORG_ID): IntegrationsStoreSnapshot {
  if (typeof window === "undefined") {
    return getSeedSnapshot();
  }

  const storageKey = getStorageKey(orgId);
  const raw = window.localStorage.getItem(storageKey);
  if (!raw) {
    return getSeedSnapshot();
  }

  try {
    const parsed = JSON.parse(raw) as Partial<IntegrationsStoreSnapshot>;
    return migrateSnapshot(parsed);
  } catch {
    console.warn("[integrations-store] Failed to parse stored data, using seed");
    return getSeedSnapshot();
  }
}

export function saveIntegrationsSnapshot(
  snapshot: IntegrationsStoreSnapshot,
  orgId = DEFAULT_ORG_ID,
): void {
  if (typeof window === "undefined") return;

  const storageKey = getStorageKey(orgId);
  const toSave: IntegrationsStoreSnapshot = {
    ...snapshot,
    updatedAt: new Date().toISOString(),
  };

  window.localStorage.setItem(storageKey, JSON.stringify(toSave));
}

export function resetIntegrationsSnapshot(orgId = DEFAULT_ORG_ID): IntegrationsStoreSnapshot {
  const seed = getSeedSnapshot();
  saveIntegrationsSnapshot(seed, orgId);
  return seed;
}
