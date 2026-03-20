export type TeamColor =
  | "neutral"
  | "orange"
  | "wine"
  | "caramel"
  | "success"
  | "warning"
  | "error";

export type TeamRoleInTeam = "leader" | "member" | "observer";

export type TeamStatus = "active" | "archived";

export interface TeamMember {
  teamId: string;
  userId: string;
  roleInTeam: TeamRoleInTeam;
  joinedAt: string;
  /** Preenchido em queries com join */
  user?: {
    id: string;
    firstName: string;
    lastName: string;
    initials: string | null;
    jobTitle: string | null;
    avatarUrl: string | null;
  };
}

export interface Team {
  id: string;
  orgId: string;
  name: string;
  description: string | null;
  color: TeamColor;
  leaderId: string | null;
  parentTeamId: string | null;
  status: TeamStatus;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
  members?: TeamMember[];
}
