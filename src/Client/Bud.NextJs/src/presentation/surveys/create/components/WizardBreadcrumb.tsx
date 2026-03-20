import { useNavigate } from "react-router-dom";
import { Breadcrumb } from "@mdonangelo/bud-ds";
import type { WizardStep } from "@/types/survey";
import { useWizard, clearWizardState } from "../SurveyWizardContext";
import styles from "./WizardBreadcrumb.module.css";

const STEP_LABELS = [
  "Escolher template",
  "Participantes",
  "Questionário",
  "Fluxo de aplicação",
  "Resumo",
];

export function WizardBreadcrumb() {
  const { state, dispatch } = useWizard();
  const navigate = useNavigate();

  /* Wizard steps 1-4 map to breadcrumb indices 1-4; index 0 is template selection */
  const currentIndex = state.step;

  const items = STEP_LABELS.map((label, i) => ({
    label,
    onClick:
      i === 0
        ? () => { clearWizardState(); navigate("/surveys"); }
        : i < currentIndex
          ? () => dispatch({ type: "SET_STEP", payload: i as WizardStep })
          : undefined,
  }));

  return (
    <div className={styles.breadcrumbWrapper}>
      <Breadcrumb items={items} current={currentIndex} />
    </div>
  );
}
