// ─── Engagement Aggregation ───────────────────────────────────────────────────
// Funções puras para calcular métricas de hábito e performance por colaborador.
// Agnóstica a React — usável em testes unitários diretamente.

import type { Mission, CheckIn } from "@/types";
import type { UserActivity } from "@/types/activity";
import type {
  AlertLevel,
  HealthStatus,
  TrendDirection,
  UserHabitMetrics,
  UserPerformanceMetrics,
  UserEngagementSummary,
  TeamEngagementSummary,
} from "@/types/engagement";
import type { TeamMember } from "@/types/team";
import {
  daysSinceUserCheckin,
  calculateCheckInStreakForUser,
  countCheckInsForUser,
  calculateUserTrend,
} from "@/lib/engagement-utils";
import {
  countActiveDaysForUser,
  getLastActiveAt,
  countSurveysCompletedForUser,
  calcAvgSurveyResponseTime,
  getPendingSurveyIds,
  getPulseInsights,
} from "@/lib/activity-store";
import { today, addWeeks, startOfWeek } from "@/lib/seed-utils";

// ── Hábito ────────────────────────────────────────────────────────────────────

/**
 * Calcula as métricas de hábito de um usuário.
 */
export function calculateHabitMetrics(
  userId: string,
  checkInHistory: Record<string, CheckIn[]>,
  activities: UserActivity[],
  teamSurveyIds: string[],
): UserHabitMetrics {
  const activeDays30d = countActiveDaysForUser(activities, userId, 30);
  const lastActiveAt = getLastActiveAt(activities, userId);
  const checkInStreak = calculateCheckInStreakForUser(userId, checkInHistory);
  const daysSinceLastCheckIn = daysSinceUserCheckin(userId, checkInHistory);
  const checkInsLast30d = countCheckInsForUser(userId, checkInHistory, 30);

  const completedSurveys = countSurveysCompletedForUser(activities, userId, teamSurveyIds);
  const surveyResponseRate =
    teamSurveyIds.length > 0 ? completedSurveys / teamSurveyIds.length : 1;

  const pendingSurveyIds = getPendingSurveyIds(activities, userId, teamSurveyIds);
  const pendingSurveysCount = pendingSurveyIds.length;

  const pulse = getPulseInsights(userId);

  return {
    activeDays30d,
    lastActiveAt,
    checkInStreak,
    daysSinceLastCheckIn,
    checkInsLast30d,
    surveyResponseRate,
    pendingSurveysCount,
    surveysRespondedLast30d: completedSurveys,
    surveysTotalLast30d: teamSurveyIds.length,
    lastPulseSentiment: pulse.lastPulseSentiment,
    lastPulseDate: pulse.lastPulseDate,
    lastPulseWorkload: pulse.lastPulseWorkload,
    pulseSentimentTrend: pulse.pulseSentimentTrend,
  };
}

// ── Performance ───────────────────────────────────────────────────────────────

/**
 * Calcula as métricas de performance de um usuário a partir das missões.
 */
export function calculatePerformanceMetrics(
  userId: string,
  allMissions: Mission[],
  checkInHistory: Record<string, CheckIn[]>,
  activities: UserActivity[],
): UserPerformanceMetrics {
  // Missões ativas onde o usuário é owner
  const userMissions = allMissions.filter(
    (m) => m.ownerId === userId && !m.deletedAt,
  );

  // KRs ativos do usuário
  const activeKRs = userMissions
    .filter((m) => m.status === "active" || m.status === "paused")
    .flatMap((m) => m.keyResults ?? [])
    .filter((kr) => kr.owner?.id === userId);

  const completedKRs = activeKRs.filter((kr) => kr.progress >= 100);
  const activeKRsCount = activeKRs.length;
  const completedKRsCount = completedKRs.length;

  const krsCompletedRate =
    activeKRsCount > 0 ? completedKRsCount / activeKRsCount : 0;

  // Progresso médio das missões ativas
  const activeMissions = userMissions.filter(
    (m) => m.status === "active" || m.status === "paused",
  );
  const avgProgress =
    activeMissions.length > 0
      ? Math.round(
          activeMissions.reduce((s, m) => s + m.progress, 0) / activeMissions.length,
        )
      : 0;

  // Confiança média dos últimos check-ins do usuário
  const userCheckIns: CheckIn[] = [];
  for (const checkIns of Object.values(checkInHistory)) {
    for (const ci of checkIns) {
      if (ci.authorId === userId && ci.confidence) {
        userCheckIns.push(ci);
      }
    }
  }

  // Pega os 5 check-ins mais recentes para calcular confiança média
  const recentCheckIns = userCheckIns
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  const avgConfidence = calculateAvgConfidence(recentCheckIns);
  const avgSurveyResponseTimeHours = calcAvgSurveyResponseTime(activities, userId);

  return {
    krsCompletedRate,
    avgProgress,
    avgConfidence,
    activeKRsCount,
    completedKRsCount,
    avgSurveyResponseTimeHours,
  };
}

/**
 * Calcula a confiança média de uma lista de check-ins.
 * Ignora "barrier" e "deprioritized" no cálculo numérico.
 */
export function calculateAvgConfidence(
  checkIns: CheckIn[],
): "high" | "medium" | "low" | null {
  const confidenceValues: Record<string, number> = {
    high: 3,
    medium: 2,
    low: 1,
  };

  const scorable = checkIns.filter(
    (ci) =>
      ci.confidence === "high" || ci.confidence === "medium" || ci.confidence === "low",
  );

  if (scorable.length === 0) return null;

  const avg =
    scorable.reduce((s, ci) => s + (confidenceValues[ci.confidence!] ?? 2), 0) /
    scorable.length;

  if (avg >= 2.5) return "high";
  if (avg >= 1.5) return "medium";
  return "low";
}

// ── Scores compostos ──────────────────────────────────────────────────────────

/**
 * Calcula o Engagement Score (0-100) baseado em métricas de hábito.
 *
 * Pesos:
 * - Check-in (streak + frequência combinados): 50%
 * - Participação em pesquisas: 30%
 * - Frequência de acesso (30d): 20%
 */
export function calculateEngagementScore(habit: UserHabitMetrics): number {
  // Check-in: média de streak (4 sem = 100) e frequência (4 por 30d = 100)
  const streakScore = Math.min(100, Math.round((habit.checkInStreak / 4) * 100));
  const freqScore = Math.min(100, Math.round((habit.checkInsLast30d / 4) * 100));
  const checkInScore = Math.round((streakScore + freqScore) / 2);

  // Pesquisas: taxa de participação linear 0-100
  const surveyScore = Math.round(habit.surveyResponseRate * 100);

  // Acesso: 20+ dias = 100, proporcional
  const accessScore = Math.min(100, Math.round((habit.activeDays30d / 20) * 100));

  const score = Math.round(
    checkInScore * 0.50 +
    surveyScore  * 0.30 +
    accessScore  * 0.20,
  );

  return Math.max(0, Math.min(100, score));
}

/**
 * Calcula o Performance Score (0-100) baseado em métricas de entrega.
 *
 * Pesos:
 * - Missões (KRs completados + progresso + confiança): 60%
 * - Participação em pesquisas: 40%
 *
 * @param surveyParticipation Taxa de participação em pesquisas do time (0-1). Default 0.5 (neutro).
 */
export function calculatePerformanceScore(
  perf: UserPerformanceMetrics,
  surveyParticipation = 0.5,
): number {
  // KRs completados: linear 0-100
  const krsScore = Math.round(perf.krsCompletedRate * 100);

  // Progresso médio: linear 0-100
  const progressScore = perf.avgProgress;

  // Confiança: high=100, medium=60, low=20, null=50
  const confidenceScore =
    perf.avgConfidence === "high" ? 100
    : perf.avgConfidence === "medium" ? 60
    : perf.avgConfidence === "low" ? 20
    : 50; // sem dados = neutro

  // Score de missões: sem KRs usa só confiança como proxy
  const missionScore =
    perf.activeKRsCount === 0
      ? Math.round(confidenceScore * 0.5 + 50 * 0.5)
      : Math.round(krsScore * 0.40 + progressScore * 0.40 + confidenceScore * 0.20);

  // Score de pesquisas: participação linear 0-100
  const surveyScore = Math.round(surveyParticipation * 100);

  const score = Math.round(missionScore * 0.60 + surveyScore * 0.40);
  return Math.max(0, Math.min(100, score));
}

// ── Health Status ──────────────────────────────────────────────────────────────

/**
 * Determina o status de saúde simplificado (3 níveis) a partir do overall score.
 *
 * - healthy:   overall >= 70
 * - attention: overall >= 40
 * - critical:  overall <  40
 */
export function determineHealthStatus(overallScore: number): HealthStatus {
  if (overallScore >= 70) return "healthy";
  if (overallScore >= 40) return "attention";
  return "critical";
}

// ── Alert Level ───────────────────────────────────────────────────────────────

/**
 * Determina o nível de alerta (semáforo) de um colaborador.
 *
 * - excellent: engagement >= 80 AND performance >= 80
 * - good: ambos >= 60
 * - attention: algum < 60
 * - critical: algum < 40 OU pesquisas pendentes >= 2
 */
export function determineAlertLevel(
  engagementScore: number,
  performanceScore: number,
  pendingSurveysCount: number,
): AlertLevel {
  if (pendingSurveysCount >= 2) return "critical";
  if (engagementScore < 40 || performanceScore < 40) return "critical";
  if (engagementScore < 60 || performanceScore < 60) return "attention";
  if (engagementScore >= 80 && performanceScore >= 80) return "excellent";
  return "good";
}

/**
 * Gera lista de alertas acionáveis para o gestor.
 */
export function buildAlertMessages(
  habit: UserHabitMetrics,
  performance: UserPerformanceMetrics,
): string[] {
  const alerts: string[] = [];

  if (habit.daysSinceLastCheckIn > 7) {
    alerts.push(`${habit.daysSinceLastCheckIn} dias sem check-in`);
  } else if (habit.daysSinceLastCheckIn > 3) {
    alerts.push(`${habit.daysSinceLastCheckIn} dias sem check-in`);
  }

  if (habit.pendingSurveysCount >= 2) {
    alerts.push(`${habit.pendingSurveysCount} pesquisas pendentes`);
  } else if (habit.pendingSurveysCount === 1) {
    alerts.push("1 pesquisa pendente");
  }

  if (habit.checkInStreak === 0) {
    alerts.push("Sem sequência de check-ins");
  }

  if (performance.avgProgress < 30 && performance.activeKRsCount > 0) {
    alerts.push(`Progresso médio baixo (${performance.avgProgress}%)`);
  }

  if (performance.avgConfidence === "low") {
    alerts.push("Confiança baixa nas metas");
  }

  if (habit.activeDays30d < 5) {
    alerts.push("Baixa frequência de acesso");
  }

  return alerts;
}

// ── Tendência ─────────────────────────────────────────────────────────────────

/**
 * Determina a tendência da semana atual vs semana anterior de check-ins.
 * Wrapper sobre calculateUserTrend para compatibilidade.
 */
export function getMemberTrend(
  userId: string,
  checkInHistory: Record<string, CheckIn[]>,
): TrendDirection {
  return calculateUserTrend(userId, checkInHistory);
}

/**
 * Calcula a taxa de check-in do time nesta semana (% de membros com check-in).
 */
export function calculateTeamCheckInRateThisWeek(
  memberIds: string[],
  checkInHistory: Record<string, CheckIn[]>,
): number {
  if (memberIds.length === 0) return 0;

  const now = today();
  const weekStart = startOfWeek(now);
  const weekEnd = addWeeks(weekStart, 1);

  let membersWithCheckIn = 0;

  for (const userId of memberIds) {
    const hasCheckIn = Object.values(checkInHistory).some((checkIns) =>
      checkIns.some((ci) => {
        if (ci.authorId !== userId) return false;
        const d = new Date(ci.createdAt);
        return d >= weekStart && d < weekEnd;
      }),
    );
    if (hasCheckIn) membersWithCheckIn++;
  }

  return membersWithCheckIn / memberIds.length;
}

// ── Resumo por membro ─────────────────────────────────────────────────────────

/**
 * Gera o resumo completo de engajamento de um membro do time.
 */
export function buildMemberEngagementSummary(
  teamMember: TeamMember,
  allMissions: Mission[],
  checkInHistory: Record<string, CheckIn[]>,
  activities: UserActivity[],
  teamSurveyIds: string[],
): UserEngagementSummary {
  const userId = teamMember.userId;
  const user = teamMember.user;

  const name = user ? `${user.firstName} ${user.lastName}` : userId;
  const initials = user?.initials ?? name.slice(0, 2).toUpperCase();
  const jobTitle = user?.jobTitle ?? null;
  const avatarUrl = user?.avatarUrl ?? null;

  const habit = calculateHabitMetrics(userId, checkInHistory, activities, teamSurveyIds);
  const performance = calculatePerformanceMetrics(userId, allMissions, checkInHistory, activities);

  const engagementScore = calculateEngagementScore(habit);
  const performanceScore = calculatePerformanceScore(performance, habit.surveyResponseRate);
  const overallScore = Math.round(engagementScore * 0.5 + performanceScore * 0.5);

  const alertLevel = determineAlertLevel(engagementScore, performanceScore, habit.pendingSurveysCount);
  const healthStatus = determineHealthStatus(overallScore);
  const alerts = buildAlertMessages(habit, performance);
  const trend = getMemberTrend(userId, checkInHistory);

  return {
    userId,
    name,
    initials,
    avatarUrl,
    jobTitle,
    habit,
    performance,
    engagementScore,
    performanceScore,
    overallScore,
    alertLevel,
    healthStatus,
    alerts,
    trend,
  };
}

/**
 * Gera resumos para todos os membros de um time.
 */
export function buildAllMemberEngagements(
  members: TeamMember[],
  allMissions: Mission[],
  checkInHistory: Record<string, CheckIn[]>,
  activities: UserActivity[],
  teamSurveyIds: string[],
): UserEngagementSummary[] {
  return members.map((m) =>
    buildMemberEngagementSummary(m, allMissions, checkInHistory, activities, teamSurveyIds),
  );
}

// ── Resumo do time ────────────────────────────────────────────────────────────

/**
 * Calcula a tendência de um score entre duas listas de valores (atual vs anterior).
 */
export function scoreToTrend(
  currentAvg: number,
  previousAvg: number,
  threshold: number = 3,
): TrendDirection {
  const diff = currentAvg - previousAvg;
  if (diff >= threshold) return "up";
  if (diff <= -threshold) return "down";
  return "stable";
}

/**
 * Agrega os dados de todos os membros em um resumo do time.
 */
export function aggregateTeamEngagement(
  teamId: string,
  members: UserEngagementSummary[],
  checkInHistory: Record<string, CheckIn[]>,
): TeamEngagementSummary {
  if (members.length === 0) {
    return {
      teamId,
      memberCount: 0,
      avgEngagementScore: 0,
      avgPerformanceScore: 0,
      engagementTrend: "stable",
      performanceTrend: "stable",
      checkInRateThisWeek: 0,
      membersWithPendingSurveys: 0,
      membersInAlert: 0,
      byAlertLevel: { excellent: 0, good: 0, attention: 0, critical: 0 },
      byHealthStatus: { healthy: 0, attention: 0, critical: 0 },
    };
  }

  const avgEngagementScore = Math.round(
    members.reduce((s, m) => s + m.engagementScore, 0) / members.length,
  );
  const avgPerformanceScore = Math.round(
    members.reduce((s, m) => s + m.performanceScore, 0) / members.length,
  );

  // Tendências: compara check-ins das últimas 2 semanas vs 2-4 semanas anteriores
  const memberIds = members.map((m) => m.userId);
  const checkInRateThisWeek = calculateTeamCheckInRateThisWeek(memberIds, checkInHistory);

  const membersWithPendingSurveys = members.filter(
    (m) => m.habit.pendingSurveysCount > 0,
  ).length;

  const membersInAlert = members.filter((m) => m.alertLevel === "critical").length;

  const byAlertLevel: Record<AlertLevel, number> = {
    excellent: 0,
    good: 0,
    attention: 0,
    critical: 0,
  };
  for (const member of members) {
    byAlertLevel[member.alertLevel]++;
  }

  const byHealthStatus: Record<HealthStatus, number> = {
    healthy: 0,
    attention: 0,
    critical: 0,
  };
  for (const member of members) {
    byHealthStatus[member.healthStatus]++;
  }

  // Tendências baseadas nos scores dos membros em up/down/stable
  const upEngagement = members.filter((m) => m.trend === "up").length;
  const downEngagement = members.filter((m) => m.trend === "down").length;
  const engagementTrend: TrendDirection =
    upEngagement > downEngagement ? "up"
    : downEngagement > upEngagement ? "down"
    : "stable";

  // Performance trend baseado no checkInRate vs média histórica
  const performanceTrend: TrendDirection =
    checkInRateThisWeek >= 0.8 ? "up"
    : checkInRateThisWeek >= 0.5 ? "stable"
    : "down";

  return {
    teamId,
    memberCount: members.length,
    avgEngagementScore,
    avgPerformanceScore,
    engagementTrend,
    performanceTrend,
    checkInRateThisWeek,
    membersWithPendingSurveys,
    membersInAlert,
    byAlertLevel,
    byHealthStatus,
  };
}
