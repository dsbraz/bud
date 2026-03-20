import { useState, useRef, useMemo, useEffect } from "react";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import {
  FilterBar,
  FilterChip,
  FilterDropdown,
  Button,
  Card,
  CardBody,
  CardDivider,
  Badge,
  Input,
  Radio,
  DatePicker,
  Modal,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Breadcrumb,
  Table,
  TableCardHeader,
  TableContent,
  TableHead,
  TableBody,
  TableRow,
  TableHeaderCell,
  TableCell,
  TableBulkActions,
  AssistantButton,
  AiAssistant,
  Alert,
  toast,
} from "@mdonangelo/bud-ds";
import { PopoverSelect, formatMultiLabel } from "@/components/PopoverSelect";
import { RowActionsPopover } from "@/components/table/RowActionsPopover";
import { useDataTable } from "@/hooks/useDataTable";
import { useFilterChips } from "@/hooks/useFilterChips";
import type { FilterOption, CalendarDate, PopoverItem } from "@mdonangelo/bud-ds";
import {
  Plus,
  MagnifyingGlass,
  FunnelSimple,
  CalendarBlank,
  ListChecks,
  Tag,
  Trash,
  PencilSimple,
  Copy,
  DownloadSimple,
  UserCircle,
  ShieldCheck,
  ChartLineUp,
  Users,
  CheckCircle,
  Check,
  ClockCountdown,
  ArrowSquareOut,
  Link,
  Pause,
  Play,
} from "@phosphor-icons/react";
import type { Icon } from "@phosphor-icons/react";
import { PageHeader } from "@/components/layout/PageHeader";
import { useSavedViews } from "@/contexts/SavedViewsContext";
import { usePeopleData } from "@/contexts/PeopleDataContext";
import { useConfigData } from "@/contexts/ConfigDataContext";
import { useSurveysData } from "@/contexts/SurveysDataContext";
import { useActivityData } from "@/contexts/ActivityDataContext";
import type { SurveyListItemData as SurveyListItem, SurveyTemplateRecord } from "@/lib/surveys-store";
import { getTemplateByType, getTypeLabel } from "./templates/surveyTemplates";
import type { SurveyTemplate } from "./templates/surveyTemplates";
import { TemplatePreviewPanel } from "./components/TemplatePreviewPanel";
import type { SurveyStatus } from "@/types/survey";
import styles from "./SurveysPage.module.css";

/* ——— Filter options ——— */

const FILTER_OPTIONS: FilterOption[] = [
  { id: "status", label: "Status", icon: FunnelSimple },
  { id: "type", label: "Tipo", icon: ListChecks },
  { id: "period", label: "Período", icon: CalendarBlank },
  { id: "category", label: "Categoria", icon: Tag },
];

const STATUS_OPTIONS = [
  { id: "all", label: "Todos" },
  { id: "draft", label: "Rascunho" },
  { id: "scheduled", label: "Agendada" },
  { id: "active", label: "Ativa" },
  { id: "paused", label: "Pausada" },
  { id: "closed", label: "Encerrada" },
  { id: "archived", label: "Arquivada" },
];

const TYPE_OPTIONS = [
  { id: "all", label: "Todos" },
  { id: "pulse", label: "Pulse" },
  { id: "clima", label: "Clima organizacional" },
  { id: "enps", label: "eNPS" },
  { id: "health_check", label: "Health Check" },
  { id: "skip_level", label: "Skip-Level" },
  { id: "performance", label: "Avaliação de desempenho" },
  { id: "360_feedback", label: "Feedback 360°" },
  { id: "feedback_solicitado", label: "Feedback solicitado" },
  { id: "custom", label: "Personalizada" },
];

const CATEGORY_OPTIONS = [
  { id: "all", label: "Todas" },
  { id: "pesquisa", label: "Pesquisa" },
  { id: "ciclo", label: "Ciclo de avaliação" },
];

/* ——— People & tag options (create modal) ——— */

/* ——— Status config ——— */

const STATUS_CONFIG: Record<SurveyStatus, { label: string; color: "neutral" | "orange" | "success" | "warning" | "error" | "wine" | "caramel" }> = {
  draft: { label: "Rascunho", color: "caramel" },
  scheduled: { label: "Agendada", color: "wine" },
  active: { label: "Ativa", color: "success" },
  paused: { label: "Pausada", color: "warning" },
  closed: { label: "Encerrada", color: "neutral" },
  archived: { label: "Arquivada", color: "neutral" },
};

/* ——— Create flow steps ——— */

const CREATE_STEPS = [
  { label: "Escolher template" },
  { label: "Participantes" },
  { label: "Questionário" },
  { label: "Fluxo de aplicação" },
  { label: "Resumo" },
];

/* ——— Helpers ——— */

function formatCalendarDate(cd: CalendarDate | null): string {
  if (!cd) return "";
  return `${String(cd.day).padStart(2, "0")}/${String(cd.month).padStart(2, "0")}/${cd.year}`;
}

function isoToCalendarDate(iso: string): CalendarDate {
  const [year = 0, month = 1, day = 1] = iso.split("-").map(Number);
  return { year, month, day };
}

function templateRecordToPreviewTemplate(template: SurveyTemplateRecord): SurveyTemplate {
  const baseTemplate = getTemplateByType(template.type);

  const sections = template.sections.map((section) => ({
    title: section.title,
    description: section.description,
    questions: template.questions
      .filter((question) => question.sectionId === section.id)
      .map((question) => ({
        type: question.type,
        text: question.text,
        isRequired: question.isRequired,
        options: question.options,
        scaleMin: question.scaleMin,
        scaleMax: question.scaleMax,
        scaleLabels: question.scaleLabels,
        ratingMax: question.ratingMax,
      })),
  }));

  const unsectioned = template.questions.filter((question) => question.sectionId === null);
  if (unsectioned.length > 0) {
    sections.unshift({
      title: "Geral",
      description: undefined,
      questions: unsectioned.map((question) => ({
        type: question.type,
        text: question.text,
        isRequired: question.isRequired,
        options: question.options,
        scaleMin: question.scaleMin,
        scaleMax: question.scaleMax,
        scaleLabels: question.scaleLabels,
        ratingMax: question.ratingMax,
      })),
    });
  }

  return {
    type: template.type,
    category: template.category,
    name: template.name,
    subtitle: template.subtitle,
    icon: baseTemplate?.icon ?? getTemplateByType("custom")?.icon ?? Users,
    defaultQuestionCount: template.questions.length,
    flowSteps: baseTemplate?.flowSteps ?? [],
    sections,
    defaultConfig: {
      isAnonymous: template.defaultConfig.isAnonymous,
      recurrence: template.defaultConfig.recurrence ?? undefined,
      aiPrefillOkrs: template.defaultConfig.aiPrefillOkrs,
      aiPrefillFeedback: template.defaultConfig.aiPrefillFeedback,
      aiBiasDetection: template.defaultConfig.aiBiasDetection,
    },
  };
}

/* ——— Component ——— */

export function SurveysPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const { views, addView, updateView, deleteView } = useSavedViews();
  const { ownerOptions } = usePeopleData();
  const { tagOptions, cyclePresetOptions, createTag } = useConfigData();
  const { surveys, templates, setSurveys, duplicateSurvey } = useSurveysData();
  const { activities } = useActivityData();

  // ── Filtro por respondente (vindo do modal de Meu time) ───────────────────
  const respondentState = location.state as { filterRespondentId?: string; filterRespondentName?: string } | null;
  const filterRespondentId   = respondentState?.filterRespondentId   ?? null;
  const filterRespondentName = respondentState?.filterRespondentName ?? null;

  const respondentChipRef = useRef<HTMLDivElement>(null);
  const [respondentDropdownOpen, setRespondentDropdownOpen] = useState(false);

  const respondentSurveyIds = useMemo(() => {
    if (!filterRespondentId) return null;
    return new Set(
      activities
        .filter((a) => a.userId === filterRespondentId && a.type === "survey_complete" && a.entityId)
        .map((a) => a.entityId!),
    );
  }, [filterRespondentId, activities]);

  function clearRespondentFilter() {
    navigate(location.pathname, { replace: true, state: {} });
  }

  function selectRespondent(id: string, name: string) {
    navigate(location.pathname, { replace: true, state: { filterRespondentId: id, filterRespondentName: name } });
    setRespondentDropdownOpen(false);
  }

  const peopleOptions = useMemo(
    () => ownerOptions.map((person) => ({
      id: person.id,
      label: person.label,
      initials: person.initials,
    })),
    [ownerOptions],
  );

  const presetPeriods = useMemo(
    () => cyclePresetOptions.map((cycle) => ({
      id: cycle.id,
      label: cycle.label,
      start: isoToCalendarDate(cycle.startDate),
      end: isoToCalendarDate(cycle.endDate),
    })),
    [cyclePresetOptions],
  );

  const baseTagOptions = useMemo(
    () => tagOptions.map((tag) => ({ id: tag.id, label: tag.label })),
    [tagOptions],
  );

  const isNewViewMode = !!(location.state as { newView?: boolean })?.newView;

  /* ——— Filter state ——— */
  const [selectedStatus, setSelectedStatus] = useState("all");
  const [selectedType, setSelectedType] = useState("all");
  const [selectedCategory, setSelectedCategory] = useState("all");
  const [selectedPeriod, setSelectedPeriod] = useState<[CalendarDate | null, CalendarDate | null]>([null, null]);

  const statusChipRef = useRef<HTMLDivElement>(null);
  const typeChipRef = useRef<HTMLDivElement>(null);
  const periodChipRef = useRef<HTMLDivElement>(null);
  const categoryChipRef = useRef<HTMLDivElement>(null);

  const chipRefs: Record<string, React.RefObject<HTMLDivElement | null>> = {
    status: statusChipRef,
    type: typeChipRef,
    period: periodChipRef,
    category: categoryChipRef,
  };

  const {
    activeFilters,
    setActiveFilters,
    openFilter,
    setOpenFilter,
    addFilterAndOpen,
    removeFilter,
    clearAllFilters,
    toggleFilterDropdown,
    getAvailableFilters,
    ignoreChipRefs,
  } = useFilterChips({
    chipRefs,
    onResetFilter: (id) => {
      if (id === "status") setSelectedStatus("all");
      if (id === "type") setSelectedType("all");
      if (id === "category") setSelectedCategory("all");
      if (id === "period") setSelectedPeriod([null, null]);
    },
  });

  /* ——— Saved views ——— */
  const viewId = searchParams.get("view");
  const currentView = viewId ? views.find((v) => v.id === viewId && v.module === "surveys") : null;
  const [saveModalOpen, setSaveModalOpen] = useState(false);
  const [viewName, setViewName] = useState("");
  const [filterBarDefaultOpen, setFilterBarDefaultOpen] = useState(isNewViewMode);

  /* ——— Load saved view filters when URL changes ——— */
  useEffect(() => {
    if (currentView) {
      const f = currentView.filters;
      setActiveFilters(f.activeFilters);
      setSelectedStatus(f.selectedStatus);
      setSelectedType(f.selectedType ?? "all");
      setSelectedCategory(f.selectedCategory ?? "all");
      setSelectedPeriod(f.selectedPeriod);
    }
  }, [viewId]); // eslint-disable-line react-hooks/exhaustive-deps

  /* ——— New view mode: clear filters and open filter popover ——— */
  useEffect(() => {
    if (isNewViewMode) {
      clearAllFilters();
      setSelectedStatus("all");
      setSelectedType("all");
      setSelectedCategory("all");
      setSelectedPeriod([null, null]);
      setFilterBarDefaultOpen(true);
      // Clear navigation state so refresh doesn't re-trigger
      window.history.replaceState({}, "");
    }
  }, [isNewViewMode]); // eslint-disable-line react-hooks/exhaustive-deps

  /* ——— Search & table state ——— */
  const [search, setSearch] = useState("");
  const [actionsPopover, setActionsPopover] = useState<string | null>(null);
  const [pauseSurvey, setPauseSurvey] = useState<SurveyListItem | null>(null);

  type SortKey = "name" | "completionRate" | "startDate";
  const {
    selectedRows,
    clearSelection,
    sortKey,
    sortDir,
    handleSort,
    getSortDirection,
    handleSelectRow,
    handleSelectAll,
  } = useDataTable<SortKey>();

  /* ——— Create modal ——— */
  const [createOpen, setCreateOpen] = useState(false);
  const [showAssistant, setShowAssistant] = useState(false);
  const [createSelectedTemplateId, setCreateSelectedTemplateId] = useState<string | null>(null);
  const [surveyName, setSurveyName] = useState("");
  const [surveyDesc, setSurveyDesc] = useState("");

  const [previewTemplateId, setPreviewTemplateId] = useState<string | null>(null);

  const createTemplates = useMemo(
    () => templates
      .filter((template) => !template.isArchived)
      .sort((a, b) => {
        if (a.isSystem !== b.isSystem) return a.isSystem ? -1 : 1;
        return a.name.localeCompare(b.name, "pt-BR");
      }),
    [templates],
  );

  const previewTemplate = useMemo<SurveyTemplate | null>(() => {
    if (!previewTemplateId) return null;
    const template = createTemplates.find((item) => item.id === previewTemplateId);
    if (!template) return null;
    return templateRecordToPreviewTemplate(template);
  }, [createTemplates, previewTemplateId]);

  const templateNameByType = useMemo(() => {
    return new Map(
      templates
        .filter((template) => !template.isArchived)
        .map((template) => [template.type, template.name]),
    );
  }, [templates]);

  /* Create modal — popover selectors */
  const [selectedOwners, setSelectedOwners] = useState<string[]>([]);
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [customTags, setCustomTags] = useState<{ id: string; label: string }[]>([]);
  const [selectedManagers, setSelectedManagers] = useState<string[]>([]);
  const [ownerPopoverOpen, setOwnerPopoverOpen] = useState(false);
  const [tagsPopoverOpen, setTagsPopoverOpen] = useState(false);
  const [managerPopoverOpen, setManagerPopoverOpen] = useState(false);
  const ownerBtnRef = useRef<HTMLButtonElement>(null);
  const tagsBtnRef = useRef<HTMLButtonElement>(null);
  const managerBtnRef = useRef<HTMLButtonElement>(null);

  /* ——— Filter handlers ——— */

  function handleClearAll() {
    clearAllFilters();
    clearRespondentFilter();
  }

  function getFilterLabel(id: string): string {
    switch (id) {
      case "status": return selectedStatus === "all" ? "Status" : `Status: ${STATUS_OPTIONS.find((o) => o.id === selectedStatus)?.label}`;
      case "type": return selectedType === "all" ? "Tipo" : `Tipo: ${TYPE_OPTIONS.find((o) => o.id === selectedType)?.label}`;
      case "category": return selectedCategory === "all" ? "Categoria" : `Categoria: ${CATEGORY_OPTIONS.find((o) => o.id === selectedCategory)?.label}`;
      case "period": {
        const [s, e] = selectedPeriod;
        if (s && e) return `Período: ${formatCalendarDate(s)} — ${formatCalendarDate(e)}`;
        return "Período";
      }
      default: return id;
    }
  }

  const filterChipIcons: Record<string, Icon> = {
    status: FunnelSimple,
    type: ListChecks,
    period: CalendarBlank,
    category: Tag,
  };

  /* ——— Saved views handlers ——— */

  function getCurrentFilters() {
    return {
      activeFilters,
      selectedTeams: [],
      selectedPeriod,
      selectedStatus,
      selectedOwners: [],
      selectedType,
      selectedCategory,
    };
  }

  function handleOpenSaveModal() {
    setViewName(currentView?.name ?? "");
    setSaveModalOpen(true);
  }

  function handleSaveView() {
    if (!viewName.trim()) return;
    if (currentView) {
      updateView(currentView.id, { name: viewName.trim(), filters: getCurrentFilters() });
      toast.success("Visualização atualizada");
    } else {
      const newId = addView({ name: viewName.trim(), module: "surveys", filters: getCurrentFilters() });
      setSearchParams({ view: newId });
      toast.success("Visualização salva");
    }
    setSaveModalOpen(false);
  }

  function handleDeleteView() {
    if (!currentView) return;
    deleteView(currentView.id);
    setSearchParams({});
    handleClearAll();
    toast.success("Visualização excluída");
  }

  function parseDate(str: string): number {
    if (!str) return 0;
    const [d = 0, m = 0, y = 0] = str.split("/").map(Number);
    return new Date(y, m - 1, d).getTime();
  }

  /* ——— Filtered data ——— */

  const filtered = useMemo(() => {
    let list = [...surveys];

    if (respondentSurveyIds) {
      list = list.filter((s) => respondentSurveyIds.has(s.id));
    }
    if (search) {
      const q = search.toLowerCase();
      list = list.filter((s) => s.name.toLowerCase().includes(q));
    }
    if (activeFilters.includes("status") && selectedStatus !== "all") {
      list = list.filter((s) => s.status === selectedStatus);
    }
    if (activeFilters.includes("type") && selectedType !== "all") {
      list = list.filter((s) => s.type === selectedType);
    }
    if (activeFilters.includes("category") && selectedCategory !== "all") {
      list = list.filter((s) => s.category === selectedCategory);
    }

    if (sortKey) {
      const dir = sortDir === "asc" ? 1 : -1;
      list.sort((a, b) => {
        switch (sortKey) {
          case "name": return dir * a.name.localeCompare(b.name);
          case "completionRate": return dir * (a.completionRate - b.completionRate);
          case "startDate": return dir * (parseDate(a.startDate) - parseDate(b.startDate));
          default: return 0;
        }
      });
    }

    return list;
  }, [surveys, search, activeFilters, selectedStatus, selectedType, selectedCategory, sortKey, sortDir]);

  const rowIds = useMemo(() => filtered.map((s) => s.id), [filtered]);

  /* ——— Indicators (derived from filtered data) ——— */

  const activeSurveys = filtered.filter((s) => s.status === "active").length;
  const avgResponseRate = useMemo(() => {
    const withResponses = filtered.filter((s) => s.status === "active" || s.status === "closed");
    if (withResponses.length === 0) return 0;
    return Math.round(withResponses.reduce((sum, s) => sum + s.completionRate, 0) / withResponses.length);
  }, [filtered]);
  const pendingSurveys = filtered.filter((s) => s.status === "scheduled" || s.status === "draft").length;
  const totalResponses = filtered.reduce((sum, s) => sum + s.totalResponses, 0);

  function handleBulkPause() {
    setSurveys((prev) =>
      prev.map((s) =>
        selectedRows.has(s.id) && s.status === "active"
          ? { ...s, status: "paused" as SurveyStatus }
          : s,
      ),
    );
    toast.success(`${selectedRows.size} pesquisa(s) pausada(s)`);
    clearSelection();
  }

  function handlePause() {
    if (!pauseSurvey) return;
    setSurveys((prev) =>
      prev.map((s) => (s.id === pauseSurvey.id ? { ...s, status: "paused" as SurveyStatus } : s)),
    );
    toast.success(`"${pauseSurvey.name}" pausada`);
    setPauseSurvey(null);
  }

  function handleResume(survey: SurveyListItem) {
    setSurveys((prev) =>
      prev.map((s) => (s.id === survey.id ? { ...s, status: "active" as SurveyStatus } : s)),
    );
    toast.success(`"${survey.name}" retomada`);
  }

  async function handleCopyLink(survey: SurveyListItem) {
    const url = `${window.location.origin}/surveys/${survey.id}/respond`;
    await navigator.clipboard.writeText(url);
    toast.success("Link copiado para a área de transferência");
  }

  function handleDuplicate(survey: SurveyListItem) {
    const duplicated = duplicateSurvey(survey.id);
    if (!duplicated) return;
    toast.success(`"${survey.name}" duplicada`);
  }

  function getRowActions(survey: SurveyListItem): PopoverItem[] {
    const items: PopoverItem[] = [];

    /* ——— Preview & share ——— */
    items.push({
      id: "preview",
      label: "Pré-visualizar",
      icon: ArrowSquareOut,
      onClick: () => window.open(`/surveys/${survey.id}/preview`, "_blank"),
    });

    const canShare = survey.status === "scheduled" || survey.status === "active" || survey.status === "paused";
    if (canShare) {
      items.push({
        id: "copy-link",
        label: "Copiar link",
        icon: Link,
        onClick: () => handleCopyLink(survey),
      });
    }

    /* ——— Core actions ——— */
    if (survey.totalResponses > 0) {
      items.push({ id: "results", label: "Ver resultados", icon: ChartLineUp, divider: true, onClick: () => navigate(`/surveys/${survey.id}/results`) });
    }

    const firstCoreItem = survey.totalResponses > 0 ? "edit" : "edit-divider";
    const editQuery = new URLSearchParams({ surveyId: survey.id });
    if (survey.templateId) {
      editQuery.set("templateId", survey.templateId);
    }

    items.push(
      {
        id: "edit",
        label: "Editar",
        icon: PencilSimple,
        divider: firstCoreItem === "edit-divider",
        onClick: () => navigate(`/surveys/new/participantes?${editQuery.toString()}`),
      },
      { id: "duplicate", label: "Duplicar", icon: Copy, onClick: () => handleDuplicate(survey) },
      { id: "download", label: "Baixar dados brutos", icon: DownloadSimple, onClick: () => toast.success("Download iniciado") },
    );

    /* ——— Status actions ——— */
    if (survey.status === "active") {
      items.push({
        id: "pause",
        label: "Pausar pesquisa",
        icon: Pause,
        divider: true,
        danger: true,
        onClick: () => setPauseSurvey(survey),
      });
    }
    if (survey.status === "paused") {
      items.push({
        id: "resume",
        label: "Retomar pesquisa",
        icon: Play,
        divider: true,
        onClick: () => handleResume(survey),
      });
    }

    return items;
  }

  /* ——— Create modal handlers ——— */

  function resetCreate() {
    setCreateOpen(false);
    setShowAssistant(false);
    setPreviewTemplateId(null);
    setCreateSelectedTemplateId(null);
    setSurveyName("");
    setSurveyDesc("");
    setSelectedOwners([]);
    setSelectedTags([]);
    setSelectedManagers([]);
    setOwnerPopoverOpen(false);
    setTagsPopoverOpen(false);
    setManagerPopoverOpen(false);
  }

  function handleNext() {
    if (!createSelectedTemplateId) return;
    const template = createTemplates.find((t) => t.id === createSelectedTemplateId);
    if (!template) return;

    resetCreate();
    navigate(`/surveys/new?templateId=${encodeURIComponent(template.id)}`, {
      state: {
        surveyType: template.type,
        category: template.category,
        templateId: template.id,
        name: surveyName,
        description: surveyDesc,
        ownerIds: selectedOwners,
        managerIds: selectedManagers,
        tagIds: selectedTags,
        cycleId: null,
      },
    });
  }

  /* ——— Completion bar color ——— */

  function getCompletionColor(rate: number): string {
    if (rate >= 80) return "var(--color-green-500)";
    if (rate >= 50) return "var(--color-yellow-500)";
    if (rate > 0) return "var(--color-red-500)";
    return "var(--color-caramel-200)";
  }

  return (
    <div className={styles.page}>
      <PageHeader title={currentView ? currentView.name : "Pesquisas"} />

      {/* ——— Main card ——— */}
      <Card padding="sm">
        <CardBody>
          <FilterBar
            key={filterBarDefaultOpen ? "open" : "default"}
            filters={getAvailableFilters(FILTER_OPTIONS)}
            onAddFilter={(id: string) => {
              setFilterBarDefaultOpen(false);
              addFilterAndOpen(id);
            }}
            onClearAll={(activeFilters.length > 0 || !!filterRespondentId) ? handleClearAll : undefined}
            onSaveView={activeFilters.length > 0 ? handleOpenSaveModal : undefined}
            saveViewLabel={currentView ? "Atualizar visualização" : "Salvar visualização"}
            defaultOpen={filterBarDefaultOpen}
            primaryAction={
              currentView ? (
                <button type="button" className={styles.deleteViewBtn} onClick={handleDeleteView} aria-label="Excluir visualização">
                  <Trash size={14} />
                  <span>Excluir</span>
                </button>
              ) : undefined
            }
          >
            {/* Chip de respondente — pré-populado via navegação do modal de Meu time */}
            {filterRespondentId && (
              <div ref={respondentChipRef} style={{ display: "inline-flex" }}>
                <FilterChip
                  label={`Respondente: ${filterRespondentName}`}
                  icon={UserCircle}
                  active={respondentDropdownOpen}
                  onClick={() => setRespondentDropdownOpen((v) => !v)}
                  onRemove={clearRespondentFilter}
                />
              </div>
            )}

            {activeFilters.map((filterId) => (
              <div key={filterId} ref={chipRefs[filterId]} style={{ display: "inline-flex" }}>
                <FilterChip
                  label={getFilterLabel(filterId)}
                  icon={filterChipIcons[filterId]}
                  active={openFilter === filterId}
                  onClick={() => toggleFilterDropdown(filterId)}
                  onRemove={() => removeFilter(filterId)}
                />
              </div>
            ))}
          </FilterBar>

          {/* Dropdown do filtro de respondente */}
          <FilterDropdown
            open={respondentDropdownOpen}
            onClose={() => setRespondentDropdownOpen(false)}
            anchorRef={respondentChipRef}
            noOverlay
          >
            <div className={styles.respondentDropdownBody}>
              {peopleOptions.map((person) => (
                <button
                  key={person.id}
                  type="button"
                  className={`${styles.filterDropdownItem} ${filterRespondentId === person.id ? styles.filterDropdownItemActive : ""}`}
                  onClick={() => selectRespondent(person.id, person.label)}
                >
                  <span>{person.label}</span>
                  {filterRespondentId === person.id && <Check size={14} className={styles.filterDropdownCheck} />}
                </button>
              ))}
            </div>
          </FilterDropdown>

          {/* Filter dropdowns */}
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
            open={openFilter === "type"}
            onClose={() => setOpenFilter(null)}
            anchorRef={typeChipRef}
            ignoreRefs={ignoreChipRefs}
            options={TYPE_OPTIONS}
            value={selectedType}
            onChange={(id) => setSelectedType(id)}
          />

          <PopoverSelect
            mode="single"
            open={openFilter === "category"}
            onClose={() => setOpenFilter(null)}
            anchorRef={categoryChipRef}
            ignoreRefs={ignoreChipRefs}
            options={CATEGORY_OPTIONS}
            value={selectedCategory}
            onChange={(id) => setSelectedCategory(id)}
          />

          <PopoverSelect
            mode="single"
            open={openFilter === "period"}
            onClose={() => setOpenFilter(null)}
            anchorRef={periodChipRef}
            ignoreRefs={ignoreChipRefs}
            options={presetPeriods}
            value={presetPeriods.find((p) =>
              selectedPeriod[0]?.year === p.start.year
              && selectedPeriod[0]?.month === p.start.month
              && selectedPeriod[0]?.day === p.start.day
              && selectedPeriod[1]?.year === p.end.year
              && selectedPeriod[1]?.month === p.end.month
              && selectedPeriod[1]?.day === p.end.day
            )?.id ?? null}
            onChange={(id) => {
              const p = presetPeriods.find((pp) => pp.id === id);
              if (p) setSelectedPeriod([p.start, p.end]);
            }}
            closeOnSelect
            footer={
              <DatePicker
                label="Personalizado"
                mode="range"
                value={selectedPeriod}
                onChange={(range: [CalendarDate | null, CalendarDate | null]) => {
                  setSelectedPeriod(range);
                  if (range[0] && range[1]) setOpenFilter(null);
                }}
              />
            }
          />

          <div className={styles.actionBar}>
            <Button variant="primary" size="md" leftIcon={Plus} onClick={() => setCreateOpen(true)}>
              Criar pesquisa
            </Button>
          </div>
        </CardBody>

        <CardDivider />

        {/* ——— Indicators ——— */}
        <CardBody>
          <div className={styles.indicatorGrid}>
            <Card padding="sm">
              <CardBody>
                <div className={styles.indicatorCard}>
                  <div className={styles.indicatorIconWrapper} style={{ background: "var(--color-green-50)", color: "var(--color-green-600)" }}>
                    <CheckCircle size={20} />
                  </div>
                  <div className={styles.indicatorContent}>
                    <span className={styles.indicatorValue}>{activeSurveys}</span>
                    <span className={styles.indicatorLabel}>Pesquisas ativas</span>
                  </div>
                </div>
              </CardBody>
            </Card>
            <Card padding="sm">
              <CardBody>
                <div className={styles.indicatorCard}>
                  <div className={styles.indicatorIconWrapper} style={{ background: "var(--color-orange-50)", color: "var(--color-orange-600)" }}>
                    <ChartLineUp size={20} />
                  </div>
                  <div className={styles.indicatorContent}>
                    <span className={styles.indicatorValue}>{avgResponseRate}%</span>
                    <span className={styles.indicatorLabel}>Taxa média de resposta</span>
                  </div>
                </div>
              </CardBody>
            </Card>
            <Card padding="sm">
              <CardBody>
                <div className={styles.indicatorCard}>
                  <div className={styles.indicatorIconWrapper} style={{ background: "var(--color-wine-50)", color: "var(--color-wine-600)" }}>
                    <ClockCountdown size={20} />
                  </div>
                  <div className={styles.indicatorContent}>
                    <span className={styles.indicatorValue}>{pendingSurveys}</span>
                    <span className={styles.indicatorLabel}>Pendentes de lançamento</span>
                  </div>
                </div>
              </CardBody>
            </Card>
            <Card padding="sm">
              <CardBody>
                <div className={styles.indicatorCard}>
                  <div className={styles.indicatorIconWrapper} style={{ background: "var(--color-caramel-100)", color: "var(--color-neutral-600)" }}>
                    <Users size={20} />
                  </div>
                  <div className={styles.indicatorContent}>
                    <span className={styles.indicatorValue}>{totalResponses.toLocaleString("pt-BR")}</span>
                    <span className={styles.indicatorLabel}>Total de respostas</span>
                  </div>
                </div>
              </CardBody>
            </Card>
          </div>
        </CardBody>

        <CardDivider />

        {/* ——— Table ——— */}
        <CardBody>
          <Table
            variant="divider"
            elevated={false}
            selectable
            selectedRows={selectedRows}
            rowIds={rowIds}
            onSelectRow={handleSelectRow}
            onSelectAll={(checked: boolean) => handleSelectAll(checked, rowIds)}
          >
          <TableCardHeader
            title="Todas as pesquisas"
            badge={<Badge color="neutral">{filtered.length}</Badge>}
            actions={
              <div className={styles.searchWrapper}>
                <Input
                  placeholder="Buscar pesquisa..."
                  value={search}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSearch(e.target.value)}
                  leftIcon={MagnifyingGlass}
                />
              </div>
            }
          />
          <TableContent>
            <TableHead>
              <TableRow>
                <TableHeaderCell isCheckbox />
                <TableHeaderCell sortable sortDirection={getSortDirection("name")} onSort={() => handleSort("name")}>Nome</TableHeaderCell>
                <TableHeaderCell>Tipo</TableHeaderCell>
                <TableHeaderCell sortable sortDirection={getSortDirection("startDate")} onSort={() => handleSort("startDate")}>Período</TableHeaderCell>
                <TableHeaderCell sortable sortDirection={getSortDirection("completionRate")} onSort={() => handleSort("completionRate")}>Respostas</TableHeaderCell>
                <TableHeaderCell>Status</TableHeaderCell>
                <TableHeaderCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {filtered.map((s) => {
                const statusConf = STATUS_CONFIG[s.status];
                return (
                  <TableRow key={s.id} rowId={s.id}>
                    <TableCell isCheckbox rowId={s.id} />
                    <TableCell>
                      <span
                        className={styles.surveyName}
                        role="link"
                        tabIndex={0}
                        onClick={() => navigate(`/surveys/${s.id}/results`)}
                        onKeyDown={(e) => { if (e.key === "Enter") navigate(`/surveys/${s.id}/results`); }}
                      >
                        {s.name}
                      </span>
                    </TableCell>
                    <TableCell>
                      <div className={styles.typeBadge}>
                        <Badge color={s.category === "ciclo" ? "wine" : "orange"} size="sm">
                          {templateNameByType.get(s.type) ?? getTypeLabel(s.type)}
                        </Badge>
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className={styles.periodText}>
                        {s.startDate && s.endDate ? `${s.startDate} — ${s.endDate}` : "-"}
                      </span>
                    </TableCell>
                    <TableCell>
                      <div className={styles.responsesCell}>
                        <span className={styles.responsesText}>
                          {s.totalResponses} / {s.totalRecipients}
                        </span>
                        <div className={styles.completionCell}>
                          <div className={styles.completionBar}>
                            <div
                              className={styles.completionFill}
                              style={{ width: `${s.completionRate}%`, background: getCompletionColor(s.completionRate) }}
                            />
                          </div>
                          <span className={styles.completionLabel}>{s.completionRate}%</span>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge color={statusConf.color} size="sm">{statusConf.label}</Badge>
                    </TableCell>
                    <TableCell>
                      <RowActionsPopover
                        className={styles.actionsField}
                        items={getRowActions(s)}
                        open={actionsPopover === s.id}
                        onToggle={() => setActionsPopover(actionsPopover === s.id ? null : s.id)}
                        onClose={() => setActionsPopover(null)}
                        buttonAriaLabel={`Abrir ações da pesquisa ${s.name}`}
                      />
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </TableContent>
          <TableBulkActions count={selectedRows.size} onClear={clearSelection}>
            <Button variant="secondary" size="md" leftIcon={Pause} onClick={handleBulkPause}>
              Pausar selecionadas
            </Button>
          </TableBulkActions>
          </Table>
        </CardBody>
      </Card>

      {/* ——— Pause confirmation ——— */}
      <Modal open={!!pauseSurvey} onClose={() => setPauseSurvey(null)} size="sm">
        <ModalHeader title="Pausar pesquisa" onClose={() => setPauseSurvey(null)} />
        <ModalBody>
          {pauseSurvey && (
            <p className={styles.confirmText}>
              Tem certeza que deseja pausar <strong>{pauseSurvey.name}</strong>?
              {pauseSurvey.totalResponses > 0 &&
                ` Esta pesquisa já possui ${pauseSurvey.totalResponses} respostas coletadas. Novos participantes não poderão responder enquanto estiver pausada.`}
            </p>
          )}
        </ModalBody>
        <ModalFooter>
          <Button variant="tertiary" size="md" onClick={() => setPauseSurvey(null)}>Cancelar</Button>
          <Button variant="danger" size="md" leftIcon={Pause} onClick={handlePause}>Pausar</Button>
        </ModalFooter>
      </Modal>

      {/* ——— Save view modal ——— */}
      <Modal open={saveModalOpen} onClose={() => setSaveModalOpen(false)} size="sm">
        <ModalHeader title={currentView ? "Atualizar visualização" : "Salvar visualização"} onClose={() => setSaveModalOpen(false)} />
        <ModalBody>
          <div className={styles.saveModalContent}>
            <Input
              label="Nome da visualização"
              value={viewName}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setViewName(e.target.value)}
              placeholder="Ex: Pesquisas ativas Q1"
            />
          </div>
        </ModalBody>
        <ModalFooter>
          <Button variant="tertiary" size="md" onClick={() => setSaveModalOpen(false)}>Cancelar</Button>
          <Button variant="primary" size="md" disabled={!viewName.trim()} onClick={handleSaveView}>
            {currentView ? "Atualizar" : "Salvar"}
          </Button>
        </ModalFooter>
      </Modal>

      {/* ——— Create survey modal ——— */}
      <Modal
        open={createOpen}
        onClose={resetCreate}
        size="lg"
        sidePanel={
          showAssistant ? (
            <AiAssistant
              onClose={() => setShowAssistant(false)}
              allowUpload
              onMessage={async () =>
                "Desculpe, ainda estou em desenvolvimento. Em breve poderei ajudá-lo!"
              }
            />
          ) : previewTemplate ? (
            <TemplatePreviewPanel
              template={previewTemplate}
              onClose={() => setPreviewTemplateId(null)}
            />
          ) : null
        }
      >
        <ModalHeader title="Criar pesquisa" onClose={resetCreate}>
            <AssistantButton
              active={showAssistant}
              onClick={() => {
                setPreviewTemplateId(null);
                setShowAssistant((v) => !v);
              }}
            />
        </ModalHeader>

        <Breadcrumb items={CREATE_STEPS} current={0} />

        <ModalBody>
          <div className={styles.createBody}>
            <div className={styles.surveyFields}>
              <input
                type="text"
                className={styles.surveyNameInput}
                placeholder="Nome da pesquisa"
                value={surveyName}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSurveyName(e.target.value)}
              />
              <input
                type="text"
                className={styles.surveyDescInput}
                placeholder="Adicionar breve descrição"
                value={surveyDesc}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSurveyDesc(e.target.value)}
              />
            </div>

            <div className={styles.surveyActions}>
              <Button
                ref={ownerBtnRef}
                variant="secondary"
                size="sm"
                leftIcon={UserCircle}
                onClick={() => setOwnerPopoverOpen((v) => !v)}
                >
                  {selectedOwners.length > 0
                  ? formatMultiLabel(selectedOwners, peopleOptions, "Responsável")
                  : "Responsável"}
              </Button>
              <Button
                ref={tagsBtnRef}
                variant="secondary"
                size="sm"
                leftIcon={Tag}
                onClick={() => setTagsPopoverOpen((v) => !v)}
              >
                {selectedTags.length > 0
                  ? formatMultiLabel(selectedTags, [...baseTagOptions, ...customTags], "Tags")
                  : "Tags"}
              </Button>
              <Button
                ref={managerBtnRef}
                variant="secondary"
                size="sm"
                leftIcon={ShieldCheck}
                onClick={() => setManagerPopoverOpen((v) => !v)}
                >
                  {selectedManagers.length > 0
                  ? formatMultiLabel(selectedManagers, peopleOptions, "Gestores da pesquisa")
                  : "Gestores da pesquisa"}
              </Button>
            </div>

            <PopoverSelect
              mode="multiple"
              open={ownerPopoverOpen}
              onClose={() => setOwnerPopoverOpen(false)}
              anchorRef={ownerBtnRef}
              options={peopleOptions}
              value={selectedOwners}
              onChange={setSelectedOwners}
              searchable
              searchPlaceholder="Buscar responsável..."
            />
            <PopoverSelect
              mode="multiple"
              open={tagsPopoverOpen}
              onClose={() => setTagsPopoverOpen(false)}
              anchorRef={tagsBtnRef}
               options={[...baseTagOptions, ...customTags]}
              value={selectedTags}
              onChange={setSelectedTags}
              renderOptionPrefix={() => <Tag size={14} />}
              searchable
              searchPlaceholder="Buscar tag..."
              creatable
              createPlaceholder="Criar nova tag..."
               onCreateOption={(label) => {
                 const created = createTag({ name: label });
                 const newTag = { id: created.id, label: created.name };
                 setCustomTags((prev) => {
                   if (prev.some((item) => item.id === newTag.id)) return prev;
                   return [...prev, newTag];
                 });
                 return newTag;
               }}
            />
            <PopoverSelect
              mode="multiple"
              open={managerPopoverOpen}
              onClose={() => setManagerPopoverOpen(false)}
              anchorRef={managerBtnRef}
              options={peopleOptions}
              value={selectedManagers}
              onChange={setSelectedManagers}
              searchable
              searchPlaceholder="Buscar gestor..."
            />

            <p className={styles.createTitle}>Escolha o seu template de pesquisa</p>

            {createTemplates.length === 0 && (
              <Alert variant="info" title="Sem templates disponíveis">
                Vá em Configurações &gt; Templates de pesquisa para criar seu primeiro template.
              </Alert>
            )}

            <div className={styles.templateGrid}>
              {createTemplates.map((template) => {
                const isSelected = createSelectedTemplateId === template.id;
                return (
                  <button
                    key={template.id}
                    type="button"
                    className={`${styles.templateCard} ${isSelected ? styles.templateCardSelected : ""}`}
                    onClick={() => setCreateSelectedTemplateId(template.id)}
                  >
                    <div className={styles.templateRadioRow}>
                      <Radio checked={isSelected} readOnly />
                      <div className={styles.templateText}>
                        <span className={styles.templateName}>{template.name}</span>
                        <span className={styles.templateDesc}>{template.subtitle}</span>
                      </div>
                    </div>
                    {template.questions.length > 0 && (
                      <span
                        className={styles.templateLink}
                        onClick={(e) => {
                          e.stopPropagation();
                          setShowAssistant(false);
                          setPreviewTemplateId(
                            previewTemplateId === template.id ? null : template.id,
                          );
                        }}
                      >
                        Visualizar modelo
                      </span>
                    )}
                  </button>
                );
              })}
            </div>
          </div>
        </ModalBody>

        <ModalFooter align="between">
          <Button variant="tertiary" size="md" onClick={resetCreate}>Cancelar</Button>
          <Button variant="primary" size="md" disabled={!createSelectedTemplateId} onClick={handleNext}>Próximo</Button>
        </ModalFooter>
      </Modal>
    </div>
  );
}
