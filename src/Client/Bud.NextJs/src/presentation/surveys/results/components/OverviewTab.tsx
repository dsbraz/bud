import { useState, useRef, useMemo } from "react";
import {
  Card,
  CardBody,
  CardHeader,
  Badge,
  Button,
  Table,
  TableContent,
  TableHead,
  TableBody,
  TableRow,
  TableHeaderCell,
  TableCell,
  Heatmap,
  Sparkline,
  FilterBar,
  FilterChip,
} from "@mdonangelo/bud-ds";
import { PopoverSelect } from "@/components/PopoverSelect";
import type { HeatmapCell, FilterOption } from "@mdonangelo/bud-ds";
import {
  Warning,
  TrendUp,
  TrendDown,
  Minus,
  Plus,
  Buildings,
} from "@phosphor-icons/react";
import type { SurveyResultData } from "../types";
import styles from "./OverviewTab.module.css";

/* ——— Priority config ——— */

const PRIORITY_CONFIG: Record<string, { label: string; color: "error" | "warning" | "neutral" }> = {
  alta: { label: "Alta", color: "error" },
  média: { label: "Média", color: "warning" },
  baixa: { label: "Baixa", color: "neutral" },
};

const ACTION_STATUS_CONFIG: Record<string, { label: string; color: "caramel" | "warning" | "success" }> = {
  pendente: { label: "Pendente", color: "caramel" },
  em_andamento: { label: "Em andamento", color: "warning" },
  concluída: { label: "Concluída", color: "success" },
};

/* ——— Filter options ——— */

const FILTER_OPTIONS: FilterOption[] = [
  { id: "department", label: "Departamento", icon: Buildings },
];

/* ——— Component ——— */

interface OverviewTabProps {
  data: SurveyResultData;
}

export function OverviewTab({ data }: OverviewTabProps) {
  const review = data.hrReview;

  /* Filters */
  const [activeFilters, setActiveFilters] = useState<string[]>([]);
  const [openFilter, setOpenFilter] = useState<string | null>(null);
  const [selectedDepartments, setSelectedDepartments] = useState<Set<string>>(new Set());
  const deptChipRef = useRef<HTMLDivElement>(null);
  const ignoreChipRefs = useMemo(() => [deptChipRef], []);

  /* Department options from data */
  const departmentOptions = useMemo(() => {
    if (!review) return [];
    return review.heatmap.departments.map((d) => ({ id: d, label: d }));
  }, [review]);

  const hasDeptFilter = selectedDepartments.size > 0;

  /* Filtered data */
  const filteredHeatmapData = useMemo<HeatmapCell[]>(() => {
    if (!review) return [];
    const entries = !hasDeptFilter
      ? review.heatmap.entries
      : review.heatmap.entries.filter((e) => selectedDepartments.has(e.department));
    return entries.map((e) => ({ row: e.department, col: e.question, value: e.score }));
  }, [review, selectedDepartments, hasDeptFilter]);

  const filteredDepartments = useMemo(() => {
    if (!review) return [];
    return !hasDeptFilter
      ? review.heatmap.departments
      : review.heatmap.departments.filter((d) => selectedDepartments.has(d));
  }, [review, selectedDepartments, hasDeptFilter]);

  const filteredAttentionPoints = useMemo(() => {
    if (!review) return [];
    return !hasDeptFilter
      ? review.attentionPoints
      : review.attentionPoints.filter((ap) => selectedDepartments.has(ap.department));
  }, [review, selectedDepartments, hasDeptFilter]);

  const filteredActionItems = useMemo(() => {
    if (!review) return [];
    return !hasDeptFilter
      ? review.actionItems
      : review.actionItems.filter((ai) => selectedDepartments.has(ai.department) || ai.department === "Geral");
  }, [review, selectedDepartments, hasDeptFilter]);

  function handleAddFilter(id: string) {
    setActiveFilters((prev) => [...prev, id]);
    requestAnimationFrame(() => setOpenFilter(id));
  }

  function handleRemoveFilter(id: string) {
    setActiveFilters((prev) => prev.filter((f) => f !== id));
    setOpenFilter(null);
    if (id === "department") setSelectedDepartments(new Set());
  }

  function handleClearAll() {
    setActiveFilters([]);
    setOpenFilter(null);
    setSelectedDepartments(new Set());
  }

  if (!review) {
    return (
      <div className={styles.tab}>
        <p>Dados de visão geral não disponíveis para esta pesquisa.</p>
      </div>
    );
  }

  const deptLabel = selectedDepartments.size === 0
    ? "Todos"
    : selectedDepartments.size === 1
      ? [...selectedDepartments][0]
      : `${selectedDepartments.size} selecionados`;

  return (
    <div className={styles.tab}>
      {/* Filters */}
      <FilterBar
        filters={FILTER_OPTIONS.filter((f) => !activeFilters.includes(f.id))}
        onAddFilter={handleAddFilter}
        onClearAll={activeFilters.length > 0 ? handleClearAll : undefined}
      >
        {activeFilters.includes("department") && (
          <div ref={deptChipRef} style={{ display: "inline-flex" }}>
            <FilterChip
              label={`Departamento: ${deptLabel}`}
              active={openFilter === "department"}
              onClick={() => setOpenFilter(openFilter === "department" ? null : "department")}
              onRemove={() => handleRemoveFilter("department")}
            />
          </div>
        )}
      </FilterBar>

      <PopoverSelect
        mode="multiple"
        open={openFilter === "department"}
        onClose={() => setOpenFilter(null)}
        anchorRef={deptChipRef}
        ignoreRefs={ignoreChipRefs}
        options={departmentOptions}
        value={Array.from(selectedDepartments)}
        onChange={(ids) => setSelectedDepartments(new Set(ids))}
      />

      {/* Heatmap — scores by question × department */}
      <Card padding="md">
        <CardHeader title="Score por departamento" description="Média das respostas numéricas por departamento e pergunta" />
        <CardBody>
          <Heatmap
            data={filteredHeatmapData}
            rows={filteredDepartments}
            columns={review.heatmap.questions}
            min={1}
            max={5}
            color="orange"
            showValues
            formatValue={(v: number) => v.toFixed(1)}
            cellSize={48}
            labelWidth={110}
            gap={3}
            columnTooltips={review.heatmap.questionLabels}
          />

          {/* Question legend */}
          <div className={styles.questionLegend}>
            {review.heatmap.questions.map((q) => (
              <div key={q} className={styles.questionLegendItem}>
                <span className={styles.questionLegendCode}>{q}</span>
                <span className={styles.questionLegendText}>
                  {review.heatmap.questionLabels[q] ?? q}
                </span>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Attention points */}
      {filteredAttentionPoints.length > 0 && (
        <Card padding="md">
          <CardHeader
            title="Pontos de atenção"
            description="Perguntas abaixo do benchmark que requerem ação"
            action={<Badge color="error" size="sm">{filteredAttentionPoints.length}</Badge>}
          />
          <CardBody>
            <div className={styles.attentionList}>
              {filteredAttentionPoints.map((ap) => (
                <div key={ap.id} className={styles.attentionItem}>
                  <div className={styles.attentionIcon}>
                    <Warning size={20} color={ap.severity === "critical" ? "var(--color-error-600)" : "var(--color-warning-600)"} />
                  </div>
                  <div className={styles.attentionContent}>
                    <div className={styles.attentionHeader}>
                      <span className={styles.attentionQuestion}>{ap.questionText}</span>
                      <div className={styles.attentionScores}>
                        <Badge color={ap.severity === "critical" ? "error" : "warning"} size="sm">
                          {ap.score.toFixed(1)} / {ap.benchmark}
                        </Badge>
                        <Badge color="neutral" size="sm">{ap.department}</Badge>
                      </div>
                    </div>
                    <p className={styles.attentionInsight}>{ap.insight}</p>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      )}

      {/* Trends — comparison with previous period */}
      {review.trends.length > 0 && (
        <Card padding="md">
          <CardHeader title="Tendências" description="Evolução comparada ao período anterior" />
          <CardBody>
            <div className={styles.trendsGrid}>
              {review.trends.map((t, i) => {
                const isPositive = t.delta > 0;
                const isNeutral = t.delta === 0;
                const TrendIcon = isPositive ? TrendUp : isNeutral ? Minus : TrendDown;
                const trendColor = isPositive ? "var(--color-success-600)" : isNeutral ? "var(--color-neutral-500)" : "var(--color-error-600)";
                const sparkColor = isPositive ? "green" : isNeutral ? "neutral" : "red";

                return (
                  <div key={i} className={styles.trendCard}>
                    <p className={styles.trendQuestion}>{t.questionText}</p>
                    <div className={styles.trendRow}>
                      <span className={styles.trendScore}>{t.current.toFixed(1)}</span>
                      <div className={styles.trendDelta} style={{ color: trendColor }}>
                        <TrendIcon size={14} />
                        <span>{isPositive ? "+" : ""}{t.delta.toFixed(1)}</span>
                      </div>
                      <Sparkline data={t.history.map((h) => h * 20)} width={80} height={24} color={sparkColor} filled />
                    </div>
                  </div>
                );
              })}
            </div>
          </CardBody>
        </Card>
      )}

      {/* Action items */}
      <Card padding="md">
        <CardHeader
          title="Plano de ação"
          description="Ações derivadas dos resultados da pesquisa"
          action={<Button variant="secondary" size="sm" leftIcon={Plus}>Nova ação</Button>}
        />
        <CardBody>
          <Table variant="divider" elevated={false}>
            <TableContent>
              <TableHead>
                <TableRow>
                  <TableHeaderCell>Ação</TableHeaderCell>
                  <TableHeaderCell>Prioridade</TableHeaderCell>
                  <TableHeaderCell>Departamento</TableHeaderCell>
                  <TableHeaderCell>Responsável</TableHeaderCell>
                  <TableHeaderCell>Status</TableHeaderCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filteredActionItems.map((item) => {
                  const prio = PRIORITY_CONFIG[item.priority] ?? { label: "Baixa", color: "neutral" as const };
                  const status = ACTION_STATUS_CONFIG[item.status] ?? { label: "Pendente", color: "caramel" as const };
                  return (
                    <TableRow key={item.id}>
                      <TableCell>
                        <div className={styles.actionCell}>
                          <span className={styles.actionTitle}>{item.title}</span>
                          <span className={styles.actionDesc}>{item.description}</span>
                        </div>
                      </TableCell>
                      <TableCell><Badge color={prio.color} size="sm">{prio.label}</Badge></TableCell>
                      <TableCell>{item.department}</TableCell>
                      <TableCell>{item.assignee ?? "—"}</TableCell>
                      <TableCell><Badge color={status.color} size="sm">{status.label}</Badge></TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </TableContent>
          </Table>
        </CardBody>
      </Card>

    </div>
  );
}
