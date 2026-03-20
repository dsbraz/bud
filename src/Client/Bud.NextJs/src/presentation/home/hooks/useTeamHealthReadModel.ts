import { useMemo } from "react";
import { usePeopleData } from "@/contexts/PeopleDataContext";
import { useMissionsData } from "@/contexts/MissionsDataContext";
import {
  getProgressByOwner,
  daysSinceUserCheckin,
  formatDaysAgo,
} from "@/lib/tempStorage/engagement-utils";

export interface TeamMemberHealth {
  id: string;
  name: string;
  initials: string;
  missions: number;
  missionsExpected: number;
  status: "good" | "attention" | "critical";
  lastCheckinDays: number;
  lastCheckinLabel: string;
}

export interface TeamHealthSummary {
  good: number;
  attention: number;
  critical: number;
}

export interface TeamHealthReadModel {
  summary: TeamHealthSummary;
  members: TeamMemberHealth[];
  teamOptions: Array<{ id: string; label: string }>;
}

export function useTeamHealthReadModel(): TeamHealthReadModel {
  const { users, teams, teamOptions } = usePeopleData();
  const { missions, checkInHistory } = useMissionsData();

  return useMemo(() => {
    const progressByOwner = getProgressByOwner(missions);
    const expectedProgress = 66; // Expected progress at this point in quarter

    const members: TeamMemberHealth[] = users
      .filter((user) => user.status === "active")
      .map((user) => {
        const ownerProgress = progressByOwner.get(user.id);
        const avgProgress =
          ownerProgress && ownerProgress.count > 0
            ? Math.round(ownerProgress.total / ownerProgress.count)
            : 50;

        const days = daysSinceUserCheckin(user.id, checkInHistory);

        let status: "good" | "attention" | "critical" = "good";
        if (avgProgress < expectedProgress - 20 || days > 10) {
          status = "critical";
        } else if (avgProgress < expectedProgress - 10 || days > 5) {
          status = "attention";
        }

        return {
          id: user.id,
          name: `${user.firstName} ${user.lastName}`,
          initials:
            user.initials ?? user.firstName.charAt(0) + user.lastName.charAt(0),
          missions: avgProgress,
          missionsExpected: expectedProgress,
          status,
          lastCheckinDays: days,
          lastCheckinLabel: formatDaysAgo(days),
        };
      })
      .sort((a, b) => {
        const statusOrder = { critical: 0, attention: 1, good: 2 };
        return statusOrder[a.status] - statusOrder[b.status];
      });

    const summary: TeamHealthSummary = {
      good: members.filter((m) => m.status === "good").length,
      attention: members.filter((m) => m.status === "attention").length,
      critical: members.filter((m) => m.status === "critical").length,
    };

    const allTeamsOption = { id: "all", label: "Todos os times" };

    return {
      summary,
      members,
      teamOptions: [allTeamsOption, ...teamOptions],
    };
  }, [users, teams, teamOptions, missions, checkInHistory]);
}
