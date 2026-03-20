import { Badge, Button } from "@mdonangelo/bud-ds";
import {
  ShieldCheck,
  ArrowsClockwise,
  Brain,
  Eye,
  EyeSlash,
  SidebarSimple,
} from "@phosphor-icons/react";
import type { SurveyTemplate } from "../templates/surveyTemplates";
import { QUESTION_TYPE_LABELS } from "@/utils/questionTypeLabels";
import styles from "./TemplatePreviewPanel.module.css";

/* ——— Component ——— */

interface TemplatePreviewPanelProps {
  template: SurveyTemplate;
  onClose: () => void;
}

export function TemplatePreviewPanel({ template, onClose }: TemplatePreviewPanelProps) {
  const totalQuestions = template.sections.reduce(
    (sum, s) => sum + s.questions.length,
    0,
  );

  const config = template.defaultConfig;

  let questionCounter = 0;

  return (
    <div className={styles.panel}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerInfo}>
          <span className={styles.headerTitle}>{template.name}</span>
          <div className={styles.headerMeta}>
            <span className={styles.headerMetaText}>{template.subtitle}</span>
          </div>
        </div>
        <Button
          variant="tertiary"
          size="md"
          leftIcon={SidebarSimple}
          onClick={onClose}
          aria-label="Fechar preview"
        />
      </div>

      {/* Scrollable body */}
      <div className={styles.body}>
        {/* Config indicators */}
        <div className={styles.configSection}>
          <div className={styles.configRow}>
            {config.isAnonymous ? (
              <EyeSlash size={14} className={styles.configIcon} />
            ) : (
              <Eye size={14} className={styles.configIcon} />
            )}
            <span className={styles.configLabel}>
              {config.isAnonymous ? "Respostas anônimas" : "Respostas identificadas"}
            </span>
          </div>
          {config.aiBiasDetection && (
            <div className={styles.configRow}>
              <ShieldCheck size={14} className={styles.configIcon} />
              <span className={styles.configLabel}>Detecção de viés por IA</span>
            </div>
          )}
          {(config.aiPrefillOkrs || config.aiPrefillFeedback) && (
            <div className={styles.configRow}>
              <Brain size={14} className={styles.configIcon} />
              <span className={styles.configLabel}>
                Pré-preenchimento por IA
                {config.aiPrefillOkrs && config.aiPrefillFeedback
                  ? " (OKRs + Feedback)"
                  : config.aiPrefillOkrs
                    ? " (OKRs)"
                    : " (Feedback)"}
              </span>
            </div>
          )}
          {config.recurrence && (
            <div className={styles.configRow}>
              <ArrowsClockwise size={14} className={styles.configIcon} />
              <span className={styles.configLabel}>
                Recorrência {config.recurrence === "weekly" ? "semanal" : config.recurrence}
              </span>
            </div>
          )}
        </div>

        <div className={styles.divider} />

        {/* Flow model */}
        {template.flowSteps.length > 0 && (
          <div className={styles.flowSection}>
            <span className={styles.flowTitle}>Fluxo de aplicação</span>
            <div className={styles.flowSteps}>
              {template.flowSteps.map((step, i) => (
                <div key={i} className={styles.flowStep}>
                  <span className={styles.flowStepNumber}>{i + 1}</span>
                  <div className={styles.flowStepContent}>
                    <span className={styles.flowStepLabel}>{step.label}</span>
                    {step.description && (
                      <span className={styles.flowStepDesc}>{step.description}</span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        <div className={styles.divider} />

        {/* Questions by section */}
        <div className={styles.questionsSection}>
          <div className={styles.flowTitle}>
            Perguntas <Badge color="neutral" size="sm">{totalQuestions}</Badge>
          </div>

          {template.sections.map((section) => (
            <div key={section.title} className={styles.sectionBlock}>
              <span className={styles.sectionName}>{section.title}</span>
              {section.questions.map((q) => {
                questionCounter++;
                return (
                  <div key={questionCounter} className={styles.questionItem}>
                    <span className={styles.questionNumber}>{questionCounter}.</span>
                    <div className={styles.questionContent}>
                      <span className={styles.questionText}>{q.text}</span>
                      <div className={styles.questionMeta}>
                        <span className={styles.questionType}>
                          {QUESTION_TYPE_LABELS[q.type] ?? q.type}
                        </span>
                        {q.isRequired && <span className={styles.requiredDot} />}
                      </div>
                      {q.options && q.options.length > 0 && (
                        <div className={styles.optionsPreview}>
                          {q.options.map((opt) => (
                            <span key={opt.id} className={styles.optionChip}>
                              {opt.label}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
