import { useState, useMemo, useRef, useCallback, type ChangeEvent } from "react";
import { MagnifyingGlass } from "@phosphor-icons/react";
import {
  Select,
  Toggle,
  Checkbox,
  Alert,
  Badge,
  Input,
  AvatarLabelGroup,
  FilterBar,
  FilterChip,
} from "@mdonangelo/bud-ds";
import { PopoverSelect } from "@/components/PopoverSelect";
import { usePeopleData } from "@/contexts/PeopleDataContext";
import { useWizard } from "../SurveyWizardContext";
import type { ScopeType, EvaluationPerspective } from "@/types/survey";
import styles from "./StepParticipants.module.css";

interface ParticipantPerson {
  id: string;
  name: string;
  role: string;
  teamIds: string[];
  teamLabels: string[];
  initials: string;
}

const SCOPE_OPTIONS = [
  { value: "company", label: "Toda a empresa" },
  { value: "team", label: "Time" },
  { value: "individual", label: "Individual" },
];

const PERSPECTIVES: {
  key: EvaluationPerspective;
  label: string;
  description: string;
}[] = [
  {
    key: "self",
    label: "Autoavaliação",
    description: "Cada colaborador irá fazer uma avaliação de si próprio",
  },
  {
    key: "manager",
    label: "Avaliação do gestor",
    description: "Gestor irá avaliar quem está diretamente abaixo",
  },
  {
    key: "peers",
    label: "Avaliação dos pares",
    description: "Defina o modelo de seleção de pares",
  },
  {
    key: "reports",
    label: "Upward (liderados)",
    description: "Liderados irão avaliar os seus gestores diretos",
  },
];

const FILTER_DEFS = [
  { id: "team", label: "Time" },
  { id: "status", label: "Status" },
];

const STATUS_FILTER_OPTIONS = [
  { id: "all", label: "Todos" },
  { id: "included", label: "Incluídos" },
  { id: "excluded", label: "Removidos" },
];

export function StepParticipants() {
  const { state, dispatch, isCiclo, participantCount } = useWizard();
  const {
    users,
    teamOptions,
    resolveTeamId,
    resolveUserId,
    getTeamNameById,
  } = usePeopleData();
  const [search, setSearch] = useState("");

  /* Filters */
  const [activeFilters, setActiveFilters] = useState<string[]>([]);
  const [openFilter, setOpenFilter] = useState<string | null>(null);
  const [filterTeam, setFilterTeam] = useState<string>("all");
  const [filterStatus, setFilterStatus] = useState<"all" | "included" | "excluded">("all");
  const teamChipRef = useRef<HTMLDivElement>(null);
  const statusChipRef = useRef<HTMLDivElement>(null);
  const ignoreChipRefs = useMemo(() => [teamChipRef, statusChipRef], []);

  const handleAddFilter = useCallback((id: string) => {
    setActiveFilters((prev) => [...prev, id]);
    requestAnimationFrame(() => setOpenFilter(id));
  }, []);

  function removeFilter(id: string) {
    setActiveFilters((prev) => prev.filter((f) => f !== id));
    if (id === "team") setFilterTeam("all");
    if (id === "status") setFilterStatus("all");
    setOpenFilter(null);
  }

  function clearAllFilters() {
    setActiveFilters([]);
    setFilterTeam("all");
    setFilterStatus("all");
    setOpenFilter(null);
  }

  const showLgpdWarning =
    state.isAnonymous && participantCount < state.lgpdMinGroupSize && participantCount > 0;

  const teamIdByLabel = useMemo(
    () => new Map(teamOptions.map((option) => [option.label, option.id])),
    [teamOptions],
  );
  const teamLabelById = useMemo(
    () => new Map(teamOptions.map((option) => [option.id, option.label])),
    [teamOptions],
  );

  const allPeople = useMemo<ParticipantPerson[]>(() => {
    return users
      .filter((user) => user.status === "active" || user.status === "invited")
      .map((user) => {
        const teamLabels = user.teams;
        const teamIds = teamLabels
          .map((label) => teamIdByLabel.get(label))
          .filter((id): id is string => !!id);

        return {
          id: user.id,
          name: `${user.firstName} ${user.lastName}`,
          role: user.jobTitle ?? "Sem cargo",
          teamIds,
          teamLabels,
          initials: user.initials ?? `${user.firstName[0] ?? ""}${user.lastName[0] ?? ""}`.toUpperCase(),
        };
      });
  }, [users, teamIdByLabel]);

  const selectedScopeTeamIds = useMemo(
    () => Array.from(new Set(state.scope.teamIds.map((teamId) => resolveTeamId(teamId)))),
    [state.scope.teamIds, resolveTeamId],
  );

  const excludedUserIdSet = useMemo(
    () => new Set(state.excludedUserIds.map((userId) => resolveUserId(userId))),
    [state.excludedUserIds, resolveUserId],
  );

  const scopeTeamOptions = useMemo(
    () => teamOptions.map((option) => ({ value: option.id, label: option.label })),
    [teamOptions],
  );

  const scopedPeople = useMemo(() => {
    if (state.scope.scopeType === "company") return allPeople;
    if (state.scope.scopeType === "team") {
      if (selectedScopeTeamIds.length === 0) return allPeople;
      const selectedTeamSet = new Set(selectedScopeTeamIds);
      return allPeople.filter((person) => person.teamIds.some((teamId) => selectedTeamSet.has(teamId)));
    }
    if (state.scope.scopeType === "individual" && state.scope.userIds.length > 0) {
      const scopedUserIds = new Set(state.scope.userIds.map((userId) => resolveUserId(userId)));
      return allPeople.filter((person) => scopedUserIds.has(person.id));
    }
    return allPeople;
  }, [allPeople, selectedScopeTeamIds, state.scope.scopeType, state.scope.userIds, resolveUserId]);

  const filteredPeople = useMemo(() => {
    let list = scopedPeople;

    if (search.trim()) {
      const q = search.toLowerCase();
      list = list.filter(
        (p) =>
          p.name.toLowerCase().includes(q) ||
          p.role.toLowerCase().includes(q) ||
          p.teamLabels.some((teamLabel) => teamLabel.toLowerCase().includes(q)),
      );
    }

    if (filterTeam !== "all") {
      list = list.filter((person) => person.teamIds.includes(filterTeam));
    }

    if (filterStatus === "included") {
      list = list.filter((person) => !excludedUserIdSet.has(person.id));
    } else if (filterStatus === "excluded") {
      list = list.filter((person) => excludedUserIdSet.has(person.id));
    }

    return list;
  }, [scopedPeople, search, filterTeam, filterStatus, excludedUserIdSet]);

  const uniqueTeamIds = useMemo(
    () => Array.from(new Set(scopedPeople.flatMap((person) => person.teamIds))).sort((a, b) => {
      const labelA = teamLabelById.get(a) ?? getTeamNameById(a) ?? a;
      const labelB = teamLabelById.get(b) ?? getTeamNameById(b) ?? b;
      return labelA.localeCompare(labelB, "pt-BR");
    }),
    [scopedPeople, teamLabelById, getTeamNameById],
  );

  const teamFilterOptions = useMemo(
    () => [
      { id: "all", label: "Todos os times" },
      ...uniqueTeamIds.map((teamId) => ({
        id: teamId,
        label: teamLabelById.get(teamId) ?? getTeamNameById(teamId) ?? teamId,
      })),
    ],
    [uniqueTeamIds, teamLabelById, getTeamNameById],
  );

  const excludedInScope = Array.from(excludedUserIdSet).filter((excludedId) =>
    scopedPeople.some((person) => person.id === excludedId),
  );
  const selectedCount = scopedPeople.length - excludedInScope.length;
  const allVisibleSelected = filteredPeople.length > 0 && filteredPeople.every(
    (person) => !excludedUserIdSet.has(person.id),
  );

  const teamChipLabel = filterTeam === "all"
    ? "Time"
    : `Time: ${teamLabelById.get(filterTeam) ?? getTeamNameById(filterTeam) ?? filterTeam}`;
  const statusChipLabel =
    filterStatus === "all" ? "Status" : filterStatus === "included" ? "Status: Incluídos" : "Status: Removidos";

  function handleTogglePerson(id: string) {
    dispatch({ type: "TOGGLE_USER_EXCLUSION", payload: resolveUserId(id) });
  }

  function handleToggleAll() {
    if (allVisibleSelected) {
      const newExcluded = new Set([...excludedUserIdSet, ...filteredPeople.map((person) => person.id)]);
      dispatch({ type: "SET_EXCLUDED_USERS", payload: Array.from(newExcluded) });
    } else {
      const visibleIds = new Set(filteredPeople.map((person) => person.id));
      dispatch({
        type: "SET_EXCLUDED_USERS",
        payload: Array.from(excludedUserIdSet).filter((id) => !visibleIds.has(id)),
      });
    }
  }

  function togglePerspective(perspective: EvaluationPerspective) {
    const current = state.perspectives.find((p) => p.perspective === perspective);
    dispatch({
      type: "SET_PERSPECTIVE_CONFIG",
      payload: {
        perspective,
        config: { enabled: !current?.enabled },
      },
    });
  }

  return (
    <div className={styles.step}>
      <h2 className={styles.pageTitle}>Participantes</h2>

      {/* ——— Part 1: Quem será avaliado? ——— */}
      <section className={styles.section}>
        <h3 className={styles.sectionTitle}>
          Parte 1: Quem será avaliado?
        </h3>

        <Select
          label="Escopo dos avaliados"
          options={SCOPE_OPTIONS}
          value={state.scope.scopeType}
          onChange={(val: string) =>
            dispatch({ type: "SET_SCOPE_TYPE", payload: val as ScopeType })
          }
        />

        {state.scope.scopeType === "team" && (
          <Select
            label="Times"
            options={scopeTeamOptions}
            multiple
            searchable
            value={selectedScopeTeamIds}
            onChange={(val: string | string[]) => {
              const newIds = Array.from(new Set((Array.isArray(val) ? val : [val]).map((teamId) => resolveTeamId(teamId))));
              const currentIds = selectedScopeTeamIds;
              const added = newIds.filter((id) => !currentIds.includes(id));
              const removed = currentIds.filter((id) => !newIds.includes(id));
              for (const id of added) dispatch({ type: "ADD_TEAM", payload: id });
              for (const id of removed) dispatch({ type: "REMOVE_TEAM", payload: id });
            }}
          />
        )}

        {/* People list */}
        <div className={styles.peopleCard}>
          <div className={styles.peopleHeader}>
            <div className={styles.peopleHeaderTop}>
              <div className={styles.peopleHeaderLeft}>
                <Checkbox
                  checked={allVisibleSelected}
                  indeterminate={!allVisibleSelected && excludedInScope.length < scopedPeople.length}
                  onChange={handleToggleAll}
                />
                <span className={styles.peopleCount}>
                  <strong>{selectedCount}</strong> de {scopedPeople.length} selecionados
                </span>
                {excludedInScope.length > 0 && (
                  <Badge color="orange" size="sm">
                    {excludedInScope.length} removido{excludedInScope.length > 1 ? "s" : ""}
                  </Badge>
                )}
              </div>
              <div className={styles.peopleSearch}>
                <Input
                  size="sm"
                  leftIcon={MagnifyingGlass}
                  placeholder="Buscar..."
                  value={search}
                  onChange={(e: ChangeEvent<HTMLInputElement>) => setSearch(e.target.value)}
                />
              </div>
            </div>

            <FilterBar
              filters={FILTER_DEFS.filter((f) => !activeFilters.includes(f.id))}
              onAddFilter={handleAddFilter}
              onClearAll={activeFilters.length > 0 ? clearAllFilters : undefined}
            >
              {activeFilters.includes("team") && (
                <div ref={teamChipRef} style={{ display: "inline-flex" }}>
                  <FilterChip
                    label={teamChipLabel}
                    active={openFilter === "team"}
                    onClick={() => setOpenFilter(openFilter === "team" ? null : "team")}
                    onRemove={() => removeFilter("team")}
                  />
                </div>
              )}
              {activeFilters.includes("status") && (
                <div ref={statusChipRef} style={{ display: "inline-flex" }}>
                  <FilterChip
                    label={statusChipLabel}
                    active={openFilter === "status"}
                    onClick={() => setOpenFilter(openFilter === "status" ? null : "status")}
                    onRemove={() => removeFilter("status")}
                  />
                </div>
              )}
            </FilterBar>

            <PopoverSelect
              mode="single"
              open={openFilter === "team"}
              onClose={() => setOpenFilter(null)}
              anchorRef={teamChipRef}
              ignoreRefs={ignoreChipRefs}
              options={teamFilterOptions}
              value={filterTeam}
              onChange={(id) => setFilterTeam(id)}
            />

            <PopoverSelect
              mode="single"
              open={openFilter === "status"}
              onClose={() => setOpenFilter(null)}
              anchorRef={statusChipRef}
              ignoreRefs={ignoreChipRefs}
              options={STATUS_FILTER_OPTIONS}
              value={filterStatus}
              onChange={(id) => setFilterStatus(id as "all" | "included" | "excluded")}
            />
          </div>

          <div className={styles.peopleList}>
            {filteredPeople.map((person) => {
              const isIncluded = !excludedUserIdSet.has(person.id);
              const teamText = person.teamLabels.length > 0 ? person.teamLabels.join(", ") : "Sem time";
              return (
                <label
                  key={person.id}
                  className={`${styles.personRow} ${!isIncluded ? styles.personExcluded : ""}`}
                >
                  <Checkbox
                    checked={isIncluded}
                    onChange={() => handleTogglePerson(person.id)}
                  />
                  <AvatarLabelGroup
                    name={person.name}
                    supportingText={`${person.role} · ${teamText}`}
                    size="sm"
                    initials={person.initials}
                  />
                </label>
              );
            })}
            {filteredPeople.length === 0 && (
              <p className={styles.emptySearch}>Nenhuma pessoa encontrada.</p>
            )}
          </div>
        </div>
      </section>

      {/* ——— Part 2: Quem avalia? (ciclo only) ——— */}
      {isCiclo && (
        <>
          <hr className={styles.divider} />

          <section className={styles.section}>
            <h3 className={styles.sectionTitle}>
              Parte 2: Quem avalia cada pessoa?
            </h3>

            <Toggle
              label="Anonimato"
              description="Minimo 5 respondentes para garantir anonimato (LGPD)"
              checked={state.isAnonymous}
              onChange={() => dispatch({ type: "SET_ANONYMOUS", payload: !state.isAnonymous })}
            />

            <div className={styles.perspectivesGrid}>
              {PERSPECTIVES.map((p) => {
                const persp = state.perspectives.find((sp) => sp.perspective === p.key);
                return (
                  <Checkbox
                    key={p.key}
                    label={p.label}
                    description={p.description}
                    checked={persp?.enabled ?? false}
                    onChange={() => togglePerspective(p.key)}
                  />
                );
              })}
            </div>
          </section>
        </>
      )}

      {/* ——— Exclusões ——— */}
      <hr className={styles.divider} />

      <section className={styles.section}>
        <h3 className={styles.sectionTitle}>
          {isCiclo ? "Parte 3: Exclusões do ciclo" : "Exclusões da pesquisa"}
        </h3>

        <div className={styles.toggleList}>
          <Toggle
            label="Período de experiência CLT"
            description="Exclui colaboradores em período de experiência (< 90 dias na empresa)"
            checked={state.excludeTrialPeriod}
            onChange={() => dispatch({ type: "TOGGLE_EXCLUSION", payload: "trialPeriod" })}
          />
          <Toggle
            label="Colaboradores em licença"
            description="Exclui colaboradores em licença paternidade, maternidade, psicológica, etc"
            checked={state.excludeLeave}
            onChange={() => dispatch({ type: "TOGGLE_EXCLUSION", payload: "leave" })}
          />
          <Toggle
            label="Férias programadas no período"
            description="Exclui quem estará de férias durante a pesquisa"
            checked={state.excludeVacation}
            onChange={() => dispatch({ type: "TOGGLE_EXCLUSION", payload: "vacation" })}
          />
        </div>
      </section>

      {/* ——— LGPD warning ——— */}
      {showLgpdWarning && (
        <Alert variant="warning" title="Grupo pequeno para anonimato">
          Para garantir anonimato conforme LGPD, o grupo mínimo recomendado é de{" "}
          {state.lgpdMinGroupSize} participantes. O grupo atual tem ~{participantCount}.
        </Alert>
      )}
    </div>
  );
}
