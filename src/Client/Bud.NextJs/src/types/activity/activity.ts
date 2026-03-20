export type ActivityType =
  | "login"
  | "checkin_create"
  | "checkin_update"
  | "survey_start"
  | "survey_complete"
  | "mission_view"
  | "mission_update"
  | "kr_update";

export type ActivityEntityType =
  | "mission"
  | "key-result"
  | "survey"
  | "checkin";

export interface UserActivity {
  id: string;
  userId: string;
  type: ActivityType;
  entityId: string | null;
  entityType: ActivityEntityType | null;
  metadata: Record<string, unknown> | null;
  createdAt: string;
}
