import { useCallback } from "react";
import { useAssistant } from "@/contexts/AssistantContext";
import { useWizard } from "../SurveyWizardContext";
import {
  QuestionnaireBuilder,
  QuestionnaireContextBridge,
} from "@/components/QuestionnaireBuilder";
import type { QuestionnaireAction } from "@/components/QuestionnaireBuilder";
import { useConfigData } from "@/contexts/ConfigDataContext";

export function StepQuestionnaire() {
  const { state, dispatch: wizardDispatch } = useWizard();
  const { companyValues, cyclePresetOptions } = useConfigData();
  const { toggle: toggleAssistant } = useAssistant();

  const questionnaireCompanyValues = companyValues.map((value) => ({
    id: value.id,
    label: value.name,
  }));

  const questionnaireCycles = cyclePresetOptions.map((cycle) => ({
    id: cycle.id,
    label: cycle.label,
    status: cycle.status,
  }));

  // Bridge: forward questionnaire actions to wizard dispatch (action shapes are compatible)
  const questionnaireDispatch = useCallback(
    (action: QuestionnaireAction) => {
      wizardDispatch(action);
    },
    [wizardDispatch],
  );

  return (
    <QuestionnaireContextBridge
      questions={state.questions}
      sections={state.sections}
      dispatch={questionnaireDispatch}
    >
      <QuestionnaireBuilder
        onAiCreate={toggleAssistant}
        companyValues={questionnaireCompanyValues}
        cycles={questionnaireCycles}
      />
    </QuestionnaireContextBridge>
  );
}
