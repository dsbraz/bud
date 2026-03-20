import { useMemo } from "react";
import { useWizard } from "../SurveyWizardContext";
import { getTypeLabel } from "../../templates/surveyTemplates";
import { usePeopleData } from "@/contexts/PeopleDataContext";
import { useConfigData } from "@/contexts/ConfigDataContext";
import { formatDateBR } from "@/lib/date-format";
import {
  Card,
  CardBody,
  CardHeader,
  Badge,
  Alert,
  DatePicker,
  ChoiceBox,
  ChoiceBoxGroup,
} from "@mdonangelo/bud-ds";
import type { CalendarDate } from "@mdonangelo/bud-ds";
import type { CyclePhase } from "@/types/survey";
import { QUESTION_TYPE_LABELS } from "@/utils/questionTypeLabels";
import styles from "./StepReview.module.css";

function isoToCalendarDate(iso: string | null): CalendarDate | undefined {
  if (!iso) return undefined;
  const d = new Date(iso);
  return { year: d.getFullYear(), month: d.getMonth() + 1, day: d.getDate() };
}

function calendarDateToIso(cd: CalendarDate): string {
  return new Date(cd.year, cd.month - 1, cd.day).toISOString();
}

function formatDateBr(iso: string | null): string {
  if (!iso) return "-";
  return formatDateBR(iso) || "-";
}

const SCOPE_LABELS: Record<string, string> = {
  company: "Toda a empresa",
  department: "Departamento",
  team: "Time",
  individual: "Individual",
};

const RECURRENCE_LABELS: Record<string, string> = {
  weekly: "Semanal",
  biweekly: "Quinzenal",
  monthly: "Mensal",
  quarterly: "Trimestral",
};

const REMINDER_LABELS: Record<string, string> = {
  daily: "Diário",
  every_2_days: "A cada 2 dias",
  weekly: "Semanal",
};

const CYCLE_PHASE_LABELS: Record<CyclePhase, string> = {
  self_evaluation: "Autoavaliação",
  peer_evaluation: "Avaliação de pares",
  manager_evaluation: "Avaliação do gestor",
  calibration: "Calibração",
  feedback: "Feedback",
};

export function StepReview() {
  const { state, dispatch, isCiclo, participantCount } = useWizard();
  const { resolveUserId, getUserDisplayName } = usePeopleData();
  const { resolveTagId, getTagById, resolveCycleId, getCycleById } = useConfigData();

  const showLgpdWarning =
    state.isAnonymous && participantCount < state.lgpdMinGroupSize && participantCount > 0;

  const channels = useMemo(() => {
    const ch: string[] = [];
    if (state.deliveryInApp) ch.push("In-App");
    if (state.deliveryEmail) ch.push("E-mail");
    if (state.deliverySlack) ch.push("Slack");
    return ch.join(", ") || "-";
  }, [state.deliveryInApp, state.deliveryEmail, state.deliverySlack]);

  const perspectivesSummary = useMemo(() => {
    if (!isCiclo) return null;
    return state.perspectives
      .filter((p) => p.enabled)
      .map((p) => {
        const labels: Record<string, string> = {
          self: "Auto",
          manager: "Gestor",
          peers: "Pares",
          reports: "Liderados",
        };
        return labels[p.perspective] ?? p.perspective;
      })
      .join(", ");
  }, [isCiclo, state.perspectives]);

  const ownersSummary = useMemo(() => {
    if (state.ownerIds.length === 0) return "-";
    const labels = state.ownerIds.map((ownerId) => getUserDisplayName(resolveUserId(ownerId)));
    return labels.join(", ");
  }, [state.ownerIds, getUserDisplayName, resolveUserId]);

  const managersSummary = useMemo(() => {
    if (state.managerIds.length === 0) return "-";
    const labels = state.managerIds.map((managerId) => getUserDisplayName(resolveUserId(managerId)));
    return labels.join(", ");
  }, [state.managerIds, getUserDisplayName, resolveUserId]);

  const tagsSummary = useMemo(() => {
    if (state.tagIds.length === 0) return "-";
    const labels = state.tagIds.map((tagId) => {
      const resolvedTagId = resolveTagId(tagId);
      return getTagById(resolvedTagId)?.name ?? resolvedTagId;
    });
    return labels.join(", ");
  }, [state.tagIds, resolveTagId, getTagById]);

  const cycleSummary = useMemo(() => {
    if (!state.cycleId) return "-";
    const resolvedCycleId = resolveCycleId(state.cycleId);
    return getCycleById(resolvedCycleId)?.name ?? resolvedCycleId;
  }, [state.cycleId, resolveCycleId, getCycleById]);

  /* First question for preview */
  const firstQuestion = state.questions[0] ?? null;

  return (
    <div className={styles.stepContent}>
      <h2 className={styles.heading}>Revisar e lançar</h2>
      <p className={styles.subheading}>
        Confira as configurações antes de {state.launchOption === "scheduled" ? "agendar" : "lançar"}
      </p>

      {/* Summary card */}
      <Card padding="md" shadow={false}>
        <CardHeader title="Resumo da configuração" />
        <CardBody>
          <dl className={styles.summaryGrid}>
            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Tipo</dt>
              <dd className={styles.summaryValue}>
                <Badge color={state.category === "ciclo" ? "wine" : "orange"} size="sm">
                  {state.type ? getTypeLabel(state.type) : "-"}
                </Badge>
              </dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Nome</dt>
              <dd className={styles.summaryValue}>{state.name || "-"}</dd>
            </div>

            {state.description && (
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>Descrição</dt>
                <dd className={styles.summaryValue}>{state.description}</dd>
              </div>
            )}

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Responsáveis</dt>
              <dd className={styles.summaryValue}>{ownersSummary}</dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Gestores</dt>
              <dd className={styles.summaryValue}>{managersSummary}</dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Tags</dt>
              <dd className={styles.summaryValue}>{tagsSummary}</dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Ciclo</dt>
              <dd className={styles.summaryValue}>{cycleSummary}</dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Perguntas</dt>
              <dd className={styles.summaryValue}>{state.questions.length}</dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Escopo</dt>
              <dd className={styles.summaryValue}>
                {SCOPE_LABELS[state.scope.scopeType] ?? state.scope.scopeType}
              </dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Participantes</dt>
              <dd className={styles.summaryValue}>~{participantCount}</dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Anonimato</dt>
              <dd className={styles.summaryValue}>
                {state.isAnonymous ? "Sim" : "Não"}
              </dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Período</dt>
              <dd className={styles.summaryValue}>
                {formatDateBr(state.startDate)} — {formatDateBr(state.endDate)}
              </dd>
            </div>

            {state.recurrence && (
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>Recorrência</dt>
                <dd className={styles.summaryValue}>
                  {RECURRENCE_LABELS[state.recurrence] ?? state.recurrence}
                </dd>
              </div>
            )}

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Análise IA</dt>
              <dd className={styles.summaryValue}>
                {state.aiAnalysisEnabled ? "Ativada" : "Desativada"}
              </dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Lembretes</dt>
              <dd className={styles.summaryValue}>
                {state.reminderEnabled
                  ? state.reminderFrequency
                    ? REMINDER_LABELS[state.reminderFrequency]
                    : "Ativados"
                  : "Desativados"}
              </dd>
            </div>

            <div className={styles.summaryItem}>
              <dt className={styles.summaryLabel}>Canais</dt>
              <dd className={styles.summaryValue}>{channels}</dd>
            </div>

            {isCiclo && perspectivesSummary && (
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>Perspectivas</dt>
                <dd className={styles.summaryValue}>{perspectivesSummary}</dd>
              </div>
            )}

            {isCiclo && state.cyclePhases.length > 0 && (
              <div className={styles.summaryItemFull}>
                <dt className={styles.summaryLabel}>Timeline do ciclo</dt>
                <dd className={styles.summaryValue}>
                  <div className={styles.cyclePhasesList}>
                    {state.cyclePhases.map((cp) => (
                      <div key={cp.phase} className={styles.cyclePhaseRow}>
                        <span className={styles.cyclePhaseName}>
                          {CYCLE_PHASE_LABELS[cp.phase]}
                        </span>
                        <span className={styles.cyclePhaseDates}>
                          {formatDateBr(cp.startDate)} — {formatDateBr(cp.endDate)}
                        </span>
                      </div>
                    ))}
                  </div>
                </dd>
              </div>
            )}
          </dl>
        </CardBody>
      </Card>

      {/* Preview card */}
      {firstQuestion && (
        <Card padding="md" shadow={false}>
          <CardHeader
            title="Prévia para o respondente"
            description="Como o participante verá a primeira pergunta"
          />
          <CardBody>
            <div className={styles.previewCard}>
              <div className={styles.previewHeader}>
                <Badge
                  color={state.category === "ciclo" ? "wine" : "orange"}
                  size="sm"
                >
                  {state.type ? getTypeLabel(state.type) : "Pesquisa"}
                </Badge>
                <span className={styles.previewName}>{state.name}</span>
              </div>

              <div className={styles.previewQuestion}>
                <p className={styles.previewQuestionLabel}>Pergunta 1 de {state.questions.length}</p>
                <p className={styles.previewQuestionText}>{firstQuestion.text}</p>

                <div className={styles.previewPlaceholder}>
                  <Badge color="neutral" size="sm">
                    {QUESTION_TYPE_LABELS[firstQuestion.type]}
                  </Badge>
                  <span className={styles.previewPlaceholderText}>
                    Campo de resposta ({QUESTION_TYPE_LABELS[firstQuestion.type]})
                  </span>
                </div>
              </div>
            </div>
          </CardBody>
        </Card>
      )}

      {/* LGPD warning */}
      {showLgpdWarning && (
        <Alert variant="warning" title="Atenção: grupo pequeno">
          O grupo de participantes (~{participantCount}) é menor que o mínimo recomendado
          ({state.lgpdMinGroupSize}) para garantir anonimato conforme LGPD.
        </Alert>
      )}

      {/* Launch options */}
      <Card padding="md" shadow={false}>
        <CardHeader title="Opção de lançamento" />
        <CardBody>
          <div className={styles.launchOptions}>
            <ChoiceBoxGroup
              value={state.launchOption}
              onChange={(val: string | undefined) =>
                dispatch({
                  type: "SET_LAUNCH_OPTION",
                  payload: (val as "now" | "scheduled") ?? "now",
                })
              }
            >
              <ChoiceBox
                value="now"
                title="Lançar agora"
                description="A pesquisa será enviada imediatamente para todos os participantes"
              />
              <ChoiceBox
                value="scheduled"
                title="Agendar lançamento"
                description="Defina uma data futura para o envio automático da pesquisa"
              />
            </ChoiceBoxGroup>

            {state.launchOption === "scheduled" && (
              <div className={styles.scheduleDatePicker}>
                <DatePicker
                  label="Data e hora do lançamento"
                  mode="single"
                  value={isoToCalendarDate(state.scheduledLaunchAt)}
                  onChange={(cd: CalendarDate | null) =>
                    dispatch({
                      type: "SET_SCHEDULED_LAUNCH",
                      payload: cd ? calendarDateToIso(cd) : null,
                    })
                  }
                />
              </div>
            )}
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
