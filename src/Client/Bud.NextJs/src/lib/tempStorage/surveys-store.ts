import type {
  Recurrence,
  SurveyCategory,
  SurveyStatus,
  SurveyType,
  SurveyWizardState,
  WizardQuestion,
  WizardSection,
} from "@/types/survey/survey";
import { SURVEY_TEMPLATES } from "@/presentation/surveys/templates/surveyTemplates";
import {
  today,
  addDays,
  addWeeks,
  addMonths,
  toIsoDate,
  startOfQuarter,
  endOfQuarter,
  getQuarter,
} from "@/lib/tempStorage/seed-utils";

export interface SurveyListItemData {
  id: string;
  templateId: string | null;
  name: string;
  type: SurveyType;
  category: SurveyCategory;
  status: SurveyStatus;
  startDate: string;
  endDate: string;
  ownerIds: string[];
  managerIds: string[];
  tagIds: string[];
  cycleId: string | null;
  totalRecipients: number;
  totalResponses: number;
  completionRate: number;
  createdAt: string;
}

export interface SurveyLocalRecord {
  id: string;
  listItem: SurveyListItemData;
  wizardState: SurveyWizardState | null;
  createdAt: string;
  updatedAt: string;
}

export interface SurveyTemplateRecord {
  id: string;
  type: SurveyType;
  name: string;
  subtitle: string;
  category: SurveyCategory;
  isSystem: boolean;
  isArchived: boolean;
  sections: WizardSection[];
  questions: WizardQuestion[];
  defaultConfig: {
    isAnonymous: boolean;
    recurrence: Recurrence | null;
    aiPrefillOkrs: boolean;
    aiPrefillFeedback: boolean;
    aiBiasDetection: boolean;
  };
  createdAt: string;
  updatedAt: string;
}

export interface SurveySubmissionRecord {
  id: string;
  surveyId: string;
  respondentKey: string;
  answers: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
  submittedAt: string;
}

export interface SurveysStoreSnapshot {
  schemaVersion: number;
  updatedAt: string;
  records: SurveyLocalRecord[];
  templates: SurveyTemplateRecord[];
  submissions: SurveySubmissionRecord[];
}

const STORAGE_KEY_PREFIX = "bud.saas.surveys-store";
const LEGACY_STORAGE_KEY = "bud.saas.surveys-store";
const STORE_SCHEMA_VERSION = 5;
const DEFAULT_ORG_ID = "org-1";

function getStorageKey(orgId: string): string {
  return `${STORAGE_KEY_PREFIX}:${orgId}`;
}

/**
 * Generate seed surveys with dates relative to current date
 */
function generateSeedSurveys(): Omit<SurveyListItemData, "templateId">[] {
  const now = today();
  const year = now.getFullYear();
  const quarter = getQuarter(now);
  const quarterStart = startOfQuarter(now);
  const quarterEnd = endOfQuarter(now);

  // Survey owners - using valid user IDs from people-store
  const hrOwners = ["ms", "rl"]; // Maria Soares (CHRO), Rafael Lima (Analista RH Sr)
  const hrManagers = ["ms"]; // Maria Soares

  return [
    // 1. Active pulse check (ending this week)
    {
      id: "1",
      name: `Pulse Check — ${now.toLocaleDateString("pt-BR", { month: "long", year: "numeric" })}`,
      type: "pulse",
      category: "pesquisa",
      status: "active",
      startDate: toIsoDate(addWeeks(now, -2)),
      endDate: toIsoDate(addDays(now, 3)),
      ownerIds: hrOwners,
      managerIds: hrManagers,
      tagIds: [],
      cycleId: null,
      totalRecipients: 342,
      totalResponses: 287,
      completionRate: 84,
      createdAt: toIsoDate(addWeeks(now, -3)),
    },
    // 2. Active clima survey (quarter-long)
    {
      id: "2",
      name: `Pesquisa de Clima Q${quarter} ${year}`,
      type: "clima",
      category: "pesquisa",
      status: "active",
      startDate: toIsoDate(addDays(quarterStart, 10)),
      endDate: toIsoDate(addDays(quarterEnd, -3)),
      ownerIds: hrOwners,
      managerIds: hrManagers,
      tagIds: [],
      cycleId: `q${quarter}-${year}`,
      totalRecipients: 350,
      totalResponses: 198,
      completionRate: 57,
      createdAt: toIsoDate(quarterStart),
    },
    // 3. Recently closed eNPS
    {
      id: "3",
      name: `eNPS — ${addMonths(now, -1).toLocaleDateString("pt-BR", { month: "long", year: "numeric" })}`,
      type: "enps",
      category: "pesquisa",
      status: "closed",
      startDate: toIsoDate(addWeeks(now, -6)),
      endDate: toIsoDate(addWeeks(now, -4)),
      ownerIds: hrOwners,
      managerIds: hrManagers,
      tagIds: [],
      cycleId: null,
      totalRecipients: 340,
      totalResponses: 312,
      completionRate: 92,
      createdAt: toIsoDate(addWeeks(now, -7)),
    },
    // 4. Active performance evaluation (quarter cycle)
    {
      id: "4",
      name: `Avaliacao de Desempenho — Ciclo Q${quarter}`,
      type: "performance",
      category: "ciclo",
      status: "active",
      startDate: toIsoDate(addDays(quarterStart, 15)),
      endDate: toIsoDate(quarterEnd),
      ownerIds: hrOwners,
      managerIds: hrManagers,
      tagIds: [],
      cycleId: `q${quarter}-${year}`,
      totalRecipients: 280,
      totalResponses: 145,
      completionRate: 52,
      createdAt: toIsoDate(addDays(quarterStart, 10)),
    },
    // 5. Scheduled 360 feedback (next month)
    {
      id: "5",
      name: "Feedback 360° — Lideranca",
      type: "360_feedback",
      category: "ciclo",
      status: "scheduled",
      startDate: toIsoDate(addWeeks(now, 2)),
      endDate: toIsoDate(addWeeks(now, 6)),
      ownerIds: hrOwners,
      managerIds: hrManagers,
      tagIds: [],
      cycleId: null,
      totalRecipients: 45,
      totalResponses: 0,
      completionRate: 0,
      createdAt: toIsoDate(addDays(now, -5)),
    },
    // 6. Draft health check
    {
      id: "6",
      name: "Health Check — Times de Engenharia",
      type: "health_check",
      category: "pesquisa",
      status: "draft",
      startDate: "",
      endDate: "",
      ownerIds: ["cm"], // Carlos Mendes - Tech Lead
      managerIds: ["pa"], // Pedro Almeida - CTO
      tagIds: [],
      cycleId: null,
      totalRecipients: 68,
      totalResponses: 0,
      completionRate: 0,
      createdAt: toIsoDate(addDays(now, -2)),
    },
    // 7. Paused skip-level
    {
      id: "7",
      name: "Skip-Level — Diretoria",
      type: "skip_level",
      category: "pesquisa",
      status: "paused",
      startDate: toIsoDate(addWeeks(now, -3)),
      endDate: toIsoDate(addWeeks(now, 1)),
      ownerIds: hrOwners,
      managerIds: ["ceo"], // Roberto Nascimento
      tagIds: [],
      cycleId: null,
      totalRecipients: 32,
      totalResponses: 14,
      completionRate: 44,
      createdAt: toIsoDate(addWeeks(now, -4)),
    },
    // 8. Closed pulse from last month
    {
      id: "8",
      name: `Pulse Check — ${addMonths(now, -1).toLocaleDateString("pt-BR", { month: "long", year: "numeric" })}`,
      type: "pulse",
      category: "pesquisa",
      status: "closed",
      startDate: toIsoDate(addWeeks(now, -6)),
      endDate: toIsoDate(addWeeks(now, -4)),
      ownerIds: hrOwners,
      managerIds: hrManagers,
      tagIds: [],
      cycleId: null,
      totalRecipients: 338,
      totalResponses: 295,
      completionRate: 87,
      createdAt: toIsoDate(addWeeks(now, -7)),
    },
    // 9. Archived clima from previous quarter
    {
      id: "9",
      name: `Clima Organizacional — ${year - 1} Anual`,
      type: "clima",
      category: "pesquisa",
      status: "archived",
      startDate: toIsoDate(addMonths(now, -4)),
      endDate: toIsoDate(addMonths(now, -3)),
      ownerIds: hrOwners,
      managerIds: hrManagers,
      tagIds: [],
      cycleId: null,
      totalRecipients: 320,
      totalResponses: 289,
      completionRate: 90,
      createdAt: toIsoDate(addMonths(now, -5)),
    },

    // ─── Pesquisas do Time Produto ──────────────────────────────────────────
    // managerIds inclui "bs" (Beatriz Santos, CPO — líder de team-produto)
    // Conecta com useTeamOverviewData → aggregateTeamSurveys filtra por managerIds

    // 10. Pulse fechado — Time Produto (resultados disponíveis, 12 submissions seed)
    {
      id: "10",
      name: `Pulse Check — Time Produto — ${addWeeks(now, -1).toLocaleDateString("pt-BR", { month: "long", year: "numeric" })}`,
      type: "pulse",
      category: "pesquisa",
      status: "closed",
      startDate: toIsoDate(addWeeks(now, -3)),
      endDate: toIsoDate(addWeeks(now, -1)),
      ownerIds: ["ms", "bs"],
      managerIds: ["bs"],
      tagIds: [],
      cycleId: null,
      totalRecipients: 12,
      totalResponses: 12,
      completionRate: 100,
      createdAt: toIsoDate(addWeeks(now, -4)),
    },

    // 11. Health Check ativo — Time Produto (em andamento, sem submissions seed)
    {
      id: "11",
      name: `Health Check — Produto Q${quarter} ${year}`,
      type: "health_check",
      category: "pesquisa",
      status: "active",
      startDate: toIsoDate(addDays(now, -7)),
      endDate: toIsoDate(addDays(now, 7)),
      ownerIds: ["bs"],
      managerIds: ["bs", "cr"],
      tagIds: [],
      cycleId: `q${quarter}-${year}`,
      totalRecipients: 12,
      totalResponses: 9,
      completionRate: 75,
      createdAt: toIsoDate(addDays(now, -10)),
    },

    // 12. eNPS fechado — Time Produto (resultados disponíveis, 12 submissions seed)
    {
      id: "12",
      name: `eNPS — Time Produto — ${addMonths(now, -1).toLocaleDateString("pt-BR", { month: "long", year: "numeric" })}`,
      type: "enps",
      category: "pesquisa",
      status: "closed",
      startDate: toIsoDate(addWeeks(now, -5)),
      endDate: toIsoDate(addWeeks(now, -3)),
      ownerIds: ["ms", "bs"],
      managerIds: ["bs"],
      tagIds: [],
      cycleId: null,
      totalRecipients: 12,
      totalResponses: 12,
      completionRate: 100,
      createdAt: toIsoDate(addWeeks(now, -6)),
    },
  ];
}

/**
 * Gera submissions seed para pesquisas fechadas do Time Produto.
 *
 * Survey 10 — Pulse Check (12 respostas)
 *   Question IDs derivados do template "pulse" via mapTemplateToRecord:
 *   pulse-question-1-1: rating 1-5     "Como você se sentiu esta semana?"
 *   pulse-question-1-2: likert 1-5     "Clareza sobre prioridades"
 *   pulse-question-1-3: likert 1-5     "Apoiado pela liderança"
 *   pulse-question-1-4: likert 1-5     "Confiante nas metas"
 *   pulse-question-1-5: multiple_choice "Carga de trabalho" (low/normal/high/overload)
 *   pulse-question-1-6: checkbox        "O que mais impactou?" (deadlines/team/tools/recognition/growth)
 *   pulse-question-1-7: text_short      Opcional
 *
 * Survey 12 — eNPS (12 respostas)
 *   enps-question-1-1: nps 0-10
 *   enps-question-1-2: text_short "O que motivou sua nota?"
 */
function generateSeedSubmissions(): SurveySubmissionRecord[] {
  const submissions: SurveySubmissionRecord[] = [];
  const baseMs = new Date("2026-03-08T10:00:00Z").getTime();

  // ── Survey 10: Pulse Check — Time Produto ─────────────────────────────────
  // Distribuição: maioria positiva (4-5), alguns neutros (3)
  // Médias esperadas: rating ~4.2, likert ~4.1, suporte à liderança ~4.3
  const pulse10 = [
    {
      r: 5,
      l1: 5,
      l2: 5,
      l3: 5,
      carga: "normal",
      impacto: ["growth"],
      texto: "Semana produtiva, time alinhado.",
    },
    {
      r: 4,
      l1: 4,
      l2: 5,
      l3: 4,
      carga: "normal",
      impacto: ["team"],
      texto: "",
    },
    {
      r: 4,
      l1: 4,
      l2: 4,
      l3: 4,
      carga: "normal",
      impacto: ["tools"],
      texto: "",
    },
    {
      r: 5,
      l1: 5,
      l2: 4,
      l3: 5,
      carga: "normal",
      impacto: ["growth", "team"],
      texto: "",
    },
    {
      r: 4,
      l1: 3,
      l2: 4,
      l3: 4,
      carga: "high",
      impacto: ["deadlines"],
      texto: "Prazo do módulo v2 deixou a semana corrida.",
    },
    {
      r: 3,
      l1: 3,
      l2: 3,
      l3: 3,
      carga: "high",
      impacto: ["deadlines", "tools"],
      texto: "",
    },
    {
      r: 4,
      l1: 4,
      l2: 5,
      l3: 4,
      carga: "normal",
      impacto: ["recognition"],
      texto: "",
    },
    {
      r: 5,
      l1: 5,
      l2: 5,
      l3: 5,
      carga: "low",
      impacto: ["growth"],
      texto: "Ótima semana, entrega tranquila.",
    },
    {
      r: 4,
      l1: 4,
      l2: 4,
      l3: 4,
      carga: "normal",
      impacto: ["team"],
      texto: "",
    },
    {
      r: 3,
      l1: 4,
      l2: 3,
      l3: 3,
      carga: "overload",
      impacto: ["deadlines"],
      texto: "Sobrecarga com demandas paralelas.",
    },
    {
      r: 4,
      l1: 4,
      l2: 4,
      l3: 5,
      carga: "normal",
      impacto: ["tools"],
      texto: "",
    },
    {
      r: 5,
      l1: 5,
      l2: 5,
      l3: 4,
      carga: "normal",
      impacto: ["growth", "team"],
      texto: "Time cada vez mais alinhado com os OKRs.",
    },
  ];

  pulse10.forEach((resp, i) => {
    submissions.push({
      id: `sub-10-${i + 1}`,
      surveyId: "10",
      respondentKey: `survey:10:respondent:anon-produto-${i + 1}`,
      answers: {
        "pulse-question-1-1": resp.r,
        "pulse-question-1-2": resp.l1,
        "pulse-question-1-3": resp.l2,
        "pulse-question-1-4": resp.l3,
        "pulse-question-1-5": resp.carga,
        "pulse-question-1-6": resp.impacto,
        ...(resp.texto ? { "pulse-question-1-7": resp.texto } : {}),
      },
      createdAt: new Date(baseMs - i * 1800000).toISOString(),
      updatedAt: new Date(baseMs - i * 1800000).toISOString(),
      submittedAt: new Date(baseMs - i * 1800000).toISOString(),
    });
  });

  // ── Survey 12: eNPS — Time Produto ────────────────────────────────────────
  // Distribuição: 7 promotores (9-10), 3 neutros (7-8), 2 detratores (5-6)
  // eNPS = ((7 - 2) / 12) × 100 = +42
  const enps12 = [
    {
      score: 10,
      motivo: "Ambiente de crescimento incrível. Produto evoluindo muito.",
    },
    { score: 9, motivo: "Ótimo time, grande oportunidade de aprendizado." },
    {
      score: 9,
      motivo: "Cultura de product management referência no mercado.",
    },
    { score: 10, motivo: "Liderança presente e times com autonomia real." },
    {
      score: 8,
      motivo: "Bom ambiente, mas processos ainda precisam amadurecer.",
    },
    { score: 7, motivo: "" },
    { score: 9, motivo: "Crescimento profissional acelerado aqui." },
    { score: 6, motivo: "Comunicação entre times precisa melhorar." },
    { score: 8, motivo: "Empresa em boa fase, produtos interessantes." },
    { score: 10, motivo: "Liderança inspiradora e visão de produto clara." },
    { score: 9, motivo: "Desafios reais, time talentoso." },
    {
      score: 5,
      motivo:
        "Processos internos precisam evoluir para suportar o crescimento.",
    },
  ];

  const enpsBaseMs = new Date("2026-02-20T10:00:00Z").getTime();
  enps12.forEach((resp, i) => {
    submissions.push({
      id: `sub-12-${i + 1}`,
      surveyId: "12",
      respondentKey: `survey:12:respondent:anon-produto-${i + 1}`,
      answers: {
        "enps-question-1-1": resp.score,
        ...(resp.motivo ? { "enps-question-1-2": resp.motivo } : {}),
      },
      createdAt: new Date(enpsBaseMs - i * 1800000).toISOString(),
      updatedAt: new Date(enpsBaseMs - i * 1800000).toISOString(),
      submittedAt: new Date(enpsBaseMs - i * 1800000).toISOString(),
    });
  });

  // ── Survey 11: Health Check — Time Produto ───────────────────────────────
  // 9 respostas — dimensões de saúde do modelo Spotify
  // Question IDs derivados do template "health_check":
  //   health_check-question-1-1: Velocidade de entrega
  //   health_check-question-1-2: Qualidade técnica
  //   health_check-question-1-3: Diversão
  //   health_check-question-1-4: Aprendizado
  //   health_check-question-1-5: Missão
  //   health_check-question-1-6: Suporte
  //   health_check-question-1-7: Trabalho em equipe
  //   health_check-question-1-8: Autonomia
  // Options: green (Saudável), yellow (Atenção), red (Crítico)
  const health11 = [
    {
      vel: "green",
      qual: "green",
      div: "green",
      apr: "green",
      mis: "green",
      sup: "green",
      col: "green",
      aut: "green",
    },
    {
      vel: "green",
      qual: "green",
      div: "yellow",
      apr: "green",
      mis: "green",
      sup: "yellow",
      col: "green",
      aut: "green",
    },
    {
      vel: "yellow",
      qual: "green",
      div: "green",
      apr: "green",
      mis: "green",
      sup: "green",
      col: "green",
      aut: "yellow",
    },
    {
      vel: "green",
      qual: "yellow",
      div: "green",
      apr: "yellow",
      mis: "green",
      sup: "green",
      col: "yellow",
      aut: "green",
    },
    {
      vel: "green",
      qual: "green",
      div: "green",
      apr: "green",
      mis: "yellow",
      sup: "green",
      col: "green",
      aut: "green",
    },
    {
      vel: "yellow",
      qual: "green",
      div: "yellow",
      apr: "green",
      mis: "green",
      sup: "yellow",
      col: "green",
      aut: "yellow",
    },
    {
      vel: "green",
      qual: "green",
      div: "green",
      apr: "green",
      mis: "green",
      sup: "green",
      col: "green",
      aut: "green",
    },
    {
      vel: "green",
      qual: "green",
      div: "green",
      apr: "yellow",
      mis: "green",
      sup: "green",
      col: "green",
      aut: "green",
    },
    {
      vel: "red",
      qual: "yellow",
      div: "green",
      apr: "green",
      mis: "green",
      sup: "yellow",
      col: "green",
      aut: "green",
    },
  ];

  const healthBaseMs = new Date("2026-03-12T09:00:00Z").getTime();
  health11.forEach((resp, i) => {
    submissions.push({
      id: `sub-11-${i + 1}`,
      surveyId: "11",
      respondentKey: `survey:11:respondent:anon-produto-${i + 1}`,
      answers: {
        "health_check-question-1-1": resp.vel,
        "health_check-question-1-2": resp.qual,
        "health_check-question-1-3": resp.div,
        "health_check-question-1-4": resp.apr,
        "health_check-question-1-5": resp.mis,
        "health_check-question-1-6": resp.sup,
        "health_check-question-1-7": resp.col,
        "health_check-question-1-8": resp.aut,
      },
      createdAt: new Date(healthBaseMs - i * 900000).toISOString(),
      updatedAt: new Date(healthBaseMs - i * 900000).toISOString(),
      submittedAt: new Date(healthBaseMs - i * 900000).toISOString(),
    });
  });

  return submissions;
}

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") {
    return structuredClone(value);
  }
  return JSON.parse(JSON.stringify(value)) as T;
}

function getTemplateIdForType(type: SurveyType): string {
  return `tpl-${type}`;
}

function mapTemplateToRecord(templateIndex: number): SurveyTemplateRecord {
  const template = SURVEY_TEMPLATES[templateIndex]!;
  const now = new Date().toISOString();

  const sections: WizardSection[] = template.sections.map(
    (section, sectionIndex) => ({
      id: `${template.type}-section-${sectionIndex + 1}`,
      title: section.title,
      description: section.description,
    }),
  );

  const questions: WizardQuestion[] = [];
  template.sections.forEach((section, sectionIndex) => {
    const sectionId = `${template.type}-section-${sectionIndex + 1}`;
    section.questions.forEach((question, questionIndex) => {
      questions.push({
        id: `${template.type}-question-${sectionIndex + 1}-${questionIndex + 1}`,
        sectionId,
        type: question.type,
        text: question.text,
        isRequired: question.isRequired,
        options: question.options,
        scaleMin: question.scaleMin,
        scaleMax: question.scaleMax,
        scaleLabels: question.scaleLabels,
        ratingMax: question.ratingMax,
      });
    });
  });

  return {
    id: getTemplateIdForType(template.type),
    type: template.type,
    name: template.name,
    subtitle: template.subtitle,
    category: template.category,
    isSystem: true,
    isArchived: false,
    sections,
    questions,
    defaultConfig: {
      isAnonymous: template.defaultConfig.isAnonymous,
      recurrence:
        (template.defaultConfig.recurrence as Recurrence | undefined) ?? null,
      aiPrefillOkrs: template.defaultConfig.aiPrefillOkrs ?? false,
      aiPrefillFeedback: template.defaultConfig.aiPrefillFeedback ?? false,
      aiBiasDetection: template.defaultConfig.aiBiasDetection ?? false,
    },
    createdAt: now,
    updatedAt: now,
  };
}

function buildBaseTemplates(): SurveyTemplateRecord[] {
  return SURVEY_TEMPLATES.map((_, index) => mapTemplateToRecord(index));
}

function buildSeedRecords(): SurveyLocalRecord[] {
  const now = new Date().toISOString();
  return generateSeedSurveys().map((listItem) => ({
    id: listItem.id,
    listItem: {
      ...listItem,
      templateId: getTemplateIdForType(listItem.type),
    },
    wizardState: null,
    createdAt: now,
    updatedAt: now,
  }));
}

function getSeedSnapshot(): SurveysStoreSnapshot {
  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: new Date().toISOString(),
    records: buildSeedRecords(),
    templates: buildBaseTemplates(),
    submissions: generateSeedSubmissions(),
  };
}

function sanitizeRecords(rawRecords: unknown): SurveyLocalRecord[] {
  if (!Array.isArray(rawRecords)) return [];

  const result: SurveyLocalRecord[] = [];

  rawRecords
    .filter((record) => !!record && typeof record === "object")
    .forEach((record) => {
      const item = record as Partial<SurveyLocalRecord>;
      const listItem = item.listItem as Partial<SurveyListItemData> | undefined;
      if (
        !listItem?.id ||
        !listItem.type ||
        !listItem.category ||
        !listItem.status
      ) {
        return;
      }

      result.push({
        id: String(item.id ?? listItem.id),
        listItem: {
          id: String(listItem.id),
          templateId:
            listItem.templateId ?? getTemplateIdForType(listItem.type),
          name: String(listItem.name ?? "Pesquisa sem titulo"),
          type: listItem.type,
          category: listItem.category,
          status: listItem.status,
          startDate: String(listItem.startDate ?? ""),
          endDate: String(listItem.endDate ?? ""),
          ownerIds: Array.isArray(listItem.ownerIds)
            ? listItem.ownerIds.map((userId) => String(userId))
            : [],
          managerIds: Array.isArray(listItem.managerIds)
            ? listItem.managerIds.map((userId) => String(userId))
            : [],
          tagIds: Array.isArray(listItem.tagIds)
            ? listItem.tagIds.map((tagId) => String(tagId))
            : [],
          cycleId: listItem.cycleId ? String(listItem.cycleId) : null,
          totalRecipients: Number(listItem.totalRecipients ?? 0),
          totalResponses: Number(listItem.totalResponses ?? 0),
          completionRate: Number(listItem.completionRate ?? 0),
          createdAt: String(listItem.createdAt ?? ""),
        },
        wizardState: item.wizardState ?? null,
        createdAt: String(item.createdAt ?? new Date().toISOString()),
        updatedAt: String(item.updatedAt ?? new Date().toISOString()),
      });
    });

  return result;
}

function sanitizeTemplates(rawTemplates: unknown): SurveyTemplateRecord[] {
  if (!Array.isArray(rawTemplates)) return [];

  return rawTemplates
    .filter((template) => !!template && typeof template === "object")
    .map((template) => {
      const item = template as Partial<SurveyTemplateRecord>;
      if (!item.id || !item.type || !item.name || !item.category) {
        return null;
      }

      return {
        id: String(item.id),
        type: item.type,
        name: String(item.name),
        subtitle: String(item.subtitle ?? ""),
        category: item.category,
        isSystem: Boolean(item.isSystem),
        isArchived: Boolean(item.isArchived),
        sections: Array.isArray(item.sections) ? cloneDeep(item.sections) : [],
        questions: Array.isArray(item.questions)
          ? cloneDeep(item.questions)
          : [],
        defaultConfig: {
          isAnonymous: Boolean(item.defaultConfig?.isAnonymous ?? true),
          recurrence:
            (item.defaultConfig?.recurrence as Recurrence | null | undefined) ??
            null,
          aiPrefillOkrs: Boolean(item.defaultConfig?.aiPrefillOkrs),
          aiPrefillFeedback: Boolean(item.defaultConfig?.aiPrefillFeedback),
          aiBiasDetection: Boolean(item.defaultConfig?.aiBiasDetection),
        },
        createdAt: String(item.createdAt ?? new Date().toISOString()),
        updatedAt: String(item.updatedAt ?? new Date().toISOString()),
      };
    })
    .filter((template): template is SurveyTemplateRecord => !!template);
}

function mergeTemplatesWithBase(
  rawTemplates: SurveyTemplateRecord[],
): SurveyTemplateRecord[] {
  const baseTemplates = buildBaseTemplates();

  // Get set of base template IDs for quick lookup
  const baseTemplateIds = new Set(baseTemplates.map((t) => t.id));

  // Merge base templates with any customizations from rawTemplates
  const mergedBase = baseTemplates.map((base) => {
    // Only merge if there's an existing template with the same ID (not just same type)
    const existing = rawTemplates.find((item) => item.id === base.id);
    if (!existing) return base;

    return {
      ...base,
      ...existing,
      type: base.type,
      category: existing.category ?? base.category,
      isSystem: true,
      isArchived: false,
      sections:
        existing.sections.length > 0 ? existing.sections : base.sections,
      questions:
        existing.questions.length > 0 ? existing.questions : base.questions,
      defaultConfig: {
        ...base.defaultConfig,
        ...existing.defaultConfig,
      },
    };
  });

  // Add user-created custom templates (those with IDs not in base templates)
  const userCustomTemplates = rawTemplates.filter(
    (item) => !baseTemplateIds.has(item.id),
  );
  return [...mergedBase, ...userCustomTemplates];
}

function sanitizeSubmissions(
  rawSubmissions: unknown,
): SurveySubmissionRecord[] {
  if (!Array.isArray(rawSubmissions)) return [];

  return rawSubmissions
    .filter((submission) => !!submission && typeof submission === "object")
    .map((submission) => {
      const item = submission as Partial<SurveySubmissionRecord>;
      if (!item.id || !item.surveyId || !item.respondentKey) {
        return null;
      }

      return {
        id: String(item.id),
        surveyId: String(item.surveyId),
        respondentKey: String(item.respondentKey),
        answers:
          item.answers && typeof item.answers === "object"
            ? cloneDeep(item.answers as Record<string, unknown>)
            : {},
        createdAt: String(item.createdAt ?? new Date().toISOString()),
        updatedAt: String(item.updatedAt ?? new Date().toISOString()),
        submittedAt: String(item.submittedAt ?? new Date().toISOString()),
      };
    })
    .filter((submission): submission is SurveySubmissionRecord => !!submission);
}

function migrateSnapshot(
  raw: Partial<SurveysStoreSnapshot> | null,
): SurveysStoreSnapshot {
  const seed = getSeedSnapshot();
  if (!raw || typeof raw !== "object") {
    return seed;
  }

  const records = sanitizeRecords(raw.records);
  const templates = mergeTemplatesWithBase(sanitizeTemplates(raw.templates));

  // Always merge seed submissions into user data — seed submissions are identified by ID
  // and new ones added to the seed will automatically appear for all users without
  // requiring a schema version bump. User-created submissions (not in seed) are preserved.
  const rawSubmissions = sanitizeSubmissions(raw.submissions);
  const seedSubmissions = seed.submissions;
  const seedSubmissionIds = new Set(seedSubmissions.map((s) => s.id));
  const userSubmissions = rawSubmissions.filter(
    (s) => !seedSubmissionIds.has(s.id),
  );
  const submissions = [...seedSubmissions, ...userSubmissions];

  // Always merge seed records into user data — new seed records (new survey IDs) are
  // injected automatically. User-created records are preserved by deduplication on ID.
  let finalRecords: typeof records;
  if (records.length === 0) {
    finalRecords = seed.records;
  } else {
    const userRecordIds = new Set(records.map((r) => r.id));
    const newSeedRecords = seed.records.filter((r) => !userRecordIds.has(r.id));
    finalRecords = [...records, ...newSeedRecords];
  }

  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt:
      typeof raw.updatedAt === "string" ? raw.updatedAt : seed.updatedAt,
    records: finalRecords,
    templates,
    submissions,
  };
}

export function loadSurveysSnapshot(
  orgId = DEFAULT_ORG_ID,
): SurveysStoreSnapshot {
  if (typeof window === "undefined") {
    return getSeedSnapshot();
  }

  const storageKey = getStorageKey(orgId);
  const raw = window.localStorage.getItem(storageKey);
  if (!raw) {
    if (orgId === DEFAULT_ORG_ID) {
      const legacyRaw = window.localStorage.getItem(LEGACY_STORAGE_KEY);
      if (legacyRaw) {
        try {
          const parsedLegacy = JSON.parse(
            legacyRaw,
          ) as Partial<SurveysStoreSnapshot>;
          const migratedLegacy = migrateSnapshot(parsedLegacy);
          window.localStorage.setItem(
            storageKey,
            JSON.stringify(migratedLegacy),
          );
          return migratedLegacy;
        } catch {
          // ignore legacy parse failures and recreate seed snapshot
        }
      }
    }

    const seed = getSeedSnapshot();
    window.localStorage.setItem(storageKey, JSON.stringify(seed));
    return seed;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<SurveysStoreSnapshot>;
    const migrated = migrateSnapshot(parsed);
    if ((parsed.schemaVersion ?? 0) !== STORE_SCHEMA_VERSION) {
      window.localStorage.setItem(storageKey, JSON.stringify(migrated));
    }
    return migrated;
  } catch {
    const seed = getSeedSnapshot();
    window.localStorage.setItem(storageKey, JSON.stringify(seed));
    return seed;
  }
}

export function saveSurveysSnapshot(
  snapshot: Omit<SurveysStoreSnapshot, "updatedAt" | "schemaVersion">,
  orgId = DEFAULT_ORG_ID,
): SurveysStoreSnapshot {
  const next: SurveysStoreSnapshot = {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: new Date().toISOString(),
    records: cloneDeep(snapshot.records),
    templates: mergeTemplatesWithBase(cloneDeep(snapshot.templates)),
    submissions: cloneDeep(snapshot.submissions),
  };

  if (typeof window !== "undefined") {
    window.localStorage.setItem(getStorageKey(orgId), JSON.stringify(next));
  }

  return next;
}

export function resetSurveysSnapshot(
  orgId = DEFAULT_ORG_ID,
): SurveysStoreSnapshot {
  const seed = getSeedSnapshot();
  if (typeof window !== "undefined") {
    window.localStorage.setItem(getStorageKey(orgId), JSON.stringify(seed));
  }
  return seed;
}
