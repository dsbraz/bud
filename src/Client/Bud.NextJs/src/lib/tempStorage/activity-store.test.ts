import { describe, it, expect } from "vitest";
import {
  getInitialActivitySnapshot,
  getActivitiesForUser,
  countActiveDaysForUser,
  getLastActiveAt,
  countSurveysCompletedForUser,
  calcAvgSurveyResponseTime,
  getPendingSurveyIds,
  addActivity,
} from "./activity-store";
import type { UserActivity } from "@/types/activity";
import { today, addDays, toIsoDateTime } from "@/lib/seed-utils";

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeActivity(
  userId: string,
  type: UserActivity["type"],
  daysAgo: number,
  overrides: Partial<UserActivity> = {},
): UserActivity {
  return {
    id: `act-${userId}-${type}-${daysAgo}`,
    userId,
    type,
    entityId: null,
    entityType: null,
    metadata: null,
    createdAt: toIsoDateTime(addDays(today(), -daysAgo)),
    ...overrides,
  };
}

// ── getInitialActivitySnapshot ────────────────────────────────────────────────

describe("getInitialActivitySnapshot", () => {
  it("retorna snapshot com versão 1", () => {
    const snapshot = getInitialActivitySnapshot();
    expect(snapshot.version).toBe(1);
  });

  it("retorna atividades para os membros do time Produto", () => {
    const snapshot = getInitialActivitySnapshot();
    const userIds = new Set(snapshot.activities.map((a) => a.userId));
    expect(userIds.has("bs")).toBe(true);
    expect(userIds.has("cr")).toBe(true);
    expect(userIds.has("gf")).toBe(true);
    expect(userIds.has("jm")).toBe(true);
    expect(userIds.has("br")).toBe(true);
  });

  it("inclui atividades de login", () => {
    const snapshot = getInitialActivitySnapshot();
    const logins = snapshot.activities.filter((a) => a.type === "login");
    expect(logins.length).toBeGreaterThan(0);
  });

  it("inclui atividades de check-in", () => {
    const snapshot = getInitialActivitySnapshot();
    const checkIns = snapshot.activities.filter((a) => a.type === "checkin_create");
    expect(checkIns.length).toBeGreaterThan(0);
  });

  it("inclui atividades de survey", () => {
    const snapshot = getInitialActivitySnapshot();
    const surveys = snapshot.activities.filter((a) => a.type === "survey_complete");
    expect(surveys.length).toBeGreaterThan(0);
  });
});

// ── getActivitiesForUser ──────────────────────────────────────────────────────

describe("getActivitiesForUser", () => {
  it("filtra por userId", () => {
    const activities = [
      makeActivity("u1", "login", 1),
      makeActivity("u2", "login", 1),
    ];
    const result = getActivitiesForUser(activities, "u1");
    expect(result.every((a) => a.userId === "u1")).toBe(true);
    expect(result).toHaveLength(1);
  });

  it("filtra pelo período de dias", () => {
    const activities = [
      makeActivity("u1", "login", 5),
      makeActivity("u1", "login", 35), // Fora do período
    ];
    const result = getActivitiesForUser(activities, "u1", 30);
    expect(result).toHaveLength(1);
  });
});

// ── countActiveDaysForUser ────────────────────────────────────────────────────

describe("countActiveDaysForUser", () => {
  it("conta dias únicos de login", () => {
    const activities = [
      makeActivity("u1", "login", 1),
      makeActivity("u1", "login", 1), // Mesmo dia — conta 1
      makeActivity("u1", "login", 3),
    ];
    expect(countActiveDaysForUser(activities, "u1")).toBe(2);
  });

  it("ignora atividades que não são login", () => {
    const activities = [
      makeActivity("u1", "checkin_create", 1),
    ];
    expect(countActiveDaysForUser(activities, "u1")).toBe(0);
  });

  it("retorna 0 sem atividades", () => {
    expect(countActiveDaysForUser([], "u1")).toBe(0);
  });
});

// ── getLastActiveAt ───────────────────────────────────────────────────────────

describe("getLastActiveAt", () => {
  it("retorna a data mais recente", () => {
    const now = today();
    const activities = [
      makeActivity("u1", "login", 5),
      makeActivity("u1", "login", 1),
      makeActivity("u1", "login", 10),
    ];
    const result = getLastActiveAt(activities, "u1");
    const expected = toIsoDateTime(addDays(now, -1));
    // Compara só o dia (não o tempo exato)
    expect(result?.split("T")[0]).toBe(expected.split("T")[0]);
  });

  it("retorna null quando sem atividades", () => {
    expect(getLastActiveAt([], "u1")).toBeNull();
  });
});

// ── countSurveysCompletedForUser ──────────────────────────────────────────────

describe("countSurveysCompletedForUser", () => {
  it("conta surveys completadas pelo usuário", () => {
    const activities = [
      makeActivity("u1", "survey_complete", 5, { entityId: "s1", entityType: "survey" }),
      makeActivity("u1", "survey_complete", 3, { entityId: "s2", entityType: "survey" }),
      makeActivity("u1", "survey_start", 4, { entityId: "s3", entityType: "survey" }), // Não completada
    ];
    expect(countSurveysCompletedForUser(activities, "u1", ["s1", "s2", "s3"])).toBe(2);
  });

  it("retorna 0 se não completou nenhuma", () => {
    expect(countSurveysCompletedForUser([], "u1", ["s1", "s2"])).toBe(0);
  });
});

// ── calcAvgSurveyResponseTime ─────────────────────────────────────────────────

describe("calcAvgSurveyResponseTime", () => {
  it("calcula média de tempo de resposta", () => {
    const activities = [
      makeActivity("u1", "survey_complete", 5, { metadata: { responseTimeHours: 12 } }),
      makeActivity("u1", "survey_complete", 3, { metadata: { responseTimeHours: 24 } }),
    ];
    expect(calcAvgSurveyResponseTime(activities, "u1")).toBe(18);
  });

  it("retorna null quando sem completions", () => {
    expect(calcAvgSurveyResponseTime([], "u1")).toBeNull();
  });
});

// ── getPendingSurveyIds ───────────────────────────────────────────────────────

describe("getPendingSurveyIds", () => {
  it("retorna surveys não respondidas", () => {
    const activities = [
      makeActivity("u1", "survey_complete", 5, { entityId: "s1", entityType: "survey" }),
    ];
    const result = getPendingSurveyIds(activities, "u1", ["s1", "s2", "s3"]);
    expect(result).toContain("s2");
    expect(result).toContain("s3");
    expect(result).not.toContain("s1");
  });

  it("retorna todas as surveys quando sem completions", () => {
    const result = getPendingSurveyIds([], "u1", ["s1", "s2"]);
    expect(result).toHaveLength(2);
  });
});

// ── addActivity ───────────────────────────────────────────────────────────────

describe("addActivity", () => {
  it("adiciona nova atividade ao snapshot", () => {
    const snapshot = { version: 1, activities: [] };
    const result = addActivity(snapshot, {
      userId: "u1",
      type: "login",
      entityId: null,
      entityType: null,
      metadata: null,
    });
    expect(result.activities).toHaveLength(1);
    expect(result.activities[0]?.userId).toBe("u1");
    expect(result.activities[0]?.type).toBe("login");
  });

  it("gera id e createdAt automaticamente", () => {
    const snapshot = { version: 1, activities: [] };
    const result = addActivity(snapshot, {
      userId: "u1",
      type: "login",
      entityId: null,
      entityType: null,
      metadata: null,
    });
    expect(result.activities[0]?.id).toBeDefined();
    expect(result.activities[0]?.createdAt).toBeDefined();
  });

  it("não muta o snapshot original", () => {
    const snapshot = { version: 1, activities: [] };
    addActivity(snapshot, {
      userId: "u1",
      type: "login",
      entityId: null,
      entityType: null,
      metadata: null,
    });
    expect(snapshot.activities).toHaveLength(0);
  });
});
