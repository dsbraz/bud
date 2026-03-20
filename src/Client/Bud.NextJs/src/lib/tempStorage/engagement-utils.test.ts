import { describe, it, expect } from "vitest";
import {
  flattenMissions,
  getKrOwnerIds,
  getProgressByOwner,
  daysSinceUserCheckin,
  formatDaysAgo,
  countCheckInsForUser,
  calculateCheckInStreakForUser,
  calculateMissionsEngagement,
  calculateSurveyEngagement,
  calculateTrend,
  calculateUserTrend,
} from "./engagement-utils";
import type { Mission, CheckIn } from "@/types";
import { today, addDays, toIsoDateTime } from "@/lib/seed-utils";

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeMission(overrides: Partial<Mission> = {}): Mission {
  return {
    id: "m1",
    orgId: "org-1",
    cycleId: null,
    parentId: null,
    depth: 0,
    path: ["m1"],
    title: "Missão Teste",
    description: null,
    ownerId: "user-1",
    teamId: "team-1",
    status: "active",
    visibility: "public",
    progress: 50,
    kanbanStatus: "doing",
    sortOrder: 0,
    dueDate: null,
    completedAt: null,
    createdAt: "2025-01-01T00:00:00Z",
    updatedAt: "2025-01-01T00:00:00Z",
    deletedAt: null,
    ...overrides,
  };
}

function makeCheckIn(userId: string, daysAgo: number, overrides: Partial<CheckIn> = {}): CheckIn {
  const date = addDays(today(), -daysAgo);
  return {
    id: `ci-${userId}-${daysAgo}`,
    keyResultId: "kr-1",
    authorId: userId,
    value: "50",
    previousValue: null,
    confidence: "medium",
    note: null,
    mentions: null,
    createdAt: toIsoDateTime(date),
    ...overrides,
  };
}

// ── flattenMissions ───────────────────────────────────────────────────────────

describe("flattenMissions", () => {
  it("retorna lista plana para missão sem filhos", () => {
    const missions = [makeMission()];
    expect(flattenMissions(missions)).toHaveLength(1);
  });

  it("inclui missões filhas no resultado", () => {
    const parent = makeMission({
      id: "parent",
      children: [makeMission({ id: "child" })],
    });
    const result = flattenMissions([parent]);
    expect(result).toHaveLength(2);
    expect(result.map((m) => m.id)).toContain("child");
  });

  it("retorna lista vazia para entrada vazia", () => {
    expect(flattenMissions([])).toHaveLength(0);
  });
});

// ── getKrOwnerIds ─────────────────────────────────────────────────────────────

describe("getKrOwnerIds", () => {
  it("retorna IDs dos owners de KRs", () => {
    const mission = makeMission({
      keyResults: [
        { id: "kr1", owner: { id: "user-1", firstName: "A", lastName: "B", initials: null } } as any,
        { id: "kr2", owner: { id: "user-2", firstName: "C", lastName: "D", initials: null } } as any,
      ],
    });
    const result = getKrOwnerIds([mission]);
    expect(result.has("user-1")).toBe(true);
    expect(result.has("user-2")).toBe(true);
  });

  it("ignora KRs sem owner", () => {
    const mission = makeMission({
      keyResults: [{ id: "kr1", owner: null } as any],
    });
    const result = getKrOwnerIds([mission]);
    expect(result.size).toBe(0);
  });
});

// ── getProgressByOwner ────────────────────────────────────────────────────────

describe("getProgressByOwner", () => {
  it("agrega progresso por owner", () => {
    const mission = makeMission({
      keyResults: [
        { id: "kr1", progress: 40, owner: { id: "u1", firstName: "A", lastName: "B", initials: null } } as any,
        { id: "kr2", progress: 60, owner: { id: "u1", firstName: "A", lastName: "B", initials: null } } as any,
      ],
    });
    const result = getProgressByOwner([mission]);
    const u1 = result.get("u1");
    expect(u1?.total).toBe(100);
    expect(u1?.count).toBe(2);
  });
});

// ── daysSinceUserCheckin ──────────────────────────────────────────────────────

describe("daysSinceUserCheckin", () => {
  it("retorna 0 para check-in hoje", () => {
    const history = { "kr-1": [makeCheckIn("u1", 0)] };
    expect(daysSinceUserCheckin("u1", history)).toBe(0);
  });

  it("retorna 3 para check-in há 3 dias", () => {
    const history = { "kr-1": [makeCheckIn("u1", 3)] };
    expect(daysSinceUserCheckin("u1", history)).toBe(3);
  });

  it("retorna 14 se sem check-ins", () => {
    expect(daysSinceUserCheckin("u1", {})).toBe(14);
  });

  it("ignora check-ins de outros usuários", () => {
    const history = { "kr-1": [makeCheckIn("u2", 1)] };
    expect(daysSinceUserCheckin("u1", history)).toBe(14);
  });

  it("usa o mais recente quando há vários check-ins", () => {
    const history = {
      "kr-1": [makeCheckIn("u1", 5), makeCheckIn("u1", 2), makeCheckIn("u1", 10)],
    };
    expect(daysSinceUserCheckin("u1", history)).toBe(2);
  });
});

// ── formatDaysAgo ─────────────────────────────────────────────────────────────

describe("formatDaysAgo", () => {
  it("retorna 'hoje' para 0", () => {
    expect(formatDaysAgo(0)).toBe("hoje");
  });

  it("retorna 'há 1 dia' para 1", () => {
    expect(formatDaysAgo(1)).toBe("há 1 dia");
  });

  it("retorna 'há N dias' para N > 1", () => {
    expect(formatDaysAgo(5)).toBe("há 5 dias");
  });
});

// ── countCheckInsForUser ──────────────────────────────────────────────────────

describe("countCheckInsForUser", () => {
  it("conta apenas check-ins do usuário nos últimos N dias", () => {
    const history = {
      "kr-1": [
        makeCheckIn("u1", 5),
        makeCheckIn("u1", 10),
        makeCheckIn("u1", 35), // Fora do range de 30 dias
        makeCheckIn("u2", 5),  // Outro usuário
      ],
    };
    expect(countCheckInsForUser("u1", history, 30)).toBe(2);
  });

  it("retorna 0 se sem check-ins", () => {
    expect(countCheckInsForUser("u1", {}, 30)).toBe(0);
  });
});

// ── calculateCheckInStreakForUser ─────────────────────────────────────────────

describe("calculateCheckInStreakForUser", () => {
  it("retorna 0 se sem check-ins", () => {
    expect(calculateCheckInStreakForUser("u1", {})).toBe(0);
  });

  it("conta semanas consecutivas", () => {
    // Check-ins nas últimas 3 semanas
    const history = {
      "kr-1": [
        makeCheckIn("u1", 3),   // Esta semana
        makeCheckIn("u1", 10),  // Semana passada
        makeCheckIn("u1", 17),  // 2 semanas atrás
      ],
    };
    const streak = calculateCheckInStreakForUser("u1", history);
    expect(streak).toBeGreaterThanOrEqual(2);
  });
});

// ── calculateMissionsEngagement ───────────────────────────────────────────────

describe("calculateMissionsEngagement", () => {
  it("retorna 50 quando sem missões", () => {
    expect(calculateMissionsEngagement([], {})).toBe(50);
  });

  it("calcula % de KRs com check-ins", () => {
    const mission = makeMission({
      keyResults: [
        { id: "kr1" } as any,
        { id: "kr2" } as any,
      ],
    });
    const history = { "kr1": [makeCheckIn("u1", 1)] }; // 1 de 2 KRs tem check-in
    expect(calculateMissionsEngagement([mission], history)).toBe(50);
  });
});

// ── calculateSurveyEngagement ─────────────────────────────────────────────────

describe("calculateSurveyEngagement", () => {
  it("retorna 70 quando sem surveys ativas", () => {
    expect(calculateSurveyEngagement([])).toBe(70);
  });

  it("calcula média de completion rate das surveys ativas", () => {
    const surveys = [
      { status: "active", completionRate: 80 },
      { status: "active", completionRate: 60 },
    ];
    expect(calculateSurveyEngagement(surveys)).toBe(70);
  });

  it("ignora surveys arquivadas", () => {
    const surveys = [
      { status: "archived", completionRate: 10 },
      { status: "active", completionRate: 80 },
    ];
    expect(calculateSurveyEngagement(surveys)).toBe(80);
  });
});

// ── calculateTrend ────────────────────────────────────────────────────────────

describe("calculateTrend", () => {
  it("retorna up quando há mais check-ins recentes", () => {
    const now = today();
    const history = {
      "kr-1": [
        // 5 check-ins nas últimas 2 semanas
        { id: "c1", authorId: "u1", createdAt: toIsoDateTime(addDays(now, -3)), keyResultId: "kr-1", value: "1", previousValue: null, confidence: null, note: null, mentions: null },
        { id: "c2", authorId: "u1", createdAt: toIsoDateTime(addDays(now, -5)), keyResultId: "kr-1", value: "1", previousValue: null, confidence: null, note: null, mentions: null },
        { id: "c3", authorId: "u1", createdAt: toIsoDateTime(addDays(now, -7)), keyResultId: "kr-1", value: "1", previousValue: null, confidence: null, note: null, mentions: null },
        // 1 check-in entre 2-4 semanas
        { id: "c4", authorId: "u1", createdAt: toIsoDateTime(addDays(now, -25)), keyResultId: "kr-1", value: "1", previousValue: null, confidence: null, note: null, mentions: null },
      ] as CheckIn[],
    };
    const result = calculateTrend(history);
    expect(result.direction).toBe("up");
  });
});

// ── calculateUserTrend ────────────────────────────────────────────────────────

describe("calculateUserTrend", () => {
  it("retorna stable quando sem check-ins", () => {
    expect(calculateUserTrend("u1", {})).toBe("stable");
  });

  it("retorna up quando mais check-ins recentes", () => {
    const now = today();
    const history = {
      "kr-1": [
        { id: "c1", authorId: "u1", createdAt: toIsoDateTime(addDays(now, -3)), keyResultId: "kr-1", value: "1", previousValue: null, confidence: null, note: null, mentions: null },
        { id: "c2", authorId: "u1", createdAt: toIsoDateTime(addDays(now, -6)), keyResultId: "kr-1", value: "1", previousValue: null, confidence: null, note: null, mentions: null },
      ] as CheckIn[],
    };
    // 2 recentes, 0 anteriores → up
    expect(calculateUserTrend("u1", history)).toBe("up");
  });
});
