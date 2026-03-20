import { createContext, useContext, useReducer, useMemo, useEffect } from "react";
import type { Dispatch, ReactNode } from "react";
import type { SurveyWizardState, WizardStep } from "@/types/survey";
import { wizardReducer, initialWizardState } from "./wizardReducer";
import type { WizardAction } from "./wizardReducer";

/* ——— Step ↔ URL slug mapping ——— */

const STEP_SLUGS: Record<number, string> = {
  1: "participantes",
  2: "questionario",
  3: "fluxo",
  4: "resumo",
};

const SLUG_TO_STEP: Record<string, WizardStep> = {
  participantes: 1,
  questionario: 2,
  fluxo: 3,
  resumo: 4,
};

export { STEP_SLUGS, SLUG_TO_STEP };

/* ——— localStorage persistence ——— */

const STORAGE_KEY = "bud_survey_wizard_state";

export function saveWizardState(state: SurveyWizardState) {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  } catch {
    // quota exceeded — ignore
  }
}

export function loadWizardState(): SurveyWizardState | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as SurveyWizardState;
  } catch {
    return null;
  }
}

export function clearWizardState() {
  sessionStorage.removeItem(STORAGE_KEY);
}

/* ——— Context ——— */

interface WizardContextValue {
  state: SurveyWizardState;
  dispatch: Dispatch<WizardAction>;
  isCiclo: boolean;
  canProceed: boolean;
  participantCount: number;
}

const WizardContext = createContext<WizardContextValue | null>(null);

function validateStep(state: SurveyWizardState): boolean {
  switch (state.step) {
    case 0:
      return state.type !== null;
    case 1:
      if (state.scope.scopeType === "team" && state.scope.teamIds.length === 0) return false;
      return true;
    case 2:
      return state.questions.length > 0;
    case 3:
      return state.name.trim().length > 0 && !!state.startDate && !!state.endDate;
    case 4:
      if (state.launchOption === "scheduled" && !state.scheduledLaunchAt) return false;
      return true;
    default:
      return false;
  }
}

function estimateParticipants(state: SurveyWizardState): number {
  let total = 0;
  if (state.scope.scopeType === "company") total = 150;
  else if (state.scope.scopeType === "team") total = state.scope.teamIds.length * 12;
  else total = state.scope.userIds.length;
  return Math.max(0, total - state.excludedUserIds.length);
}

export function SurveyWizardProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(wizardReducer, initialWizardState);

  // Persist state to sessionStorage on every change (except step 0 / initial)
  useEffect(() => {
    if (state.type !== null) {
      saveWizardState(state);
    }
  }, [state]);

  const value = useMemo<WizardContextValue>(() => ({
    state,
    dispatch,
    isCiclo: state.category === "ciclo",
    canProceed: validateStep(state),
    participantCount: estimateParticipants(state),
  }), [state]);

  return (
    <WizardContext.Provider value={value}>
      {children}
    </WizardContext.Provider>
  );
}

export function useWizard(): WizardContextValue {
  const ctx = useContext(WizardContext);
  if (!ctx) throw new Error("useWizard must be used within SurveyWizardProvider");
  return ctx;
}
