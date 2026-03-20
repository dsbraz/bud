export type CycleType = "quarterly" | "semi_annual" | "annual" | "custom";

export type CycleStatus = "planning" | "active" | "review" | "ended" | "archived";

export interface Cycle {
  id: string;
  orgId: string;
  name: string;
  type: CycleType;
  startDate: string;
  endDate: string;
  status: CycleStatus;
  okrDefinitionDeadline: string | null;
  midReviewDate: string | null;
  createdAt: string;
  updatedAt: string;
}
