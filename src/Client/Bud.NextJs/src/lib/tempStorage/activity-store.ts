// ─── Activity Store ───────────────────────────────────────────────────────────
// Persistência e seed de atividades dos usuários.
// TODO: Integrar com autenticação real para rastreamento de logins.

import type {
  UserActivity,
  ActivityEntityType,
} from "@/types/activity/activity";

import {
  today,
  addDays,
  toIsoDateTime,
  hashString,
} from "@/lib/tempStorage/seed-utils";

// ── Schema ────────────────────────────────────────────────────────────────────

const STORE_SCHEMA_VERSION = 1;
const STORE_KEY = "bud_activity_store_v1";

export interface ActivityStoreSnapshot {
  version: number;
  activities: UserActivity[];
}

const TEAM_PRODUTO_MEMBERS = [
  { id: "bs", name: "Bruno Santos" },
  { id: "cr", name: "Carla Rodrigues" },
  { id: "gf", name: "Gabriel Ferreira" },
  { id: "jm", name: "Juliana Mendes" },
  { id: "br", name: "Beatriz Ramos" },
];

const TEAM_KR_IDS = [
  "kp1",
  "kp2",
  "kp3",
  "kp4",
  "kp5",
  "kp6",
  "kp7",
  "kp8",
  "kp9",
];

function makeActivityId(
  type: string,
  userId: string,
  daysAgo: number,
  index: number,
): string {
  const hash = hashString(`${type}-${userId}-${daysAgo}-${index}`);
  return `act-${hash.toString(16)}`;
}

function generateLoginActivities(userId: string): UserActivity[] {
  const activities: UserActivity[] = [];
  const now = today();
  const hash = hashString(userId);

  // Padrão de acesso: bs=muito ativo, cr=ativo, gf=regular, jm=irregular, br=ativo
  const accessPatterns: Record<string, number> = {
    bs: 0.85, // 85% dos dias
    cr: 0.75,
    gf: 0.65,
    jm: 0.45, // Mais irregular — vai gerar alertas
    br: 0.7,
  };
  const accessRate = accessPatterns[userId] ?? 0.65;

  for (let daysAgo = 0; daysAgo <= 30; daysAgo++) {
    // Skipa fins de semana (dia 0=dom, 6=sab)
    const d = addDays(now, -daysAgo);
    const weekday = d.getDay();
    if (weekday === 0 || weekday === 6) continue;

    // Determina se o usuário acessou neste dia (determinístico)
    const dayHash = hashString(`${userId}-login-${daysAgo}`);
    const accessed = (dayHash % 100) / 100 < accessRate;
    if (!accessed) continue;

    activities.push({
      id: makeActivityId("login", userId, daysAgo, hash),
      userId,
      type: "login",
      entityId: null,
      entityType: null,
      metadata: null,
      createdAt: toIsoDateTime(d),
    });
  }

  return activities;
}

/**
 * Gera atividades de check-in para um usuário.
 */
function generateCheckInActivities(userId: string): UserActivity[] {
  const activities: UserActivity[] = [];
  const now = today();

  // Padrões de check-in por usuário
  const checkinPatterns: Record<string, number[]> = {
    bs: [0, 7, 14, 21, 28], // Weekly — streak 4+
    cr: [2, 9, 16, 25], // Weekly offset — streak 3
    gf: [1, 8, 17, 29], // Irregular mas consistente — streak 3
    jm: [5, 20], // Esparso — streak 1, vai gerar alerta
    br: [3, 10, 17, 24], // Regular — streak 3
  };

  const pattern = checkinPatterns[userId] ?? [7, 14, 21];

  pattern.forEach((daysAgo, index) => {
    // Escolhe um KR do time de forma determinística
    const krIndex = hashString(`${userId}-kr-${daysAgo}`) % TEAM_KR_IDS.length;
    const krId = TEAM_KR_IDS[krIndex]!;

    const date = addDays(now, -daysAgo);

    // Cria check-in
    activities.push({
      id: makeActivityId("checkin_create", userId, daysAgo, index),
      userId,
      type: "checkin_create",
      entityId: krId,
      entityType: "checkin" as ActivityEntityType,
      metadata: { keyResultId: krId },
      createdAt: toIsoDateTime(date),
    });

    // Às vezes atualiza o check-in (bs e cr são mais diligentes)
    if (userId === "bs" || userId === "cr") {
      activities.push({
        id: makeActivityId("checkin_update", userId, daysAgo, index + 100),
        userId,
        type: "checkin_update",
        entityId: krId,
        entityType: "checkin" as ActivityEntityType,
        metadata: { keyResultId: krId },
        createdAt: toIsoDateTime(addDays(date, 1)),
      });
    }
  });

  return activities;
}

/**
 * Gera atividades de pesquisa para um usuário.
 */
function generateSurveyActivities(userId: string): UserActivity[] {
  const activities: UserActivity[] = [];
  const now = today();

  // Padrões de resposta por usuário (surveyId → daysAgo de quando respondeu)
  const surveyPatterns: Record<string, Record<string, number | null>> = {
    bs: { "10": 15, "11": 8, "12": null }, // Pendente: pesquisa 12
    cr: { "10": 14, "11": 7, "12": 1 }, // Respondeu tudo
    gf: { "10": 16, "11": 9, "12": 2 }, // Respondeu tudo
    jm: { "10": null, "11": null, "12": null }, // Pendente: todas! — alerta crítico
    br: { "10": 13, "11": 6, "12": 3 }, // Respondeu tudo
  };

  const pattern = surveyPatterns[userId] ?? {};

  for (const [surveyId, daysAgo] of Object.entries(pattern)) {
    if (daysAgo === null) continue; // Não respondeu

    const date = addDays(now, -(daysAgo + 2)); // Start 2 dias antes

    // survey_start
    activities.push({
      id: makeActivityId(
        "survey_start",
        userId,
        daysAgo + 2,
        hashString(surveyId),
      ),
      userId,
      type: "survey_start",
      entityId: surveyId,
      entityType: "survey" as ActivityEntityType,
      metadata: { surveyId },
      createdAt: toIsoDateTime(date),
    });

    // survey_complete
    activities.push({
      id: makeActivityId(
        "survey_complete",
        userId,
        daysAgo,
        hashString(surveyId) + 1,
      ),
      userId,
      type: "survey_complete",
      entityId: surveyId,
      entityType: "survey" as ActivityEntityType,
      metadata: { surveyId, responseTimeHours: daysAgo > 0 ? 48 : 6 },
      createdAt: toIsoDateTime(addDays(now, -daysAgo)),
    });
  }

  return activities;
}

/**
 * Gera atividades de atualização de missões/KRs.
 */
function generateMissionActivities(userId: string): UserActivity[] {
  const activities: UserActivity[] = [];
  const now = today();

  // bs e gf atualizam KRs mais frequentemente
  const updateDays: Record<string, number[]> = {
    bs: [1, 5, 10, 15],
    cr: [3, 11, 20],
    gf: [2, 7, 14, 22],
    jm: [25], // Raramente
    br: [4, 12, 19],
  };

  const days = updateDays[userId] ?? [];

  days.forEach((daysAgo, index) => {
    const krIndex =
      hashString(`${userId}-mission-${daysAgo}`) % TEAM_KR_IDS.length;
    const krId = TEAM_KR_IDS[krIndex]!;

    activities.push({
      id: makeActivityId("kr_update", userId, daysAgo, index),
      userId,
      type: "kr_update",
      entityId: krId,
      entityType: "key-result" as ActivityEntityType,
      metadata: { keyResultId: krId },
      createdAt: toIsoDateTime(addDays(now, -daysAgo)),
    });
  });

  return activities;
}

/**
 * Gera o seed completo de atividades para o time Produto.
 */
function generateSeedActivities(): UserActivity[] {
  const activities: UserActivity[] = [];

  for (const member of TEAM_PRODUTO_MEMBERS) {
    activities.push(...generateLoginActivities(member.id));
    activities.push(...generateCheckInActivities(member.id));
    activities.push(...generateSurveyActivities(member.id));
    activities.push(...generateMissionActivities(member.id));
  }

  // Ordena por data decrescente
  activities.sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );

  return activities;
}

// ── Persistência ──────────────────────────────────────────────────────────────

export function loadActivitySnapshot(): ActivityStoreSnapshot {
  try {
    const raw = localStorage.getItem(STORE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as ActivityStoreSnapshot;
      if (parsed.version === STORE_SCHEMA_VERSION) return parsed;
    }
  } catch {
    // Ignora erros de parse
  }

  return getInitialActivitySnapshot();
}

export function saveActivitySnapshot(snapshot: ActivityStoreSnapshot): void {
  try {
    localStorage.setItem(STORE_KEY, JSON.stringify(snapshot));
  } catch {
    // Ignora erros de storage
  }
}

export function getInitialActivitySnapshot(): ActivityStoreSnapshot {
  return {
    version: STORE_SCHEMA_VERSION,
    activities: generateSeedActivities(),
  };
}

// ── Queries ───────────────────────────────────────────────────────────────────

/**
 * Retorna atividades de um usuário nos últimos N dias.
 */
export function getActivitiesForUser(
  activities: UserActivity[],
  userId: string,
  days: number = 30,
): UserActivity[] {
  const cutoff = addDays(today(), -days);
  return activities.filter(
    (a) => a.userId === userId && new Date(a.createdAt) >= cutoff,
  );
}

/**
 * Conta dias únicos de acesso (login) para um usuário nos últimos N dias.
 */
export function countActiveDaysForUser(
  activities: UserActivity[],
  userId: string,
  days: number = 30,
): number {
  const cutoff = addDays(today(), -days);
  const loginDays = new Set<string>();

  for (const activity of activities) {
    if (
      activity.userId === userId &&
      activity.type === "login" &&
      new Date(activity.createdAt) >= cutoff
    ) {
      const day = activity.createdAt.split("T")[0]!;
      loginDays.add(day);
    }
  }

  return loginDays.size;
}

/**
 * Retorna a data da última atividade de um usuário.
 */
export function getLastActiveAt(
  activities: UserActivity[],
  userId: string,
): string | null {
  const userActivities = activities
    .filter((a) => a.userId === userId)
    .sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    );

  return userActivities[0]?.createdAt ?? null;
}

/**
 * Conta pesquisas respondidas por um usuário.
 */
export function countSurveysCompletedForUser(
  activities: UserActivity[],
  userId: string,
  surveyIds: string[],
): number {
  const completedIds = new Set<string>();
  for (const activity of activities) {
    if (
      activity.userId === userId &&
      activity.type === "survey_complete" &&
      activity.entityId &&
      surveyIds.includes(activity.entityId)
    ) {
      completedIds.add(activity.entityId);
    }
  }
  return completedIds.size;
}

/**
 * Calcula tempo médio de resposta a pesquisas (em horas) para um usuário.
 */
export function calcAvgSurveyResponseTime(
  activities: UserActivity[],
  userId: string,
): number | null {
  const completions = activities.filter(
    (a) => a.userId === userId && a.type === "survey_complete",
  );

  if (completions.length === 0) return null;

  const times: number[] = [];
  for (const completion of completions) {
    const meta = completion.metadata;
    if (meta && typeof meta["responseTimeHours"] === "number") {
      times.push(meta["responseTimeHours"] as number);
    }
  }

  if (times.length === 0) return null;
  return Math.round(times.reduce((s, v) => s + v, 0) / times.length);
}

// ── Pulse insights ────────────────────────────────────────────────────────────

type PulseWorkload = "low" | "normal" | "high" | "overload";

interface PulseInsights {
  lastPulseSentiment: number | null;
  lastPulseDate: string | null;
  lastPulseWorkload: PulseWorkload | null;
  pulseSentimentTrend: "improving" | "stable" | "declining" | null;
}

/**
 * Retorna os dados do último pulse de um colaborador.
 *
 * Em produção, esses valores viriam de uma query nas respostas de pesquisas
 * não-anônimas vinculadas ao usuário. No mock, são determinísticos por userId
 * para simular o que seria visível ao gestor direto.
 *
 * Reflete o seed de activities-store: bs/cr/gf/br responderam surveys 10–11,
 * jm não respondeu nenhuma (todos pendentes).
 */
export function getPulseInsights(userId: string): PulseInsights {
  const now = today();

  const MOCK: Record<string, PulseInsights> = {
    bs: {
      lastPulseSentiment: 4,
      lastPulseDate: toIsoDateTime(addDays(now, -8)),
      lastPulseWorkload: "normal",
      pulseSentimentTrend: "improving",
    },
    cr: {
      lastPulseSentiment: 4,
      lastPulseDate: toIsoDateTime(addDays(now, -7)),
      lastPulseWorkload: "high",
      pulseSentimentTrend: "stable",
    },
    gf: {
      lastPulseSentiment: 5,
      lastPulseDate: toIsoDateTime(addDays(now, -9)),
      lastPulseWorkload: "normal",
      pulseSentimentTrend: "improving",
    },
    jm: {
      lastPulseSentiment: null,
      lastPulseDate: null,
      lastPulseWorkload: null,
      pulseSentimentTrend: null,
    },
    br: {
      lastPulseSentiment: 3,
      lastPulseDate: toIsoDateTime(addDays(now, -6)),
      lastPulseWorkload: "high",
      pulseSentimentTrend: "declining",
    },
  };

  return (
    MOCK[userId] ?? {
      lastPulseSentiment: null,
      lastPulseDate: null,
      lastPulseWorkload: null,
      pulseSentimentTrend: null,
    }
  );
}

/**
 * Retorna IDs de pesquisas que um usuário NÃO respondeu (mas deveria).
 */
export function getPendingSurveyIds(
  activities: UserActivity[],
  userId: string,
  allSurveyIds: string[],
): string[] {
  const completedIds = new Set(
    activities
      .filter(
        (a) =>
          a.userId === userId && a.type === "survey_complete" && a.entityId,
      )
      .map((a) => a.entityId!),
  );
  return allSurveyIds.filter((id) => !completedIds.has(id));
}

/**
 * Adiciona uma nova atividade ao snapshot.
 */
export function addActivity(
  snapshot: ActivityStoreSnapshot,
  activity: Omit<UserActivity, "id" | "createdAt">,
): ActivityStoreSnapshot {
  const newActivity: UserActivity = {
    ...activity,
    id: makeActivityId(activity.type, activity.userId, 0, Date.now()),
    createdAt: toIsoDateTime(new Date()),
  };

  return {
    ...snapshot,
    activities: [newActivity, ...snapshot.activities],
  };
}
