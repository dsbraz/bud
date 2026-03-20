export interface SubTask {
  id: string;
  taskId: string;
  title: string;
  isDone: boolean;
  sortOrder: number;
}

export interface MissionTask {
  id: string;
  missionId: string | null;
  keyResultId: string | null;
  title: string;
  description: string | null;
  ownerId: string | null;
  dueDate: string | null;
  isDone: boolean;
  sortOrder: number;
  completedAt: string | null;
  createdAt: string;
  updatedAt: string;
  /** Preenchido em queries com join */
  owner?: {
    id: string;
    firstName: string;
    lastName: string;
    initials: string | null;
  };
  subtasks?: SubTask[];
  contributesTo?: { missionId: string; missionTitle: string }[];
}
