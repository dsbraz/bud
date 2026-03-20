"use client";

import { useState, useMemo, useCallback, type ReactNode } from "react";
import {
  Modal,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
  Input,
  Select,
  Toggle,
  Badge,
  Breadcrumb,
  Card,
  CardHeader,
  CardBody,
  CardFooter,
  Chart,
  GoalProgressBar,
  Heatmap,
  FilterChip,
  ChoiceBox,
  ChoiceBoxGroup,
} from "@mdonangelo/bud-ds";
import type { BreadcrumbItem, HeatmapCell } from "@mdonangelo/bud-ds";
import { LineChart, Line, ResponsiveContainer } from "recharts";
import {
  MagnifyingGlass,
  SquaresFour,
  ArrowUp,
  ArrowDown,
  Sparkle,
} from "@phosphor-icons/react";
import { usePeopleData } from "@/contexts/PeopleDataContext";

/* ——— Types ——— */

type SubjectType = "self" | "team" | "people" | "company";
type MetricCategory = "okr" | "engagement" | "performance";
type VizType =
  | "metric"
  | "bar"
  | "donut"
  | "trend"
  | "progress"
  | "ranking"
  | "heatmap"
  | "table";
type Period = "week" | "month" | "quarter" | "semester" | "year";
type Comparison = "none" | "previous" | "target" | "average";

interface MetricOption {
  id: string;
  name: string;
  description: string;
  category: MetricCategory;
  color: string;
  suggestedViz: VizType;
  mockValue: string;
  mockDelta: number;
}

interface SubjectOption {
  value: SubjectType;
  title: string;
  description: string;
}

interface PopularWidget {
  label: string;
  subject: SubjectType;
  metricId: string;
  viz: VizType;
}

/* ——— Mock data ——— */

const METRICS: MetricOption[] = [
  {
    id: "okr-progress",
    name: "Progresso médio dos OKRs",
    description: "Média de conclusão de todos os OKRs ativos",
    category: "okr",
    color: "var(--color-orange-500)",
    suggestedViz: "progress",
    mockValue: "52%",
    mockDelta: 4.2,
  },
  {
    id: "okr-risk",
    name: "Missões em risco",
    description: "OKRs com progresso abaixo do esperado",
    category: "okr",
    color: "var(--color-red-500)",
    suggestedViz: "metric",
    mockValue: "3",
    mockDelta: -1,
  },
  {
    id: "okr-score",
    name: "Score médio do ciclo",
    description: "Nota média ponderada do ciclo atual",
    category: "okr",
    color: "var(--color-orange-400)",
    suggestedViz: "trend",
    mockValue: "7.4",
    mockDelta: 0.3,
  },
  {
    id: "okr-done",
    name: "OKRs concluídos",
    description: "Total de OKRs finalizados no período",
    category: "okr",
    color: "var(--color-green-500)",
    suggestedViz: "bar",
    mockValue: "12",
    mockDelta: 2,
  },
  {
    id: "eng-checkin",
    name: "Taxa de check-in",
    description: "Percentual de check-ins realizados na semana",
    category: "engagement",
    color: "var(--color-green-500)",
    suggestedViz: "trend",
    mockValue: "86%",
    mockDelta: 3.1,
  },
  {
    id: "eng-weekly",
    name: "Engajamento semanal",
    description: "Índice de interações na plataforma",
    category: "engagement",
    color: "var(--color-orange-500)",
    suggestedViz: "trend",
    mockValue: "70%",
    mockDelta: 5.2,
  },
  {
    id: "eng-stale",
    name: "Sem atualização",
    description: "Pessoas sem atividade há mais de 7 dias",
    category: "engagement",
    color: "var(--color-yellow-500)",
    suggestedViz: "ranking",
    mockValue: "5",
    mockDelta: -2,
  },
  {
    id: "eng-nps",
    name: "NPS interno",
    description: "Net Promoter Score da equipe",
    category: "engagement",
    color: "var(--color-wine-500)",
    suggestedViz: "donut",
    mockValue: "72",
    mockDelta: 8,
  },
  {
    id: "perf-score",
    name: "Score de performance",
    description: "Nota de performance consolidada",
    category: "performance",
    color: "var(--color-orange-500)",
    suggestedViz: "metric",
    mockValue: "8.1",
    mockDelta: 0.4,
  },
  {
    id: "perf-pdi",
    name: "PDIs ativos",
    description: "Planos de desenvolvimento em andamento",
    category: "performance",
    color: "var(--color-green-400)",
    suggestedViz: "bar",
    mockValue: "7",
    mockDelta: 1,
  },
];

const POPULAR_WIDGETS: PopularWidget[] = [
  {
    label: "Progresso OKRs do time",
    subject: "team",
    metricId: "okr-progress",
    viz: "progress",
  },
  {
    label: "Meu engajamento semanal",
    subject: "self",
    metricId: "eng-weekly",
    viz: "trend",
  },
  {
    label: "Missões em risco",
    subject: "team",
    metricId: "okr-risk",
    viz: "metric",
  },
];

const TREND_DATA = [
  { w: "S1", v: 42 },
  { w: "S2", v: 55 },
  { w: "S3", v: 48 },
  { w: "S4", v: 62 },
  { w: "S5", v: 58 },
  { w: "S6", v: 70 },
];

const HEATMAP_DATA: HeatmapCell[] = [
  { row: "Seg", col: "S1", value: 8 },
  { row: "Seg", col: "S2", value: 5 },
  { row: "Seg", col: "S3", value: 9 },
  { row: "Ter", col: "S1", value: 3 },
  { row: "Ter", col: "S2", value: 7 },
  { row: "Ter", col: "S3", value: 6 },
  { row: "Qua", col: "S1", value: 6 },
  { row: "Qua", col: "S2", value: 9 },
  { row: "Qua", col: "S3", value: 4 },
];

const VIZ_OPTIONS: { type: VizType; label: string }[] = [
  { type: "metric", label: "Métrica" },
  { type: "bar", label: "Barras" },
  { type: "donut", label: "Rosca" },
  { type: "trend", label: "Tendência" },
  { type: "progress", label: "Progresso" },
  { type: "ranking", label: "Ranking" },
  { type: "heatmap", label: "Mapa calor" },
  { type: "table", label: "Tabela" },
];

function buildSubjectOptions(
  teamName: string | null,
  teamMemberCount: number,
): SubjectOption[] {
  const teamTitle = teamName
    ? `Meu time: ${teamName} (${teamMemberCount} ${teamMemberCount === 1 ? "pessoa" : "pessoas"})`
    : "Meu time";

  return [
    {
      value: "self" as SubjectType,
      title: "Eu mesmo",
      description: "Minhas missões, score, check-ins",
    },
    {
      value: "team" as SubjectType,
      title: teamTitle,
      description: "Visão consolidada do time",
    },
    {
      value: "people" as SubjectType,
      title: "Pessoas específicas",
      description: "Escolher colaboradores",
    },
    {
      value: "company" as SubjectType,
      title: "Empresa toda",
      description: "Indicadores globais",
    },
  ];
}

const CATEGORY_LABELS: Record<MetricCategory, string> = {
  okr: "OKRs",
  engagement: "Engajamento",
  performance: "Performance",
};

const CATEGORY_BADGE_COLORS: Record<
  MetricCategory,
  "orange" | "caramel" | "wine"
> = {
  okr: "orange",
  engagement: "caramel",
  performance: "wine",
};

const PERIOD_OPTIONS = [
  { value: "week", label: "Esta semana" },
  { value: "month", label: "Este mês" },
  { value: "quarter", label: "Este trimestre" },
  { value: "semester", label: "Este semestre" },
  { value: "year", label: "Este ano" },
];

const COMPARISON_OPTIONS = [
  { value: "none", label: "Sem comparação" },
  { value: "previous", label: "Período anterior" },
  { value: "target", label: "Meta definida" },
  { value: "average", label: "Média da empresa" },
];

/* ——— Step labels ——— */

const BREADCRUMB_LABELS = [
  "Sobre quem",
  "Que dado",
  "Visualização",
  "Configurar",
];

/* ——— Component ——— */

interface WidgetBuilderProps {
  open: boolean;
  onClose: () => void;
  onAdd?: (config: WidgetConfig) => void;
}

interface WidgetConfig {
  subject: SubjectType;
  selectedPeople: string[];
  metricId: string;
  viz: VizType;
  period: Period;
  comparison: Comparison;
  showVariation: boolean;
  showTarget: boolean;
  alertBelow: boolean;
  name: string;
}

export function WidgetBuilder({ open, onClose, onAdd }: WidgetBuilderProps) {
  const { users, teams, currentUser } = usePeopleData();
  const [step, setStep] = useState(1);

  const breadcrumbItems: BreadcrumbItem[] = BREADCRUMB_LABELS.map(
    (label, i) => ({
      label,
      onClick: i < step - 1 ? () => setStep(i + 1) : undefined,
    }),
  );

  // Get current user's team info for dynamic subject options
  const userTeam = useMemo(() => {
    if (!currentUser) return null;
    // Find team where current user is a member
    return (
      teams.find((t) => t.members?.some((m) => m.userId === currentUser.id)) ??
      null
    );
  }, [teams, currentUser]);

  const teamMemberCount = userTeam?.members?.length ?? 0;

  const subjectOptions = useMemo(
    () => buildSubjectOptions(userTeam?.name ?? null, teamMemberCount),
    [userTeam?.name, teamMemberCount],
  );

  // Map users to person format for people selection
  const peopleOptions = useMemo(
    () =>
      users
        .filter((u) => u.status === "active")
        .map((u) => ({
          id: u.id,
          name: `${u.firstName} ${u.lastName}`.trim(),
          role: u.jobTitle ?? "",
        })),
    [users],
  );

  // Step 1
  const [subject, setSubject] = useState<SubjectType | undefined>();
  const [selectedPeople, setSelectedPeople] = useState<string[]>([]);
  const [peopleSearch, setPeopleSearch] = useState("");

  // Step 2
  const [categoryFilter, setCategoryFilter] = useState<"all" | MetricCategory>(
    "all",
  );
  const [metricSearch, setMetricSearch] = useState("");
  const [selectedMetric, setSelectedMetric] = useState<string | undefined>();

  // Step 3
  const [selectedViz, setSelectedViz] = useState<VizType | undefined>();

  // Step 4
  const [period, setPeriod] = useState<Period>("month");
  const [comparison, setComparison] = useState<Comparison>("none");
  const [showVariation, setShowVariation] = useState(true);
  const [showTarget, setShowTarget] = useState(false);
  const [alertBelow, setAlertBelow] = useState(false);
  const [widgetName, setWidgetName] = useState("");

  const metric = useMemo(
    () => METRICS.find((m) => m.id === selectedMetric),
    [selectedMetric],
  );

  // Auto-suggest viz when metric changes
  const handleMetricSelect = useCallback((id: string) => {
    setSelectedMetric(id);
    const m = METRICS.find((x) => x.id === id);
    if (m) setSelectedViz(m.suggestedViz);
  }, []);

  // Auto-suggest widget name
  const suggestedName = useMemo(() => {
    if (!metric) return "";
    const subjectLabel =
      subject === "self"
        ? "Meu"
        : subject === "team"
          ? "Time"
          : subject === "company"
            ? "Empresa"
            : "Pessoas";
    return `${subjectLabel} — ${metric.name}`;
  }, [metric, subject]);

  // Filtered metrics
  const filteredMetrics = useMemo(() => {
    let list = METRICS;
    if (categoryFilter !== "all") {
      list = list.filter((m) => m.category === categoryFilter);
    }
    if (metricSearch) {
      const q = metricSearch.toLowerCase();
      list = list.filter(
        (m) =>
          m.name.toLowerCase().includes(q) ||
          m.description.toLowerCase().includes(q),
      );
    }
    return list;
  }, [categoryFilter, metricSearch]);

  // Filtered people
  const filteredPeople = useMemo(() => {
    if (!peopleSearch) return peopleOptions;
    const q = peopleSearch.toLowerCase();
    return peopleOptions.filter((p) => p.name.toLowerCase().includes(q));
  }, [peopleSearch, peopleOptions]);

  // Step validation
  const canProceed = useMemo(() => {
    switch (step) {
      case 1:
        if (!subject) return false;
        if (subject === "people" && selectedPeople.length === 0) return false;
        return true;
      case 2:
        return !!selectedMetric;
      case 3:
        return !!selectedViz;
      case 4:
        return true;
      default:
        return false;
    }
  }, [step, subject, selectedPeople, selectedMetric, selectedViz]);

  const handleNext = useCallback(() => {
    if (step < 4) setStep((s) => s + 1);
  }, [step]);

  const handleBack = useCallback(() => {
    if (step > 1) setStep((s) => s - 1);
  }, [step]);

  const handleAdd = useCallback(() => {
    onAdd?.({
      subject: subject!,
      selectedPeople,
      metricId: selectedMetric!,
      viz: selectedViz!,
      period,
      comparison,
      showVariation,
      showTarget,
      alertBelow,
      name: widgetName || suggestedName,
    });
    handleClose();
  }, [
    subject,
    selectedPeople,
    selectedMetric,
    selectedViz,
    period,
    comparison,
    showVariation,
    showTarget,
    alertBelow,
    widgetName,
    suggestedName,
    onAdd,
  ]);

  const handleClose = useCallback(() => {
    onClose();
    // Reset state after animation
    setTimeout(() => {
      setStep(1);
      setSubject(undefined);
      setSelectedPeople([]);
      setPeopleSearch("");
      setCategoryFilter("all");
      setMetricSearch("");
      setSelectedMetric(undefined);
      setSelectedViz(undefined);
      setPeriod("month");
      setComparison("none");
      setShowVariation(true);
      setShowTarget(false);
      setAlertBelow(false);
      setWidgetName("");
    }, 250);
  }, [onClose]);

  const handlePopularClick = useCallback((pw: PopularWidget) => {
    setSubject(pw.subject);
    setSelectedMetric(pw.metricId);
    setSelectedViz(pw.viz);
    const m = METRICS.find((x) => x.id === pw.metricId);
    if (m) {
      const subjectLabel =
        pw.subject === "self"
          ? "Meu"
          : pw.subject === "team"
            ? "Time"
            : "Empresa";
      setWidgetName(`${subjectLabel} — ${m.name}`);
    }
    setStep(4);
  }, []);

  /* ——— Render steps ——— */

  const renderStep1 = () => (
    <div className="flex flex-col gap-[var(--sp-md)] [animation:fadeSlideIn_200ms_cubic-bezier(0.16,1,0.3,1)]">
      <div className="flex flex-col gap-[var(--sp-2xs)]">
        <h4 className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)] m-0">
          Populares para você
        </h4>
        <div className="flex flex-wrap gap-[var(--sp-2xs)]">
          {POPULAR_WIDGETS.map((pw) => (
            <Button
              key={pw.label}
              variant="secondary"
              size="sm"
              onClick={() => handlePopularClick(pw)}
            >
              {pw.label}
            </Button>
          ))}
        </div>
      </div>

      <p className="[font-family:var(--font-body)] text-[var(--text-sm)] text-[var(--color-neutral-500)] m-0">
        Ou selecione sobre quem este widget vai exibir dados.
      </p>

      <ChoiceBoxGroup
        value={subject}
        onChange={(val) => setSubject(val as SubjectType | undefined)}
      >
        {subjectOptions.map((opt) => (
          <ChoiceBox
            key={opt.value}
            value={opt.value}
            title={opt.title}
            description={opt.description}
          />
        ))}
      </ChoiceBoxGroup>

      {subject === "people" && (
        <div className="flex flex-col gap-[var(--sp-xs)] [animation:fadeSlideIn_200ms_cubic-bezier(0.16,1,0.3,1)]">
          <div className="flex items-center gap-[var(--sp-xs)]">
            <Input
              leftIcon={MagnifyingGlass}
              placeholder="Buscar colaborador..."
              value={peopleSearch}
              onChange={(e) => setPeopleSearch(e.target.value)}
            />
            {selectedPeople.length > 0 && (
              <Badge color="orange" size="sm">
                {selectedPeople.length}{" "}
                {selectedPeople.length === 1 ? "selecionado" : "selecionados"}
              </Badge>
            )}
          </div>
          <div className="flex flex-col gap-[var(--sp-3xs)] max-h-[200px] overflow-y-auto">
            <ChoiceBoxGroup
              multiple
              value={selectedPeople}
              onChange={(val) => setSelectedPeople(val as string[])}
            >
              {filteredPeople.map((person) => (
                <ChoiceBox
                  key={person.id}
                  value={person.id}
                  title={person.name}
                  description={person.role}
                />
              ))}
            </ChoiceBoxGroup>
          </div>
        </div>
      )}
    </div>
  );

  const renderStep2 = () => (
    <div className="flex flex-col gap-[var(--sp-md)] [animation:fadeSlideIn_200ms_cubic-bezier(0.16,1,0.3,1)]">
      <p className="[font-family:var(--font-body)] text-[var(--text-sm)] text-[var(--color-neutral-500)] m-0">
        Escolha a métrica que deseja acompanhar.
      </p>

      <div className="flex gap-[var(--sp-2xs)] flex-wrap">
        {(["all", "okr", "engagement", "performance"] as const).map((cat) => (
          <FilterChip
            key={cat}
            label={cat === "all" ? "Todos" : CATEGORY_LABELS[cat]}
            active={categoryFilter === cat}
            onClick={() => setCategoryFilter(cat)}
          />
        ))}
      </div>

      <Input
        leftIcon={MagnifyingGlass}
        placeholder="Buscar métrica..."
        value={metricSearch}
        onChange={(e) => setMetricSearch(e.target.value)}
      />

      <div className="flex flex-col gap-[var(--sp-3xs)] max-h-[260px] overflow-y-auto">
        <ChoiceBoxGroup
          value={selectedMetric}
          onChange={(val) => handleMetricSelect(val as string)}
        >
          {filteredMetrics.map((m) => (
            <ChoiceBox
              key={m.id}
              value={m.id}
              title={m.name}
              description={m.description}
            />
          ))}
        </ChoiceBoxGroup>
        {filteredMetrics.length === 0 && (
          <p className="[font-family:var(--font-body)] text-[var(--text-sm)] text-[var(--color-neutral-400)] text-center p-[var(--sp-lg)] m-0">
            Nenhuma métrica encontrada.
          </p>
        )}
      </div>
    </div>
  );

  const renderStep3 = () => (
    <div className="flex flex-col gap-[var(--sp-md)] [animation:fadeSlideIn_200ms_cubic-bezier(0.16,1,0.3,1)]">
      <p className="[font-family:var(--font-body)] text-[var(--text-sm)] text-[var(--color-neutral-500)] m-0">
        Como deseja visualizar este dado?
      </p>

      {metric?.suggestedViz && (
        <div className="flex items-center gap-[var(--sp-2xs)] px-[var(--sp-xs)] py-[var(--sp-2xs)] rounded-[var(--radius-sm)] bg-[var(--color-orange-50)] text-[var(--color-orange-700)] [font-family:var(--font-body)] text-[var(--text-xs)]">
          <Sparkle size={16} />
          <span>
            Sugerido para <strong>{metric.name}</strong>:{" "}
            {VIZ_OPTIONS.find((v) => v.type === metric.suggestedViz)?.label}
          </span>
        </div>
      )}

      <ChoiceBoxGroup
        value={selectedViz}
        onChange={(val) => setSelectedViz(val as VizType)}
      >
        {VIZ_OPTIONS.map((viz) => (
          <ChoiceBox
            key={viz.type}
            value={viz.type}
            title={viz.label}
            description={
              metric?.suggestedViz === viz.type ? "Recomendado" : undefined
            }
          />
        ))}
      </ChoiceBoxGroup>
    </div>
  );

  const renderStep4 = () => (
    <div className="flex flex-col gap-[var(--sp-md)] [animation:fadeSlideIn_200ms_cubic-bezier(0.16,1,0.3,1)]">
      <p className="[font-family:var(--font-body)] text-[var(--text-sm)] text-[var(--color-neutral-500)] m-0">
        Ajuste as configurações finais do widget.
      </p>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-[var(--sp-sm)]">
        <Select
          label="Período"
          options={PERIOD_OPTIONS}
          value={period}
          onChange={(val) => setPeriod(val as Period)}
        />
        <Select
          label="Comparar com"
          options={COMPARISON_OPTIONS}
          value={comparison}
          onChange={(val) => setComparison(val as Comparison)}
        />
      </div>

      <div className="flex flex-col gap-[var(--sp-xs)] py-[var(--sp-sm)] border-t border-b border-[var(--color-caramel-100)]">
        <Toggle
          label="Mostrar variação"
          description="Exibir delta em relação ao período anterior"
          checked={showVariation}
          onChange={(e) => setShowVariation(e.target.checked)}
        />
        <Toggle
          label="Exibir meta"
          description="Mostrar linha ou barra de meta no gráfico"
          checked={showTarget}
          onChange={(e) => setShowTarget(e.target.checked)}
        />
        <Toggle
          label="Alerta quando abaixo do limite"
          description="Notificar quando o valor cair abaixo da meta"
          checked={alertBelow}
          onChange={(e) => setAlertBelow(e.target.checked)}
        />
      </div>

      <Input
        label="Nome do widget"
        placeholder={suggestedName || "Nome do widget"}
        value={widgetName}
        onChange={(e) => setWidgetName(e.target.value)}
      />
    </div>
  );

  /* ——— Side panel ——— */

  const renderPreview = (): ReactNode => {
    const hasData = !!metric && !!selectedViz;

    return (
      <div className="flex flex-col gap-[var(--sp-md)] p-[var(--sp-lg)] max-[480px]:p-[var(--sp-sm)] h-full bg-[var(--color-white)] border border-[var(--color-caramel-200)] rounded-[var(--radius-sm)] shadow-[var(--shadow-md)]">
        <h3 className="[font-family:var(--font-heading)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-700)] m-0">
          Pré-visualização
        </h3>

        {hasData ? (
          <Card>
            <CardHeader title={widgetName || suggestedName || metric.name}>
              <Badge color={CATEGORY_BADGE_COLORS[metric.category]} size="sm">
                {CATEGORY_LABELS[metric.category]}
              </Badge>
            </CardHeader>
            <CardBody>
              <div className="flex items-baseline gap-[var(--sp-2xs)]">
                <span className="[font-family:var(--font-heading)] text-[var(--text-2xl)] font-semibold text-[var(--color-neutral-900)] leading-none">
                  {metric.mockValue}
                </span>
                {showVariation && (
                  <span
                    className={`inline-flex items-center gap-[2px] [font-family:var(--font-label)] text-[var(--text-xs)] rounded-[var(--radius-2xs)] px-[var(--sp-3xs)] py-[2px] ${
                      metric.mockDelta >= 0
                        ? "text-[var(--color-green-700)] bg-[var(--color-green-50)]"
                        : "text-[var(--color-red-700)] bg-[var(--color-red-50)]"
                    }`}
                  >
                    {metric.mockDelta >= 0 ? (
                      <ArrowUp size={14} />
                    ) : (
                      <ArrowDown size={14} />
                    )}
                    {Math.abs(metric.mockDelta)}
                    {metric.mockValue.includes("%") ? "pp" : ""}
                  </span>
                )}
              </div>
              <div className="flex items-center justify-center min-h-[48px]">
                {renderMiniViz(selectedViz, metric)}
              </div>
            </CardBody>
            <CardFooter>
              <span className="[font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-400)]">
                {PERIOD_OPTIONS.find((p) => p.value === period)?.label}
              </span>
              {comparison !== "none" && (
                <span className="[font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-400)]">
                  vs{" "}
                  {COMPARISON_OPTIONS.find(
                    (c) => c.value === comparison,
                  )?.label.toLowerCase()}
                </span>
              )}
            </CardFooter>
          </Card>
        ) : (
          <div className="flex flex-col items-center justify-center gap-[var(--sp-xs)] px-[var(--sp-md)] py-[var(--sp-3xl)] text-[var(--color-neutral-400)] text-center">
            <SquaresFour size={32} />
            <p className="[font-family:var(--font-body)] text-[var(--text-sm)] m-0 max-w-[200px]">
              Configure os dados ao lado para ver a pré-visualização
            </p>
          </div>
        )}
      </div>
    );
  };

  return (
    <Modal
      open={open}
      onClose={handleClose}
      size="lg"
      sidePanel={renderPreview()}
    >
      <ModalHeader
        title="Adicionar widget"
        description="Configure um novo widget para sua home"
        onClose={handleClose}
      />

      <div className="w-full">
        <Breadcrumb items={breadcrumbItems} current={step - 1} />
      </div>

      <ModalBody>
        <div className="md:min-h-[320px]">
          {step === 1 && renderStep1()}
          {step === 2 && renderStep2()}
          {step === 3 && renderStep3()}
          {step === 4 && renderStep4()}
        </div>
      </ModalBody>

      <ModalFooter align="between">
        <Button variant="secondary" onClick={handleBack} disabled={step === 1}>
          Voltar
        </Button>
        {step < 4 ? (
          <Button onClick={handleNext} disabled={!canProceed}>
            Próximo
          </Button>
        ) : (
          <Button onClick={handleAdd}>Adicionar à home</Button>
        )}
      </ModalFooter>
    </Modal>
  );
}

/* ——— Mini visualization SVG previews ——— */

function renderMiniViz(type: VizType, metric: MetricOption): ReactNode {
  const numericValue = parseFloat(metric.mockValue) || 0;
  const chartValue = metric.mockValue.includes("%")
    ? numericValue
    : Math.min(numericValue * 10, 100);

  switch (type) {
    case "metric":
      return null;
    case "bar": {
      const barValues = [42, 62, 83, 54, 75, 88];
      return (
        <div className="flex items-end gap-[var(--sp-3xs)] w-full h-[48px]">
          {barValues.map((v, i) => (
            <div key={i} className="flex-1 h-full flex items-end">
              <div
                className="w-full rounded-t-[var(--radius-2xs)] bg-[var(--color-chart-1)] transition-[height] duration-[200ms] [transition-timing-function:cubic-bezier(0.16,1,0.3,1)]"
                style={{ height: `${v}%` }}
              />
            </div>
          ))}
        </div>
      );
    }
    case "donut":
      return <Chart value={chartValue} size={80} />;
    case "trend":
      return (
        <ResponsiveContainer width="100%" height={60}>
          <LineChart data={TREND_DATA}>
            <Line
              type="monotone"
              dataKey="v"
              stroke="var(--color-chart-1)"
              strokeWidth={2}
              dot={false}
            />
          </LineChart>
        </ResponsiveContainer>
      );
    case "progress":
      return (
        <div className="flex flex-col gap-[var(--sp-3xs)] w-full">
          <GoalProgressBar label="KR 1" value={52} expected={60} target={100} />
          <GoalProgressBar label="KR 2" value={78} expected={70} target={100} />
          <GoalProgressBar label="KR 3" value={35} expected={50} target={100} />
        </div>
      );
    case "ranking":
      return (
        <div className="flex flex-col gap-[var(--sp-3xs)] w-full">
          <GoalProgressBar
            label="Ana C."
            value={92}
            expected={80}
            target={100}
          />
          <GoalProgressBar
            label="Lucas M."
            value={78}
            expected={80}
            target={100}
          />
          <GoalProgressBar
            label="Rafael C."
            value={65}
            expected={80}
            target={100}
          />
        </div>
      );
    case "heatmap":
      return (
        <Heatmap
          data={HEATMAP_DATA}
          rows={["Seg", "Ter", "Qua"]}
          columns={["S1", "S2", "S3"]}
          cellSize={28}
          gap={2}
          showValues={false}
        />
      );
    case "table":
      return (
        <div className="flex flex-col gap-[2px] w-full">
          <div className="flex gap-[var(--sp-2xs)]">
            <span className="flex-1 px-[var(--sp-3xs)] py-[2px] bg-[var(--color-caramel-100)] rounded-[var(--radius-2xs)] [font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-400)] text-center">
              —
            </span>
            <span className="flex-1 px-[var(--sp-3xs)] py-[2px] bg-[var(--color-caramel-100)] rounded-[var(--radius-2xs)] [font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-400)] text-center">
              —
            </span>
            <span className="flex-1 px-[var(--sp-3xs)] py-[2px] bg-[var(--color-caramel-100)] rounded-[var(--radius-2xs)] [font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-400)] text-center">
              —
            </span>
          </div>
          <div className="flex gap-[var(--sp-2xs)]">
            <span className="flex-1 px-[var(--sp-3xs)] py-[2px] bg-[var(--color-caramel-100)] rounded-[var(--radius-2xs)] [font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-400)] text-center">
              —
            </span>
            <span className="flex-1 px-[var(--sp-3xs)] py-[2px] bg-[var(--color-caramel-100)] rounded-[var(--radius-2xs)] [font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-400)] text-center">
              —
            </span>
            <span className="flex-1 px-[var(--sp-3xs)] py-[2px] bg-[var(--color-caramel-100)] rounded-[var(--radius-2xs)] [font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-400)] text-center">
              —
            </span>
          </div>
        </div>
      );
    default:
      return null;
  }
}
