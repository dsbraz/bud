import { useState, useRef, useMemo, type ChangeEvent } from "react";
import {
  Card,
  CardBody,
  CardHeader,
  Badge,
  Button,
  Input,
  DatePicker,
  Table,
  TableCardHeader,
  TableContent,
  TableHead,
  TableBody,
  TableRow,
  TableHeaderCell,
  TableCell,
  Avatar,
  AvatarLabelGroup,
  Tooltip,
  Alert,
  FilterBar,
  FilterChip,
  toast,
} from "@mdonangelo/bud-ds";
import type { CalendarDate, FilterOption } from "@mdonangelo/bud-ds";
import {
  MagnifyingGlass,
  Export,
  Warning,
  Users,
  CheckCircle,
  ClockCountdown,
  ChartLineUp,
  Buildings,
  FunnelSimple,
  TrendUp,
  WarningCircle,
  CalendarBlank,
} from "@phosphor-icons/react";
import { PopoverSelect, formatMultiLabel } from "@/components/PopoverSelect";
import type { SurveyResultData, CalibrationParticipant } from "../types";
import { CalibrationModal } from "./CalibrationModal";
import styles from "./CalibrationTab.module.css";

/* ——— Score color ——— */

function getScoreColor(score: number): string {
  if (score >= 4) return "var(--color-success-600)";
  if (score >= 3) return "var(--color-warning-600)";
  return "var(--color-error-600)";
}

/* ——— Calibration session status ——— */

const SESSION_STATUS_CONFIG: Record<string, { label: string; color: "caramel" | "warning" | "success" }> = {
  rascunho: { label: "Rascunho", color: "caramel" },
  em_andamento: { label: "Em andamento", color: "warning" },
  finalizada: { label: "Finalizada", color: "success" },
};

/* ——— Component ——— */

interface CalibrationTabProps {
  data: SurveyResultData;
}

export function CalibrationTab({ data }: CalibrationTabProps) {
  if (!data.calibration) {
    return (
      <div className={styles.tab}>
        <Alert variant="info" title="Sem dados">Dados de calibragem não disponíveis para esta pesquisa.</Alert>
      </div>
    );
  }
  return <CycleCalibration data={data} />;
}

/* ——— 9-Box Grid ——— */

interface NineBoxCellConfig {
  row: number;
  col: number;
  key: string;
  label: string;
  baseColor: string;       // CSS color for the cell bg at max density
  baseColorLight: string;  // lighter version for low density
}

const NINE_BOX_CELLS: NineBoxCellConfig[] = [
  // row 0 = alto potencial (top), row 2 = baixo potencial (bottom)
  // col 0 = baixo desempenho (left), col 2 = alto desempenho (right)
  { row: 0, col: 0, key: "0-0", label: "Enigma",           baseColor: "var(--color-warning-200)", baseColorLight: "var(--color-warning-50)" },
  { row: 0, col: 1, key: "0-1", label: "Alto potencial",   baseColor: "var(--color-green-200)",   baseColorLight: "var(--color-green-50)" },
  { row: 0, col: 2, key: "0-2", label: "Estrela",          baseColor: "var(--color-green-300)",   baseColorLight: "var(--color-green-50)" },
  { row: 1, col: 0, key: "1-0", label: "Questionável",     baseColor: "var(--color-error-200)",   baseColorLight: "var(--color-error-50)" },
  { row: 1, col: 1, key: "1-1", label: "Mantenedor",       baseColor: "var(--color-caramel-200)", baseColorLight: "var(--color-caramel-50)" },
  { row: 1, col: 2, key: "1-2", label: "Forte desempenho", baseColor: "var(--color-green-200)",   baseColorLight: "var(--color-green-50)" },
  { row: 2, col: 0, key: "2-0", label: "Insuficiente",     baseColor: "var(--color-error-300)",   baseColorLight: "var(--color-error-50)" },
  { row: 2, col: 1, key: "2-1", label: "Eficaz",           baseColor: "var(--color-caramel-200)", baseColorLight: "var(--color-caramel-50)" },
  { row: 2, col: 2, key: "2-2", label: "Comprometido",     baseColor: "var(--color-warning-200)", baseColorLight: "var(--color-warning-50)" },
];

const POTENTIAL_ROW: Record<string, number> = { alto: 0, médio: 1, baixo: 2 };

function getPerformanceCol(score: number): number {
  if (score >= 3.7) return 2;
  if (score >= 2.5) return 1;
  return 0;
}

/** Compute the 9-box cell key for a participant */
function getParticipantCellKey(p: CalibrationParticipant): string {
  const perfScore = p.finalScore ?? (p.managerScore * 0.5 + (p.score360 ?? p.selfScore) * 0.3 + p.selfScore * 0.2);
  const row = POTENTIAL_ROW[p.potential];
  const col = getPerformanceCol(perfScore);
  return `${row}-${col}`;
}

interface NineBoxCellData {
  all: CalibrationParticipant[];
  calibrated: number;
  pending: number;
  percent: number; // % of total participants
}

interface NineBoxGridProps {
  participants: CalibrationParticipant[];
  total: number;
  activeCell: string | null;
  onCellClick: (cellKey: string | null) => void;
}

function NineBoxGrid({ participants, total, activeCell, onCellClick }: NineBoxGridProps) {
  const isSmallScale = total <= 50;

  const { cells, maxCount } = useMemo(() => {
    const map = new Map<string, NineBoxCellData>();
    // Initialize all cells
    for (const c of NINE_BOX_CELLS) {
      map.set(c.key, { all: [], calibrated: 0, pending: 0, percent: 0 });
    }
    for (const p of participants) {
      const key = getParticipantCellKey(p);
      const cell = map.get(key)!;
      cell.all.push(p);
      if (p.status === "calibrado") cell.calibrated++;
      else cell.pending++;
    }
    let max = 0;
    for (const cell of map.values()) {
      cell.percent = total > 0 ? Math.round((cell.all.length / total) * 100) : 0;
      if (cell.all.length > max) max = cell.all.length;
    }
    return { cells: map, maxCount: max };
  }, [participants, total]);

  return (
    <Card padding="md">
      <CardHeader
        title="Mapa de talentos (9-Box)"
        description={`${total} colaboradores — clique em um quadrante para filtrar a tabela`}
        action={activeCell ? (
          <Button variant="secondary" size="sm" onClick={() => onCellClick(null)}>
            Limpar filtro
          </Button>
        ) : undefined}
      />
      <CardBody>
        <div className={styles.nineBoxWrapper}>
          {/* Y-axis label */}
          <div className={styles.nineBoxYLabel}>
            <span>Potencial</span>
          </div>

          {/* Y-axis ticks */}
          <div className={styles.nineBoxYTicks}>
            <span>Alto</span>
            <span>Médio</span>
            <span>Baixo</span>
          </div>

          <div className={styles.nineBoxContent}>
            {/* Grid */}
            <div className={styles.nineBoxGrid}>
              {NINE_BOX_CELLS.map((cellConfig) => {
                const data = cells.get(cellConfig.key)!;
                const count = data.all.length;
                const isActive = activeCell === cellConfig.key;
                const isDimmed = activeCell !== null && !isActive;
                // Density: interpolate between light and full color based on count/maxCount
                const density = maxCount > 0 ? count / maxCount : 0;
                const showAvatars = isSmallScale && count <= 4 && count > 0;

                return (
                  <button
                    key={cellConfig.key}
                    className={`${styles.nineBoxCell} ${isActive ? styles.nineBoxCellActive : ""} ${isDimmed ? styles.nineBoxCellDimmed : ""}`}
                    style={{
                      background: count === 0
                        ? "var(--color-neutral-50)"
                        : density > 0.5
                          ? cellConfig.baseColor
                          : cellConfig.baseColorLight,
                      gridRow: cellConfig.row + 1,
                      gridColumn: cellConfig.col + 1,
                    }}
                    onClick={() => onCellClick(isActive ? null : cellConfig.key)}
                    aria-label={`${cellConfig.label}: ${count} colaboradores, ${data.calibrated} calibrados`}
                    aria-pressed={isActive}
                  >
                    <span className={styles.nineBoxCellLabel}>{cellConfig.label}</span>

                    {count > 0 ? (
                      <>
                        <span className={styles.nineBoxCount}>{count}</span>
                        <span className={styles.nineBoxPercent}>{data.percent}%</span>

                        {/* Calibration mini progress */}
                        <div className={styles.nineBoxProgress}>
                          <div
                            className={styles.nineBoxProgressFill}
                            style={{ width: `${count > 0 ? (data.calibrated / count) * 100 : 0}%` }}
                          />
                        </div>
                        <span className={styles.nineBoxCalibrated}>
                          {data.calibrated}/{count}
                        </span>

                        {/* Avatars only at small scale */}
                        {showAvatars && (
                          <div className={styles.nineBoxAvatars}>
                            {data.all.map((p) => (
                              <Tooltip key={p.id} content={p.name}>
                                <span className={styles.nineBoxAvatarWrap}>
                                  <Avatar size="xs" initials={p.initials} />
                                </span>
                              </Tooltip>
                            ))}
                          </div>
                        )}
                      </>
                    ) : (
                      <span className={styles.nineBoxEmpty}>—</span>
                    )}
                  </button>
                );
              })}
            </div>

            {/* X-axis ticks */}
            <div className={styles.nineBoxXTicks}>
              <span>Baixo</span>
              <span>Médio</span>
              <span>Alto</span>
            </div>

            {/* X-axis label */}
            <div className={styles.nineBoxXLabel}>
              <span>Desempenho</span>
            </div>
          </div>
        </div>
      </CardBody>
    </Card>
  );
}

/* ——— Filter options ——— */

const CALIBRATION_FILTER_OPTIONS: FilterOption[] = [
  { id: "department", label: "Departamento", icon: Buildings },
  { id: "status", label: "Status", icon: FunnelSimple },
  { id: "potential", label: "Potencial", icon: TrendUp },
  { id: "bias", label: "Alerta de viés", icon: WarningCircle },
  { id: "period", label: "Período de resposta", icon: CalendarBlank },
];

const STATUS_OPTIONS = [
  { id: "all", label: "Todos" },
  { id: "calibrado", label: "Calibrado" },
  { id: "pendente", label: "Pendente" },
];

const POTENTIAL_OPTIONS = [
  { id: "all", label: "Todos" },
  { id: "alto", label: "Alto" },
  { id: "médio", label: "Médio" },
  { id: "baixo", label: "Baixo" },
];

const BIAS_OPTIONS = [
  { id: "all", label: "Todos" },
  { id: "with_alert", label: "Com alerta" },
  { id: "no_alert", label: "Sem alerta" },
];

/** Helper: format CalendarDate to display string DD/MM/YYYY */
function formatCalendarDate(cd: CalendarDate): string {
  return `${String(cd.day).padStart(2, "0")}/${String(cd.month).padStart(2, "0")}/${cd.year}`;
}

/* ——— Modo A: Ciclo — Calibração completa ——— */

function CycleCalibration({ data }: { data: SurveyResultData }) {
  const calibration = data.calibration!;

  /* ——— Filter state ——— */
  const [activeFilters, setActiveFilters] = useState<string[]>([]);
  const [openFilter, setOpenFilter] = useState<string | null>(null);
  const [selectedDepartments, setSelectedDepartments] = useState<string[]>([]);
  const [selectedStatus, setSelectedStatus] = useState("all");
  const [selectedPotential, setSelectedPotential] = useState("all");
  const [selectedBias, setSelectedBias] = useState("all");
  const [selectedPeriod, setSelectedPeriod] = useState<[CalendarDate | null, CalendarDate | null]>([null, null]);

  /* Filter chip refs (FilterChip doesn't support ref) */
  const deptChipRef = useRef<HTMLDivElement>(null);
  const statusChipRef = useRef<HTMLDivElement>(null);
  const potentialChipRef = useRef<HTMLDivElement>(null);
  const biasChipRef = useRef<HTMLDivElement>(null);
  const periodChipRef = useRef<HTMLDivElement>(null);

  const chipRefs: Record<string, React.RefObject<HTMLDivElement | null>> = {
    department: deptChipRef,
    status: statusChipRef,
    potential: potentialChipRef,
    bias: biasChipRef,
    period: periodChipRef,
  };
  const ignoreChipRefs = useMemo(() => Object.values(chipRefs), []);

  /* ——— Table / grid state ——— */
  const [search, setSearch] = useState("");
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortDir, setSortDir] = useState<"asc" | "desc">("asc");
  const [selectedRows, setSelectedRows] = useState<Set<string>>(new Set());
  const [nineBoxFilter, setNineBoxFilter] = useState<string | null>(null);
  const [modalParticipant, setModalParticipant] = useState<CalibrationParticipant | null>(null);

  /* ——— Derived filter options ——— */
  const departmentOptions = useMemo(() => {
    const deps = [...new Set(calibration.participants.map((p) => p.department))].sort();
    return deps.map((d) => ({ id: d, label: d }));
  }, [calibration.participants]);

  /* ——— Filter handlers ——— */
  function handleAddFilter(id: string) {
    setActiveFilters((prev) => [...prev, id]);
    requestAnimationFrame(() => setOpenFilter(id));
  }

  function handleRemoveFilter(id: string) {
    setActiveFilters((prev) => prev.filter((f) => f !== id));
    setOpenFilter(null);
    if (id === "department") setSelectedDepartments([]);
    if (id === "status") setSelectedStatus("all");
    if (id === "potential") setSelectedPotential("all");
    if (id === "bias") setSelectedBias("all");
    if (id === "period") setSelectedPeriod([null, null]);
  }

  function handleClearAll() {
    setActiveFilters([]);
    setOpenFilter(null);
    setSelectedDepartments([]);
    setSelectedStatus("all");
    setSelectedPotential("all");
    setSelectedBias("all");
    setSelectedPeriod([null, null]);
  }

  function getFilterLabel(id: string): string {
    switch (id) {
      case "department":
        return `Departamento: ${formatMultiLabel(selectedDepartments, departmentOptions, "Todos")}`;
      case "status": {
        const opt = STATUS_OPTIONS.find((o) => o.id === selectedStatus);
        return selectedStatus === "all" ? "Status" : `Status: ${opt?.label ?? ""}`;
      }
      case "potential": {
        const opt = POTENTIAL_OPTIONS.find((o) => o.id === selectedPotential);
        return selectedPotential === "all" ? "Potencial" : `Potencial: ${opt?.label ?? ""}`;
      }
      case "bias": {
        const opt = BIAS_OPTIONS.find((o) => o.id === selectedBias);
        return selectedBias === "all" ? "Alerta de viés" : `Viés: ${opt?.label ?? ""}`;
      }
      case "period": {
        if (selectedPeriod[0] && selectedPeriod[1]) {
          return `Período: ${formatCalendarDate(selectedPeriod[0])} – ${formatCalendarDate(selectedPeriod[1])}`;
        }
        return "Período de resposta";
      }
      default:
        return id;
    }
  }

  /* ——— Data filtering ——— */
  const filtered = useMemo(() => {
    let items = calibration.participants;

    if (search) {
      const q = search.toLowerCase();
      items = items.filter((p) => p.name.toLowerCase().includes(q));
    }
    if (selectedDepartments.length > 0) {
      const deptSet = new Set(selectedDepartments);
      items = items.filter((p) => deptSet.has(p.department));
    }
    if (selectedStatus !== "all") {
      items = items.filter((p) => p.status === selectedStatus);
    }
    if (selectedPotential !== "all") {
      items = items.filter((p) => p.potential === selectedPotential);
    }
    if (selectedBias !== "all") {
      items = items.filter((p) =>
        selectedBias === "with_alert" ? !!p.biasAlert : !p.biasAlert,
      );
    }
    if (selectedPeriod[0] && selectedPeriod[1]) {
      const startIso = `${selectedPeriod[0].year}-${String(selectedPeriod[0].month).padStart(2, "0")}-${String(selectedPeriod[0].day).padStart(2, "0")}`;
      const endIso = `${selectedPeriod[1].year}-${String(selectedPeriod[1].month).padStart(2, "0")}-${String(selectedPeriod[1].day).padStart(2, "0")}`;
      items = items.filter((p) => p.respondedAt >= startIso && p.respondedAt <= endIso);
    }
    return items;
  }, [calibration.participants, search, selectedDepartments, selectedStatus, selectedPotential, selectedBias, selectedPeriod]);

  /* Apply 9-box filter on top of other filters (only affects table) */
  const tableFiltered = useMemo(() => {
    if (!nineBoxFilter) return filtered;
    return filtered.filter((p) => getParticipantCellKey(p) === nineBoxFilter);
  }, [filtered, nineBoxFilter]);

  const sorted = useMemo(() => {
    if (!sortKey) return tableFiltered;
    return [...tableFiltered].sort((a, b) => {
      let av: number | string, bv: number | string;
      switch (sortKey) {
        case "name": av = a.name; bv = b.name; break;
        case "department": av = a.department; bv = b.department; break;
        case "selfScore": av = a.selfScore; bv = b.selfScore; break;
        case "managerScore": av = a.managerScore; bv = b.managerScore; break;
        case "finalScore": av = a.finalScore ?? 0; bv = b.finalScore ?? 0; break;
        default: return 0;
      }
      if (typeof av === "string") return sortDir === "asc" ? av.localeCompare(bv as string) : (bv as string).localeCompare(av);
      return sortDir === "asc" ? (av as number) - (bv as number) : (bv as number) - (av as number);
    });
  }, [tableFiltered, sortKey, sortDir]);

  const rowIds = useMemo(() => sorted.map((p) => p.id), [sorted]);

  /* Sorting */
  function handleSort(key: string) {
    if (sortKey === key) setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    else { setSortKey(key); setSortDir("asc"); }
  }
  function getSortDirection(key: string) { return sortKey === key ? sortDir : undefined; }

  /* Selection */
  function handleSelectRow(id: string, checked: boolean) {
    setSelectedRows((prev) => {
      const next = new Set(prev);
      if (checked) next.add(id); else next.delete(id);
      return next;
    });
  }
  function handleSelectAll(checked: boolean) {
    setSelectedRows(checked ? new Set(rowIds) : new Set());
  }

  /* KPIs */
  const pendingCount = calibration.totalParticipants - calibration.calibratedCount;
  const completionRate = Math.round((calibration.calibratedCount / calibration.totalParticipants) * 100);

  /* Session status */
  const sessionConfig = SESSION_STATUS_CONFIG[calibration.sessionStatus] ?? { label: "Rascunho", color: "caramel" as const };

  function handleSaveCalibration(
    participant: CalibrationParticipant,
    _finalScore: number,
    _classification: string,
    _justification: string,
  ) {
    toast.success(`Calibragem de ${participant.name} salva com sucesso`);
    setModalParticipant(null);
  }

  return (
    <div className={styles.tab}>
      {/* Session bar */}
      <div className={styles.sessionBar}>
        <div className={styles.sessionLeft}>
          <Badge color={sessionConfig.color} size="md">{sessionConfig.label}</Badge>
          <span className={styles.sessionText}>{calibration.calibratedCount}/{calibration.totalParticipants} calibrados</span>
        </div>
        <div className={styles.sessionRight}>
          <Button variant="secondary" size="sm" leftIcon={Export}>Exportar</Button>
        </div>
      </div>

      {/* Filters */}
      <FilterBar
        filters={CALIBRATION_FILTER_OPTIONS.filter((f) => !activeFilters.includes(f.id))}
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

      {/* Filter popovers */}
      <PopoverSelect
        mode="multiple"
        open={openFilter === "department"}
        onClose={() => setOpenFilter(null)}
        anchorRef={deptChipRef}
        ignoreRefs={ignoreChipRefs}
        options={departmentOptions}
        value={selectedDepartments}
        onChange={setSelectedDepartments}
        searchable
        searchPlaceholder="Buscar departamento..."
      />
      <PopoverSelect
        mode="single"
        open={openFilter === "status"}
        onClose={() => setOpenFilter(null)}
        anchorRef={statusChipRef}
        ignoreRefs={ignoreChipRefs}
        options={STATUS_OPTIONS}
        value={selectedStatus}
        onChange={(id) => setSelectedStatus(id)}
      />
      <PopoverSelect
        mode="single"
        open={openFilter === "potential"}
        onClose={() => setOpenFilter(null)}
        anchorRef={potentialChipRef}
        ignoreRefs={ignoreChipRefs}
        options={POTENTIAL_OPTIONS}
        value={selectedPotential}
        onChange={(id) => setSelectedPotential(id)}
      />
      <PopoverSelect
        mode="single"
        open={openFilter === "bias"}
        onClose={() => setOpenFilter(null)}
        anchorRef={biasChipRef}
        ignoreRefs={ignoreChipRefs}
        options={BIAS_OPTIONS}
        value={selectedBias}
        onChange={(id) => setSelectedBias(id)}
      />
      <PopoverSelect
        mode="single"
        open={openFilter === "period"}
        onClose={() => setOpenFilter(null)}
        anchorRef={periodChipRef}
        ignoreRefs={ignoreChipRefs}
        options={[]}
        value=""
        onChange={() => {}}
        emptyText=""
        footer={
          <div className={styles.periodPicker}>
            <DatePicker
              label="De – Até"
              mode="range"
              size="sm"
              value={selectedPeriod}
              onChange={(range: [CalendarDate | null, CalendarDate | null]) => {
                setSelectedPeriod(range);
                if (range[0] && range[1]) {
                  setOpenFilter(null);
                }
              }}
            />
          </div>
        }
      />

      {/* KPIs */}
      <div className={styles.kpiGrid}>
        {[
          { label: "Participantes", value: calibration.totalParticipants, icon: Users, bg: "var(--color-caramel-100)", iconColor: "var(--color-caramel-600)" },
          { label: "Calibrados", value: calibration.calibratedCount, icon: CheckCircle, bg: "var(--color-green-50)", iconColor: "var(--color-green-600)" },
          { label: "Pendentes", value: pendingCount, icon: ClockCountdown, bg: "var(--color-warning-50)", iconColor: "var(--color-warning-600)" },
          { label: "Taxa de conclusão", value: `${completionRate}%`, icon: ChartLineUp, bg: "var(--color-orange-50)", iconColor: "var(--color-orange-600)" },
        ].map((kpi) => {
          const Icon = kpi.icon;
          return (
            <Card key={kpi.label} padding="sm">
              <CardBody>
                <div className={styles.kpiCard}>
                  <div className={styles.kpiIconWrapper} style={{ background: kpi.bg }}>
                    <Icon size={20} color={kpi.iconColor} />
                  </div>
                  <div className={styles.kpiContent}>
                    <span className={styles.kpiValue}>{kpi.value}</span>
                    <span className={styles.kpiLabel}>{kpi.label}</span>
                  </div>
                </div>
              </CardBody>
            </Card>
          );
        })}
      </div>

      {/* 9-Box Grid */}
      <NineBoxGrid
        participants={filtered}
        total={filtered.length}
        activeCell={nineBoxFilter}
        onCellClick={setNineBoxFilter}
      />

      {/* Calibration table */}
      <Table
        variant="divider"
        elevated={false}
        selectable
        selectedRows={selectedRows}
        rowIds={rowIds}
        onSelectRow={handleSelectRow}
        onSelectAll={handleSelectAll}
      >
        <TableCardHeader
          title="Calibração"
          badge={
            <div className={styles.tableBadges}>
              <Badge color="neutral" size="sm">{sorted.length}</Badge>
              {nineBoxFilter && (
                <Badge color="orange" size="sm">
                  {NINE_BOX_CELLS.find((c) => c.key === nineBoxFilter)?.label ?? ""}
                </Badge>
              )}
            </div>
          }
          actions={
            <div className={styles.tableActions}>
              <Input
                size="sm"
                leftIcon={MagnifyingGlass}
                placeholder="Buscar colaborador..."
                value={search}
                onChange={(e: ChangeEvent<HTMLInputElement>) => setSearch(e.target.value)}
              />
            </div>
          }
        />
        <TableContent>
          <TableHead>
            <TableRow>
              <TableHeaderCell isCheckbox />
              <TableHeaderCell sortable sortDirection={getSortDirection("name")} onSort={() => handleSort("name")}>Colaborador</TableHeaderCell>
              <TableHeaderCell sortable sortDirection={getSortDirection("department")} onSort={() => handleSort("department")}>Departamento</TableHeaderCell>
              <TableHeaderCell sortable sortDirection={getSortDirection("selfScore")} onSort={() => handleSort("selfScore")}>Auto</TableHeaderCell>
              <TableHeaderCell sortable sortDirection={getSortDirection("managerScore")} onSort={() => handleSort("managerScore")}>Gestor</TableHeaderCell>
              <TableHeaderCell sortable sortDirection={getSortDirection("finalScore")} onSort={() => handleSort("finalScore")}>Score Final</TableHeaderCell>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Alerta</TableHeaderCell>
              <TableHeaderCell>Ação</TableHeaderCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {sorted.map((p) => (
              <TableRow key={p.id} rowId={p.id}>
                <TableCell isCheckbox rowId={p.id} />
                <TableCell>
                  <AvatarLabelGroup name={p.name} initials={p.initials} size="sm" />
                </TableCell>
                <TableCell>{p.department}</TableCell>
                <TableCell>
                  <span style={{ color: getScoreColor(p.selfScore), fontWeight: 600 }}>{p.selfScore.toFixed(1)}</span>
                </TableCell>
                <TableCell>
                  <span style={{ color: getScoreColor(p.managerScore), fontWeight: 600 }}>{p.managerScore.toFixed(1)}</span>
                </TableCell>
                <TableCell>
                  {p.finalScore != null
                    ? <span style={{ color: getScoreColor(p.finalScore), fontWeight: 700, fontSize: "var(--text-sm)" }}>{p.finalScore.toFixed(1)}</span>
                    : <span style={{ color: "var(--color-neutral-400)" }}>—</span>
                  }
                </TableCell>
                <TableCell>
                  <Badge color={p.status === "calibrado" ? "success" : "warning"} size="sm">
                    {p.status === "calibrado" ? "Calibrado" : "Pendente"}
                  </Badge>
                </TableCell>
                <TableCell>
                  {p.biasAlert ? (
                    <Tooltip content={p.biasAlert}>
                      <Warning size={16} color="var(--color-warning-600)" />
                    </Tooltip>
                  ) : null}
                </TableCell>
                <TableCell>
                  <Button
                    variant={p.status === "pendente" ? "primary" : "secondary"}
                    size="sm"
                    onClick={() => setModalParticipant(p)}
                  >
                    {p.status === "pendente" ? "Calibrar" : "Ver"}
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </TableContent>
      </Table>

      {/* Calibration modal */}
      {modalParticipant && (
        <CalibrationModal
          participant={modalParticipant}
          open={!!modalParticipant}
          onClose={() => setModalParticipant(null)}
          onSave={handleSaveCalibration}
          surveyType={data.surveyType}
        />
      )}
    </div>
  );
}
