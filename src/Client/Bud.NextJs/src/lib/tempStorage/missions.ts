import type { ComponentType } from "react";
import type { IconProps } from "@phosphor-icons/react";
import {
  Trophy,
  ArrowsInLineVertical,
  PlugsConnected,
  Target,
  Crosshair,
  ChartLineUp,
  TrendDown,
  ArrowsInLineHorizontal,
  ChartBar,
} from "@phosphor-icons/react";
import type {
  Mission,
  KeyResult,
  GoalType,
  KRStatus,
  MissionTask,
  MissionMember,
  SubTask,
  CheckIn,
  Tag,
} from "@/types";
import {
  getCurrentCycleInfo,
  deterministicCreatedAt,
  deterministicUpdatedAt,
  generateCheckInsForKR,
  toIsoDate,
  getQuarter,
  today,
  addDays,
} from "@/lib/tempStorage/seed-utils";

/* ——— Numeric helpers ——— */

/** Parse decimal string (from DB) to number */
export function numVal(v: string | null | undefined): number {
  if (v == null || v === "") return 0;
  return Number(v) || 0;
}

/* ——— Goal type helpers ——— */

const GOAL_TYPE_ICONS: Record<GoalType, ComponentType<IconProps>> = {
  reach: Crosshair,
  above: ChartLineUp,
  below: TrendDown,
  between: ArrowsInLineHorizontal,
  reduce: TrendDown,
  survey: ChartBar,
};

export function getGoalTypeIcon(goalType: GoalType): ComponentType<IconProps> {
  return GOAL_TYPE_ICONS[goalType] ?? Trophy;
}

/** Build a human-readable goal label from KR fields */
export function getGoalLabel(kr: KeyResult): string {
  const target = numVal(kr.targetValue);
  const low = numVal(kr.lowThreshold);
  const high = numVal(kr.highThreshold);
  const unit =
    kr.unitLabel ??
    (kr.unit === "percent" ? "%" : kr.unit === "currency" ? "R$" : "");

  switch (kr.goalType) {
    case "reach":
      return `Atingir ${unit === "R$" || unit === "US$" ? `${unit} ${target.toLocaleString("pt-BR")}` : `${target}${unit ? ` ${unit}` : ""}`}`;
    case "above":
      return `Manter acima de ${unit} ${low}`.trim();
    case "below":
      return `Manter abaixo de ${unit} ${high}`.trim();
    case "between":
      return `Manter entre ${low} e ${high}`;
    case "reduce":
      return `Reduzir para ${target}${unit ? ` ${unit}` : ""}`;
    case "survey":
      return `De pesquisa vinculada`;
    default:
      return "";
  }
}

/* ——— Status helpers ——— */

const STATUS_LABELS: Record<KRStatus, string> = {
  on_track: "No ritmo",
  attention: "Atenção",
  off_track: "Atrasado",
  completed: "Concluído",
};

export function getKRStatusLabel(status: KRStatus): string {
  return STATUS_LABELS[status] ?? status;
}

/** Map KRStatus to DS Badge color */
export function getKRStatusBadge(
  status: KRStatus,
): "success" | "warning" | "error" | "neutral" {
  switch (status) {
    case "on_track":
      return "success";
    case "attention":
      return "warning";
    case "off_track":
      return "error";
    case "completed":
      return "success";
    default:
      return "neutral";
  }
}

/* ——— Period helpers ——— */

export function formatPeriodRange(
  start: string | null,
  end: string | null,
): string {
  if (!start || !end) return "";
  const fmt = (d: string) => {
    const [y, m, day] = d.split("-");
    return `${day}/${m}/${y}`;
  };
  return `${fmt(start)} à ${fmt(end)}`;
}

/* ——— Owner helpers ——— */

export function getOwnerName(owner?: {
  firstName: string;
  lastName: string;
}): string {
  if (!owner) return "";
  return `${owner.firstName} ${owner.lastName}`;
}

export function getOwnerInitials(owner?: {
  firstName: string;
  lastName: string;
  initials: string | null;
}): string {
  if (!owner) return "??";
  if (owner.initials) return owner.initials;
  return `${owner.firstName[0] ?? ""}${owner.lastName[0] ?? ""}`.toUpperCase();
}

/* ——— Indicator icon helper (by content/type) ——— */

export function getIndicatorIcon(kr: KeyResult): ComponentType<IconProps> {
  if (
    kr.goalType === "between" ||
    kr.goalType === "above" ||
    kr.goalType === "below"
  ) {
    return ArrowsInLineVertical;
  }
  if (kr.measurementMode === "external") return PlugsConnected;
  if (kr.measurementMode === "mission") return Target;
  return getGoalTypeIcon(kr.goalType);
}

/* ——— Check-in chart data ——— */

export interface ChartPoint {
  date: string;
  value: number;
}

/* ——— Mock data ——— */

// User IDs must match people-store.ts exactly
const MS_OWNER = {
  id: "ms",
  firstName: "Maria",
  lastName: "Soares",
  initials: "MS",
};
const BR_OWNER = {
  id: "br",
  firstName: "Beatriz",
  lastName: "Ramos",
  initials: "BR",
};
const LO_OWNER = {
  id: "lo",
  firstName: "Lucas",
  lastName: "Oliveira",
  initials: "LO",
};
const JM_OWNER = {
  id: "jm",
  firstName: "Joao",
  lastName: "Martins",
  initials: "JM",
};
const AF_OWNER = {
  id: "af",
  firstName: "Ana",
  lastName: "Ferreira",
  initials: "AF",
};
const CS_OWNER = {
  id: "cs",
  firstName: "Carla",
  lastName: "Santos",
  initials: "CS",
};
const RM_OWNER = {
  id: "rm",
  firstName: "Rafael",
  lastName: "Mendes",
  initials: "RM",
};
const CM_OWNER = {
  id: "cm",
  firstName: "Carlos",
  lastName: "Mendes",
  initials: "CM",
};

// Time Produto — IDs must match people-store.ts exactly
const BS_OWNER = {
  id: "bs",
  firstName: "Beatriz",
  lastName: "Santos",
  initials: "BS",
};
const CR_OWNER = {
  id: "cr",
  firstName: "Camila",
  lastName: "Rocha",
  initials: "CR",
};
const GF_OWNER = {
  id: "gf",
  firstName: "Gustavo",
  lastName: "Fonseca",
  initials: "GF",
};

const ORG_ID = "org-1";

// Helper to build a MissionMember entry for a supporter
function mkSupporter(
  missionId: string,
  user: typeof MS_OWNER,
  addedBy: string,
  addedAt: string,
): MissionMember {
  return {
    missionId,
    userId: user.id,
    role: "supporter",
    addedAt,
    addedBy,
    user: {
      id: user.id,
      firstName: user.firstName,
      lastName: user.lastName,
      initials: user.initials,
      jobTitle: null,
      avatarUrl: null,
    },
  };
}

function mkSubtask(
  id: string,
  title: string,
  isDone: boolean,
  sortOrder = 0,
): SubTask {
  return { id, taskId: "", title, isDone, sortOrder };
}

function mkTask(
  id: string,
  title: string,
  isDone: boolean,
  owner: typeof MS_OWNER,
  opts?: {
    description?: string;
    missionId?: string | null;
    keyResultId?: string | null;
    subtasks?: SubTask[];
    contributesTo?: { missionId: string; missionTitle: string }[];
  },
): MissionTask {
  return {
    id,
    missionId: opts?.missionId ?? null,
    keyResultId: opts?.keyResultId ?? null,
    title,
    description: opts?.description ?? null,
    ownerId: owner.id,
    dueDate: null,
    isDone,
    sortOrder: 0,
    completedAt: isDone ? "2026-03-01T00:00:00Z" : null,
    createdAt: "2026-01-15T00:00:00Z",
    updatedAt: "2026-03-07T00:00:00Z",
    owner,
    subtasks: opts?.subtasks,
    contributesTo: opts?.contributesTo,
  };
}

function mkKR(
  id: string,
  title: string,
  opts: {
    missionId: string;
    goalType: GoalType;
    progress: number;
    targetValue?: string;
    currentValue?: string;
    startValue?: string;
    lowThreshold?: string;
    highThreshold?: string;
    expectedValue?: string;
    status: KRStatus;
    owner: typeof MS_OWNER;
    periodLabel?: string;
    periodStart?: string;
    periodEnd?: string;
    unit?: string;
    unitLabel?: string;
    children?: KeyResult[];
    tasks?: MissionTask[];
    contributesTo?: { missionId: string; missionTitle: string }[];
  },
): KeyResult {
  return {
    id,
    orgId: ORG_ID,
    missionId: opts.missionId,
    parentKrId: null,
    title,
    description: null,
    ownerId: opts.owner.id,
    measurementMode: "manual",
    linkedMissionId: null,
    linkedSurveyId: null,
    externalSource: null,
    externalConfig: null,
    goalType: opts.goalType,
    targetValue: opts.targetValue ?? "100",
    currentValue: opts.currentValue ?? String(opts.progress),
    startValue: opts.startValue ?? "0",
    lowThreshold: opts.lowThreshold ?? null,
    highThreshold: opts.highThreshold ?? null,
    unit: (opts.unit as KeyResult["unit"]) ?? "percent",
    unitLabel: opts.unitLabel ?? null,
    expectedValue: opts.expectedValue ?? null,
    status: opts.status,
    progress: opts.progress,
    periodLabel: opts.periodLabel ?? `Q${CURRENT_QUARTER} ${CURRENT_YEAR}`,
    periodStart: opts.periodStart ?? CYCLE_START_ISO,
    periodEnd: opts.periodEnd ?? CYCLE_END_ISO,
    sortOrder: 0,
    createdAt: deterministicCreatedAt(id, cycleInfo.quarterStart),
    updatedAt: deterministicUpdatedAt(id),
    deletedAt: null,
    owner: opts.owner,
    children: opts.children,
    tasks: opts.tasks,
    contributesTo: opts.contributesTo,
  };
}

// Get current cycle info for date generation
const cycleInfo = getCurrentCycleInfo();
const CURRENT_CYCLE_ID = cycleInfo.quarterId;
const CYCLE_START_ISO = toIsoDate(cycleInfo.quarterStart);
const CYCLE_END_ISO = toIsoDate(cycleInfo.quarterEnd);
const CURRENT_QUARTER = getQuarter(today());
const CURRENT_YEAR = today().getFullYear();

// Due dates relativos ao dia atual — usados nas missões do time Produto
const _now = today();
const DUE_14_DAYS = toIsoDate(addDays(_now, 14));
const DUE_21_DAYS = toIsoDate(addDays(_now, 21));
const DUE_30_DAYS = toIsoDate(addDays(_now, 30));
const COMPLETED_7_DAYS_AGO = toIsoDate(addDays(_now, -7));

// ─── Tag helpers ─────────────────────────────────────────────────────────────
// IDs devem bater com config-store.ts (tags de org-1)
function mkTag(id: string, name: string, color: string): Tag {
  const now = new Date().toISOString();
  return {
    id,
    orgId: ORG_ID,
    name,
    color,
    createdAt: now,
    updatedAt: now,
    deletedAt: null,
  };
}

const TAG_ESTRATEGIA = mkTag("tag-estrategia", "Estratégia", "wine");
const TAG_CRESCIMENTO = mkTag("tag-crescimento", "Crescimento", "orange");
const TAG_DESIGN = mkTag("tag-design", "Design", "wine");
const TAG_CULTURA = mkTag("tag-cultura", "Cultura", "caramel");
const TAG_ENGENHARIA = mkTag("tag-engenharia", "Engenharia", "orange");

export const MOCK_MISSIONS: Mission[] = [
  {
    id: "m1",
    orgId: ORG_ID,
    cycleId: CURRENT_CYCLE_ID,
    parentId: null,
    depth: 0,
    path: ["m1"],
    title: "Usar o nosso conhecimento para mudar o patamar do negócio",
    description: null,
    ownerId: MS_OWNER.id,
    teamId: null,
    status: "active",
    visibility: "public",
    progress: 75,
    kanbanStatus: "doing",
    sortOrder: 0,
    dueDate: null,
    completedAt: null,
    createdAt: deterministicCreatedAt("m1", cycleInfo.quarterStart),
    updatedAt: deterministicUpdatedAt("m1"),
    deletedAt: null,
    owner: MS_OWNER,
    keyResults: [
      mkKR("i1", "Receita de Crédito Imobiliário", {
        missionId: "m1",
        goalType: "reach",
        progress: 30,
        targetValue: "100",
        expectedValue: "66",
        status: "attention",
        owner: MS_OWNER,
        unitLabel: "R$",
        contributesTo: [
          {
            missionId: "m2",
            missionTitle:
              "Acelerar a governança, a performance e o desenvolvimento interno colaborativo",
          },
        ],
        children: [
          mkKR("i1-sub1", "Receita via canal digital", {
            missionId: "m1",
            goalType: "reach",
            progress: 45,
            targetValue: "100",
            expectedValue: "66",
            status: "attention",
            owner: BR_OWNER,
          }),
          mkKR("i1-sub2", "Receita via canal presencial", {
            missionId: "m1",
            goalType: "reach",
            progress: 18,
            targetValue: "100",
            expectedValue: "66",
            status: "off_track",
            owner: LO_OWNER,
          }),
        ],
        tasks: [
          mkTask(
            "it1",
            "Mapear leads qualificados do trimestre",
            true,
            MS_OWNER,
            {
              keyResultId: "i1",
              description:
                "Identificar e qualificar os leads com maior potencial de conversão",
              subtasks: [
                mkSubtask("st1", "Levantar dados do CRM", true),
                mkSubtask("st2", "Classificar por score", true),
                mkSubtask("st3", "Enviar lista para comercial", false),
              ],
            },
          ),
          mkTask(
            "it2",
            "Negociar condições com banco parceiro",
            false,
            BR_OWNER,
            {
              keyResultId: "i1",
              subtasks: [
                mkSubtask("st4", "Agendar reunião com gerente do banco", true),
                mkSubtask("st5", "Enviar proposta de termos", false),
              ],
            },
          ),
          mkTask("it3", "Atualizar projeção financeira", false, LO_OWNER, {
            keyResultId: "i1",
          }),
        ],
      }),
      mkKR("i2", "NPS do onboarding", {
        missionId: "m1",
        goalType: "between",
        progress: 65,
        targetValue: "100",
        lowThreshold: "50",
        highThreshold: "90",
        status: "on_track",
        owner: JM_OWNER,
      }),
    ],
    tasks: [
      mkTask(
        "tk1",
        "Revisar contratos pendentes com jurídico",
        true,
        MS_OWNER,
        { missionId: "m1" },
      ),
      mkTask("tk2", "Agendar reunião com time comercial", true, AF_OWNER, {
        missionId: "m1",
      }),
      mkTask("tk3", "Preparar relatório de pipeline Q1", false, MS_OWNER, {
        missionId: "m1",
        contributesTo: [
          {
            missionId: "m2",
            missionTitle:
              "Acelerar a governança, a performance e o desenvolvimento interno colaborativo",
          },
        ],
      }),
    ],
    children: [
      {
        id: "m1-sub",
        orgId: ORG_ID,
        cycleId: CURRENT_CYCLE_ID,
        parentId: "m1",
        depth: 1,
        path: ["m1", "m1-sub"],
        title: "Expandir a base de clientes enterprise",
        description: null,
        ownerId: CS_OWNER.id,
        teamId: null,
        status: "active",
        visibility: "public",
        progress: 42,
        kanbanStatus: "doing",
        sortOrder: 0,
        dueDate: null,
        completedAt: null,
        createdAt: deterministicCreatedAt("m1-sub", cycleInfo.quarterStart),
        updatedAt: deterministicUpdatedAt("m1-sub"),
        deletedAt: null,
        owner: CS_OWNER,
        keyResults: [
          mkKR("i8", "Novos contratos enterprise", {
            missionId: "m1-sub",
            goalType: "reach",
            progress: 40,
            targetValue: "100",
            expectedValue: "66",
            status: "off_track",
            owner: CS_OWNER,
          }),
          mkKR("i9", "Ticket médio enterprise", {
            missionId: "m1-sub",
            goalType: "above",
            progress: 72,
            targetValue: "100",
            lowThreshold: "50",
            status: "on_track",
            owner: RM_OWNER,
          }),
        ],
      },
    ],
    restrictedSummary: { keyResults: 2, tasks: 1, children: 0 },
  },
  {
    id: "m2",
    orgId: ORG_ID,
    cycleId: CURRENT_CYCLE_ID,
    parentId: null,
    depth: 0,
    path: ["m2"],
    title:
      "Acelerar a governança, a performance e o desenvolvimento interno colaborativo",
    description: null,
    ownerId: MS_OWNER.id,
    teamId: null,
    status: "active",
    visibility: "public",
    progress: 55,
    kanbanStatus: "doing",
    sortOrder: 1,
    dueDate: null,
    completedAt: null,
    createdAt: deterministicCreatedAt("m2", cycleInfo.quarterStart),
    updatedAt: deterministicUpdatedAt("m2"),
    deletedAt: null,
    owner: MS_OWNER,
    keyResults: [
      mkKR("i5", "Módulo de pesquisas v2", {
        missionId: "m2",
        goalType: "reach",
        progress: 72,
        targetValue: "100",
        expectedValue: "66",
        status: "on_track",
        owner: MS_OWNER,
      }),
      mkKR("i6", "Integração Slack e Teams", {
        missionId: "m2",
        goalType: "reach",
        progress: 85,
        targetValue: "100",
        expectedValue: "66",
        status: "on_track",
        owner: CM_OWNER, // Carlos Mendes - Tech Lead (Engenharia)
      }),
      mkKR("i7", "Adesão aos check-ins semanais", {
        missionId: "m2",
        goalType: "reach",
        progress: 35,
        targetValue: "100",
        expectedValue: "66",
        status: "off_track",
        owner: CS_OWNER, // Carla Santos - People Business Partner
        periodLabel: "Q1 2026",
        periodStart: "2026-01-01",
        periodEnd: "2026-03-31",
      }),
    ],
    tasks: [
      mkTask(
        "tk5",
        "Definir escopo do módulo de pesquisas v2",
        true,
        MS_OWNER,
        { missionId: "m2" },
      ),
      mkTask("tk6", "Criar protótipos de alta fidelidade", false, JM_OWNER, {
        missionId: "m2",
      }),
      mkTask("tk7", "Validar fluxo com 3 clientes beta", false, MS_OWNER, {
        missionId: "m2",
      }),
    ],
    externalContributions: [
      {
        type: "indicator",
        id: "i1",
        title: "Receita de Crédito Imobiliário",
        progress: 30,
        status: "attention",
        owner: MS_OWNER,
        sourceMission: {
          id: "m1",
          title: "Usar o nosso conhecimento para mudar o patamar do negócio",
        },
      },
      {
        type: "task",
        id: "tk3",
        title: "Preparar relatório de pipeline Q1",
        isDone: false,
        owner: MS_OWNER,
        sourceMission: {
          id: "m1",
          title: "Usar o nosso conhecimento para mudar o patamar do negócio",
        },
      },
    ],
  },
  {
    id: "m3",
    orgId: ORG_ID,
    cycleId: null,
    parentId: null,
    depth: 0,
    path: ["m3"],
    title: "Implementar programa de mentoria entre líderes",
    description: null,
    ownerId: MS_OWNER.id,
    teamId: null,
    status: "draft",
    visibility: "public",
    progress: 0,
    kanbanStatus: "uncategorized",
    sortOrder: 2,
    dueDate: null,
    completedAt: null,
    createdAt: deterministicCreatedAt("m3", cycleInfo.quarterStart),
    updatedAt: deterministicUpdatedAt("m3"),
    deletedAt: null,
    owner: MS_OWNER,
    keyResults: [],
  },

  // ─────────────────────────────────────────────────────────────────────────
  // Missões do Time Produto (team-produto)
  // Owners: bs (CPO), cr (PM), gf (Designer), jm (People Ops), br (PM)
  // ─────────────────────────────────────────────────────────────────────────

  {
    id: "mp1",
    orgId: ORG_ID,
    cycleId: CURRENT_CYCLE_ID,
    parentId: null,
    depth: 0,
    path: ["mp1"],
    title: "Lançar módulo de Missões v2",
    description:
      "Redesenhar e reimplementar o módulo de OKRs com nova UX e integrações.",
    ownerId: BS_OWNER.id,
    teamId: "team-produto",
    status: "active",
    visibility: "public",
    progress: 72,
    kanbanStatus: "doing",
    sortOrder: 3,
    dueDate: DUE_30_DAYS,
    completedAt: null,
    createdAt: deterministicCreatedAt("mp1", cycleInfo.quarterStart),
    updatedAt: deterministicUpdatedAt("mp1"),
    deletedAt: null,
    owner: BS_OWNER,
    team: { id: "team-produto", name: "Produto", color: "caramel" },
    tags: [TAG_ESTRATEGIA],
    keyResults: [
      mkKR("kp1", "Entregar 3 features core do módulo", {
        missionId: "mp1",
        goalType: "reach",
        progress: 80,
        targetValue: "3",
        currentValue: "2.4",
        startValue: "0",
        expectedValue: "2",
        status: "on_track",
        owner: BS_OWNER,
        unit: "number",
        unitLabel: "features",
      }),
      mkKR("kp2", "Zero bugs críticos em produção", {
        missionId: "mp1",
        goalType: "below",
        progress: 65,
        targetValue: "0",
        currentValue: "2",
        startValue: "8",
        highThreshold: "0",
        status: "on_track",
        owner: GF_OWNER,
        unit: "number",
        unitLabel: "bugs",
      }),
    ],
    tasks: [
      mkTask(
        "tp1",
        "Finalizar especificação técnica das APIs",
        true,
        BS_OWNER,
        { missionId: "mp1" },
      ),
      mkTask("tp2", "Implementar tela de Kanban de missões", true, GF_OWNER, {
        missionId: "mp1",
      }),
      mkTask("tp3", "Validar UX com 5 clientes beta", false, CR_OWNER, {
        missionId: "mp1",
      }),
    ],
    members: [
      mkSupporter(
        "mp1",
        CR_OWNER,
        BS_OWNER.id,
        deterministicCreatedAt("mp1", cycleInfo.quarterStart),
      ),
      mkSupporter(
        "mp1",
        GF_OWNER,
        BS_OWNER.id,
        deterministicCreatedAt("mp1", cycleInfo.quarterStart),
      ),
    ],
  },

  {
    id: "mp2",
    orgId: ORG_ID,
    cycleId: CURRENT_CYCLE_ID,
    parentId: null,
    depth: 0,
    path: ["mp2"],
    title: "Aumentar adoção do produto em 50%",
    description:
      "Crescer a base de usuários ativos diários e melhorar o NPS do produto.",
    ownerId: CR_OWNER.id,
    teamId: "team-produto",
    status: "active",
    visibility: "public",
    progress: 45,
    kanbanStatus: "doing",
    sortOrder: 4,
    dueDate: DUE_14_DAYS,
    completedAt: null,
    createdAt: deterministicCreatedAt("mp2", cycleInfo.quarterStart),
    updatedAt: deterministicUpdatedAt("mp2"),
    deletedAt: null,
    owner: CR_OWNER,
    team: { id: "team-produto", name: "Produto", color: "caramel" },
    tags: [TAG_CRESCIMENTO],
    keyResults: [
      mkKR("kp3", "DAU crescer de 2.000 para 3.000 usuários", {
        missionId: "mp2",
        goalType: "reach",
        progress: 42,
        targetValue: "3000",
        currentValue: "2252",
        startValue: "2000",
        expectedValue: "66",
        status: "attention",
        owner: CR_OWNER,
        unit: "number",
        unitLabel: "usuários",
      }),
      mkKR("kp4", "NPS do produto atingir 50 pontos", {
        missionId: "mp2",
        goalType: "above",
        progress: 48,
        targetValue: "100",
        currentValue: "48",
        startValue: "32",
        lowThreshold: "50",
        status: "attention",
        owner: CR_OWNER,
        unit: "number",
        unitLabel: "NPS",
      }),
    ],
    tasks: [
      mkTask(
        "tp4",
        "Redesenhar tela de boas-vindas para novos usuários",
        true,
        GF_OWNER,
        { missionId: "mp2" },
      ),
      mkTask(
        "tp5",
        "Criar campanha de reativação para usuários inativos",
        false,
        CR_OWNER,
        { missionId: "mp2" },
      ),
      mkTask("tp6", "Implementar tour guiado de onboarding", false, GF_OWNER, {
        missionId: "mp2",
      }),
    ],
    members: [
      mkSupporter(
        "mp2",
        BS_OWNER,
        CR_OWNER.id,
        deterministicCreatedAt("mp2", cycleInfo.quarterStart),
      ),
      mkSupporter(
        "mp2",
        JM_OWNER,
        CR_OWNER.id,
        deterministicCreatedAt("mp2", cycleInfo.quarterStart),
      ),
    ],
  },

  {
    id: "mp3",
    orgId: ORG_ID,
    cycleId: CURRENT_CYCLE_ID,
    parentId: null,
    depth: 0,
    path: ["mp3"],
    title: "Redesenhar fluxo de onboarding",
    description:
      "Simplificar o processo de onboarding para reduzir time-to-value do cliente.",
    ownerId: GF_OWNER.id,
    teamId: "team-produto",
    status: "active",
    visibility: "public",
    progress: 88,
    kanbanStatus: "doing",
    sortOrder: 5,
    dueDate: null,
    completedAt: null,
    createdAt: deterministicCreatedAt("mp3", cycleInfo.quarterStart),
    updatedAt: deterministicUpdatedAt("mp3"),
    deletedAt: null,
    owner: GF_OWNER,
    team: { id: "team-produto", name: "Produto", color: "caramel" },
    tags: [TAG_DESIGN],
    keyResults: [
      mkKR("kp5", "Reduzir tempo médio de onboarding em 40%", {
        missionId: "mp3",
        goalType: "reduce",
        progress: 90,
        targetValue: "12",
        currentValue: "13",
        startValue: "20",
        status: "on_track",
        owner: GF_OWNER,
        unit: "number",
        unitLabel: "min",
      }),
      mkKR("kp6", "Taxa de completude do onboarding ≥ 85%", {
        missionId: "mp3",
        goalType: "above",
        progress: 86,
        targetValue: "100",
        currentValue: "86",
        startValue: "58",
        lowThreshold: "85",
        status: "on_track",
        owner: BR_OWNER,
        unit: "percent",
        unitLabel: "%",
      }),
    ],
    tasks: [
      mkTask(
        "tp7",
        "Mapear jornada atual do usuário com analytics",
        true,
        GF_OWNER,
        { missionId: "mp3" },
      ),
      mkTask("tp8", "Prototipar novo fluxo em Figma", true, GF_OWNER, {
        missionId: "mp3",
      }),
      mkTask("tp9", "Testar protótipo com 10 usuários reais", true, CR_OWNER, {
        missionId: "mp3",
      }),
      mkTask("tp10", "Implementar e deployar novo fluxo", false, BS_OWNER, {
        missionId: "mp3",
      }),
    ],
    members: [
      mkSupporter(
        "mp3",
        BS_OWNER,
        GF_OWNER.id,
        deterministicCreatedAt("mp3", cycleInfo.quarterStart),
      ),
      mkSupporter(
        "mp3",
        CR_OWNER,
        GF_OWNER.id,
        deterministicCreatedAt("mp3", cycleInfo.quarterStart),
      ),
    ],
  },

  {
    id: "mp4",
    orgId: ORG_ID,
    cycleId: CURRENT_CYCLE_ID,
    parentId: null,
    depth: 0,
    path: ["mp4"],
    title: "Implementar sistema de feedback contínuo",
    description:
      "Criar ciclo de feedback estruturado entre gestores e times via plataforma.",
    ownerId: JM_OWNER.id,
    teamId: "team-produto",
    status: "active",
    visibility: "public",
    progress: 28,
    kanbanStatus: "doing",
    sortOrder: 6,
    dueDate: DUE_21_DAYS,
    completedAt: null,
    createdAt: deterministicCreatedAt("mp4", cycleInfo.quarterStart),
    updatedAt: deterministicUpdatedAt("mp4"),
    deletedAt: null,
    owner: JM_OWNER,
    team: { id: "team-produto", name: "Produto", color: "caramel" },
    tags: [TAG_CULTURA],
    keyResults: [
      mkKR("kp7", "Ciclo completo de feedback 360 implementado na plataforma", {
        missionId: "mp4",
        goalType: "reach",
        progress: 25,
        targetValue: "100",
        currentValue: "25",
        startValue: "0",
        expectedValue: "66",
        status: "off_track",
        owner: JM_OWNER,
        unit: "percent",
      }),
      mkKR("kp8", "80% dos gestores usando o sistema semanalmente", {
        missionId: "mp4",
        goalType: "above",
        progress: 30,
        targetValue: "100",
        currentValue: "30",
        startValue: "0",
        lowThreshold: "80",
        status: "off_track",
        owner: JM_OWNER,
        unit: "percent",
        unitLabel: "%",
      }),
    ],
    tasks: [
      mkTask("tp11", "Mapear requisitos com time de People", true, JM_OWNER, {
        missionId: "mp4",
      }),
      mkTask(
        "tp12",
        "Criar wireframes do módulo de feedback",
        false,
        GF_OWNER,
        { missionId: "mp4" },
      ),
      mkTask("tp13", "Desenvolver API de feedback 360", false, BS_OWNER, {
        missionId: "mp4",
      }),
      mkTask("tp14", "Treinar gestores no novo módulo", false, JM_OWNER, {
        missionId: "mp4",
      }),
    ],
    members: [
      mkSupporter(
        "mp4",
        GF_OWNER,
        JM_OWNER.id,
        deterministicCreatedAt("mp4", cycleInfo.quarterStart),
      ),
      mkSupporter(
        "mp4",
        BS_OWNER,
        JM_OWNER.id,
        deterministicCreatedAt("mp4", cycleInfo.quarterStart),
      ),
    ],
  },

  {
    id: "mp5",
    orgId: ORG_ID,
    cycleId: CURRENT_CYCLE_ID,
    parentId: null,
    depth: 0,
    path: ["mp5"],
    title: "Documentar e publicar APIs públicas v1",
    description:
      "Criar documentação completa das APIs públicas para integração de parceiros.",
    ownerId: BR_OWNER.id,
    teamId: "team-produto",
    status: "completed",
    visibility: "public",
    progress: 100,
    kanbanStatus: "done",
    sortOrder: 7,
    dueDate: null,
    completedAt: COMPLETED_7_DAYS_AGO,
    createdAt: deterministicCreatedAt("mp5", cycleInfo.quarterStart),
    updatedAt: deterministicUpdatedAt("mp5"),
    deletedAt: null,
    owner: BR_OWNER,
    team: { id: "team-produto", name: "Produto", color: "caramel" },
    tags: [TAG_ENGENHARIA],
    keyResults: [
      mkKR("kp9", "100% das APIs públicas documentadas no Swagger", {
        missionId: "mp5",
        goalType: "reach",
        progress: 100,
        targetValue: "100",
        currentValue: "100",
        startValue: "0",
        status: "completed",
        owner: BR_OWNER,
        unit: "percent",
      }),
    ],
    tasks: [
      mkTask("tp15", "Auditar todos os endpoints existentes", true, BR_OWNER, {
        missionId: "mp5",
      }),
      mkTask(
        "tp16",
        "Escrever documentação no Swagger/OpenAPI",
        true,
        BR_OWNER,
        { missionId: "mp5" },
      ),
      mkTask(
        "tp17",
        "Criar exemplos de uso para cada endpoint",
        true,
        BR_OWNER,
        { missionId: "mp5" },
      ),
      mkTask("tp18", "Publicar portal do desenvolvedor", true, BS_OWNER, {
        missionId: "mp5",
      }),
    ],
    members: [
      mkSupporter(
        "mp5",
        BS_OWNER,
        BR_OWNER.id,
        deterministicCreatedAt("mp5", cycleInfo.quarterStart),
      ),
    ],
  },
];

/* ——— Check-in mock data (generated with relative dates) ——— */

// Generate check-in dates for each KR
const i1Dates = generateCheckInsForKR("i1", 5);
const i2Dates = generateCheckInsForKR("i2", 2);
const i5Dates = generateCheckInsForKR("i5", 2);

// Time Produto — check-in dates
const kp1Dates = generateCheckInsForKR("kp1", 4);
const kp2Dates = generateCheckInsForKR("kp2", 3);
const kp3Dates = generateCheckInsForKR("kp3", 5);
const kp4Dates = generateCheckInsForKR("kp4", 3);
const kp5Dates = generateCheckInsForKR("kp5", 3);
const kp7Dates = generateCheckInsForKR("kp7", 4);
const kp9Dates = generateCheckInsForKR("kp9", 2);

export const MOCK_CHECKIN_HISTORY: Record<string, CheckIn[]> = {
  i1: [
    {
      id: "ck1",
      keyResultId: "i1",
      authorId: MS_OWNER.id,
      value: "30",
      previousValue: "22",
      confidence: "medium",
      note: "Fechamos 3 novos contratos essa semana, pipeline está avançando bem.",
      mentions: ["Ana Ferreira"],
      createdAt: i1Dates[0]?.dateIso ?? "",
      author: MS_OWNER,
    },
    {
      id: "ck2",
      keyResultId: "i1",
      authorId: MS_OWNER.id,
      value: "22",
      previousValue: "15",
      confidence: "medium",
      note: "Reunião com diretoria trouxe novas metas.",
      mentions: null,
      createdAt: i1Dates[1]?.dateIso ?? "",
      author: MS_OWNER,
    },
    {
      id: "ck3",
      keyResultId: "i1",
      authorId: AF_OWNER.id,
      value: "15",
      previousValue: "10",
      confidence: "low",
      note: null,
      mentions: null,
      createdAt: i1Dates[2]?.dateIso ?? "",
      author: AF_OWNER,
    },
    {
      id: "ck4",
      keyResultId: "i1",
      authorId: MS_OWNER.id,
      value: "10",
      previousValue: "5",
      confidence: "low",
      note: "Início do trimestre, pipeline sendo construído.",
      mentions: null,
      createdAt: i1Dates[3]?.dateIso ?? "",
      author: MS_OWNER,
    },
    {
      id: "ck5",
      keyResultId: "i1",
      authorId: AF_OWNER.id,
      value: "5",
      previousValue: "0",
      confidence: null,
      note: null,
      mentions: null,
      createdAt: i1Dates[4]?.dateIso ?? "",
      author: AF_OWNER,
    },
  ],
  i2: [
    {
      id: "ck6",
      keyResultId: "i2",
      authorId: JM_OWNER.id,
      value: "65",
      previousValue: "60",
      confidence: "high",
      note: "NPS subiu após melhorias no onboarding.",
      mentions: null,
      createdAt: i2Dates[0]?.dateIso ?? "",
      author: JM_OWNER,
    },
    {
      id: "ck7",
      keyResultId: "i2",
      authorId: JM_OWNER.id,
      value: "60",
      previousValue: "55",
      confidence: "high",
      note: null,
      mentions: null,
      createdAt: i2Dates[1]?.dateIso ?? "",
      author: JM_OWNER,
    },
  ],
  i5: [
    {
      id: "ck8",
      keyResultId: "i5",
      authorId: MS_OWNER.id,
      value: "72",
      previousValue: "65",
      confidence: "high",
      note: "Sprint finalizado, 3 features entregues.",
      mentions: null,
      createdAt: i5Dates[0]?.dateIso ?? "",
      author: MS_OWNER,
    },
    {
      id: "ck9",
      keyResultId: "i5",
      authorId: MS_OWNER.id,
      value: "65",
      previousValue: "50",
      confidence: "high",
      note: null,
      mentions: null,
      createdAt: i5Dates[1]?.dateIso ?? "",
      author: MS_OWNER,
    },
  ],

  // ── Time Produto ──────────────────────────────────────────────────────────

  kp1: [
    {
      id: "ckp1-1",
      keyResultId: "kp1",
      authorId: BS_OWNER.id,
      value: "80",
      previousValue: "68",
      confidence: "high",
      note: "2 features entregues no sprint. Kanban e listagem prontos, falta o painel de detalhes.",
      mentions: null,
      createdAt: kp1Dates[0]?.dateIso ?? "",
      author: BS_OWNER,
    },
    {
      id: "ckp1-2",
      keyResultId: "kp1",
      authorId: BS_OWNER.id,
      value: "68",
      previousValue: "52",
      confidence: "high",
      note: "Velocidade do time está ótima. Refinando edge cases antes de marcar feature como pronta.",
      mentions: null,
      createdAt: kp1Dates[1]?.dateIso ?? "",
      author: BS_OWNER,
    },
    {
      id: "ckp1-3",
      keyResultId: "kp1",
      authorId: GF_OWNER.id,
      value: "52",
      previousValue: "38",
      confidence: "medium",
      note: "Revisão de UX concluída. Ajustes no componente de GoalProgressBar integrados ao design.",
      mentions: null,
      createdAt: kp1Dates[2]?.dateIso ?? "",
      author: GF_OWNER,
    },
    {
      id: "ckp1-4",
      keyResultId: "kp1",
      authorId: BS_OWNER.id,
      value: "38",
      previousValue: "0",
      confidence: "medium",
      note: "Início do sprint. Especificações aprovadas e dev iniciado.",
      mentions: null,
      createdAt: kp1Dates[3]?.dateIso ?? "",
      author: BS_OWNER,
    },
  ],

  kp2: [
    {
      id: "ckp2-1",
      keyResultId: "kp2",
      authorId: GF_OWNER.id,
      value: "2",
      previousValue: "4",
      confidence: "high",
      note: "2 bugs críticos corrigidos. Pipeline de testes automatizados ajudando muito.",
      mentions: null,
      createdAt: kp2Dates[0]?.dateIso ?? "",
      author: GF_OWNER,
    },
    {
      id: "ckp2-2",
      keyResultId: "kp2",
      authorId: GF_OWNER.id,
      value: "4",
      previousValue: "6",
      confidence: "medium",
      note: "Ajustes de performance na árvore de missões reduziram bugs de rendering.",
      mentions: null,
      createdAt: kp2Dates[1]?.dateIso ?? "",
      author: GF_OWNER,
    },
    {
      id: "ckp2-3",
      keyResultId: "kp2",
      authorId: BS_OWNER.id,
      value: "6",
      previousValue: "8",
      confidence: "medium",
      note: "Identificamos origem dos bugs: estado compartilhado no context. Refatorando.",
      mentions: null,
      createdAt: kp2Dates[2]?.dateIso ?? "",
      author: BS_OWNER,
    },
  ],

  kp3: [
    {
      id: "ckp3-1",
      keyResultId: "kp3",
      authorId: CR_OWNER.id,
      value: "2252",
      previousValue: "2180",
      confidence: "medium",
      note: "Crescimento abaixo do esperado. Precisamos intensificar ações de ativação de novos usuários.",
      mentions: null,
      createdAt: kp3Dates[0]?.dateIso ?? "",
      author: CR_OWNER,
    },
    {
      id: "ckp3-2",
      keyResultId: "kp3",
      authorId: CR_OWNER.id,
      value: "2180",
      previousValue: "2090",
      confidence: "medium",
      note: null,
      mentions: null,
      createdAt: kp3Dates[1]?.dateIso ?? "",
      author: CR_OWNER,
    },
    {
      id: "ckp3-3",
      keyResultId: "kp3",
      authorId: CR_OWNER.id,
      value: "2090",
      previousValue: "2041",
      confidence: "low",
      note: "Campanha de reativação não atingiu resultado esperado. Repensando abordagem.",
      mentions: null,
      createdAt: kp3Dates[2]?.dateIso ?? "",
      author: CR_OWNER,
    },
    {
      id: "ckp3-4",
      keyResultId: "kp3",
      authorId: CR_OWNER.id,
      value: "2041",
      previousValue: "2020",
      confidence: "low",
      note: "Crescimento orgânico fraco. Precisamos de incentivos para novos usuários.",
      mentions: null,
      createdAt: kp3Dates[3]?.dateIso ?? "",
      author: CR_OWNER,
    },
    {
      id: "ckp3-5",
      keyResultId: "kp3",
      authorId: BS_OWNER.id,
      value: "2020",
      previousValue: "2000",
      confidence: "medium",
      note: "Baseline definido. Estratégia de crescimento apresentada para o time.",
      mentions: null,
      createdAt: kp3Dates[4]?.dateIso ?? "",
      author: BS_OWNER,
    },
  ],

  kp4: [
    {
      id: "ckp4-1",
      keyResultId: "kp4",
      authorId: CR_OWNER.id,
      value: "48",
      previousValue: "42",
      confidence: "medium",
      note: "NPS subiu após melhorias no onboarding, mas ainda abaixo da meta de 50.",
      mentions: null,
      createdAt: kp4Dates[0]?.dateIso ?? "",
      author: CR_OWNER,
    },
    {
      id: "ckp4-2",
      keyResultId: "kp4",
      authorId: CR_OWNER.id,
      value: "42",
      previousValue: "36",
      confidence: "medium",
      note: "Feedback de usuários: interface mais clara mas integrações ainda confusas.",
      mentions: null,
      createdAt: kp4Dates[1]?.dateIso ?? "",
      author: CR_OWNER,
    },
    {
      id: "ckp4-3",
      keyResultId: "kp4",
      authorId: CR_OWNER.id,
      value: "36",
      previousValue: "32",
      confidence: "low",
      note: "NPS inicial coletado. Principais reclamações: velocidade da plataforma e falta de mobile.",
      mentions: null,
      createdAt: kp4Dates[2]?.dateIso ?? "",
      author: CR_OWNER,
    },
  ],

  kp5: [
    {
      id: "ckp5-1",
      keyResultId: "kp5",
      authorId: GF_OWNER.id,
      value: "13",
      previousValue: "16",
      confidence: "high",
      note: "Novo fluxo validado com usuários beta. Redução de 35% no tempo médio. Ajustes finais em progresso.",
      mentions: null,
      createdAt: kp5Dates[0]?.dateIso ?? "",
      author: GF_OWNER,
    },
    {
      id: "ckp5-2",
      keyResultId: "kp5",
      authorId: GF_OWNER.id,
      value: "16",
      previousValue: "18",
      confidence: "high",
      note: "Protótipo testado com 10 usuários. Eliminamos 4 etapas redundantes do fluxo.",
      mentions: null,
      createdAt: kp5Dates[1]?.dateIso ?? "",
      author: GF_OWNER,
    },
    {
      id: "ckp5-3",
      keyResultId: "kp5",
      authorId: GF_OWNER.id,
      value: "18",
      previousValue: "20",
      confidence: "medium",
      note: "Mapeamento de jornada concluído. Identificamos gargalo no passo de configuração inicial.",
      mentions: null,
      createdAt: kp5Dates[2]?.dateIso ?? "",
      author: GF_OWNER,
    },
  ],

  kp7: [
    {
      id: "ckp7-1",
      keyResultId: "kp7",
      authorId: JM_OWNER.id,
      value: "25",
      previousValue: "20",
      confidence: "low",
      note: "Desenvolvimento mais lento que o esperado. Dependência de API do módulo de People ainda pendente.",
      mentions: null,
      createdAt: kp7Dates[0]?.dateIso ?? "",
      author: JM_OWNER,
    },
    {
      id: "ckp7-2",
      keyResultId: "kp7",
      authorId: JM_OWNER.id,
      value: "20",
      previousValue: "15",
      confidence: "low",
      note: "Wireframes aprovados. Início de desenvolvimento, mas cronograma está apertado.",
      mentions: null,
      createdAt: kp7Dates[1]?.dateIso ?? "",
      author: JM_OWNER,
    },
    {
      id: "ckp7-3",
      keyResultId: "kp7",
      authorId: JM_OWNER.id,
      value: "15",
      previousValue: "8",
      confidence: "medium",
      note: null,
      mentions: null,
      createdAt: kp7Dates[2]?.dateIso ?? "",
      author: JM_OWNER,
    },
    {
      id: "ckp7-4",
      keyResultId: "kp7",
      authorId: JM_OWNER.id,
      value: "8",
      previousValue: "0",
      confidence: "medium",
      note: "Requisitos mapeados com time de People. Arquitetura definida.",
      mentions: null,
      createdAt: kp7Dates[3]?.dateIso ?? "",
      author: JM_OWNER,
    },
  ],

  kp9: [
    {
      id: "ckp9-1",
      keyResultId: "kp9",
      authorId: BR_OWNER.id,
      value: "100",
      previousValue: "85",
      confidence: "high",
      note: "Portal do desenvolvedor publicado. Todas as APIs documentadas e revisadas. Missão concluída!",
      mentions: null,
      createdAt: kp9Dates[0]?.dateIso ?? "",
      author: BR_OWNER,
    },
    {
      id: "ckp9-2",
      keyResultId: "kp9",
      authorId: BR_OWNER.id,
      value: "85",
      previousValue: "60",
      confidence: "high",
      note: "Últimas 3 APIs documentadas. Revisão de qualidade em andamento.",
      mentions: null,
      createdAt: kp9Dates[1]?.dateIso ?? "",
      author: BR_OWNER,
    },
  ],
};

export const MOCK_CHART_DATA: Record<string, ChartPoint[]> = {
  i1: [
    { date: "Jan", value: 0 },
    { date: "Fev 1", value: 5 },
    { date: "Fev 2", value: 10 },
    { date: "Fev 3", value: 15 },
    { date: "Fev 4", value: 22 },
    { date: "Mar 1", value: 30 },
  ],
  i2: [
    { date: "Jan", value: 45 },
    { date: "Fev 1", value: 50 },
    { date: "Fev 2", value: 52 },
    { date: "Fev 3", value: 55 },
    { date: "Fev 4", value: 60 },
    { date: "Mar 1", value: 65 },
  ],
  i5: [
    { date: "Jan", value: 20 },
    { date: "Fev 1", value: 35 },
    { date: "Fev 2", value: 50 },
    { date: "Fev 3", value: 55 },
    { date: "Fev 4", value: 65 },
    { date: "Mar 1", value: 72 },
  ],
};

/* ——— Date formatting for check-ins ——— */

export function formatCheckinDate(isoDate: string): string {
  const d = new Date(isoDate);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
}
