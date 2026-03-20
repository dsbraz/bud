import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Alert, Button } from "@mdonangelo/bud-ds";
import { CheckCircle } from "@phosphor-icons/react";
import { useSurveysData } from "@/contexts/SurveysDataContext";
import { SurveyRenderer } from "../components/SurveyRenderer";
import { BudLogo } from "@/components/BudLogo";
import styles from "./SurveyRespondPage.module.css";

/**
 * Respondent-facing survey page — standalone (no sidebar).
 * Allows real submission and shows a thank-you screen on completion.
 */
export function SurveyRespondPage() {
  const { surveyId } = useParams<{ surveyId: string }>();
  const navigate = useNavigate();
  const { getRendererDataBySurveyId, submitSurveyResponse } = useSurveysData();
  const [submitted, setSubmitted] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const surveyData = surveyId
    ? getRendererDataBySurveyId(surveyId)
    : null;

  if (!surveyData) {
    return (
      <div className={styles.errorContainer}>
        <Alert variant="error" title="Pesquisa não encontrada">
          Não foi possível carregar a pesquisa
          {surveyId ? ` (ID: ${surveyId})` : ""}.
          Verifique o link e tente novamente.
        </Alert>
        <div className={styles.errorActions}>
          <Button variant="secondary" size="md" onClick={() => navigate("/surveys")}>Voltar para pesquisas</Button>
        </div>
      </div>
    );
  }

  function handleSubmit(answers: Record<string, unknown>) {
    if (submitting) return;
    setSubmitting(true);

    try {
      if (surveyId) {
        submitSurveyResponse({
          surveyId,
          answers,
        });
      }
      setSubmitted(true);
    } finally {
      setSubmitting(false);
    }
  }

  if (submitted) {
    return (
      <div className={styles.thankYouContainer}>
        <div className={styles.thankYouCard}>
          <div className={styles.thankYouIcon}>
            <CheckCircle size={64} color="var(--color-green-500)" />
          </div>
          <h1 className={styles.thankYouTitle}>Obrigado!</h1>
          <p className={styles.thankYouText}>
            Sua resposta à pesquisa <strong>{surveyData.name}</strong> foi enviada com sucesso.
          </p>
          {surveyData.isAnonymous && (
            <p className={styles.thankYouAnonymous}>
              Esta pesquisa é anônima — suas respostas não serão associadas à sua identidade.
            </p>
          )}
          <p className={styles.thankYouHint}>Você já pode fechar esta janela com segurança.</p>
          <div className={styles.thankYouLogo}>
            <BudLogo height={24} color="var(--color-neutral-300)" />
          </div>
        </div>
      </div>
    );
  }

  return (
    <SurveyRenderer
      survey={surveyData}
      mode="respond"
      onSubmit={handleSubmit}
      submitting={submitting}
    />
  );
}
