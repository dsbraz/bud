import { useState, type ChangeEvent } from "react";
import {
  Card,
  CardBody,
  CardHeader,
  Button,
  Input,
  Select,
  Toggle,
  Radio,
  Checkbox,
  Textarea,
  DatePicker,
  Badge,
  Alert,
  toast,
} from "@mdonangelo/bud-ds";
import type { CalendarDate } from "@mdonangelo/bud-ds";
import {
  Eye,
  Broadcast,
  Trash,
  Pause,
  Play,
  Clock,
  EnvelopeSimple,
  Copy,
} from "@phosphor-icons/react";
import type { Icon } from "@phosphor-icons/react";
import type { SurveyResultData } from "../types";
import styles from "./SettingsTab.module.css";

/* ——— Status actions ——— */

const STATUS_ACTIONS: Record<string, { label: string; icon: Icon; variant: "primary" | "secondary" | "danger" }[]> = {
  active: [
    { label: "Pausar pesquisa", icon: Pause, variant: "secondary" },
    { label: "Encerrar pesquisa", icon: Clock, variant: "danger" },
  ],
  paused: [
    { label: "Reativar pesquisa", icon: Play, variant: "primary" },
    { label: "Encerrar pesquisa", icon: Clock, variant: "danger" },
  ],
  draft: [
    { label: "Publicar pesquisa", icon: Broadcast, variant: "primary" },
  ],
  scheduled: [
    { label: "Cancelar agendamento", icon: Trash, variant: "danger" },
  ],
  closed: [
    { label: "Reabrir pesquisa", icon: Play, variant: "secondary" },
  ],
  archived: [],
};

/* ——— Component ——— */

interface SettingsTabProps {
  data: SurveyResultData;
}

export function SettingsTab({ data }: SettingsTabProps) {
  const [startPeriod = "", endPeriod = ""] = (data.period ?? "").split(" – ");

  /* Período */
  const [startDate, setStartDate] = useState<CalendarDate | null>(
    parseSimpleDate(startPeriod)
  );
  const [endDate, setEndDate] = useState<CalendarDate | null>(
    parseSimpleDate(endPeriod)
  );

  /* Modo de aplicação */
  const [applicationMode, setApplicationMode] = useState<"single" | "recurring">("single");
  const [recurrence, setRecurrence] = useState("monthly");
  const [recurrenceDay, setRecurrenceDay] = useState("monday");

  /* Canais de entrega */
  const [deliveryInApp, setDeliveryInApp] = useState(true);
  const [deliveryEmail, setDeliveryEmail] = useState(true);
  const [deliverySlack, setDeliverySlack] = useState(false);

  /* IA */
  const [aiAnalysis, setAiAnalysis] = useState(true);
  const [aiBiasDetection, setAiBiasDetection] = useState(false);

  /* Notificações */
  const [reminderEnabled, setReminderEnabled] = useState(true);
  const [reminderFrequency, setReminderFrequency] = useState("3_days");
  const [reminderMessage, setReminderMessage] = useState(
    "Olá! Sua pesquisa ainda está pendente. Reserve alguns minutos para responder."
  );

  /* Publicação */
  const [visibility, setVisibility] = useState("managers_hr");
  const [detailLevel, setDetailLevel] = useState("aggregated");
  const [autoPublish, setAutoPublish] = useState(false);

  /* Anonimato */
  const [anonymous, setAnonymous] = useState(true);
  const [minGroupSize, setMinGroupSize] = useState("5");

  /* Link */
  const surveyLink = `https://app.bud.com.br/s/${data.surveyId}`;

  const statusActions = STATUS_ACTIONS[data.status] ?? [];

  function handleSave() {
    toast.success("Configurações salvas com sucesso");
  }

  function handleCopyLink() {
    navigator.clipboard.writeText(surveyLink);
    toast.success("Link copiado para a área de transferência");
  }

  return (
    <div className={styles.tab}>
      {/* Status e ações */}
      <Card padding="md">
        <CardHeader title="Status da pesquisa" description="Gerencie o estado atual da pesquisa" />
        <CardBody>
          <div className={styles.statusSection}>
            <div className={styles.statusInfo}>
              <span className={styles.statusLabel}>Status atual:</span>
              <Badge
                color={
                  data.status === "active" ? "success" :
                  data.status === "paused" ? "warning" :
                  data.status === "draft" ? "caramel" :
                  data.status === "scheduled" ? "wine" :
                  "neutral"
                }
                size="md"
              >
                {data.status === "active" ? "Ativa" :
                 data.status === "paused" ? "Pausada" :
                 data.status === "draft" ? "Rascunho" :
                 data.status === "scheduled" ? "Agendada" :
                 data.status === "closed" ? "Encerrada" :
                 "Arquivada"}
              </Badge>
            </div>
            {statusActions.length > 0 && (
              <div className={styles.statusActions}>
                {statusActions.map((action) => (
                  <Button
                    key={action.label}
                    variant={action.variant}
                    size="md"
                    leftIcon={action.icon}
                    onClick={() => toast.success(action.label)}
                  >
                    {action.label}
                  </Button>
                ))}
              </div>
            )}
          </div>
        </CardBody>
      </Card>

      {/* Período */}
      <Card padding="md">
        <CardHeader title="Período" description="Defina as datas de início e encerramento da pesquisa" />
        <CardBody>
          <div className={styles.formRow}>
            <DatePicker
              label="Data de início"
              mode="single"
              value={startDate}
              onChange={setStartDate}
              size="md"
            />
            <DatePicker
              label="Data de encerramento"
              mode="single"
              value={endDate}
              onChange={setEndDate}
              size="md"
            />
          </div>
          {data.status === "active" && (
            <Alert variant="info" title="Pesquisa em andamento">
              Alterar as datas de uma pesquisa ativa notificará os participantes.
            </Alert>
          )}
        </CardBody>
      </Card>

      {/* Modo de aplicação */}
      <Card padding="md">
        <CardHeader title="Modo de aplicação" description="Defina se a pesquisa é aplicada uma única vez ou de forma recorrente" />
        <CardBody>
          <div className={styles.formStack}>
            <div className={styles.radioGroup}>
              <Radio
                name="applicationMode"
                label="Coleta única"
                description="Aplicar uma única vez no período definido"
                checked={applicationMode === "single"}
                onChange={() => setApplicationMode("single")}
              />
              <Radio
                name="applicationMode"
                label="Recorrente"
                description="Aplicar automaticamente na frequência definida"
                checked={applicationMode === "recurring"}
                onChange={() => setApplicationMode("recurring")}
              />
            </div>
            {applicationMode === "recurring" && (
              <div className={styles.formRow}>
                <Select
                  label="Frequência"
                  options={[
                    { value: "weekly", label: "Semanal" },
                    { value: "biweekly", label: "Quinzenal" },
                    { value: "monthly", label: "Mensal" },
                    { value: "quarterly", label: "Trimestral" },
                  ]}
                  value={recurrence}
                  onChange={(v: string | undefined) => setRecurrence(v ?? "monthly")}
                  size="md"
                />
                <Select
                  label="Dia de aplicação"
                  options={[
                    { value: "monday", label: "Segunda-feira" },
                    { value: "tuesday", label: "Terça-feira" },
                    { value: "wednesday", label: "Quarta-feira" },
                    { value: "thursday", label: "Quinta-feira" },
                    { value: "friday", label: "Sexta-feira" },
                  ]}
                  value={recurrenceDay}
                  onChange={(v: string | undefined) => setRecurrenceDay(v ?? "monday")}
                  size="md"
                />
              </div>
            )}
          </div>
        </CardBody>
      </Card>

      {/* Canais de entrega */}
      <Card padding="md">
        <CardHeader title="Canais de entrega" description="Escolha por onde os participantes receberão a pesquisa" />
        <CardBody>
          <div className={styles.channelsRow}>
            <Checkbox
              label="In-App"
              checked={deliveryInApp}
              onChange={() => setDeliveryInApp(!deliveryInApp)}
            />
            <Checkbox
              label="E-mail"
              checked={deliveryEmail}
              onChange={() => setDeliveryEmail(!deliveryEmail)}
            />
            <Checkbox
              label="Slack"
              checked={deliverySlack}
              onChange={() => setDeliverySlack(!deliverySlack)}
            />
          </div>
        </CardBody>
      </Card>

      {/* Inteligência Artificial */}
      <Card padding="md">
        <CardHeader title="Inteligência Artificial" description="Configure os recursos de IA para análise das respostas" />
        <CardBody>
          <div className={styles.formStack}>
            <Toggle
              label="Análise de IA"
              description="Gerar insights e recomendações automaticamente a partir das respostas"
              checked={aiAnalysis}
              onChange={() => setAiAnalysis(!aiAnalysis)}
            />
            <Toggle
              label="Detecção de viés"
              description="Identificar padrões de viés inconsciente nas respostas"
              checked={aiBiasDetection}
              onChange={() => setAiBiasDetection(!aiBiasDetection)}
            />
          </div>
        </CardBody>
      </Card>

      {/* Lembretes e notificações */}
      <Card padding="md">
        <CardHeader title="Lembretes" description="Configure os lembretes automáticos para respondentes pendentes" />
        <CardBody>
          <div className={styles.formStack}>
            <Toggle
              label="Enviar lembretes automáticos"
              description="Notificar respondentes que ainda não completaram a pesquisa"
              checked={reminderEnabled}
              onChange={() => setReminderEnabled(!reminderEnabled)}
            />
            {reminderEnabled && (
              <>
                <Select
                  label="Frequência dos lembretes"
                  options={[
                    { value: "daily", label: "Diariamente" },
                    { value: "2_days", label: "A cada 2 dias" },
                    { value: "3_days", label: "A cada 3 dias" },
                    { value: "weekly", label: "Semanalmente" },
                  ]}
                  value={reminderFrequency}
                  onChange={(v: string | undefined) => setReminderFrequency(v ?? "3_days")}
                  size="md"
                />
                <Textarea
                  label="Mensagem do lembrete"
                  value={reminderMessage}
                  onChange={(e: ChangeEvent<HTMLTextAreaElement>) => setReminderMessage(e.target.value)}
                  rows={3}
                />
                <Button variant="secondary" size="sm" leftIcon={EnvelopeSimple}>
                  Enviar lembrete agora
                </Button>
              </>
            )}
          </div>
        </CardBody>
      </Card>

      {/* Anonimato e privacidade */}
      <Card padding="md">
        <CardHeader title="Anonimato e privacidade" description="Configurações de proteção de dados dos respondentes" />
        <CardBody>
          <div className={styles.formStack}>
            <Toggle
              label="Respostas anônimas"
              description="Os respondentes não serão identificados nos resultados"
              checked={anonymous}
              onChange={() => setAnonymous(!anonymous)}
            />
            <Select
              label="Tamanho mínimo do grupo para exibição"
              options={[
                { value: "3", label: "3 respondentes" },
                { value: "5", label: "5 respondentes (recomendado LGPD)" },
                { value: "10", label: "10 respondentes" },
              ]}
              value={minGroupSize}
              onChange={(v: string | undefined) => setMinGroupSize(v ?? "5")}
              size="md"
            />
            {anonymous && (
              <Alert variant="success" title="Anonimato ativo">
                Resultados com menos de {minGroupSize} respondentes serão agregados automaticamente.
              </Alert>
            )}
          </div>
        </CardBody>
      </Card>

      {/* Publicação dos resultados */}
      <Card padding="md">
        <CardHeader title="Publicação dos resultados" description="Controle quem pode ver os resultados e em que nível de detalhe" />
        <CardBody>
          <div className={styles.formStack}>
            <Select
              label="Visibilidade dos resultados"
              options={[
                { value: "hr_only", label: "Apenas RH" },
                { value: "managers_hr", label: "Gestores + RH" },
                { value: "all", label: "Todos os participantes" },
              ]}
              value={visibility}
              onChange={(v: string | undefined) => setVisibility(v ?? "managers_hr")}
              size="md"
            />
            <Select
              label="Nível de detalhe"
              options={[
                { value: "aggregated", label: "Apenas agregados" },
                { value: "by_department", label: "Por departamento" },
                { value: "by_team", label: "Por time" },
              ]}
              value={detailLevel}
              onChange={(v: string | undefined) => setDetailLevel(v ?? "aggregated")}
              size="md"
            />
            <Toggle
              label="Publicar automaticamente ao encerrar"
              description="Os resultados serão publicados assim que a pesquisa for encerrada"
              checked={autoPublish}
              onChange={() => setAutoPublish(!autoPublish)}
            />
            <div className={styles.publishActions}>
              <Button variant="secondary" size="md" leftIcon={Eye}>Pré-visualizar</Button>
              <Button variant="primary" size="md" leftIcon={Broadcast}>Publicar resultados</Button>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Link de compartilhamento */}
      <Card padding="md">
        <CardHeader title="Link da pesquisa" description="Compartilhe o link direto com os participantes" />
        <CardBody>
          <div className={styles.linkRow}>
            <Input
              size="md"
              value={surveyLink}
              readOnly
            />
            <Button variant="secondary" size="md" leftIcon={Copy} onClick={handleCopyLink}>
              Copiar
            </Button>
          </div>
        </CardBody>
      </Card>

      {/* Zona de perigo */}
      <Card padding="md">
        <CardHeader title="Zona de perigo" description="Ações irreversíveis" />
        <CardBody>
          <div className={styles.dangerSection}>
            <div className={styles.dangerItem}>
              <div className={styles.dangerText}>
                <span className={styles.dangerTitle}>Arquivar pesquisa</span>
                <span className={styles.dangerDesc}>A pesquisa será movida para o arquivo. Os resultados serão preservados.</span>
              </div>
              <Button variant="secondary" size="md" onClick={() => toast.warning("Pesquisa arquivada")}>
                Arquivar
              </Button>
            </div>
            <div className={styles.dangerItem}>
              <div className={styles.dangerText}>
                <span className={styles.dangerTitle}>Excluir pesquisa</span>
                <span className={styles.dangerDesc}>Esta ação é irreversível. Todos os dados e resultados serão perdidos.</span>
              </div>
              <Button variant="danger" size="md" leftIcon={Trash} onClick={() => toast.error("Ação não disponível no modo demonstração")}>
                Excluir
              </Button>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Save bar */}
      <div className={styles.saveBar}>
        <Button variant="primary" size="md" onClick={handleSave}>Salvar configurações</Button>
      </div>
    </div>
  );
}

/* ——— Helpers ——— */

function parseSimpleDate(str: string): CalendarDate | null {
  if (!str) return null;
  const trimmed = str.trim();
  const [dayPart = "", monthPart = "", yearPart = ""] = trimmed.split("/");
  if (!dayPart || !monthPart || !yearPart) return null;
  const day = parseInt(dayPart, 10);
  const month = parseInt(monthPart, 10);
  const year = parseInt(yearPart, 10);
  if (isNaN(day) || isNaN(month) || isNaN(year)) return null;
  return { year, month, day };
}
