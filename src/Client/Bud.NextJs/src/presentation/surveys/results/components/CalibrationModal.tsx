import { useState, useMemo, type ChangeEvent } from "react";
import {
  Modal,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
  Card,
  CardBody,
  Select,
  Toggle,
  Textarea,
  Alert,
  GoalGaugeBar,
  Badge,
} from "@mdonangelo/bud-ds";
import { FloppyDisk } from "@phosphor-icons/react";
import type { CalibrationParticipant } from "../types";
import type { SurveyType } from "@/types/survey";
import styles from "./CalibrationModal.module.css";

/* ——— 9-Box mapping ——— */

type PerfLevel = "baixo" | "médio" | "alto";
type PotLevel = "baixo" | "médio" | "alto";

const PERFORMANCE_OPTIONS = [
  { value: "baixo", label: "Baixo" },
  { value: "médio", label: "Médio" },
  { value: "alto", label: "Alto" },
];

const POTENTIAL_OPTIONS = [
  { value: "baixo", label: "Baixo" },
  { value: "médio", label: "Médio" },
  { value: "alto", label: "Alto" },
];

/** 9-box label by [potential][performance] */
const NINE_BOX_LABELS: Record<PotLevel, Record<PerfLevel, { label: string; color: "error" | "warning" | "caramel" | "success" | "neutral" }>> = {
  alto: {
    baixo:  { label: "Enigma",           color: "warning" },
    médio:  { label: "Alto potencial",   color: "success" },
    alto:   { label: "Estrela",          color: "success" },
  },
  médio: {
    baixo:  { label: "Questionável",     color: "error" },
    médio:  { label: "Mantenedor",       color: "caramel" },
    alto:   { label: "Forte desempenho", color: "success" },
  },
  baixo: {
    baixo:  { label: "Insuficiente",     color: "error" },
    médio:  { label: "Eficaz",           color: "caramel" },
    alto:   { label: "Comprometido",     color: "warning" },
  },
};

/** Derive performance level from numeric score */
function derivePerformanceLevel(score: number): PerfLevel {
  if (score >= 3.7) return "alto";
  if (score >= 2.5) return "médio";
  return "baixo";
}

/* ——— Score gauge status ——— */

function getGaugeStatus(score: number): "on-track" | "attention" | "off-track" {
  if (score >= 4) return "on-track";
  if (score >= 3) return "attention";
  return "off-track";
}

/* ——— Component ——— */

interface CalibrationModalProps {
  participant: CalibrationParticipant;
  open: boolean;
  onClose: () => void;
  onSave: (participant: CalibrationParticipant, finalScore: number, classification: string, justification: string) => void;
  surveyType: SurveyType;
}

export function CalibrationModal({ participant, open, onClose, onSave, surveyType }: CalibrationModalProps) {
  /* Compute auto-derived performance level */
  const autoPerformance = useMemo(() => {
    const score = participant.finalScore ??
      (participant.managerScore * 0.5 + (participant.score360 ?? participant.selfScore) * 0.3 + participant.selfScore * 0.2);
    return derivePerformanceLevel(score);
  }, [participant]);

  const [performance, setPerformance] = useState<PerfLevel>(autoPerformance);
  const [potential, setPotential] = useState<PotLevel>(participant.potential);
  const [deloitte1, setDeloitte1] = useState(false);
  const [deloitte2, setDeloitte2] = useState(false);
  const [justification, setJustification] = useState("");

  const is360OrPerformance = surveyType === "360_feedback" || surveyType === "performance";

  /* 9-box result from current selections */
  const nineBoxResult = NINE_BOX_LABELS[potential][performance];

  /* Whether the gestor changed from auto-derived values */
  const isOverridden = performance !== autoPerformance || potential !== participant.potential;

  function handleSave() {
    const computedScore = participant.finalScore ??
      Math.round((participant.managerScore * 0.5 + (participant.score360 ?? participant.selfScore) * 0.3 + participant.selfScore * 0.2) * 10) / 10;

    onSave(participant, computedScore, nineBoxResult.label, justification);
  }

  return (
    <Modal open={open} onClose={onClose} size="lg">
      <ModalHeader title={`Calibragem — ${participant.name}`} onClose={onClose}>
        <Badge color={participant.status === "calibrado" ? "success" : "warning"} size="sm">
          {participant.status === "calibrado" ? "Calibrado" : "Pendente"}
        </Badge>
      </ModalHeader>
      <ModalBody>
        <div className={styles.modalContent}>
          {/* Score comparison */}
          <div className={styles.scoresGrid}>
            <Card padding="sm">
              <CardBody>
                <div className={styles.scoreCard}>
                  <span className={styles.scoreLabel}>Autoavaliação</span>
                  <GoalGaugeBar
                    label=""
                    value={participant.selfScore * 20}
                    max={100}
                    goalType="above"
                    status={getGaugeStatus(participant.selfScore)}
                  />
                  <span className={styles.scoreNumber}>{participant.selfScore.toFixed(1)}</span>
                </div>
              </CardBody>
            </Card>
            <Card padding="sm">
              <CardBody>
                <div className={styles.scoreCard}>
                  <span className={styles.scoreLabel}>Gestor</span>
                  <GoalGaugeBar
                    label=""
                    value={participant.managerScore * 20}
                    max={100}
                    goalType="above"
                    status={getGaugeStatus(participant.managerScore)}
                  />
                  <span className={styles.scoreNumber}>{participant.managerScore.toFixed(1)}</span>
                </div>
              </CardBody>
            </Card>
            {participant.score360 != null && (
              <Card padding="sm">
                <CardBody>
                  <div className={styles.scoreCard}>
                    <span className={styles.scoreLabel}>360°</span>
                    <GoalGaugeBar
                      label=""
                      value={participant.score360 * 20}
                      max={100}
                      goalType="above"
                      status={getGaugeStatus(participant.score360)}
                    />
                    <span className={styles.scoreNumber}>{participant.score360.toFixed(1)}</span>
                  </div>
                </CardBody>
              </Card>
            )}
          </div>

          {/* 9-Box classification */}
          <div className={styles.nineBoxSection}>
            <span className={styles.fieldLabel}>Posição no 9-Box</span>
            <div className={styles.nineBoxRow}>
              <Select
                label="Desempenho"
                options={PERFORMANCE_OPTIONS}
                value={performance}
                onChange={(v: string | undefined) => setPerformance((v as PerfLevel | undefined) ?? "médio")}
                size="md"
              />
              <Select
                label="Potencial"
                options={POTENTIAL_OPTIONS}
                value={potential}
                onChange={(v: string | undefined) => setPotential((v as PotLevel | undefined) ?? "médio")}
                size="md"
              />
              <div className={styles.nineBoxResultCard}>
                <Badge color={nineBoxResult.color} size="lg">{nineBoxResult.label}</Badge>
                {isOverridden && (
                  <span className={styles.overrideHint}>Ajustado manualmente</span>
                )}
              </div>
            </div>
          </div>

          {/* Deloitte questions (360/performance only) */}
          {is360OrPerformance && (
            <div className={styles.deloitteSection}>
              <span className={styles.fieldLabel}>Perguntas Deloitte</span>
              <Toggle
                label="Se fosse meu dinheiro, eu daria a esta pessoa o maior aumento possível"
                checked={deloitte1}
                onChange={() => setDeloitte1(!deloitte1)}
              />
              <Toggle
                label="Eu sempre quero esta pessoa no meu time"
                checked={deloitte2}
                onChange={() => setDeloitte2(!deloitte2)}
              />
            </div>
          )}

          {/* Justification */}
          <Textarea
            label="Justificativa"
            value={justification}
            onChange={(e: ChangeEvent<HTMLTextAreaElement>) => setJustification(e.target.value)}
            rows={4}
            placeholder="Descreva a justificativa para esta calibragem..."
          />

          {/* Bias alert */}
          {participant.biasAlert && (
            <Alert variant="warning" title="Alerta de viés detectado pela IA">
              {participant.biasAlert}. Considere revisar os scores antes de finalizar a calibragem.
            </Alert>
          )}

          {/* Support data */}
          <Card padding="sm">
            <CardBody>
              <span className={styles.fieldLabel}>Dados de suporte</span>
              <div className={styles.supportGrid}>
                {participant.okrCompletion != null && (
                  <div className={styles.supportItem}>
                    <span className={styles.supportLabel}>OKRs concluídos</span>
                    <span className={styles.supportValue}>{participant.okrCompletion}%</span>
                  </div>
                )}
                {participant.feedbackCount != null && (
                  <div className={styles.supportItem}>
                    <span className={styles.supportLabel}>Feedbacks recebidos</span>
                    <span className={styles.supportValue}>{participant.feedbackCount}</span>
                  </div>
                )}
                {participant.pulseMean != null && (
                  <div className={styles.supportItem}>
                    <span className={styles.supportLabel}>Pulse médio</span>
                    <span className={styles.supportValue}>{participant.pulseMean.toFixed(1)}</span>
                  </div>
                )}
                {participant.score360 != null && (
                  <div className={styles.supportItem}>
                    <span className={styles.supportLabel}>Score 360°</span>
                    <span className={styles.supportValue}>{participant.score360.toFixed(1)}</span>
                  </div>
                )}
              </div>
            </CardBody>
          </Card>
        </div>
      </ModalBody>
      <ModalFooter>
        <Button variant="secondary" size="md" onClick={onClose}>Cancelar</Button>
        <Button variant="primary" size="md" leftIcon={FloppyDisk} onClick={handleSave}>Salvar calibragem</Button>
      </ModalFooter>
    </Modal>
  );
}
