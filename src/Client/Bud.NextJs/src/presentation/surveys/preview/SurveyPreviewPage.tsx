import { useParams } from "react-router-dom";
import { Alert } from "@mdonangelo/bud-ds";
import { useSurveysData } from "@/contexts/SurveysDataContext";
import { SurveyRenderer } from "../components/SurveyRenderer";
import styles from "./SurveyPreviewPage.module.css";

/**
 * Preview page for a survey — standalone layout (no sidebar).
 * Shows the survey exactly as a respondent would see it,
 * with a warning banner and interactive fields (no real submission).
 */
export function SurveyPreviewPage() {
  const { surveyId } = useParams<{ surveyId: string }>();
  const { getRendererDataBySurveyId } = useSurveysData();

  const surveyData = surveyId
    ? getRendererDataBySurveyId(surveyId)
    : null;

  if (!surveyData) {
    return (
      <div className={styles.page}>
        <Alert variant="error" title="Pesquisa não encontrada">
          Não foi possível carregar os dados da pesquisa
          {surveyId ? ` (ID: ${surveyId})` : ""}.
        </Alert>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <Alert variant="warning" title="Modo de pré-visualização">
        As interações nesta tela são apenas para teste e não salvam respostas reais.
      </Alert>
      <SurveyRenderer survey={surveyData} mode="preview" />
    </div>
  );
}
