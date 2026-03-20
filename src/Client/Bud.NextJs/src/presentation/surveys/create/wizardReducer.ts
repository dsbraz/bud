import type {
  SurveyWizardState,
  WizardStep,
  SurveyType,
  SurveyCategory,
  ScopeType,
  ApplicationMode,
  Recurrence,
  WeekDay,
  ReminderFrequency,
  WizardPerspective,
  WizardCyclePhase,
  WizardQuestion,
  WizardSection,
  EvaluationPerspective,
} from "@/types/survey";
import { getTemplateByType } from "../templates/surveyTemplates";

/* ——— Actions ——— */

export type WizardAction =
  | {
      type: "SELECT_TEMPLATE";
      payload: {
        surveyType: SurveyType;
        category: SurveyCategory;
        template?: {
          name: string;
          sections: WizardSection[];
          questions: WizardQuestion[];
          defaultConfig: {
            isAnonymous: boolean;
            recurrence?: Recurrence | null;
            aiPrefillOkrs?: boolean;
            aiPrefillFeedback?: boolean;
            aiBiasDetection?: boolean;
          };
        };
      };
    }
  | { type: "SET_STEP"; payload: WizardStep }
  | { type: "NEXT_STEP" }
  | { type: "PREV_STEP" }
  | { type: "SET_NAME"; payload: string }
  | { type: "SET_DESCRIPTION"; payload: string }
  | { type: "SET_METADATA"; payload: { ownerIds?: string[]; managerIds?: string[]; tagIds?: string[]; cycleId?: string | null } }
  | { type: "SET_SCOPE_TYPE"; payload: ScopeType }
  | { type: "ADD_TEAM"; payload: string }
  | { type: "REMOVE_TEAM"; payload: string }
  | { type: "TOGGLE_USER_EXCLUSION"; payload: string }
  | { type: "SET_EXCLUDED_USERS"; payload: string[] }
  | { type: "TOGGLE_EXCLUSION"; payload: "trialPeriod" | "leave" | "vacation" }
  | { type: "SET_PERSPECTIVE_CONFIG"; payload: { perspective: EvaluationPerspective; config: Partial<WizardPerspective> } }
  | { type: "ADD_QUESTION"; payload: WizardQuestion }
  | { type: "REMOVE_QUESTION"; payload: string }
  | { type: "UPDATE_QUESTION"; payload: { id: string; changes: Partial<WizardQuestion> } }
  | { type: "REORDER_QUESTIONS"; payload: WizardQuestion[] }
  | { type: "REORDER_ALL"; payload: { sections: WizardSection[]; questions: WizardQuestion[] } }
  | { type: "ADD_SECTION"; payload: WizardSection }
  | { type: "UPDATE_SECTION"; payload: { id: string; changes: Partial<WizardSection> } }
  | { type: "REMOVE_SECTION"; payload: string }
  | { type: "SET_ANONYMOUS"; payload: boolean }
  | { type: "SET_LGPD_MIN_GROUP"; payload: number }
  | { type: "SET_AI_ANALYSIS"; payload: boolean }
  | { type: "SET_AI_PREFILL_OKRS"; payload: boolean }
  | { type: "SET_AI_PREFILL_FEEDBACK"; payload: boolean }
  | { type: "SET_AI_BIAS_DETECTION"; payload: boolean }
  | { type: "SET_APPLICATION_MODE"; payload: ApplicationMode }
  | { type: "SET_RECURRENCE"; payload: Recurrence | null }
  | { type: "SET_RECURRENCE_DAY"; payload: WeekDay | null }
  | { type: "SET_REMINDER_ENABLED"; payload: boolean }
  | { type: "SET_REMINDER_FREQUENCY"; payload: ReminderFrequency | null }
  | { type: "SET_NOTIFY_MANAGERS"; payload: boolean }
  | { type: "SET_MANAGER_NOTIFICATION_TEMPLATE"; payload: string }
  | { type: "SET_NON_RESPONDENT_NOTIFICATION_TEMPLATE"; payload: string }
  | { type: "SET_DELIVERY_CHANNEL"; payload: { channel: "inApp" | "email" | "slack"; enabled: boolean } }
  | { type: "SET_START_DATE"; payload: string | null }
  | { type: "SET_END_DATE"; payload: string | null }
  | { type: "SET_CYCLE_PHASES"; payload: WizardCyclePhase[] }
  | { type: "SET_LAUNCH_OPTION"; payload: "now" | "scheduled" }
  | { type: "SET_SCHEDULED_LAUNCH"; payload: string | null }
  | { type: "LOAD_DRAFT"; payload: SurveyWizardState }
  | { type: "RESET" };

/* ——— Initial State ——— */

const DEFAULT_PERSPECTIVES: WizardPerspective[] = [
  { perspective: "self", enabled: true, isAnonymous: false, peerSelectionMethod: "manager_assigns", minEvaluators: 1, maxEvaluators: 1 },
  { perspective: "manager", enabled: true, isAnonymous: false, peerSelectionMethod: "manager_assigns", minEvaluators: 1, maxEvaluators: 1 },
  { perspective: "peers", enabled: true, isAnonymous: true, peerSelectionMethod: "manager_assigns", minEvaluators: 3, maxEvaluators: 5 },
  { perspective: "reports", enabled: false, isAnonymous: true, peerSelectionMethod: "automatic", minEvaluators: 3, maxEvaluators: 10 },
];

export const initialWizardState: SurveyWizardState = {
  step: 0,
  type: null,
  category: null,
  name: "",
  description: "",
  ownerIds: [],
  managerIds: [],
  tagIds: [],
  cycleId: null,

  scope: { scopeType: "company", teamIds: [], userIds: [] },
  excludedUserIds: [],
  excludeTrialPeriod: false,
  excludeLeave: false,
  excludeVacation: false,
  perspectives: DEFAULT_PERSPECTIVES,

  sections: [],
  questions: [],

  isAnonymous: true,
  lgpdMinGroupSize: 5,
  aiAnalysisEnabled: true,
  aiPrefillOkrs: false,
  aiPrefillFeedback: false,
  aiBiasDetection: false,
  applicationMode: "single",
  recurrence: null,
  recurrenceDay: null,
  reminderEnabled: true,
  reminderFrequency: null,
  notifyManagers: true,
  managerNotificationTemplate: "Olá! A pesquisa \"{nome}\" está em andamento. Acompanhe a adesão do seu time e incentive a participação.",
  nonRespondentNotificationTemplate: "Olá! Você ainda não respondeu a pesquisa \"{nome}\". Sua participação é muito importante para nós.",
  deliveryInApp: true,
  deliveryEmail: true,
  deliverySlack: false,
  startDate: null,
  endDate: null,
  cyclePhases: [],

  launchOption: "now",
  scheduledLaunchAt: null,
};

/* ——— Helpers ——— */

let questionIdCounter = 0;
let sectionIdCounter = 0;

function generateQuestionId(): string {
  return `q-${Date.now()}-${++questionIdCounter}`;
}

function generateSectionId(): string {
  return `s-${Date.now()}-${++sectionIdCounter}`;
}

function loadTemplateQuestions(surveyType: SurveyType): { sections: WizardSection[]; questions: WizardQuestion[] } {
  const template = getTemplateByType(surveyType);
  if (!template) return { sections: [], questions: [] };

  const sections: WizardSection[] = [];
  const questions: WizardQuestion[] = [];

  for (const section of template.sections) {
    const sectionId = generateSectionId();
    sections.push({ id: sectionId, title: section.title, description: section.description });

    for (const q of section.questions) {
      questions.push({
        id: generateQuestionId(),
        sectionId,
        type: q.type,
        text: q.text,
        isRequired: q.isRequired,
        options: q.options,
        scaleMin: q.scaleMin,
        scaleMax: q.scaleMax,
        scaleLabels: q.scaleLabels,
        ratingMax: q.ratingMax,
      });
    }
  }

  return { sections, questions };
}

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") {
    return structuredClone(value);
  }
  return JSON.parse(JSON.stringify(value)) as T;
}

/* ——— Reducer ——— */

export function wizardReducer(state: SurveyWizardState, action: WizardAction): SurveyWizardState {
  switch (action.type) {
    case "SELECT_TEMPLATE": {
      const { surveyType, category, template: selectedTemplate } = action.payload;
      const template = getTemplateByType(surveyType);
      const fallbackQuestions = loadTemplateQuestions(surveyType);
      const sections = selectedTemplate?.sections.length
        ? cloneDeep(selectedTemplate.sections)
        : fallbackQuestions.sections;
      const questions = selectedTemplate?.questions.length
        ? cloneDeep(selectedTemplate.questions)
        : fallbackQuestions.questions;
      const config = selectedTemplate?.defaultConfig ?? template?.defaultConfig ?? { isAnonymous: true };

      return {
        ...initialWizardState,
        step: 1,
        type: surveyType,
        category,
        name: selectedTemplate?.name ?? template?.name ?? "",
        sections,
        questions,
        isAnonymous: config.isAnonymous ?? true,
        recurrence: (config.recurrence as Recurrence) ?? null,
        aiPrefillOkrs: config.aiPrefillOkrs ?? false,
        aiPrefillFeedback: config.aiPrefillFeedback ?? false,
        aiBiasDetection: config.aiBiasDetection ?? false,
        perspectives: category === "ciclo" ? DEFAULT_PERSPECTIVES : [],
      };
    }

    case "SET_STEP":
      return { ...state, step: action.payload };

    case "NEXT_STEP":
      return { ...state, step: Math.min(4, state.step + 1) as WizardStep };

    case "PREV_STEP":
      return { ...state, step: Math.max(0, state.step - 1) as WizardStep };

    case "SET_NAME":
      return { ...state, name: action.payload };

    case "SET_DESCRIPTION":
      return { ...state, description: action.payload };

    case "SET_METADATA":
      return {
        ...state,
        ownerIds: action.payload.ownerIds ?? state.ownerIds,
        managerIds: action.payload.managerIds ?? state.managerIds,
        tagIds: action.payload.tagIds ?? state.tagIds,
        cycleId: action.payload.cycleId === undefined ? state.cycleId : action.payload.cycleId,
      };

    case "SET_SCOPE_TYPE":
      return { ...state, scope: { ...state.scope, scopeType: action.payload, teamIds: action.payload === "company" ? [] : state.scope.teamIds } };

    case "ADD_TEAM":
      return { ...state, scope: { ...state.scope, teamIds: [...state.scope.teamIds, action.payload] } };

    case "REMOVE_TEAM":
      return { ...state, scope: { ...state.scope, teamIds: state.scope.teamIds.filter((id) => id !== action.payload) } };

    case "TOGGLE_USER_EXCLUSION": {
      const userId = action.payload;
      const excluded = state.excludedUserIds.includes(userId)
        ? state.excludedUserIds.filter((id) => id !== userId)
        : [...state.excludedUserIds, userId];
      return { ...state, excludedUserIds: excluded };
    }

    case "SET_EXCLUDED_USERS":
      return { ...state, excludedUserIds: action.payload };

    case "TOGGLE_EXCLUSION": {
      const key = action.payload === "trialPeriod" ? "excludeTrialPeriod" : action.payload === "leave" ? "excludeLeave" : "excludeVacation";
      return { ...state, [key]: !state[key] };
    }

    case "SET_PERSPECTIVE_CONFIG":
      return {
        ...state,
        perspectives: state.perspectives.map((p) =>
          p.perspective === action.payload.perspective ? { ...p, ...action.payload.config } : p,
        ),
      };

    case "ADD_QUESTION":
      return { ...state, questions: [...state.questions, action.payload] };

    case "REMOVE_QUESTION":
      return { ...state, questions: state.questions.filter((q) => q.id !== action.payload) };

    case "UPDATE_QUESTION":
      return {
        ...state,
        questions: state.questions.map((q) =>
          q.id === action.payload.id ? { ...q, ...action.payload.changes } : q,
        ),
      };

    case "REORDER_QUESTIONS":
      return { ...state, questions: action.payload };

    case "REORDER_ALL":
      return { ...state, sections: action.payload.sections, questions: action.payload.questions };

    case "ADD_SECTION":
      return { ...state, sections: [...state.sections, action.payload] };

    case "UPDATE_SECTION":
      return {
        ...state,
        sections: state.sections.map((s) =>
          s.id === action.payload.id ? { ...s, ...action.payload.changes } : s,
        ),
      };

    case "REMOVE_SECTION":
      return {
        ...state,
        sections: state.sections.filter((s) => s.id !== action.payload),
        questions: state.questions.map((q) => q.sectionId === action.payload ? { ...q, sectionId: null } : q),
      };

    case "SET_ANONYMOUS":
      return { ...state, isAnonymous: action.payload };

    case "SET_LGPD_MIN_GROUP":
      return { ...state, lgpdMinGroupSize: action.payload };

    case "SET_AI_ANALYSIS":
      return { ...state, aiAnalysisEnabled: action.payload };

    case "SET_AI_PREFILL_OKRS":
      return { ...state, aiPrefillOkrs: action.payload };

    case "SET_AI_PREFILL_FEEDBACK":
      return { ...state, aiPrefillFeedback: action.payload };

    case "SET_AI_BIAS_DETECTION":
      return { ...state, aiBiasDetection: action.payload };

    case "SET_APPLICATION_MODE":
      return {
        ...state,
        applicationMode: action.payload,
        recurrence: action.payload === "single" ? null : state.recurrence,
        recurrenceDay: action.payload === "single" ? null : state.recurrenceDay,
      };

    case "SET_RECURRENCE":
      return { ...state, recurrence: action.payload };

    case "SET_RECURRENCE_DAY":
      return { ...state, recurrenceDay: action.payload };

    case "SET_REMINDER_ENABLED":
      return { ...state, reminderEnabled: action.payload };

    case "SET_REMINDER_FREQUENCY":
      return { ...state, reminderFrequency: action.payload };

    case "SET_NOTIFY_MANAGERS":
      return { ...state, notifyManagers: action.payload };

    case "SET_MANAGER_NOTIFICATION_TEMPLATE":
      return { ...state, managerNotificationTemplate: action.payload };

    case "SET_NON_RESPONDENT_NOTIFICATION_TEMPLATE":
      return { ...state, nonRespondentNotificationTemplate: action.payload };

    case "SET_DELIVERY_CHANNEL": {
      const { channel, enabled } = action.payload;
      const key = channel === "inApp" ? "deliveryInApp" : channel === "email" ? "deliveryEmail" : "deliverySlack";
      return { ...state, [key]: enabled };
    }

    case "SET_START_DATE":
      return { ...state, startDate: action.payload };

    case "SET_END_DATE":
      return { ...state, endDate: action.payload };

    case "SET_CYCLE_PHASES":
      return { ...state, cyclePhases: action.payload };

    case "SET_LAUNCH_OPTION":
      return { ...state, launchOption: action.payload };

    case "SET_SCHEDULED_LAUNCH":
      return { ...state, scheduledLaunchAt: action.payload };

    case "LOAD_DRAFT":
      return { ...initialWizardState, ...action.payload };

    case "RESET":
      return initialWizardState;

    default:
      return state;
  }
}
