import { useMemo } from "react";
import { usePeopleData } from "@/contexts/PeopleDataContext";
import { useMissionsData } from "@/contexts/MissionsDataContext";
import { useSurveysData } from "@/contexts/SurveysDataContext";
import {
  getKrOwnerIds,
  calculateMissionsEngagement,
  calculateSurveyEngagement,
  calculateTrend,
  calculatePersonEngagement,
  generateWeeklyEngagementData,
} from "@/lib/tempStorage/engagement-utils";

export interface PersonEngagement {
  id: string;
  name: string;
  initials: string;
  role: string;
  value: number;
  trend: number;
  trendDirection: "up" | "down";
}

export interface EngagementMetrics {
  overall: number;
  missionsUpdated: number;
  surveyParticipation: number;
  trend: number;
  trendDirection: "up" | "down";
}

export interface EngagementReadModel {
  metrics: EngagementMetrics;
  teamMembers: PersonEngagement[];
  teamOptions: Array<{ id: string; label: string }>;
  weeklyData: Array<{
    week: string;
    engajamento: number;
    missoes: number;
    pulso: number;
  }>;
}

export function useEngagementReadModel(): EngagementReadModel {
  const { users, teamOptions } = usePeopleData();
  const { missions, checkInHistory } = useMissionsData();
  const { surveys } = useSurveysData();

  return useMemo(() => {
    const missionsUpdated = calculateMissionsEngagement(
      missions,
      checkInHistory,
    );
    const surveyParticipation = calculateSurveyEngagement(surveys);
    const overall = Math.round(
      missionsUpdated * 0.6 + surveyParticipation * 0.4,
    );

    const { value: trend, direction: trendDirection } =
      calculateTrend(checkInHistory);

    const metrics: EngagementMetrics = {
      overall,
      missionsUpdated,
      surveyParticipation,
      trend,
      trendDirection,
    };

    const krOwnerIds = getKrOwnerIds(missions);

    const teamMembers: PersonEngagement[] = users
      .filter((user) => user.status === "active")
      .map((user) => {
        const {
          value,
          trend: personTrend,
          trendDirection: personTrendDir,
        } = calculatePersonEngagement(user.id, checkInHistory, krOwnerIds);

        return {
          id: user.id,
          name: `${user.firstName} ${user.lastName}`,
          initials:
            user.initials ?? user.firstName.charAt(0) + user.lastName.charAt(0),
          role: user.jobTitle ?? "Colaborador",
          value,
          trend: personTrend,
          trendDirection: personTrendDir,
        };
      })
      .sort((a, b) => b.value - a.value);

    const weeklyData = generateWeeklyEngagementData(checkInHistory);

    const allTeamsOption = { id: "all", label: "Todos os times" };

    return {
      metrics,
      teamMembers,
      teamOptions: [allTeamsOption, ...teamOptions],
      weeklyData,
    };
  }, [users, teamOptions, missions, checkInHistory, surveys]);
}
