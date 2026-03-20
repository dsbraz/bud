/* ——— Enums ——— */

export type SurveyType =
  | "pulse"
  | "clima"
  | "enps"
  | "health_check"
  | "skip_level"
  | "custom"
  | "performance"
  | "360_feedback"
  | "feedback_solicitado";

export type SurveyCategory = "pesquisa" | "ciclo";

export type SurveyStatus =
  | "draft"
  | "scheduled"
  | "active"
  | "paused"
  | "closed"
  | "archived";

export type QuestionType =
  | "text_short"
  | "text_long"
  | "multiple_choice"
  | "checkbox"
  | "dropdown"
  | "likert"
  | "nps"
  | "rating"
  | "ranking"
  | "date"
  | "yes_no";

export type ScopeType = "company" | "department" | "team" | "individual";

export type EvaluationPerspective = "self" | "manager" | "peers" | "reports";

export type PeerSelectionMethod =
  | "manager_assigns"
  | "self_select"
  | "automatic";

export type ApplicationMode = "single" | "recurring";

export type Recurrence = "weekly" | "biweekly" | "monthly" | "quarterly";

export type WeekDay =
  | "monday"
  | "tuesday"
  | "wednesday"
  | "thursday"
  | "friday"
  | "saturday"
  | "sunday";

export type ReminderFrequency = "daily" | "every_2_days" | "weekly";

export type CyclePhase =
  | "self_evaluation"
  | "peer_evaluation"
  | "manager_evaluation"
  | "calibration"
  | "feedback";

export type ParticipantRole = "respondent" | "evaluated" | "evaluator";

export type ParticipantStatus = "pending" | "in_progress" | "completed";

/* ——— Entities ——— */

export interface Survey {
  id: string;
  orgId: string;
  createdBy: string;
  type: SurveyType;
  category: SurveyCategory;
  name: string;
  description: string | null;
  status: SurveyStatus;
  isAnonymous: boolean;
  lgpdMinGroupSize: number;
  excludeTrialPeriod: boolean;
  excludeLeave: boolean;
  excludeVacation: boolean;
  aiAnalysisEnabled: boolean;
  aiPrefillOkrs: boolean;
  aiPrefillFeedback: boolean;
  aiBiasDetection: boolean;
  recurrence: Recurrence | null;
  reminderEnabled: boolean;
  reminderFrequency: ReminderFrequency | null;
  notifyManagers: boolean;
  deliveryInApp: boolean;
  deliveryEmail: boolean;
  deliverySlack: boolean;
  startDate: string | null;
  endDate: string | null;
  scheduledLaunchAt: string | null;
  launchedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SurveySection {
  id: string;
  surveyId: string;
  title: string;
  description: string | null;
  sortOrder: number;
}

export interface SurveyQuestion {
  id: string;
  surveyId: string;
  sectionId: string | null;
  type: QuestionType;
  text: string;
  isRequired: boolean;
  sortOrder: number;
  metadata: Record<string, unknown> | null;
  options?: SurveyQuestionOption[];
}

export interface SurveyQuestionOption {
  id: string;
  questionId: string;
  label: string;
  value: string | null;
  sortOrder: number;
}

export interface SurveyParticipant {
  id: string;
  surveyId: string;
  userId: string;
  role: ParticipantRole;
  status: ParticipantStatus;
  notifiedAt: string | null;
  viewedAt: string | null;
  completedAt: string | null;
  createdAt: string;
}

export interface SurveyScope {
  id: string;
  surveyId: string;
  scopeType: ScopeType;
  teamId: string | null;
  userId: string | null;
}

export interface EvaluationPerspectiveConfig {
  id: string;
  surveyId: string;
  perspective: EvaluationPerspective;
  enabled: boolean;
  isAnonymous: boolean;
  peerSelectionMethod: PeerSelectionMethod | null;
  minEvaluators: number | null;
  maxEvaluators: number | null;
}

export interface SurveyCyclePhase {
  id: string;
  surveyId: string;
  phase: CyclePhase;
  startDate: string | null;
  endDate: string | null;
  sortOrder: number;
}

/* ——— Wizard State ——— */

export type WizardStep = 0 | 1 | 2 | 3 | 4;

export interface QuestionOption {
  id: string;
  label: string;
}

export interface WizardQuestion {
  id: string;
  sectionId: string | null;
  type: QuestionType;
  text: string;
  description?: string;
  isRequired: boolean;
  options?: QuestionOption[];
  scaleMin?: number;
  scaleMax?: number;
  scaleLabels?: Record<string, string>;
  ratingMax?: number;
  companyValueId?: string | null;
  cycleId?: string | null;
}

export interface WizardSection {
  id: string;
  title: string;
  description?: string;
}

export interface WizardScope {
  scopeType: ScopeType;
  teamIds: string[];
  userIds: string[];
}

export interface WizardPerspective {
  perspective: EvaluationPerspective;
  enabled: boolean;
  isAnonymous: boolean;
  peerSelectionMethod: PeerSelectionMethod;
  minEvaluators: number;
  maxEvaluators: number;
}

export interface WizardCyclePhase {
  phase: CyclePhase;
  startDate: string | null;
  endDate: string | null;
}

export interface SurveyWizardState {
  step: WizardStep;
  type: SurveyType | null;
  category: SurveyCategory | null;
  name: string;
  description: string;
  ownerIds: string[];
  managerIds: string[];
  tagIds: string[];
  cycleId: string | null;
  scope: WizardScope;
  excludedUserIds: string[];
  excludeTrialPeriod: boolean;
  excludeLeave: boolean;
  excludeVacation: boolean;
  perspectives: WizardPerspective[];
  sections: WizardSection[];
  questions: WizardQuestion[];
  isAnonymous: boolean;
  lgpdMinGroupSize: number;
  aiAnalysisEnabled: boolean;
  aiPrefillOkrs: boolean;
  aiPrefillFeedback: boolean;
  aiBiasDetection: boolean;
  applicationMode: ApplicationMode;
  recurrence: Recurrence | null;
  recurrenceDay: WeekDay | null;
  reminderEnabled: boolean;
  reminderFrequency: ReminderFrequency | null;
  notifyManagers: boolean;
  managerNotificationTemplate: string;
  nonRespondentNotificationTemplate: string;
  deliveryInApp: boolean;
  deliveryEmail: boolean;
  deliverySlack: boolean;
  startDate: string | null;
  endDate: string | null;
  cyclePhases: WizardCyclePhase[];
  launchOption: "now" | "scheduled";
  scheduledLaunchAt: string | null;
}
