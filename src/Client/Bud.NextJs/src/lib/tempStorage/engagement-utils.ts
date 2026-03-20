// ─── Engagement Utils ─────────────────────────────────────────────────────────
// Utilitários puros para cálculo de métricas de engajamento.
// Extraídos de useTeamHealthReadModel e useEngagementReadModel para evitar
// duplicação e permitir uso em testes e fora de componentes React.

import type { Mission, CheckIn } from "@/types";
import {
  today,
  addWeeks,
  startOfWeek,
  addDays,
  hashString,
} from "@/lib/tempStorage/seed-utils";

// ── Missões ───────────────────────────────────────────────────────────────────

/**
 * Achata a hierarquia de missões em uma lista plana.
 */
export function flattenMissions(missions: Mission[]): Mission[] {
  const flat: Mission[] = [];
  for (const mission of missions) {
    flat.push(mission);
    if (mission.children?.length) {
      flat.push(...flattenMissions(mission.children));
    }
  }
  return flat;
}

/**
 * Retorna IDs de todos os usuários que são owners de KRs.
 */
export function getKrOwnerIds(missions: Mission[]): Set<string> {
  const ownerIds = new Set<string>();
  const allMissions = flattenMissions(missions);
  for (const mission of allMissions) {
    for (const kr of mission.keyResults ?? []) {
      if (kr.owner?.id) ownerIds.add(kr.owner.id);
    }
  }
  return ownerIds;
}

/**
 * Calcula o progresso médio de KRs por owner.
 * Retorna um Map de userId → { total, count }.
 */
export function getProgressByOwner(
  missions: Mission[],
): Map<string, { total: number; count: number }> {
  const map = new Map<string, { total: number; count: number }>();
  const allMissions = flattenMissions(missions);
  for (const mission of allMissions) {
    for (const kr of mission.keyResults ?? []) {
      if (kr.owner?.id) {
        const current = map.get(kr.owner.id) ?? { total: 0, count: 0 };
        current.total += kr.progress;
        current.count += 1;
        map.set(kr.owner.id, current);
      }
    }
  }
  return map;
}

// ── Check-ins ─────────────────────────────────────────────────────────────────

/**
 * Calcula quantos dias se passaram desde o último check-in do usuário.
 * Retorna 14 se não houver nenhum check-in.
 */
export function daysSinceUserCheckin(
  userId: string,
  checkInHistory: Record<string, CheckIn[]>,
): number {
  const now = today();
  let latestCheckIn: Date | null = null;

  for (const checkIns of Object.values(checkInHistory)) {
    for (const checkIn of checkIns) {
      if (checkIn.authorId === userId) {
        const checkInDate = new Date(checkIn.createdAt);
        if (!latestCheckIn || checkInDate > latestCheckIn) {
          latestCheckIn = checkInDate;
        }
      }
    }
  }

  if (!latestCheckIn) return 14;

  const diffMs = now.getTime() - latestCheckIn.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
  return Math.max(0, diffDays);
}

/**
 * Formata dias atrás em português.
 */
export function formatDaysAgo(days: number): string {
  if (days === 0) return "hoje";
  if (days === 1) return "há 1 dia";
  return `há ${days} dias`;
}

/**
 * Conta o número de check-ins feitos por um usuário nos últimos N dias.
 */
export function countCheckInsForUser(
  userId: string,
  checkInHistory: Record<string, CheckIn[]>,
  days: number = 30,
): number {
  const cutoff = addDays(today(), -days);
  let count = 0;
  for (const checkIns of Object.values(checkInHistory)) {
    for (const checkIn of checkIns) {
      if (
        checkIn.authorId === userId &&
        new Date(checkIn.createdAt) >= cutoff
      ) {
        count++;
      }
    }
  }
  return count;
}

/**
 * Calcula a streak (semanas consecutivas com pelo menos 1 check-in) de um usuário.
 * Conta para trás a partir da semana atual.
 */
export function calculateCheckInStreakForUser(
  userId: string,
  checkInHistory: Record<string, CheckIn[]>,
  maxWeeks: number = 12,
): number {
  const now = today();
  const weekStart = startOfWeek(now);

  let streak = 0;

  for (let i = 0; i < maxWeeks; i++) {
    const wStart = addWeeks(weekStart, -i);
    const wEnd = addWeeks(wStart, 1);

    const hasCheckIn = Object.values(checkInHistory).some((checkIns) =>
      checkIns.some((ci) => {
        if (ci.authorId !== userId) return false;
        const d = new Date(ci.createdAt);
        return d >= wStart && d < wEnd;
      }),
    );

    if (!hasCheckIn && i > 0) break; // Para na primeira semana sem check-in (exceto semana atual)
    if (hasCheckIn) streak++;
    else if (i === 0) continue; // Semana atual ainda pode estar em andamento
  }

  return streak;
}

// ── Engajamento geral ─────────────────────────────────────────────────────────

/**
 * Calcula o percentual de KRs com check-ins (proxy de missions engagement).
 */
export function calculateMissionsEngagement(
  missions: Mission[],
  checkInHistory: Record<string, unknown[]>,
): number {
  const allMissions = flattenMissions(missions);
  let totalKrs = 0;
  let krsWithCheckins = 0;

  for (const mission of allMissions) {
    for (const kr of mission.keyResults ?? []) {
      totalKrs++;
      const checkIns = checkInHistory[kr.id] ?? [];
      if (checkIns.length > 0) krsWithCheckins++;
    }
  }

  if (totalKrs === 0) return 50;
  return Math.round((krsWithCheckins / totalKrs) * 100);
}

/**
 * Calcula o engajamento médio de pesquisas com base na completion rate.
 */
export function calculateSurveyEngagement(
  surveys: Array<{ status: string; completionRate: number }>,
): number {
  const activeSurveys = surveys.filter(
    (s) => s.status === "active" || s.status === "scheduled",
  );
  if (activeSurveys.length === 0) return 70;
  const totalCompletion = activeSurveys.reduce(
    (sum, s) => sum + (s.completionRate ?? 0),
    0,
  );
  return Math.round(totalCompletion / activeSurveys.length);
}

/**
 * Calcula tendência global de check-ins (últimas 2 semanas vs 2-4 semanas atrás).
 */
export function calculateTrend(checkInHistory: Record<string, CheckIn[]>): {
  value: number;
  direction: "up" | "down";
} {
  const now = today();
  const twoWeeksAgo = addWeeks(now, -2);
  const fourWeeksAgo = addWeeks(now, -4);

  let recentCount = 0;
  let olderCount = 0;

  for (const checkIns of Object.values(checkInHistory)) {
    for (const checkIn of checkIns) {
      const checkInDate = new Date(checkIn.createdAt);
      if (checkInDate >= twoWeeksAgo) recentCount++;
      else if (checkInDate >= fourWeeksAgo) olderCount++;
    }
  }

  const baseline = olderCount || 1;
  const changePercent = ((recentCount - olderCount) / baseline) * 100;
  const trend = Math.round(Math.abs(changePercent) * 10) / 100;

  return {
    value: Math.min(trend, 15),
    direction: recentCount >= olderCount ? "up" : "down",
  };
}

/**
 * Calcula tendência de um usuário nas últimas 4 semanas.
 * Compara check-ins recentes (2 semanas) vs anteriores (2-4 semanas).
 */
export function calculateUserTrend(
  userId: string,
  checkInHistory: Record<string, CheckIn[]>,
): "up" | "down" | "stable" {
  const now = today();
  const twoWeeksAgo = addWeeks(now, -2);
  const fourWeeksAgo = addWeeks(now, -4);

  let recentCount = 0;
  let olderCount = 0;

  for (const checkIns of Object.values(checkInHistory)) {
    for (const checkIn of checkIns) {
      if (checkIn.authorId !== userId) continue;
      const d = new Date(checkIn.createdAt);
      if (d >= twoWeeksAgo) recentCount++;
      else if (d >= fourWeeksAgo) olderCount++;
    }
  }

  if (recentCount > olderCount) return "up";
  if (recentCount < olderCount) return "down";
  return "stable";
}

/**
 * Calcula engajamento por pessoa com base na atividade de check-in.
 * Retorna valor 0-100 + trend.
 */
export function calculatePersonEngagement(
  userId: string,
  checkInHistory: Record<string, CheckIn[]>,
  krOwnerIds: Set<string>,
): { value: number; trend: number; trendDirection: "up" | "down" } {
  const hasKrs = krOwnerIds.has(userId);
  const baseEngagement = hasKrs ? 60 : 40;

  let totalCheckIns = 0;
  let recentCheckIns = 0;
  const twoWeeksAgo = addWeeks(today(), -2);

  for (const checkIns of Object.values(checkInHistory)) {
    for (const checkIn of checkIns) {
      if (checkIn.authorId === userId) {
        totalCheckIns++;
        if (new Date(checkIn.createdAt) >= twoWeeksAgo) recentCheckIns++;
      }
    }
  }

  const activityBonus = Math.min(totalCheckIns * 5, 30);
  const value = Math.min(100, baseEngagement + activityBonus);

  // Trend determinístico baseado no hash do userId
  const hash = hashString(userId);
  const trendBase = (hash % 100) / 10 - 3; // -3 a 7
  const trend = Math.abs(Math.round(trendBase * 10) / 10);
  const trendDirection = trendBase >= 0 ? ("up" as const) : ("down" as const);

  return { value, trend, trendDirection };
}

/**
 * Gera dados semanais de engajamento baseados no histórico de check-ins.
 */
export function generateWeeklyEngagementData(
  checkInHistory: Record<string, CheckIn[]>,
): Array<{
  week: string;
  engajamento: number;
  missoes: number;
  pulso: number;
}> {
  const now = today();
  const data: Array<{
    week: string;
    engajamento: number;
    missoes: number;
    pulso: number;
  }> = [];

  for (let i = 7; i >= 0; i--) {
    const weekStart = startOfWeek(addWeeks(now, -i));
    const weekEnd = addWeeks(weekStart, 1);
    const weekLabel = `Sem ${8 - i}`;

    let weekCheckIns = 0;
    for (const checkIns of Object.values(checkInHistory)) {
      for (const checkIn of checkIns) {
        const checkInDate = new Date(checkIn.createdAt);
        if (checkInDate >= weekStart && checkInDate < weekEnd) weekCheckIns++;
      }
    }

    const baseEngajamento = 55 + (7 - i) * 3;
    const baseMissoes = 40 + weekCheckIns * 8 + (7 - i) * 2;
    const basePulso = 60 + (7 - i) * 2;

    data.push({
      week: weekLabel,
      engajamento: Math.min(100, baseEngajamento),
      missoes: Math.min(100, baseMissoes),
      pulso: Math.min(100, basePulso),
    });
  }

  return data;
}
