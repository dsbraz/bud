import { describe, it, expect } from "vitest";
import {
  calculateHabitMetrics,
  calculateAvgConfidence,
  calculateEngagementScore,
  calculatePerformanceScore,
  determineAlertLevel,
  determineHealthStatus,
  buildAlertMessages,
  buildMemberEngagementSummary,
  aggregateTeamEngagement,
} from "./engagement-aggregation";
import type { CheckIn } from "@/types";
import type { UserActivity } from "@/types/activity";
import type { TeamMember } from "@/types/team";
import type { UserEngagementSummary } from "@/types/engagement";
import { today, addDays, toIsoDateTime } from "@/lib/seed-utils";

// ── Helpers ───────────────────────────────────────────────────────────────────

const HABIT_DEFAULTS = {
  surveysRespondedLast30d: 3,
  surveysTotalLast30d: 3,
  lastPulseSentiment: 4 as number | null,
  lastPulseDate: "2026-03-07T10:00:00Z" as string | null,
  lastPulseWorkload: "normal" as "low" | "normal" | "high" | "overload" | null,
  pulseSentimentTrend: "stable" as "improving" | "stable" | "declining" | null,
};

function makeCheckIn(userId: string, daysAgo: number, confidence: CheckIn["confidence"] = "medium"): CheckIn {
  return {
    id: `ci-${userId}-${daysAgo}`,
    keyResultId: "kr-1",
    authorId: userId,
    value: "50",
    previousValue: null,
    confidence,
    note: null,
    mentions: null,
    createdAt: toIsoDateTime(addDays(today(), -daysAgo)),
  };
}

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

function makeTeamMember(overrides: Partial<TeamMember> = {}): TeamMember {
  return {
    teamId: "team-1",
    userId: "u1",
    roleInTeam: "member",
    joinedAt: "2025-01-01T00:00:00Z",
    user: {
      id: "u1",
      firstName: "Ana",
      lastName: "Silva",
      initials: "AS",
      jobTitle: "Dev",
      avatarUrl: null,
    },
    ...overrides,
  };
}

function makeMemberEngagement(overrides: Partial<UserEngagementSummary> = {}): UserEngagementSummary {
  return {
    userId: "u1",
    name: "Ana Silva",
    initials: "AS",
    avatarUrl: null,
    jobTitle: "Dev",
    habit: {
      activeDays30d: 20,
      lastActiveAt: null,
      checkInStreak: 4,
      daysSinceLastCheckIn: 2,
      checkInsLast30d: 4,
      surveyResponseRate: 1,
      pendingSurveysCount: 0,
      ...HABIT_DEFAULTS,
    },
    performance: {
      krsCompletedRate: 0.75,
      avgProgress: 70,
      avgConfidence: "high",
      activeKRsCount: 4,
      completedKRsCount: 3,
      avgSurveyResponseTimeHours: 12,
    },
    engagementScore: 85,
    performanceScore: 80,
    overallScore: 83,
    alertLevel: "excellent",
    healthStatus: "healthy" as const,
    alerts: [],
    trend: "up",
    ...overrides,
  };
}

// ── calculateHabitMetrics ─────────────────────────────────────────────────────

describe("calculateHabitMetrics", () => {
  it("calcula activeDays30d a partir de atividades de login", () => {
    const activities = [
      makeActivity("u1", "login", 1),
      makeActivity("u1", "login", 5),
      makeActivity("u1", "login", 35), // Fora do range
    ];
    const result = calculateHabitMetrics("u1", {}, activities, []);
    expect(result.activeDays30d).toBe(2);
  });

  it("calcula surveyResponseRate corretamente", () => {
    const activities = [
      makeActivity("u1", "survey_complete", 5, { entityId: "s1", entityType: "survey" }),
    ];
    const result = calculateHabitMetrics("u1", {}, activities, ["s1", "s2"]);
    expect(result.surveyResponseRate).toBe(0.5);
  });

  it("pendingSurveysCount = 0 quando respondeu todas", () => {
    const activities = [
      makeActivity("u1", "survey_complete", 5, { entityId: "s1", entityType: "survey" }),
      makeActivity("u1", "survey_complete", 3, { entityId: "s2", entityType: "survey" }),
    ];
    const result = calculateHabitMetrics("u1", {}, activities, ["s1", "s2"]);
    expect(result.pendingSurveysCount).toBe(0);
  });

  it("retorna daysSinceLastCheckIn correto", () => {
    const history = { "kr-1": [makeCheckIn("u1", 3)] };
    const result = calculateHabitMetrics("u1", history, [], []);
    expect(result.daysSinceLastCheckIn).toBe(3);
  });
});

// ── calculateAvgConfidence ────────────────────────────────────────────────────

describe("calculateAvgConfidence", () => {
  it("retorna null quando sem check-ins", () => {
    expect(calculateAvgConfidence([])).toBeNull();
  });

  it("retorna high quando maioria é high", () => {
    const checkIns = [
      makeCheckIn("u1", 1, "high"),
      makeCheckIn("u1", 2, "high"),
      makeCheckIn("u1", 3, "medium"),
    ];
    expect(calculateAvgConfidence(checkIns)).toBe("high");
  });

  it("retorna low quando maioria é low", () => {
    const checkIns = [
      makeCheckIn("u1", 1, "low"),
      makeCheckIn("u1", 2, "low"),
    ];
    expect(calculateAvgConfidence(checkIns)).toBe("low");
  });

  it("ignora 'barrier' e 'deprioritized' no cálculo", () => {
    const checkIns = [
      makeCheckIn("u1", 1, "high"),
      makeCheckIn("u1", 2, "barrier"),
    ];
    // Só 'high' conta — resultado deve ser high
    expect(calculateAvgConfidence(checkIns)).toBe("high");
  });
});

// ── calculateEngagementScore ──────────────────────────────────────────────────

describe("calculateEngagementScore", () => {
  it("retorna score entre 0 e 100", () => {
    const habit = { ...HABIT_DEFAULTS, activeDays30d: 15, lastActiveAt: null, checkInStreak: 3, daysSinceLastCheckIn: 2, checkInsLast30d: 3, surveyResponseRate: 0.8, pendingSurveysCount: 0 };
    const score = calculateEngagementScore(habit);
    expect(score).toBeGreaterThanOrEqual(0);
    expect(score).toBeLessThanOrEqual(100);
  });

  it("retorna score alto para usuário muito ativo", () => {
    const habit = { ...HABIT_DEFAULTS, activeDays30d: 22, lastActiveAt: null, checkInStreak: 6, daysSinceLastCheckIn: 1, checkInsLast30d: 8, surveyResponseRate: 1, pendingSurveysCount: 0 };
    expect(calculateEngagementScore(habit)).toBeGreaterThan(70);
  });

  it("retorna score baixo para usuário inativo", () => {
    const habit = { ...HABIT_DEFAULTS, activeDays30d: 2, lastActiveAt: null, checkInStreak: 0, daysSinceLastCheckIn: 14, checkInsLast30d: 0, surveyResponseRate: 0, pendingSurveysCount: 3 };
    expect(calculateEngagementScore(habit)).toBeLessThan(30);
  });
});

// ── calculatePerformanceScore ─────────────────────────────────────────────────

describe("calculatePerformanceScore", () => {
  it("retorna score entre 0 e 100", () => {
    const perf = {
      krsCompletedRate: 0.5,
      avgProgress: 60,
      avgConfidence: "medium" as const,
      activeKRsCount: 4,
      completedKRsCount: 2,
      avgSurveyResponseTimeHours: 24,
    };
    const score = calculatePerformanceScore(perf);
    expect(score).toBeGreaterThanOrEqual(0);
    expect(score).toBeLessThanOrEqual(100);
  });

  it("retorna score neutro quando sem KRs ativos", () => {
    const perf = {
      krsCompletedRate: 0,
      avgProgress: 0,
      avgConfidence: null,
      activeKRsCount: 0,
      completedKRsCount: 0,
      avgSurveyResponseTimeHours: null,
    };
    const score = calculatePerformanceScore(perf);
    expect(score).toBeGreaterThan(30);
    expect(score).toBeLessThan(70);
  });
});

// ── determineHealthStatus ─────────────────────────────────────────────────────

describe("determineHealthStatus", () => {
  it("healthy quando overall >= 70", () => {
    expect(determineHealthStatus(70)).toBe("healthy");
    expect(determineHealthStatus(85)).toBe("healthy");
    expect(determineHealthStatus(100)).toBe("healthy");
  });

  it("attention quando overall entre 40 e 69", () => {
    expect(determineHealthStatus(40)).toBe("attention");
    expect(determineHealthStatus(55)).toBe("attention");
    expect(determineHealthStatus(69)).toBe("attention");
  });

  it("critical quando overall < 40", () => {
    expect(determineHealthStatus(39)).toBe("critical");
    expect(determineHealthStatus(0)).toBe("critical");
    expect(determineHealthStatus(20)).toBe("critical");
  });
});

// ── determineAlertLevel ───────────────────────────────────────────────────────

describe("determineAlertLevel", () => {
  it("excellent quando ambos >= 80", () => {
    expect(determineAlertLevel(85, 82, 0)).toBe("excellent");
  });

  it("good quando ambos entre 60-79", () => {
    expect(determineAlertLevel(70, 65, 0)).toBe("good");
  });

  it("attention quando algum < 60", () => {
    expect(determineAlertLevel(55, 75, 0)).toBe("attention");
  });

  it("critical quando algum < 40", () => {
    expect(determineAlertLevel(35, 70, 0)).toBe("critical");
  });

  it("critical quando pendingSurveys >= 2", () => {
    expect(determineAlertLevel(80, 80, 2)).toBe("critical");
  });
});

// ── buildAlertMessages ────────────────────────────────────────────────────────

describe("buildAlertMessages", () => {
  const baseHabit = {
    ...HABIT_DEFAULTS,
    activeDays30d: 20,
    lastActiveAt: null,
    checkInStreak: 4,
    daysSinceLastCheckIn: 2,
    checkInsLast30d: 4,
    surveyResponseRate: 1,
    pendingSurveysCount: 0,
  };
  const basePerf = {
    krsCompletedRate: 0.75,
    avgProgress: 70,
    avgConfidence: "medium" as const,
    activeKRsCount: 4,
    completedKRsCount: 3,
    avgSurveyResponseTimeHours: 12,
  };

  it("gera alerta para muitos dias sem check-in", () => {
    const alerts = buildAlertMessages({ ...baseHabit, daysSinceLastCheckIn: 10 }, basePerf);
    expect(alerts.some((a) => a.includes("dias sem check-in"))).toBe(true);
  });

  it("gera alerta para pesquisas pendentes", () => {
    const alerts = buildAlertMessages({ ...baseHabit, pendingSurveysCount: 2 }, basePerf);
    expect(alerts.some((a) => a.includes("pesquisa"))).toBe(true);
  });

  it("gera alerta para progresso baixo", () => {
    const alerts = buildAlertMessages(baseHabit, { ...basePerf, avgProgress: 15 });
    expect(alerts.some((a) => a.includes("Progresso"))).toBe(true);
  });

  it("retorna array vazio para usuário saudável", () => {
    const alerts = buildAlertMessages(baseHabit, basePerf);
    expect(alerts).toHaveLength(0);
  });
});

// ── buildMemberEngagementSummary ──────────────────────────────────────────────

describe("buildMemberEngagementSummary", () => {
  it("usa dados do user para nome e initials", () => {
    const member = makeTeamMember();
    const result = buildMemberEngagementSummary(member, [], {}, [], []);
    expect(result.name).toBe("Ana Silva");
    expect(result.initials).toBe("AS");
  });

  it("calcula scores, alertLevel e healthStatus", () => {
    const member = makeTeamMember();
    const result = buildMemberEngagementSummary(member, [], {}, [], []);
    expect(result.engagementScore).toBeGreaterThanOrEqual(0);
    expect(result.engagementScore).toBeLessThanOrEqual(100);
    expect(result.performanceScore).toBeGreaterThanOrEqual(0);
    expect(result.performanceScore).toBeLessThanOrEqual(100);
    expect(result.overallScore).toBeGreaterThanOrEqual(0);
    expect(result.overallScore).toBeLessThanOrEqual(100);
    expect(["excellent", "good", "attention", "critical"]).toContain(result.alertLevel);
    expect(["healthy", "attention", "critical"]).toContain(result.healthStatus);
  });

  it("overallScore é média 50/50 de engagement e performance", () => {
    const member = makeTeamMember();
    const result = buildMemberEngagementSummary(member, [], {}, [], []);
    const expected = Math.round(result.engagementScore * 0.5 + result.performanceScore * 0.5);
    expect(result.overallScore).toBe(expected);
  });
});

// ── aggregateTeamEngagement ───────────────────────────────────────────────────

describe("aggregateTeamEngagement", () => {
  it("retorna zeros para time vazio", () => {
    const result = aggregateTeamEngagement("team-1", [], {});
    expect(result.memberCount).toBe(0);
    expect(result.avgEngagementScore).toBe(0);
    expect(result.byHealthStatus).toEqual({ healthy: 0, attention: 0, critical: 0 });
  });

  it("calcula médias corretamente", () => {
    const members = [
      makeMemberEngagement({ engagementScore: 80, performanceScore: 70 }),
      makeMemberEngagement({ userId: "u2", engagementScore: 60, performanceScore: 50 }),
    ];
    const result = aggregateTeamEngagement("team-1", members, {});
    expect(result.avgEngagementScore).toBe(70);
    expect(result.avgPerformanceScore).toBe(60);
  });

  it("conta membros por nível de alerta (alertLevel)", () => {
    const members = [
      makeMemberEngagement({ alertLevel: "excellent", healthStatus: "healthy" }),
      makeMemberEngagement({ userId: "u2", alertLevel: "critical", healthStatus: "critical", overallScore: 25 }),
      makeMemberEngagement({ userId: "u3", alertLevel: "attention", healthStatus: "attention", overallScore: 55 }),
    ];
    const result = aggregateTeamEngagement("team-1", members, {});
    expect(result.byAlertLevel.excellent).toBe(1);
    expect(result.byAlertLevel.critical).toBe(1);
    expect(result.byAlertLevel.attention).toBe(1);
    expect(result.membersInAlert).toBe(1);
  });

  it("conta membros por status de saúde (byHealthStatus)", () => {
    const members = [
      makeMemberEngagement({ healthStatus: "healthy", overallScore: 80 }),
      makeMemberEngagement({ userId: "u2", healthStatus: "critical", overallScore: 25 }),
      makeMemberEngagement({ userId: "u3", healthStatus: "attention", overallScore: 55 }),
    ];
    const result = aggregateTeamEngagement("team-1", members, {});
    expect(result.byHealthStatus.healthy).toBe(1);
    expect(result.byHealthStatus.critical).toBe(1);
    expect(result.byHealthStatus.attention).toBe(1);
  });

  it("conta membros com pesquisas pendentes", () => {
    const members = [
      makeMemberEngagement({ habit: { ...HABIT_DEFAULTS, activeDays30d: 20, lastActiveAt: null, checkInStreak: 4, daysSinceLastCheckIn: 2, checkInsLast30d: 4, surveyResponseRate: 1, pendingSurveysCount: 0 } }),
      makeMemberEngagement({ userId: "u2", habit: { ...HABIT_DEFAULTS, activeDays30d: 10, lastActiveAt: null, checkInStreak: 1, daysSinceLastCheckIn: 5, checkInsLast30d: 2, surveyResponseRate: 0.5, pendingSurveysCount: 2 } }),
    ];
    const result = aggregateTeamEngagement("team-1", members, {});
    expect(result.membersWithPendingSurveys).toBe(1);
  });
});
