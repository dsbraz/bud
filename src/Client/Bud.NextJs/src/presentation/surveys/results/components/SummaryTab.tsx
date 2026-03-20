import { useState, useRef, useMemo } from "react";
import {
  Card,
  CardBody,
  FilterBar,
  FilterChip,
  Accordion,
  AccordionItem,
} from "@mdonangelo/bud-ds";
import { PopoverSelect } from "@/components/PopoverSelect";
import type { FilterOption } from "@mdonangelo/bud-ds";
import {
  Eye,
  PlayCircle,
  ChatCircle,
  CheckCircle,
  Clock,
  Buildings,
  CalendarBlank,
} from "@phosphor-icons/react";
import type { SurveyResultData } from "../types";
import { QuestionResultCard } from "./QuestionResultCard";
import { usePeopleData } from "@/contexts/PeopleDataContext";
import { useConfigData } from "@/contexts/ConfigDataContext";
import styles from "./SummaryTab.module.css";

/* ——— Filter options ——— */

const FILTER_OPTIONS: FilterOption[] = [
  { id: "department", label: "Departamento", icon: Buildings },
  { id: "period", label: "Período", icon: CalendarBlank },
];

/* ——— KPI config ——— */

interface KpiConfig {
  label: string;
  icon: React.ElementType;
  bg: string;
  iconColor: string;
  key: keyof SurveyResultData["kpis"];
  format?: (v: number | string) => string;
}

const KPI_CONFIGS: KpiConfig[] = [
  { label: "Visualizações", icon: Eye, bg: "var(--color-caramel-100)", iconColor: "var(--color-caramel-600)", key: "views" },
  { label: "Pesquisas iniciadas", icon: PlayCircle, bg: "var(--color-wine-50)", iconColor: "var(--color-wine-600)", key: "started" },
  { label: "Respostas recebidas", icon: ChatCircle, bg: "var(--color-orange-50)", iconColor: "var(--color-orange-600)", key: "responses" },
  { label: "Taxa de conclusão", icon: CheckCircle, bg: "var(--color-green-50)", iconColor: "var(--color-green-600)", key: "completionRate", format: (v) => `${v}%` },
  { label: "Tempo médio", icon: Clock, bg: "var(--color-neutral-100)", iconColor: "var(--color-neutral-600)", key: "avgCompletionTime" },
];

/* ——— Component ——— */

interface SummaryTabProps {
  data: SurveyResultData;
}

export function SummaryTab({ data }: SummaryTabProps) {
  /* Get dynamic filter options from contexts */
  const { teamOptions } = usePeopleData();
  const { cycles } = useConfigData();

  /* Build department options from teams */
  const departmentOptions = useMemo(() => {
    const options = [{ id: "all", label: "Todos" }];
    teamOptions.forEach((team) => {
      options.push({ id: team.id, label: team.label });
    });
    return options;
  }, [teamOptions]);

  /* Build period options from cycles */
  const periodOptions = useMemo(() => {
    const options = [{ id: "all", label: "Todo o período" }];
    cycles
      .filter((cycle) => cycle.type === "quarterly")
      .slice(0, 4)
      .forEach((cycle) => {
        options.push({ id: cycle.id, label: cycle.name });
      });
    return options;
  }, [cycles]);

  /* Filters */
  const [activeFilters, setActiveFilters] = useState<string[]>([]);
  const [openFilter, setOpenFilter] = useState<string | null>(null);
  const [selectedDepartment, setSelectedDepartment] = useState("all");
  const [selectedPeriod, setSelectedPeriod] = useState("all");

  const deptChipRef = useRef<HTMLDivElement>(null);
  const periodChipRef = useRef<HTMLDivElement>(null);

  const chipRefs: Record<string, React.RefObject<HTMLDivElement | null>> = {
    department: deptChipRef,
    period: periodChipRef,
  };
  const ignoreChipRefs = useMemo(() => Object.values(chipRefs), []);

  function handleAddFilter(id: string) {
    setActiveFilters((prev) => [...prev, id]);
    requestAnimationFrame(() => setOpenFilter(id));
  }

  function handleRemoveFilter(id: string) {
    setActiveFilters((prev) => prev.filter((f) => f !== id));
    setOpenFilter(null);
    if (id === "department") setSelectedDepartment("all");
    if (id === "period") setSelectedPeriod("all");
  }

  function handleClearAll() {
    setActiveFilters([]);
    setOpenFilter(null);
    setSelectedDepartment("all");
    setSelectedPeriod("all");
  }

  function getFilterLabel(id: string): string {
    if (id === "department") {
      const opt = departmentOptions.find((o) => o.id === selectedDepartment);
      return `Departamento: ${opt?.label ?? "Todos"}`;
    }
    if (id === "period") {
      const opt = periodOptions.find((o) => o.id === selectedPeriod);
      return `Período: ${opt?.label ?? "Todo o período"}`;
    }
    return id;
  }

  /* Question index across sections */
  let questionIndex = 0;
  const firstSection = data.sections[0];

  return (
    <div className={styles.tab}>
      {/* Filters */}
      <FilterBar
        filters={FILTER_OPTIONS.filter((f) => !activeFilters.includes(f.id))}
        onAddFilter={handleAddFilter}
        onClearAll={activeFilters.length > 0 ? handleClearAll : undefined}
      >
        {activeFilters.map((filterId) => (
          <div key={filterId} ref={chipRefs[filterId]} style={{ display: "inline-flex" }}>
            <FilterChip
              label={getFilterLabel(filterId)}
              active={openFilter === filterId}
              onClick={() => setOpenFilter(openFilter === filterId ? null : filterId)}
              onRemove={() => handleRemoveFilter(filterId)}
            />
          </div>
        ))}
      </FilterBar>

      {/* Department filter dropdown */}
      <PopoverSelect
        mode="single"
        open={openFilter === "department"}
        onClose={() => setOpenFilter(null)}
        anchorRef={deptChipRef}
        ignoreRefs={ignoreChipRefs}
        options={departmentOptions}
        value={selectedDepartment}
        onChange={(id) => setSelectedDepartment(id)}
      />

      {/* Period filter dropdown */}
      <PopoverSelect
        mode="single"
        open={openFilter === "period"}
        onClose={() => setOpenFilter(null)}
        anchorRef={periodChipRef}
        ignoreRefs={ignoreChipRefs}
        options={periodOptions}
        value={selectedPeriod}
        onChange={(id) => setSelectedPeriod(id)}
      />

      {/* KPIs */}
      <div className={styles.indicatorGrid}>
        {KPI_CONFIGS.map((kpi) => {
          const Icon = kpi.icon;
          const raw = data.kpis[kpi.key];
          const value = kpi.format ? kpi.format(raw) : String(raw);

          return (
            <Card key={kpi.key} padding="sm">
              <CardBody>
                <div className={styles.indicatorCard}>
                  <div className={styles.indicatorIconWrapper} style={{ background: kpi.bg }}>
                    <Icon size={20} color={kpi.iconColor} />
                  </div>
                  <div className={styles.indicatorContent}>
                    <span className={styles.indicatorValue}>{value}</span>
                    <span className={styles.indicatorLabel}>{kpi.label}</span>
                  </div>
                </div>
              </CardBody>
            </Card>
          );
        })}
      </div>

      {/* Results by section */}
      <div className={styles.sectionsArea}>
        <h3 className={styles.sectionTitle}>Resultados por pergunta</h3>
        {data.sections.length === 1 ? (
          <div className={styles.questionList}>
            {firstSection?.questions.map((q, i) => (
              <QuestionResultCard key={q.questionId} result={q} index={i} />
            ))}
          </div>
        ) : (
          <Accordion>
            {data.sections.map((section) => {
              const sectionQuestions = section.questions;
              const startIndex = questionIndex;
              questionIndex += sectionQuestions.length;

              return (
                <AccordionItem
                  key={section.title}
                  title={section.title}
                  description={`${sectionQuestions.length} perguntas`}
                  defaultOpen
                >
                  <div className={styles.questionList}>
                    {sectionQuestions.map((q, i) => (
                      <QuestionResultCard
                        key={q.questionId}
                        result={q}
                        index={startIndex + i}
                      />
                    ))}
                  </div>
                </AccordionItem>
              );
            })}
          </Accordion>
        )}
      </div>
    </div>
  );
}
