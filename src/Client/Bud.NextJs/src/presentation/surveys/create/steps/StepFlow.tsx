import { useWizard } from "../SurveyWizardContext";
import {
  Select,
  Toggle,
  Checkbox,
  Radio,
  Textarea,
  DatePicker,
  Card,
  CardBody,
  Alert,
} from "@mdonangelo/bud-ds";
import type { CalendarDate } from "@mdonangelo/bud-ds";
import type { Recurrence, WeekDay, ReminderFrequency, CyclePhase } from "@/types/survey";
import styles from "./StepFlow.module.css";

function isoToCalendarDate(iso: string | null): CalendarDate | undefined {
  if (!iso) return undefined;
  const d = new Date(iso);
  return { year: d.getFullYear(), month: d.getMonth() + 1, day: d.getDate() };
}

function calendarDateToIso(cd: CalendarDate): string {
  return new Date(cd.year, cd.month - 1, cd.day).toISOString();
}

const RECURRENCE_OPTIONS = [
  { value: "weekly", label: "Semanal" },
  { value: "biweekly", label: "Quinzenal" },
  { value: "monthly", label: "Mensal" },
  { value: "quarterly", label: "Trimestral" },
];

const WEEKDAY_OPTIONS = [
  { value: "monday", label: "Segunda-feira" },
  { value: "tuesday", label: "Terça-feira" },
  { value: "wednesday", label: "Quarta-feira" },
  { value: "thursday", label: "Quinta-feira" },
  { value: "friday", label: "Sexta-feira" },
  { value: "saturday", label: "Sábado" },
  { value: "sunday", label: "Domingo" },
];

const REMINDER_FREQ_OPTIONS = [
  { value: "daily", label: "Diário" },
  { value: "every_2_days", label: "A cada 2 dias" },
  { value: "weekly", label: "Semanal" },
];

const CYCLE_PHASE_LABELS: Record<CyclePhase, string> = {
  self_evaluation: "Autoavaliação",
  peer_evaluation: "Avaliação de pares",
  manager_evaluation: "Avaliação do gestor",
  calibration: "Calibração",
  feedback: "Feedback",
};

const CYCLE_PHASES_ORDER: CyclePhase[] = [
  "self_evaluation",
  "peer_evaluation",
  "manager_evaluation",
  "calibration",
  "feedback",
];

export function StepFlow() {
  const { state, dispatch, isCiclo } = useWizard();

  return (
    <div className={styles.stepContent}>
      <h2 className={styles.heading}>Configuração</h2>
      <p className={styles.subheading}>
        Defina as configurações {isCiclo ? "do ciclo" : "da pesquisa"}
      </p>

      {/* Período */}
      <section className={styles.formSection}>
        <h3 className={styles.sectionTitle}>Período</h3>
        <div className={styles.formRow}>
          <div className={styles.formField}>
            <DatePicker
              label="Data de início"
              mode="single"
              value={isoToCalendarDate(state.startDate)}
              onChange={(cd: CalendarDate | null) =>
                dispatch({
                  type: "SET_START_DATE",
                  payload: cd ? calendarDateToIso(cd) : null,
                })
              }
            />
          </div>
          <div className={styles.formField}>
            <DatePicker
              label="Data de término"
              mode="single"
              value={isoToCalendarDate(state.endDate)}
              onChange={(cd: CalendarDate | null) =>
                dispatch({
                  type: "SET_END_DATE",
                  payload: cd ? calendarDateToIso(cd) : null,
                })
              }
              minDate={isoToCalendarDate(state.startDate)}
            />
          </div>
        </div>
      </section>

      {/* Timeline do ciclo */}
      {isCiclo && (
        <section className={styles.formSection}>
          <h3 className={styles.sectionTitle}>Timeline do ciclo</h3>
          <p className={styles.sectionDescription}>
            Defina as datas de cada fase do ciclo de avaliação
          </p>

          <div className={styles.cycleTimeline}>
            {CYCLE_PHASES_ORDER.map((phase, index) => {
              const phaseData = state.cyclePhases.find((cp) => cp.phase === phase);
              return (
                <div key={phase} className={styles.timelineItem}>
                  <div className={styles.timelineIndicator}>
                    <div className={styles.timelineDot}>
                      <span className={styles.timelineNumber}>{index + 1}</span>
                    </div>
                    {index < CYCLE_PHASES_ORDER.length - 1 && (
                      <div className={styles.timelineLine} />
                    )}
                  </div>

                  <Card padding="sm" shadow={false}>
                    <CardBody>
                      <div className={styles.phaseContent}>
                        <h4 className={styles.phaseTitle}>
                          {CYCLE_PHASE_LABELS[phase]}
                        </h4>
                        <div className={styles.phaseDates}>
                          <div className={styles.formField}>
                            <DatePicker
                              label="Início"
                              mode="single"
                              size="sm"
                              value={isoToCalendarDate(phaseData?.startDate ?? null)}
                              onChange={(cd: CalendarDate | null) => {
                                const updated = [...state.cyclePhases];
                                const idx = updated.findIndex((cp) => cp.phase === phase);
                                const entry = {
                                  phase,
                                  startDate: cd ? calendarDateToIso(cd) : null,
                                  endDate: phaseData?.endDate ?? null,
                                };
                                if (idx >= 0) updated[idx] = entry;
                                else updated.push(entry);
                                dispatch({ type: "SET_CYCLE_PHASES", payload: updated });
                              }}
                            />
                          </div>
                          <div className={styles.formField}>
                            <DatePicker
                              label="Fim"
                              mode="single"
                              size="sm"
                              value={isoToCalendarDate(phaseData?.endDate ?? null)}
                              onChange={(cd: CalendarDate | null) => {
                                const updated = [...state.cyclePhases];
                                const idx = updated.findIndex((cp) => cp.phase === phase);
                                const entry = {
                                  phase,
                                  startDate: phaseData?.startDate ?? null,
                                  endDate: cd ? calendarDateToIso(cd) : null,
                                };
                                if (idx >= 0) updated[idx] = entry;
                                else updated.push(entry);
                                dispatch({ type: "SET_CYCLE_PHASES", payload: updated });
                              }}
                              minDate={isoToCalendarDate(phaseData?.startDate ?? null)}
                            />
                          </div>
                        </div>
                      </div>
                    </CardBody>
                  </Card>
                </div>
              );
            })}
          </div>
        </section>
      )}

      {/* Modo de aplicação */}
      <section className={styles.formSection}>
        <h3 className={styles.sectionTitle}>Aplicação</h3>
        <p className={styles.sectionDescription}>
          Defina se {isCiclo ? "o ciclo" : "a pesquisa"} será aplicado uma única vez ou de forma recorrente
        </p>

        <div className={styles.radioGroup}>
          <Radio
            name="applicationMode"
            label="Coleta única"
            description="Aplicar uma única vez no período definido"
            checked={state.applicationMode === "single"}
            onChange={() =>
              dispatch({ type: "SET_APPLICATION_MODE", payload: "single" })
            }
          />
          <Radio
            name="applicationMode"
            label="Recorrente"
            description="Aplicar automaticamente na frequência definida"
            checked={state.applicationMode === "recurring"}
            onChange={() =>
              dispatch({ type: "SET_APPLICATION_MODE", payload: "recurring" })
            }
          />
        </div>

        {state.applicationMode === "recurring" && (
          <div className={styles.recurrenceFields}>
            <div className={styles.formRow}>
              <div className={styles.formField}>
                <Select
                  label="Frequência"
                  options={RECURRENCE_OPTIONS}
                  value={state.recurrence ?? ""}
                  onChange={(val: string) =>
                    dispatch({
                      type: "SET_RECURRENCE",
                      payload: (val as Recurrence) || null,
                    })
                  }
                />
              </div>
              <div className={styles.formField}>
                <Select
                  label="Dia de aplicação"
                  options={WEEKDAY_OPTIONS}
                  value={state.recurrenceDay ?? ""}
                  onChange={(val: string) =>
                    dispatch({
                      type: "SET_RECURRENCE_DAY",
                      payload: (val as WeekDay) || null,
                    })
                  }
                />
              </div>
            </div>
          </div>
        )}
      </section>

      {/* Privacidade */}
      <section className={styles.formSection}>
        <h3 className={styles.sectionTitle}>Privacidade</h3>
        <Toggle
          label="Respostas anônimas"
          description="Os respondentes não serão identificados nos resultados"
          checked={state.isAnonymous}
          onChange={() =>
            dispatch({ type: "SET_ANONYMOUS", payload: !state.isAnonymous })
          }
        />
        {state.isAnonymous && (
          <Alert variant="info" title="LGPD">
            Para pesquisas anônimas, grupos com menos de {state.lgpdMinGroupSize} pessoas
            terão resultados agregados para proteger a identidade dos respondentes.
          </Alert>
        )}
      </section>

      {/* IA */}
      <section className={styles.formSection}>
        <h3 className={styles.sectionTitle}>Inteligência Artificial</h3>
        <div className={styles.toggleStack}>
          <Toggle
            label="Análise de IA"
            description="Gerar insights e recomendações automaticamente a partir das respostas"
            checked={state.aiAnalysisEnabled}
            onChange={() =>
              dispatch({
                type: "SET_AI_ANALYSIS",
                payload: !state.aiAnalysisEnabled,
              })
            }
          />

          {isCiclo && (
            <>
              <Toggle
                label="Pré-preencher com OKRs"
                description="Sugerir respostas com base nos OKRs e resultados do período"
                checked={state.aiPrefillOkrs}
                onChange={() =>
                  dispatch({
                    type: "SET_AI_PREFILL_OKRS",
                    payload: !state.aiPrefillOkrs,
                  })
                }
              />
              <Toggle
                label="Pré-preencher com feedback"
                description="Sugerir respostas com base nos feedbacks recebidos no período"
                checked={state.aiPrefillFeedback}
                onChange={() =>
                  dispatch({
                    type: "SET_AI_PREFILL_FEEDBACK",
                    payload: !state.aiPrefillFeedback,
                  })
                }
              />
              <Toggle
                label="Detecção de viés"
                description="Identificar padrões de viés inconsciente nas avaliações"
                checked={state.aiBiasDetection}
                onChange={() =>
                  dispatch({
                    type: "SET_AI_BIAS_DETECTION",
                    payload: !state.aiBiasDetection,
                  })
                }
              />
            </>
          )}
        </div>
      </section>

      {/* Lembretes e Notificações */}
      <section className={styles.formSection}>
        <h3 className={styles.sectionTitle}>Lembretes e notificações</h3>
        <div className={styles.toggleStack}>
          <Toggle
            label="Enviar lembretes"
            description="Notificar participantes que ainda não responderam"
            checked={state.reminderEnabled}
            onChange={() =>
              dispatch({
                type: "SET_REMINDER_ENABLED",
                payload: !state.reminderEnabled,
              })
            }
          />

          {state.reminderEnabled && (
            <>
              <div className={styles.formField}>
                <Select
                  label="Frequência dos lembretes"
                  options={REMINDER_FREQ_OPTIONS}
                  value={state.reminderFrequency ?? ""}
                  onChange={(val: string) =>
                    dispatch({
                      type: "SET_REMINDER_FREQUENCY",
                      payload: (val as ReminderFrequency) || null,
                    })
                  }
                />
              </div>

              <Toggle
                label="Notificar gestores"
                description="Gestores recebem resumo de adesão do time"
                checked={state.notifyManagers}
                onChange={() =>
                  dispatch({
                    type: "SET_NOTIFY_MANAGERS",
                    payload: !state.notifyManagers,
                  })
                }
              />
            </>
          )}
        </div>
      </section>

      {/* Modelos de notificação */}
      <section
        className={styles.formSection}
        style={{ display: state.reminderEnabled ? "flex" : "none" }}
      >
        <h3 className={styles.sectionTitle}>Modelos de notificação</h3>
        <p className={styles.sectionDescription}>
          Personalize as mensagens enviadas para cada público. Use {"{nome}"} para incluir o nome {isCiclo ? "do ciclo" : "da pesquisa"}.
        </p>

        <div className={styles.notificationTemplates}>
          <div className={styles.templateCard}>
            <h4 className={styles.templateLabel}>Não respondentes</h4>
            <p className={styles.templateDescription}>
              Enviado para participantes que ainda não responderam
            </p>
            <Textarea
              label="Mensagem"
              value={state.nonRespondentNotificationTemplate ?? ""}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                dispatch({
                  type: "SET_NON_RESPONDENT_NOTIFICATION_TEMPLATE",
                  payload: e.target.value,
                })
              }
              rows={3}
            />
          </div>

          <div
            className={styles.templateCard}
            style={{ display: state.notifyManagers ? "flex" : "none" }}
          >
            <h4 className={styles.templateLabel}>Gestores</h4>
            <p className={styles.templateDescription}>
              Enviado para gestores com o resumo de adesão do time
            </p>
            <Textarea
              label="Mensagem para gestores"
              value={state.managerNotificationTemplate ?? ""}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                dispatch({
                  type: "SET_MANAGER_NOTIFICATION_TEMPLATE",
                  payload: e.target.value,
                })
              }
              rows={3}
            />
          </div>
        </div>
      </section>

      {/* Canais de entrega */}
      <section className={styles.formSection}>
        <h3 className={styles.sectionTitle}>Canais de entrega</h3>
        <div className={styles.channelsRow}>
          <Checkbox
            label="In-App"
            checked={state.deliveryInApp}
            onChange={() =>
              dispatch({
                type: "SET_DELIVERY_CHANNEL",
                payload: { channel: "inApp", enabled: !state.deliveryInApp },
              })
            }
          />
          <Checkbox
            label="E-mail"
            checked={state.deliveryEmail}
            onChange={() =>
              dispatch({
                type: "SET_DELIVERY_CHANNEL",
                payload: { channel: "email", enabled: !state.deliveryEmail },
              })
            }
          />
          <Checkbox
            label="Slack"
            checked={state.deliverySlack}
            onChange={() =>
              dispatch({
                type: "SET_DELIVERY_CHANNEL",
                payload: { channel: "slack", enabled: !state.deliverySlack },
              })
            }
          />
        </div>
      </section>

    </div>
  );
}
