import { useState, useMemo } from "react";
import {
  Modal,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
  Badge,
  Alert,
} from "@mdonangelo/bud-ds";
import { ArrowLeft, ArrowRight, PaperPlaneTilt } from "@phosphor-icons/react";
import { useWizard } from "../SurveyWizardContext";
import { PreviewQuestionRenderer } from "./PreviewQuestionRenderer";
import type { WizardQuestion, WizardSection } from "@/types/survey";
import styles from "./SurveyPreviewModal.module.css";

interface Props {
  open: boolean;
  onClose: () => void;
}

interface SectionWithQuestions {
  section: WizardSection | null;
  questions: WizardQuestion[];
}

export function SurveyPreviewModal({ open, onClose }: Props) {
  const { state } = useWizard();
  const [currentSectionIndex, setCurrentSectionIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, unknown>>({});

  // Group questions by section, preserving order
  const sectionGroups = useMemo<SectionWithQuestions[]>(() => {
    const groups: SectionWithQuestions[] = [];
    const sectionMap = new Map<string | null, WizardQuestion[]>();

    for (const q of state.questions) {
      const key = q.sectionId;
      if (!sectionMap.has(key)) sectionMap.set(key, []);
      sectionMap.get(key)!.push(q);
    }

    // Ordered sections first
    for (const section of state.sections) {
      const questions = sectionMap.get(section.id) ?? [];
      if (questions.length > 0) {
        groups.push({ section, questions });
      }
      sectionMap.delete(section.id);
    }

    // Unsectioned questions
    const unsectioned = sectionMap.get(null);
    if (unsectioned && unsectioned.length > 0) {
      groups.push({ section: null, questions: unsectioned });
    }

    return groups;
  }, [state.questions, state.sections]);

  const totalQuestions = state.questions.length;
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
  const progress = totalSections > 0
    ? Math.round(((currentSectionIndex + 1) / totalSections) * 100)
    : 0;

  function handleAnswer(questionId: string, value: unknown) {
    setAnswers((prev) => ({ ...prev, [questionId]: value }));
  }

  function handleNext() {
    if (isLastSection) {
      handleClose();
      return;
    }
    setCurrentSectionIndex((i) => i + 1);
  }

  function handlePrev() {
    setCurrentSectionIndex((i) => Math.max(0, i - 1));
  }

  function handleClose() {
    setCurrentSectionIndex(0);
    setAnswers({});
    onClose();
  }

  if (!open) return null;

  return (
    <Modal open={open} onClose={handleClose} size="lg">
      <ModalHeader
        title="Preview da pesquisa"
        onClose={handleClose}
      >
        <Badge color="wine" size="sm">Modo preview</Badge>
      </ModalHeader>

      <ModalBody>
        <div className={styles.previewContainer}>
          {/* Survey header — as respondent sees */}
          <div className={styles.surveyHeader}>
            <h1 className={styles.surveyTitle}>{state.name || "Pesquisa sem título"}</h1>
            {state.description && (
              <p className={styles.surveyDescription}>{state.description}</p>
            )}
            {state.isAnonymous && (
              <div className={styles.anonymousBadge}>
                <Badge color="success" size="sm">Anônima</Badge>
              </div>
            )}
          </div>

          {/* Progress bar */}
          <div className={styles.progressWrapper}>
            <div className={styles.progressBar}>
              <div className={styles.progressFill} style={{ width: `${progress}%` }} />
            </div>
            <span className={styles.progressLabel}>
              {totalSections > 1
                ? `Seção ${currentSectionIndex + 1} de ${totalSections}`
                : `${totalQuestions} perguntas`
              }
            </span>
          </div>

          {/* Current section */}
          {currentGroup && (
            <div className={styles.sectionBlock}>
              {currentGroup.section && (
                <div className={styles.sectionHeader}>
                  <h2 className={styles.sectionTitle}>{currentGroup.section.title}</h2>
                  {currentGroup.section.description && (
                    <p className={styles.sectionDescription}>
                      {currentGroup.section.description}
                    </p>
                  )}
                </div>
              )}

              <div className={styles.questionsList}>
                {currentGroup.questions.map((q, i) => (
                  <PreviewQuestionRenderer
                    key={q.id}
                    question={q}
                    index={questionOffset + i}
                    total={totalQuestions}
                    value={answers[q.id] ?? null}
                    onChange={(v) => handleAnswer(q.id, v)}
                  />
                ))}
              </div>
            </div>
          )}

          {totalQuestions === 0 && (
            <Alert variant="info" title="Sem perguntas">
              Adicione perguntas no passo "Questionário" para visualizar o preview.
            </Alert>
          )}

          {/* Disclaimer */}
          <p className={styles.disclaimer}>
            As respostas neste preview não são salvas. Esta é apenas uma visualização.
          </p>
        </div>
      </ModalBody>

      <ModalFooter align="between">
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
          rightIcon={isLastSection ? PaperPlaneTilt : ArrowRight}
          onClick={handleNext}
        >
          {isLastSection ? "Finalizar preview" : "Próxima seção"}
        </Button>
      </ModalFooter>
    </Modal>
  );
}
