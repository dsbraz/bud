import { useState, useMemo, useCallback } from "react";
import { Button, Badge, Alert } from "@mdonangelo/bud-ds";
import { ArrowLeft, ArrowRight, PaperPlaneTilt } from "@phosphor-icons/react";
import { QuestionField } from "./QuestionField";
import type { WizardQuestion, WizardSection } from "@/types/survey";
import styles from "./SurveyRenderer.module.css";

/* ——— Types ——— */

export interface SurveyRendererData {
  name: string;
  description?: string;
  isAnonymous?: boolean;
  sections: WizardSection[];
  questions: WizardQuestion[];
}

export interface SurveyRendererProps {
  /** Survey content to render */
  survey: SurveyRendererData;
  /** "preview" shows banner warning, "respond" enables real submission */
  mode: "preview" | "respond";
  /** Called with all answers when form is submitted (respond mode only) */
  onSubmit?: (answers: Record<string, unknown>) => void;
  /** Whether the form is currently submitting */
  submitting?: boolean;
}

/* ——— Section grouping ——— */

interface SectionGroup {
  section: WizardSection | null;
  questions: WizardQuestion[];
}

function groupQuestionsBySection(
  sections: WizardSection[],
  questions: WizardQuestion[],
): SectionGroup[] {
  const groups: SectionGroup[] = [];
  const sectionMap = new Map<string | null, WizardQuestion[]>();

  for (const q of questions) {
    const key = q.sectionId;
    if (!sectionMap.has(key)) sectionMap.set(key, []);
    sectionMap.get(key)!.push(q);
  }

  // Ordered sections first
  for (const section of sections) {
    const sectionQuestions = sectionMap.get(section.id) ?? [];
    if (sectionQuestions.length > 0) {
      groups.push({ section, questions: sectionQuestions });
    }
    sectionMap.delete(section.id);
  }

  // Unsectioned questions
  const unsectioned = sectionMap.get(null);
  if (unsectioned && unsectioned.length > 0) {
    groups.push({ section: null, questions: unsectioned });
  }

  return groups;
}

/* ——— Component ——— */

export function SurveyRenderer({
  survey,
  mode,
  onSubmit,
  submitting = false,
}: SurveyRendererProps) {
  const [currentSectionIndex, setCurrentSectionIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, unknown>>({});
  const [errors, setErrors] = useState<Set<string>>(new Set());

  const sectionGroups = useMemo(
    () => groupQuestionsBySection(survey.sections, survey.questions),
    [survey.sections, survey.questions],
  );

  const totalQuestions = survey.questions.length;
  const currentGroup = sectionGroups[currentSectionIndex] ?? null;
  const totalSections = sectionGroups.length;
  const isLastSection = currentSectionIndex >= totalSections - 1;
  const isFirstSection = currentSectionIndex === 0;

  // Count questions before current section for numbering
  const questionOffset = useMemo(() => {
    let offset = 0;
    for (let i = 0; i < currentSectionIndex; i++) {
      offset += sectionGroups[i]?.questions.length ?? 0;
    }
    return offset;
  }, [currentSectionIndex, sectionGroups]);

  // Progress percentage
  const progress =
    totalSections > 0
      ? Math.round(((currentSectionIndex + 1) / totalSections) * 100)
      : 0;

  const handleAnswer = useCallback((questionId: string, value: unknown) => {
    setAnswers((prev) => ({ ...prev, [questionId]: value }));
    // Clear error when user answers
    setErrors((prev) => {
      if (!prev.has(questionId)) return prev;
      const next = new Set(prev);
      next.delete(questionId);
      return next;
    });
  }, []);

  /** Validate current section's required questions. Returns true if valid. */
  const validateCurrentSection = useCallback((): boolean => {
    if (mode === "preview" || !currentGroup) return true;

    const newErrors = new Set<string>();
    for (const q of currentGroup.questions) {
      if (!q.isRequired) continue;
      const answer = answers[q.id];
      const isEmpty =
        answer === undefined ||
        answer === null ||
        answer === "" ||
        (Array.isArray(answer) && answer.length === 0);
      if (isEmpty) {
        newErrors.add(q.id);
      }
    }

    if (newErrors.size > 0) {
      setErrors((prev) => new Set([...prev, ...newErrors]));
      return false;
    }
    return true;
  }, [mode, currentGroup, answers]);

  function handleNext() {
    if (!validateCurrentSection()) return;

    if (isLastSection) {
      if (mode === "preview") {
        // Reset to start so admin can review again
        setCurrentSectionIndex(0);
        setAnswers({});
        window.scrollTo({ top: 0, behavior: "smooth" });
        return;
      }
      onSubmit?.(answers);
      return;
    }
    setCurrentSectionIndex((i) => i + 1);
    // Scroll to top of form
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handlePrev() {
    setCurrentSectionIndex((i) => Math.max(0, i - 1));
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  if (totalQuestions === 0) {
    return (
      <div className={styles.container}>
        <Alert variant="info" title="Sem perguntas">
          Esta pesquisa ainda não possui perguntas.
        </Alert>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {/* Preview banner */}
      {mode === "preview" && (
        <Alert variant="warning" title="Modo preview">
          Esta é uma visualização. As respostas não serão salvas.
        </Alert>
      )}

      {/* Survey header */}
      <div className={styles.surveyHeader}>
        <h1 className={styles.surveyTitle}>
          {survey.name || "Pesquisa sem título"}
        </h1>
        {survey.description && (
          <p className={styles.surveyDescription}>{survey.description}</p>
        )}
        {survey.isAnonymous && (
          <div className={styles.badgeRow}>
            <Badge color="success" size="sm">
              Anônima
            </Badge>
          </div>
        )}
      </div>

      {/* Progress bar */}
      <div className={styles.progressWrapper}>
        <div className={styles.progressBar}>
          <div
            className={styles.progressFill}
            style={{ width: `${progress}%` }}
          />
        </div>
        <span className={styles.progressLabel}>
          {totalSections > 1
            ? `Seção ${currentSectionIndex + 1} de ${totalSections}`
            : `${totalQuestions} perguntas`}
        </span>
      </div>

      {/* Current section */}
      {currentGroup && (
        <div className={styles.sectionBlock}>
          {currentGroup.section && (
            <div className={styles.sectionHeader}>
              <h2 className={styles.sectionTitle}>
                {currentGroup.section.title}
              </h2>
              {currentGroup.section.description && (
                <p className={styles.sectionDescription}>
                  {currentGroup.section.description}
                </p>
              )}
            </div>
          )}

          <div className={styles.questionsList}>
            {currentGroup.questions.map((q, i) => (
              <QuestionField
                key={q.id}
                question={q}
                index={questionOffset + i}
                total={totalQuestions}
                value={answers[q.id] ?? null}
                onChange={(v) => handleAnswer(q.id, v)}
                error={errors.has(q.id)}
              />
            ))}
          </div>
        </div>
      )}

      {/* Navigation */}
      <div className={styles.navigation}>
        <Button
          variant="secondary"
          size="md"
          leftIcon={ArrowLeft}
          onClick={handlePrev}
          disabled={isFirstSection}
        >
          Anterior
        </Button>
        <Button
          variant="primary"
          size="md"
          rightIcon={isLastSection ? (mode === "preview" ? ArrowRight : PaperPlaneTilt) : ArrowRight}
          onClick={handleNext}
          loading={submitting}
        >
          {isLastSection
            ? mode === "preview"
              ? "Reiniciar preview"
              : "Enviar respostas"
            : "Próxima seção"}
        </Button>
      </div>

    </div>
  );
}
