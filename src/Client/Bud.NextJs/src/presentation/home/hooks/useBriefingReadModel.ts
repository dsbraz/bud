import { useMemo } from "react";
import type { Icon } from "@phosphor-icons/react";
import {
  Lightning,
  WarningCircle,
  CalendarCheck,
  Trophy,
  ChartLineDown,
  Users,
} from "@phosphor-icons/react";
import { useMissionsData } from "@/contexts/MissionsDataContext";
import { useSurveysData } from "@/contexts/SurveysDataContext";
import { usePeopleData } from "@/contexts/PeopleDataContext";

export interface BriefingAction {
  label: string;
  href?: string;
  onClick?: () => void;
}

export interface BriefingItem {
  id: string;
  icon: Icon;
  priority: "high" | "normal" | "positive";
  text: string;
  action?: BriefingAction;
}

/**
 * Determines mission health based on progress and due date
 * - at-risk: active mission with progress < 50% and less than 2 weeks to due date
 * - healthy: completed or progress >= 70%
 * - needs-attention: everything else that's active
 */
function getMissionHealth(mission: { status: string; progress: number; dueDate: string | null }): "at-risk" | "healthy" | "needs-attention" {
  if (mission.status === "completed") return "healthy";
  if (mission.status !== "active") return "needs-attention";

  if (mission.progress >= 70) return "healthy";

  // Check if close to due date with low progress
  if (mission.dueDate && mission.progress < 50) {
    const [day, month, year] = mission.dueDate.split("/").map(Number);
    if (day && month && year) {
      const dueDate = new Date(year, month - 1, day);
      const today = new Date();
      const daysUntilDue = Math.ceil((dueDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
      if (daysUntilDue <= 14 && daysUntilDue > 0) {
        return "at-risk";
      }
    }
  }

  return mission.progress < 30 ? "at-risk" : "needs-attention";
}

export function useBriefingReadModel(): BriefingItem[] {
  const { missions } = useMissionsData();
  const { surveys } = useSurveysData();
  const { users } = usePeopleData();

  return useMemo(() => {
    const items: BriefingItem[] = [];
    const activeMissions = missions.filter((m) => m.status === "active");

    // 1. Check for at-risk missions
    const atRiskMissions = activeMissions.filter((m) => getMissionHealth(m) === "at-risk");
    if (atRiskMissions.length > 0) {
      const lowestProgress = atRiskMissions.reduce(
        (min, m) => Math.min(min, m.progress),
        100,
      );
      const worstMission = atRiskMissions.find((m) => m.progress === lowestProgress);

      items.push({
        id: "missions-at-risk",
        icon: WarningCircle,
        priority: "high",
        text: worstMission
          ? `"${worstMission.title}" está com ${worstMission.progress}% de progresso e prazo próximo.`
          : `${atRiskMissions.length} missão(ões) precisam de atenção urgente.`,
        action: {
          label: "Ver missões",
          href: "/missions",
        },
      });
    }

    // 2. Check for active surveys with low response rate
    const activeSurveys = surveys.filter((s) => s.status === "active");
    for (const survey of activeSurveys) {
      if (survey.completionRate < 70 && survey.totalRecipients > 0) {
        // Calculate days until end if we have end date
        let daysLeft = 0;
        if (survey.endDate) {
          const [day, month, year] = survey.endDate.split("/").map(Number);
          if (day && month && year) {
            const endDate = new Date(year, month - 1, day);
            const today = new Date();
            daysLeft = Math.ceil(
              (endDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24),
            );
          }
        }

        const isUrgent = daysLeft > 0 && daysLeft <= 7;
        const urgencyText = isUrgent
          ? `Encerra em ${daysLeft} dia${daysLeft > 1 ? "s" : ""} com apenas ${survey.completionRate}% de respostas.`
          : `${survey.completionRate}% de respostas até agora.`;

        items.push({
          id: `survey-${survey.id}`,
          icon: CalendarCheck,
          priority: isUrgent ? "high" : "normal",
          text: `${survey.name}: ${urgencyText}`,
          action: {
            label: isUrgent ? "Enviar lembrete" : "Ver respostas",
            href: `/surveys/${survey.id}/results`,
          },
        });
        break; // Only show one survey alert
      }
    }

    // 3. Team performance highlight
    const completedMissions = missions.filter((m) => m.status === "completed");
    const healthyMissions = activeMissions.filter((m) => getMissionHealth(m) === "healthy");
    const totalHealthy = completedMissions.length + healthyMissions.length;

    if (missions.length > 0) {
      const healthyPercentage = Math.round((totalHealthy / missions.length) * 100);

      if (healthyPercentage >= 70) {
        items.push({
          id: "team-performance",
          icon: Trophy,
          priority: "positive",
          text: `${healthyPercentage}% das missões estão saudáveis. Ótimo trabalho do time!`,
          action: {
            label: "Ver missões",
            href: "/missions",
          },
        });
      } else if (healthyPercentage < 40 && missions.length > 3) {
        items.push({
          id: "team-performance-warning",
          icon: ChartLineDown,
          priority: "high",
          text: `Apenas ${healthyPercentage}% das missões estão no caminho certo. Revise as prioridades.`,
          action: {
            label: "Revisar missões",
            href: "/missions",
          },
        });
      }
    }

    // 4. Team size info (only if no alerts)
    const activeUsers = users.filter((u) => u.status === "active").length;
    if (items.length < 3 && activeUsers > 5) {
      items.push({
        id: "team-size",
        icon: Users,
        priority: "normal",
        text: `${activeUsers} colaboradores ativos no seu time. Mantenha a cadência de check-ins.`,
        action: {
          label: "Ver time",
          href: "/people/users",
        },
      });
    }

    // 5. If no items, add a default positive message
    if (items.length === 0) {
      items.push({
        id: "all-good",
        icon: Lightning,
        priority: "positive",
        text: "Nenhuma ação urgente. Aproveite para planejar o próximo ciclo.",
        action: {
          label: "Ver missões",
          href: "/missions",
        },
      });
    }

    // Sort by priority: high first, then normal, then positive
    const priorityOrder = { high: 0, normal: 1, positive: 2 };
    items.sort((a, b) => priorityOrder[a.priority] - priorityOrder[b.priority]);

    // Limit to 4 items
    return items.slice(0, 4);
  }, [missions, surveys, users]);
}
