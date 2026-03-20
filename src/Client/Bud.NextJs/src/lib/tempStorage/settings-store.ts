/**
 * Settings Store — Local-first persistence for AI and platform settings
 *
 * This store follows the same architectural patterns as config-store, people-store,
 * missions-store, and surveys-store for consistency and future backend integration.
 */

import { today, addDays, toIsoDate } from "./seed-utils";

// ─── Types ───

export type ToneId = "profissional" | "direto" | "empatico" | "coach" | "energetico";
export type ProactivityLevel = "minimum" | "moderate" | "default" | "maximum";
export type TransparencyMode = "always" | "on_demand";
export type DataSourceType = "notion" | "confluence" | "gdrive" | "sharepoint" | "jira" | "sheets" | "custom";
export type DataSourceStatus = "connected" | "disconnected" | "error";
export type KnowledgeDocType = "file" | "text" | "url";

export interface CustomToneRecord {
  id: string;
  orgId: string;
  label: string;
  description: string;
  example: string;
  createdAt: string;
  updatedAt: string;
}

export interface DataSourceRecord {
  id: string;
  orgId: string;
  type: DataSourceType;
  name: string;
  description: string;
  status: DataSourceStatus;
  enabled: boolean;
  url: string;
  lastSync: string | null;
  accessSummary: string | null;
  tools: string[];
  connectedAt: string;
  updatedAt: string;
}

export interface KnowledgeDocRecord {
  id: string;
  orgId: string;
  name: string;
  type: KnowledgeDocType;
  size: string | null;
  content: string | null;
  addedAt: string;
}

export interface SuggestionTypes {
  oneOnOnePrep: boolean;
  coachingTips: boolean;
  teamAlerts: boolean;
  reviewDrafts: boolean;
  okrSuggestions: boolean;
  dailyBriefing: boolean;
}

export interface OrgAiSettings {
  orgId: string;

  // Tab 1: Assistente
  toneId: ToneId;
  language: string;
  respectWorkingHours: boolean;
  workingHoursStart: string;
  workingHoursEnd: string;

  // Tab 2: Sugestões
  proactivityLevel: ProactivityLevel;
  suggestionTypes: SuggestionTypes;
  transparencyMode: TransparencyMode;

  // Tab 3: Fontes de dados (stored separately for normalization)
  // Tab 4: Avançado
  customInstructions: string;
  biasDetectionEnabled: boolean;
  dataSharingEnabled: boolean;
  monthlyUsageLimit: number | null;

  // Meta
  createdAt: string;
  updatedAt: string;
}

export interface SettingsStoreSnapshot {
  schemaVersion: number;
  updatedAt: string;
  aiSettingsByOrg: Record<string, OrgAiSettings>;
  dataSourcesByOrg: Record<string, DataSourceRecord[]>;
  customTonesByOrg: Record<string, CustomToneRecord[]>;
  knowledgeDocsByOrg: Record<string, KnowledgeDocRecord[]>;
}

// ─── Constants ───

const STORAGE_KEY = "bud.saas.settings-store";
const STORE_SCHEMA_VERSION = 2;
const DEFAULT_ORG_ID = "org-1";

// ─── Tone Presets (System) ───

export interface TonePreset {
  id: ToneId;
  label: string;
  description: string;
  example: string;
  iconName: string;
}

export const TONE_PRESETS: TonePreset[] = [
  {
    id: "profissional",
    label: "Profissional",
    description: "Comunicação clara e formal, ideal para contextos corporativos tradicionais",
    example: "Identifiquei que o engajamento do time de Design caiu 12% neste mês. Recomendo agendar um check-in com a equipe para investigar possíveis causas.",
    iconName: "Handshake",
  },
  {
    id: "direto",
    label: "Direto e objetivo",
    description: "Vai direto ao ponto, sem rodeios. Para gestores que valorizam eficiência",
    example: "Engajamento de Design caiu 12%. Ação sugerida: check-in com o time esta semana.",
    iconName: "Target",
  },
  {
    id: "empatico",
    label: "Empático e acolhedor",
    description: "Tom humano e cuidadoso, bom para culturas que priorizam bem-estar",
    example: "Notei que o time de Design pode estar passando por um momento desafiador — o engajamento caiu um pouco este mês. Que tal conversar com eles para entender como estão se sentindo?",
    iconName: "ChatCircle",
  },
  {
    id: "coach",
    label: "Coach",
    description: "Faz perguntas ao invés de dar respostas, estimulando reflexão do gestor",
    example: "O engajamento do Design caiu 12%. O que você acha que pode ter mudado recentemente? Existe algum fator que vale investigar com o time?",
    iconName: "Brain",
  },
  {
    id: "energetico",
    label: "Energético e motivacional",
    description: "Tom positivo e encorajador, que celebra conquistas e inspira ação",
    example: "Opa, o engajamento do Design merece atenção — caiu 12%, mas nada que uma boa conversa não resolva! Que tal agendar um papo com o time?",
    iconName: "Lightning",
  },
];

// ─── Data Source Catalog ───

export interface DataSourceCatalogItem {
  type: DataSourceType;
  name: string;
  description: string;
  tools: string[];
  requiresPro: boolean;
}

export const DATA_SOURCE_CATALOG: DataSourceCatalogItem[] = [
  {
    type: "notion",
    name: "Notion",
    description: "Acesse páginas, bases de dados e documentos do Notion como contexto para a IA.",
    tools: ["Ler páginas", "Buscar documentos", "Listar bases"],
    requiresPro: false,
  },
  {
    type: "confluence",
    name: "Confluence",
    description: "Conecte sua wiki do Confluence para que a IA acesse políticas, processos e documentação interna.",
    tools: ["Ler páginas", "Buscar conteúdo", "Espaços"],
    requiresPro: false,
  },
  {
    type: "gdrive",
    name: "Google Drive",
    description: "Acesse documentos, planilhas e apresentações do Google Drive como referência para a IA.",
    tools: ["Ler arquivos", "Buscar documentos", "Listar pastas"],
    requiresPro: false,
  },
  {
    type: "sharepoint",
    name: "SharePoint",
    description: "Conecte o SharePoint para acessar documentos e políticas organizacionais.",
    tools: ["Ler documentos", "Buscar conteúdo", "Sites"],
    requiresPro: false,
  },
  {
    type: "jira",
    name: "Jira",
    description: "Atualize o progresso de OKRs automaticamente quando tarefas são concluídas no Jira.",
    tools: ["Sync de progresso", "OKR automático", "Tarefas"],
    requiresPro: false,
  },
  {
    type: "sheets",
    name: "Google Sheets",
    description: "Importe e exporte dados de performance, OKRs e relatórios para planilhas.",
    tools: ["Exportar relatórios", "Importar dados", "Sync automático"],
    requiresPro: false,
  },
  {
    type: "custom",
    name: "Fonte personalizada",
    description: "Conecte qualquer API REST compatível para estender as capacidades da IA.",
    tools: ["Customizável"],
    requiresPro: true,
  },
];

// ─── Utility Functions ───

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") return structuredClone(value);
  return JSON.parse(JSON.stringify(value)) as T;
}

function nowIso(): string {
  return new Date().toISOString();
}

function generateId(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

// ─── Default Settings Factory ───

function createDefaultAiSettings(orgId: string): OrgAiSettings {
  const now = nowIso();
  return {
    orgId,
    toneId: "profissional",
    language: "pt-BR",
    respectWorkingHours: true,
    workingHoursStart: "08:00",
    workingHoursEnd: "19:00",
    proactivityLevel: "default",
    suggestionTypes: {
      oneOnOnePrep: true,
      coachingTips: true,
      teamAlerts: true,
      reviewDrafts: true,
      okrSuggestions: false,
      dailyBriefing: true,
    },
    transparencyMode: "always",
    customInstructions: "Use linguagem acessível e evite jargões técnicos. Sempre sugira próximos passos concretos ao final de cada análise.",
    biasDetectionEnabled: true,
    dataSharingEnabled: false,
    monthlyUsageLimit: null,
    createdAt: now,
    updatedAt: now,
  };
}

function createSeedDataSources(orgId: string): DataSourceRecord[] {
  const now = nowIso();
  return [
    {
      id: "ds-1",
      orgId,
      type: "notion",
      name: "Base de conhecimento interna",
      description: "Documentos e políticas da empresa via Notion.",
      status: "connected",
      enabled: true,
      url: "https://notion.so/acme/kb",
      lastSync: now,
      accessSummary: "3 bases de dados",
      tools: ["Ler páginas", "Buscar documentos"],
      connectedAt: now,
      updatedAt: now,
    },
    {
      id: "ds-2",
      orgId,
      type: "sheets",
      name: "Dados de performance",
      description: "Métricas e indicadores de performance do time.",
      status: "connected",
      enabled: true,
      url: "https://docs.google.com/spreadsheets/d/...",
      lastSync: now,
      accessSummary: "2 planilhas",
      tools: ["Métricas", "Histórico", "Comparativos"],
      connectedAt: now,
      updatedAt: now,
    },
  ];
}

function createSeedKnowledgeDocs(orgId: string): KnowledgeDocRecord[] {
  // Generate dates relative to today (docs added in the past)
  const now = today();
  const doc1Date = toIsoDate(addDays(now, -60)); // Added 2 months ago
  const doc2Date = toIsoDate(addDays(now, -45)); // Added 1.5 months ago
  const doc3Date = toIsoDate(addDays(now, -30)); // Added 1 month ago

  return [
    {
      id: "doc-1",
      orgId,
      name: "Manual de cultura ACME",
      type: "file",
      size: "2.4 MB",
      content: null,
      addedAt: doc1Date,
    },
    {
      id: "doc-2",
      orgId,
      name: "Política de avaliação de desempenho",
      type: "file",
      size: "1.1 MB",
      content: null,
      addedAt: doc2Date,
    },
    {
      id: "doc-3",
      orgId,
      name: "Descrição e valores da empresa",
      type: "text",
      size: null,
      content: "A ACME LTDA é uma empresa de tecnologia com 350 colaboradores, fundada em 2015. Valores: inovação, transparência, colaboração.",
      addedAt: doc3Date,
    },
  ];
}

// ─── Seed Snapshot ───

function getSeedSnapshot(): SettingsStoreSnapshot {
  const now = nowIso();
  const orgIds = ["org-1", "brq", "initech"];

  const aiSettingsByOrg: Record<string, OrgAiSettings> = {};
  const dataSourcesByOrg: Record<string, DataSourceRecord[]> = {};
  const customTonesByOrg: Record<string, CustomToneRecord[]> = {};
  const knowledgeDocsByOrg: Record<string, KnowledgeDocRecord[]> = {};

  for (const orgId of orgIds) {
    aiSettingsByOrg[orgId] = createDefaultAiSettings(orgId);
    customTonesByOrg[orgId] = [];

    // Only seed data for default org
    if (orgId === DEFAULT_ORG_ID) {
      dataSourcesByOrg[orgId] = createSeedDataSources(orgId);
      knowledgeDocsByOrg[orgId] = createSeedKnowledgeDocs(orgId);
    } else {
      dataSourcesByOrg[orgId] = [];
      knowledgeDocsByOrg[orgId] = [];
    }
  }

  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: now,
    aiSettingsByOrg,
    dataSourcesByOrg,
    customTonesByOrg,
    knowledgeDocsByOrg,
  };
}

// ─── Sanitization Functions ───

function sanitizeAiSettings(raw: unknown, orgId: string): OrgAiSettings | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Partial<OrgAiSettings>;

  const defaults = createDefaultAiSettings(orgId);
  const now = nowIso();

  const validTones: ToneId[] = ["profissional", "direto", "empatico", "coach", "energetico"];
  const validProactivity: ProactivityLevel[] = ["minimum", "moderate", "default", "maximum"];
  const validTransparency: TransparencyMode[] = ["always", "on_demand"];

  return {
    orgId,
    toneId: validTones.includes(item.toneId as ToneId) ? (item.toneId as ToneId) : defaults.toneId,
    language: typeof item.language === "string" ? item.language : defaults.language,
    respectWorkingHours: typeof item.respectWorkingHours === "boolean" ? item.respectWorkingHours : defaults.respectWorkingHours,
    workingHoursStart: typeof item.workingHoursStart === "string" ? item.workingHoursStart : defaults.workingHoursStart,
    workingHoursEnd: typeof item.workingHoursEnd === "string" ? item.workingHoursEnd : defaults.workingHoursEnd,
    proactivityLevel: validProactivity.includes(item.proactivityLevel as ProactivityLevel)
      ? (item.proactivityLevel as ProactivityLevel)
      : defaults.proactivityLevel,
    suggestionTypes: {
      oneOnOnePrep: typeof item.suggestionTypes?.oneOnOnePrep === "boolean" ? item.suggestionTypes.oneOnOnePrep : defaults.suggestionTypes.oneOnOnePrep,
      coachingTips: typeof item.suggestionTypes?.coachingTips === "boolean" ? item.suggestionTypes.coachingTips : defaults.suggestionTypes.coachingTips,
      teamAlerts: typeof item.suggestionTypes?.teamAlerts === "boolean" ? item.suggestionTypes.teamAlerts : defaults.suggestionTypes.teamAlerts,
      reviewDrafts: typeof item.suggestionTypes?.reviewDrafts === "boolean" ? item.suggestionTypes.reviewDrafts : defaults.suggestionTypes.reviewDrafts,
      okrSuggestions: typeof item.suggestionTypes?.okrSuggestions === "boolean" ? item.suggestionTypes.okrSuggestions : defaults.suggestionTypes.okrSuggestions,
      dailyBriefing: typeof item.suggestionTypes?.dailyBriefing === "boolean" ? item.suggestionTypes.dailyBriefing : defaults.suggestionTypes.dailyBriefing,
    },
    transparencyMode: validTransparency.includes(item.transparencyMode as TransparencyMode)
      ? (item.transparencyMode as TransparencyMode)
      : defaults.transparencyMode,
    customInstructions: typeof item.customInstructions === "string" ? item.customInstructions : defaults.customInstructions,
    biasDetectionEnabled: typeof item.biasDetectionEnabled === "boolean" ? item.biasDetectionEnabled : defaults.biasDetectionEnabled,
    dataSharingEnabled: typeof item.dataSharingEnabled === "boolean" ? item.dataSharingEnabled : defaults.dataSharingEnabled,
    monthlyUsageLimit: typeof item.monthlyUsageLimit === "number" ? item.monthlyUsageLimit : null,
    createdAt: typeof item.createdAt === "string" ? item.createdAt : now,
    updatedAt: typeof item.updatedAt === "string" ? item.updatedAt : now,
  };
}

function sanitizeDataSource(raw: unknown, orgId: string): DataSourceRecord | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Partial<DataSourceRecord>;
  if (!item.id || !item.type || !item.name) return null;

  const now = nowIso();
  const validTypes: DataSourceType[] = ["notion", "confluence", "gdrive", "sharepoint", "jira", "sheets", "custom"];
  const validStatuses: DataSourceStatus[] = ["connected", "disconnected", "error"];

  return {
    id: String(item.id),
    orgId,
    type: validTypes.includes(item.type as DataSourceType) ? (item.type as DataSourceType) : "custom",
    name: String(item.name),
    description: typeof item.description === "string" ? item.description : "",
    status: validStatuses.includes(item.status as DataSourceStatus) ? (item.status as DataSourceStatus) : "disconnected",
    enabled: typeof item.enabled === "boolean" ? item.enabled : false,
    url: typeof item.url === "string" ? item.url : "",
    lastSync: typeof item.lastSync === "string" ? item.lastSync : null,
    accessSummary: typeof item.accessSummary === "string" ? item.accessSummary : null,
    tools: Array.isArray(item.tools) ? item.tools.map(String) : [],
    connectedAt: typeof item.connectedAt === "string" ? item.connectedAt : now,
    updatedAt: typeof item.updatedAt === "string" ? item.updatedAt : now,
  };
}

function sanitizeCustomTone(raw: unknown, orgId: string): CustomToneRecord | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Partial<CustomToneRecord>;
  if (!item.id || !item.label) return null;

  const now = nowIso();

  return {
    id: String(item.id),
    orgId,
    label: String(item.label),
    description: typeof item.description === "string" ? item.description : "",
    example: typeof item.example === "string" ? item.example : "",
    createdAt: typeof item.createdAt === "string" ? item.createdAt : now,
    updatedAt: typeof item.updatedAt === "string" ? item.updatedAt : now,
  };
}

function sanitizeKnowledgeDoc(raw: unknown, orgId: string): KnowledgeDocRecord | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Partial<KnowledgeDocRecord>;
  if (!item.id || !item.name) return null;

  const validTypes: KnowledgeDocType[] = ["file", "text", "url"];

  return {
    id: String(item.id),
    orgId,
    name: String(item.name),
    type: validTypes.includes(item.type as KnowledgeDocType) ? (item.type as KnowledgeDocType) : "text",
    size: typeof item.size === "string" ? item.size : null,
    content: typeof item.content === "string" ? item.content : null,
    addedAt: typeof item.addedAt === "string" ? item.addedAt : new Date().toISOString().split("T")[0]!,
  };
}

// ─── Migration ───

function migrateSnapshot(raw: Partial<SettingsStoreSnapshot> | null): SettingsStoreSnapshot {
  const seed = getSeedSnapshot();
  if (!raw || typeof raw !== "object") return seed;

  const aiSettingsByOrg: Record<string, OrgAiSettings> = {};
  const dataSourcesByOrg: Record<string, DataSourceRecord[]> = {};
  const customTonesByOrg: Record<string, CustomToneRecord[]> = {};
  const knowledgeDocsByOrg: Record<string, KnowledgeDocRecord[]> = {};

  // Migrate AI settings
  if (raw.aiSettingsByOrg && typeof raw.aiSettingsByOrg === "object") {
    for (const [orgId, settings] of Object.entries(raw.aiSettingsByOrg)) {
      const sanitized = sanitizeAiSettings(settings, orgId);
      if (sanitized) {
        aiSettingsByOrg[orgId] = sanitized;
      }
    }
  }

  // Ensure default org has settings
  if (!aiSettingsByOrg[DEFAULT_ORG_ID]) {
    aiSettingsByOrg[DEFAULT_ORG_ID] = seed.aiSettingsByOrg[DEFAULT_ORG_ID]!;
  }

  // Migrate data sources
  if (raw.dataSourcesByOrg && typeof raw.dataSourcesByOrg === "object") {
    for (const [orgId, sources] of Object.entries(raw.dataSourcesByOrg)) {
      if (Array.isArray(sources)) {
        dataSourcesByOrg[orgId] = sources
          .map((s) => sanitizeDataSource(s, orgId))
          .filter((s): s is DataSourceRecord => s !== null);
      }
    }
  }

  // Ensure default org has data sources
  if (!dataSourcesByOrg[DEFAULT_ORG_ID]) {
    dataSourcesByOrg[DEFAULT_ORG_ID] = seed.dataSourcesByOrg[DEFAULT_ORG_ID] ?? [];
  }

  // Migrate custom tones
  if (raw.customTonesByOrg && typeof raw.customTonesByOrg === "object") {
    for (const [orgId, tones] of Object.entries(raw.customTonesByOrg)) {
      if (Array.isArray(tones)) {
        customTonesByOrg[orgId] = tones
          .map((t) => sanitizeCustomTone(t, orgId))
          .filter((t): t is CustomToneRecord => t !== null);
      }
    }
  }

  // Ensure default org has custom tones array
  if (!customTonesByOrg[DEFAULT_ORG_ID]) {
    customTonesByOrg[DEFAULT_ORG_ID] = [];
  }

  // Migrate knowledge docs
  if (raw.knowledgeDocsByOrg && typeof raw.knowledgeDocsByOrg === "object") {
    for (const [orgId, docs] of Object.entries(raw.knowledgeDocsByOrg)) {
      if (Array.isArray(docs)) {
        knowledgeDocsByOrg[orgId] = docs
          .map((d) => sanitizeKnowledgeDoc(d, orgId))
          .filter((d): d is KnowledgeDocRecord => d !== null);
      }
    }
  }

  // Ensure default org has knowledge docs
  if (!knowledgeDocsByOrg[DEFAULT_ORG_ID]) {
    knowledgeDocsByOrg[DEFAULT_ORG_ID] = seed.knowledgeDocsByOrg[DEFAULT_ORG_ID] ?? [];
  }

  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: typeof raw.updatedAt === "string" ? raw.updatedAt : seed.updatedAt,
    aiSettingsByOrg,
    dataSourcesByOrg,
    customTonesByOrg,
    knowledgeDocsByOrg,
  };
}

// ─── Public API ───

export function loadSettingsSnapshot(): SettingsStoreSnapshot {
  if (typeof window === "undefined") {
    return getSeedSnapshot();
  }

  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    const seed = getSeedSnapshot();
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(seed));
    return seed;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<SettingsStoreSnapshot>;
    const migrated = migrateSnapshot(parsed);
    if ((parsed.schemaVersion ?? 0) !== STORE_SCHEMA_VERSION) {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(migrated));
    }
    return migrated;
  } catch {
    const seed = getSeedSnapshot();
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(seed));
    return seed;
  }
}

export function saveSettingsSnapshot(
  snapshot: Omit<SettingsStoreSnapshot, "schemaVersion" | "updatedAt">,
): SettingsStoreSnapshot {
  const next: SettingsStoreSnapshot = {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: nowIso(),
    aiSettingsByOrg: cloneDeep(snapshot.aiSettingsByOrg),
    dataSourcesByOrg: cloneDeep(snapshot.dataSourcesByOrg),
    customTonesByOrg: cloneDeep(snapshot.customTonesByOrg),
    knowledgeDocsByOrg: cloneDeep(snapshot.knowledgeDocsByOrg),
  };

  if (typeof window !== "undefined") {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  }

  return next;
}

export function resetSettingsSnapshot(): SettingsStoreSnapshot {
  const seed = getSeedSnapshot();
  if (typeof window !== "undefined") {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(seed));
  }
  return seed;
}

// ─── Helper Exports ───

export { generateId, createDefaultAiSettings };
