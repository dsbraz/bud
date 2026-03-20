import type { CheckIn } from "./check-in";
import type { MissionTask } from "./mission-task";

export type GoalType =
  | "reach"
  | "above"
  | "below"
  | "between"
  | "reduce"
  | "survey";

export type MeasurementMode = "manual" | "task" | "mission" | "external";

export type KRUnit = "percent" | "currency" | "count" | "custom";

export type KRStatus = "on_track" | "attention" | "off_track" | "completed";

export interface KeyResult {
  id: string;
  orgId: string;
  missionId: string;
  parentKrId: string | null;
  title: string;
  description: string | null;
  ownerId: string;
  measurementMode: MeasurementMode;
  goalType: GoalType;
  targetValue: string | null;
  currentValue: string;
  startValue: string;
  lowThreshold: string | null;
  highThreshold: string | null;
  unit: KRUnit;
  unitLabel: string | null;
  expectedValue: string | null;
  status: KRStatus;
  progress: number;
  periodLabel: string | null;
  periodStart: string | null;
  periodEnd: string | null;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
  linkedMissionId: string | null;
  linkedSurveyId: string | null;
  externalSource: string | null;
  externalConfig: string | null;
  /** Preenchido em queries com join */
  owner?: {
    id: string;
    firstName: string;
    lastName: string;
    initials: string | null;
  };
  checkIns?: CheckIn[];
  tasks?: MissionTask[];
  children?: KeyResult[];
  linkedMission?: import("./mission").Mission;
  contributesTo?: { missionId: string; missionTitle: string }[];
}
