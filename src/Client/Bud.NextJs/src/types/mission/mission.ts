import type { Tag } from "@/types/tag/tag";

export type MissionStatus =
  | "draft"
  | "active"
  | "paused"
  | "completed"
  | "cancelled";

export type MissionVisibility = "public" | "team_only" | "private";

export type KanbanStatus = "uncategorized" | "todo" | "doing" | "done";

export type MissionMemberRole = "owner" | "supporter" | "observer";

export type MissionLinkType =
  | "related"
  | "depends_on"
  | "contributes_to"
  | "blocks"
  | "duplicates";

export interface MissionMember {
  missionId: string;
  userId: string;
  role: MissionMemberRole;
  addedAt: string;
  addedBy: string | null;
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

export interface MissionLink {
  id: string;
  sourceMissionId: string;
  targetMissionId: string;
  linkType: MissionLinkType;
  createdBy: string | null;
  createdAt: string;
  /** Preenchido em queries com join */
  target?: { id: string; title: string; status: MissionStatus; progress: number };
  source?: { id: string; title: string; status: MissionStatus; progress: number };
}

export interface ExternalContribution {
  type: "indicator" | "task";
  id: string;
  title: string;
  progress?: number;
  isDone?: boolean;
  status?: import("./key-result").KRStatus;
  owner?: { firstName: string; lastName: string; initials: string | null };
  sourceMission: { id: string; title: string };
}

export interface Mission {
  id: string;
  orgId: string;
  cycleId: string | null;
  parentId: string | null;
  /** Profundidade na árvore: 0 = raiz, 1 = filha, etc. */
  depth: number;
  /** Materialized path: [raiz_id, ..., self_id] para queries eficientes de subárvore */
  path: string[];
  title: string;
  description: string | null;
  ownerId: string;
  teamId: string | null;
  status: MissionStatus;
  visibility: MissionVisibility;
  progress: number;
  kanbanStatus: KanbanStatus;
  sortOrder: number;
  dueDate: string | null;
  completedAt: string | null;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
  /** Preenchido em queries com join */
  owner?: {
    id: string;
    firstName: string;
    lastName: string;
    initials: string | null;
  };
  team?: { id: string; name: string; color: string };
  keyResults?: import("./key-result").KeyResult[];
  tasks?: import("./mission-task").MissionTask[];
  children?: Mission[];
  tags?: Tag[];
  /** FUTURE */
  members?: MissionMember[];
  outgoingLinks?: MissionLink[];
  incomingLinks?: MissionLink[];
  externalContributions?: ExternalContribution[];
  restrictedSummary?: { keyResults: number; tasks: number; children: number };
}
