import { useEffect, useRef } from "react";
import { useLocation, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { SurveyWizardProvider, useWizard, STEP_SLUGS, SLUG_TO_STEP, loadWizardState } from "./SurveyWizardContext";
import { useSurveysData } from "@/contexts/SurveysDataContext";
import { WizardTopBar } from "./components/WizardTopBar";
import { WizardBreadcrumb } from "./components/WizardBreadcrumb";
import { WizardFooter } from "./components/WizardFooter";
import { StepParticipants } from "./steps/StepParticipants";
import { StepQuestionnaire } from "./steps/StepQuestionnaire";
import { StepFlow } from "./steps/StepFlow";
import { StepReview } from "./steps/StepReview";
import type { SurveyType, SurveyCategory } from "@/types/survey";
import styles from "./SurveyWizardPage.module.css";

interface LocationState {
  surveyType: SurveyType;
  category: SurveyCategory;
  templateId?: string;
  name?: string;
  description?: string;
  ownerIds?: string[];
  managerIds?: string[];
  tagIds?: string[];
  cycleId?: string | null;
}

function WizardContent() {
  const { state, dispatch } = useWizard();
  const location = useLocation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { getWizardStateBySurveyId, getSurveyTemplateById, getSurveyRecordById } = useSurveysData();
  const { step: stepSlug } = useParams<{ step?: string }>();
  const locState = location.state as LocationState | null;
  const editingSurveyId = searchParams.get("surveyId");
  const templateIdFromQuery = searchParams.get("templateId");
  const templateIdFromRecord = editingSurveyId
    ? getSurveyRecordById(editingSurveyId)?.listItem.templateId ?? null
    : null;
  const initializedRef = useRef(false);

  // Initialize wizard: from location.state (new wizard) or sessionStorage (refresh)
  useEffect(() => {
    if (initializedRef.current) return;
    initializedRef.current = true;

    if (locState?.surveyType) {
      const templateId = locState.templateId ?? templateIdFromQuery ?? undefined;
      const selectedTemplate = templateId
        ? getSurveyTemplateById(templateId)
        : null;

      // Fresh wizard from surveys list
      dispatch({
        type: "SELECT_TEMPLATE",
        payload: {
          surveyType: locState.surveyType,
          category: locState.category,
          template: selectedTemplate
            ? {
                name: selectedTemplate.name,
                sections: selectedTemplate.sections,
                questions: selectedTemplate.questions,
                defaultConfig: selectedTemplate.defaultConfig,
              }
            : undefined,
        },
      });
      if (locState.name) {
        dispatch({ type: "SET_NAME", payload: locState.name });
      }
      if (locState.description) {
        dispatch({ type: "SET_DESCRIPTION", payload: locState.description });
      }
      if (locState.ownerIds || locState.managerIds || locState.tagIds || locState.cycleId !== undefined) {
        dispatch({
          type: "SET_METADATA",
          payload: {
            ownerIds: locState.ownerIds,
            managerIds: locState.managerIds,
            tagIds: locState.tagIds,
            cycleId: locState.cycleId,
          },
        });
      }
      // If URL has a step slug, navigate to that step
      if (stepSlug && SLUG_TO_STEP[stepSlug] !== undefined) {
        dispatch({ type: "SET_STEP", payload: SLUG_TO_STEP[stepSlug] });
      }
    } else if (editingSurveyId) {
      const existingDraft = getWizardStateBySurveyId(editingSurveyId);
      if (existingDraft) {
        dispatch({ type: "LOAD_DRAFT", payload: existingDraft });
        if (stepSlug && SLUG_TO_STEP[stepSlug] !== undefined) {
          dispatch({ type: "SET_STEP", payload: SLUG_TO_STEP[stepSlug] });
        }
      } else {
        navigate("/surveys", { replace: true });
      }
    } else {
      // No location.state → try restoring from sessionStorage (page refresh)
      const saved = loadWizardState();
      if (saved && saved.type !== null) {
        dispatch({ type: "LOAD_DRAFT", payload: saved });
        // If URL has a valid step slug, use it; otherwise use saved step
        if (stepSlug && SLUG_TO_STEP[stepSlug] !== undefined) {
          dispatch({ type: "SET_STEP", payload: SLUG_TO_STEP[stepSlug] });
        }
      } else {
        // No saved state either → go back to surveys
        navigate("/surveys", { replace: true });
      }
    }
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Sync URL when step changes
  useEffect(() => {
    if (!initializedRef.current) return;
    const slug = STEP_SLUGS[state.step];
    if (slug) {
      const queryParams = new URLSearchParams();
      if (editingSurveyId) queryParams.set("surveyId", editingSurveyId);
      const templateId = locState?.templateId ?? templateIdFromQuery ?? templateIdFromRecord;
      if (templateId) {
        queryParams.set("templateId", templateId);
      }

      const query = queryParams.toString();
      const targetPath = `/surveys/new/${slug}${query ? `?${query}` : ""}`;
      if (`${location.pathname}${location.search}` !== targetPath) {
        navigate(targetPath, { replace: true });
      }
    }
  }, [state.step, editingSurveyId, templateIdFromQuery, templateIdFromRecord]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    document.querySelector("[data-scroll-region]")?.scrollTo({ top: 0 });
    window.scrollTo({ top: 0 });
  }, [state.step]);

  const stepComponents: Record<number, React.ReactNode> = {
    1: <StepParticipants key="participants" />,
    2: <StepQuestionnaire key="questionnaire" />,
    3: <StepFlow key="flow" />,
    4: <StepReview key="review" />,
  };

  return (
    <div className={styles.page}>
      <WizardTopBar />

      <div className={styles.card}>
        <WizardBreadcrumb />

        <div className={styles.content}>
          {stepComponents[state.step] ?? null}
        </div>

        <WizardFooter />
      </div>
    </div>
  );
}

export function SurveyWizardPage() {
  return (
    <SurveyWizardProvider>
      <WizardContent />
    </SurveyWizardProvider>
  );
}
