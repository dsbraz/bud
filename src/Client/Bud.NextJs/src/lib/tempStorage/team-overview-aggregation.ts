// ─── Team Overview Aggregation ───────────────────────────────────────────────
// Funções puras para agregar dados de Missões + Pesquisas por time.
// Agnóstica a React — usável em testes unitários diretamente.
//
// NOTA: Métricas de membros foram migradas para engagement-aggregation.ts

import type { Mission } from "@/types/mission";
import type { SurveyListItemData } from "@/lib/surveys-store";

// ── Tipos de saída ────────────────────────────────────────────────────────────

export interface MissionSummaryItem {
  id: string;
  title: string;
  progress: number;
  status: Mission["status"];
  isAtRisk: boolean;
  ownerId: string;
  teamId: string | null;
  dueDate: string | null;
}

export interface TeamMissionsSummary {
  totalActive: number;
  avgProgress: number; // 0-100
  onTrackCount: number;
  atRiskCount: number;
  completedCount: number;
  missions: MissionSummaryItem[];
}

export interface TeamSurveySummary {
  surveyId: string;
  surveyName: string;
  surveyType: string;
  status: string; // SurveyStatus — closed/active/scheduled/etc.
  endDate: string;
  responseCount: number;
  totalRecipients: number;
  completionRate: number; // 0-100
}

// ── Missões ───────────────────────────────────────────────────────────────────

/** Considera uma missão como "em risco" se progresso < 50 e tem dueDate dentro de 30 dias. */
function isMissionAtRisk(mission: Mission): boolean {
  if (mission.status === "completed" || mission.status === "cancelled") return false;
  if (mission.progress >= 70) return false;
  if (!mission.dueDate) return mission.progress < 40;
  const now = Date.now();
  const due = new Date(mission.dueDate).getTime();
  const daysLeft = (due - now) / (1000 * 60 * 60 * 24);
  return daysLeft < 30 && mission.progress < 60;
}

/**
 * Agrega as missões do time (filtradas por teamId e/ou memberIds).
 * Inclui missões onde teamId bate OU onde ownerId é membro do time.
 */
export function aggregateTeamMissions(
  allMissions: Mission[],
  teamId: string | null,
  memberIds: string[],
): TeamMissionsSummary {
  const relevant = allMissions.filter((m) => {
    if (m.status === "cancelled" || m.deletedAt) return false;
    if (teamId && m.teamId === teamId) return true;
    if (memberIds.includes(m.ownerId)) return true;
    return false;
  });

  const active = relevant.filter((m) => m.status === "active" || m.status === "paused");
  const completed = relevant.filter((m) => m.status === "completed");

  const missions: MissionSummaryItem[] = relevant.map((m) => ({
    id: m.id,
    title: m.title,
    progress: m.progress,
    status: m.status,
    isAtRisk: isMissionAtRisk(m),
    ownerId: m.ownerId,
    teamId: m.teamId,
    dueDate: m.dueDate,
  }));

  const avgProgress =
    active.length > 0
      ? Math.round(active.reduce((s, m) => s + m.progress, 0) / active.length)
      : 0;

  const atRiskCount = active.filter(isMissionAtRisk).length;
  const onTrackCount = active.length - atRiskCount;

  return {
    totalActive: active.length,
    avgProgress,
    onTrackCount,
    atRiskCount,
    completedCount: completed.length,
    missions,
  };
}

// ── Pesquisas ─────────────────────────────────────────────────────────────────

/**
 * Retorna o resumo de pesquisas relevantes para o time.
 * "Relevante" = pesquisa ativa/fechada cujos managers incluem membros do time.
 */
export function aggregateTeamSurveys(
  allSurveys: SurveyListItemData[],
  memberIds: string[],
): TeamSurveySummary[] {
  const relevant = allSurveys.filter((s) => {
    if (s.status === "archived") return false;
    return s.managerIds.some((id) => memberIds.includes(id));
  });

  return relevant.map((s) => ({
    surveyId: s.id,
    surveyName: s.name,
    surveyType: s.type,
    status: s.status,
    endDate: s.endDate,
    responseCount: s.totalResponses,
    totalRecipients: s.totalRecipients,
    completionRate: s.completionRate,
  }));
}

// ── Score global ──────────────────────────────────────────────────────────────

/**
 * Calcula um score agregado do time baseado em missões e pesquisas (0-100).
 */
export function calculateTeamOverallScore(
  missionsSummary: TeamMissionsSummary,
  latestSurveyScore: number | null,
): number {
  const scores: number[] = [];

  if (missionsSummary.totalActive > 0) {
    scores.push(missionsSummary.avgProgress);
  }

  if (latestSurveyScore !== null) {
    scores.push(latestSurveyScore);
  }

  if (scores.length === 0) return 0;
  return Math.round(scores.reduce((s, n) => s + n, 0) / scores.length);
}
