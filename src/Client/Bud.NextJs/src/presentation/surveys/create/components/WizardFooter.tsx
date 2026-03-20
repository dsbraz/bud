import { useNavigate, useSearchParams } from "react-router-dom";
import { FloppyDisk, ArrowRight } from "@phosphor-icons/react";
import { Button, toast } from "@mdonangelo/bud-ds";
import { useWizard, clearWizardState } from "../SurveyWizardContext";
import { useSurveysData } from "@/contexts/SurveysDataContext";
import styles from "./WizardFooter.module.css";

export function WizardFooter() {
  const { state, dispatch, canProceed } = useWizard();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { upsertSurveyFromWizard } = useSurveysData();

  const editingSurveyId = searchParams.get("surveyId") ?? undefined;
  const selectedTemplateId = searchParams.get("templateId") ?? undefined;

  const isLastStep = state.step === 4;

  function handleSaveDraft() {
    upsertSurveyFromWizard(state, {
      surveyId: editingSurveyId,
      mode: "draft",
      templateId: selectedTemplateId,
    });
    toast.success("Rascunho salvo com sucesso!");
    clearWizardState();
    navigate("/surveys");
  }

  function handleBack() {
    if (state.step <= 1) {
      clearWizardState();
      navigate("/surveys");
    } else {
      dispatch({ type: "PREV_STEP" });
    }
  }

  function handleContinue() {
    if (isLastStep) {
      upsertSurveyFromWizard(state, {
        surveyId: editingSurveyId,
        mode: "launch",
        templateId: selectedTemplateId,
      });
      toast.success("Pesquisa lançada com sucesso!");
      clearWizardState();
      navigate("/surveys");
      return;
    }
    dispatch({ type: "NEXT_STEP" });
  }

  return (
    <div className={styles.footer}>
      <Button variant="tertiary" size="md" onClick={handleBack}>
        {state.step <= 1 ? "Cancelar" : "Voltar"}
      </Button>

      <div className={styles.footerRight}>
        <Button
          variant="secondary"
          size="md"
          leftIcon={FloppyDisk}
          onClick={handleSaveDraft}
        >
          Salvar rascunho
        </Button>
        <Button
          variant="primary"
          size="md"
          rightIcon={isLastStep ? undefined : ArrowRight}
          onClick={handleContinue}
          disabled={!canProceed}
        >
          {isLastStep ? "Lançar pesquisa" : "Próximo"}
        </Button>
      </div>
    </div>
  );
}
