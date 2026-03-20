"use client";

import { useMemo, useState } from "react";
import {
  Card,
  CardHeader,
  CardBody,
  GoalProgressBar,
  DropdownButton,
  Button,
  Modal,
  ModalHeader,
  ModalBody,
  Badge,
} from "@mdonangelo/bud-ds";
import type { DropdownItem } from "@mdonangelo/bud-ds";
import { ArrowsOutSimple, Users } from "@phosphor-icons/react";
import { usePeopleData } from "@/contexts/PeopleDataContext";

export interface KeyResultSummary {
  label: string;
  value: number;
  expected: number;
  target: number;
  owner: string;
  status: "on-track" | "attention" | "off-track";
}

const STATUS_BADGE = {
  "on-track": { color: "success" as const, label: "No ritmo" },
  attention: { color: "warning" as const, label: "Atenção" },
  "off-track": { color: "error" as const, label: "Atrasado" },
};

interface MissionsCardProps {
  title: string;
  value: number;
  expected: number;
  target: number;
  showTeamFilter?: boolean;
  keyResults?: KeyResultSummary[];
}

const ALL_TEAMS_OPTION: DropdownItem = { id: "all", label: "Todos os times" };

export function MissionsCard({
  title,
  value,
  expected,
  target,
  showTeamFilter,
  keyResults = [],
}: MissionsCardProps) {
  const { teamOptions } = usePeopleData();
  const [expanded, setExpanded] = useState(false);
  const [selectedTeam, setSelectedTeam] =
    useState<DropdownItem>(ALL_TEAMS_OPTION);

  // Build team dropdown items with "Todos os times" as first option
  const teams: DropdownItem[] = useMemo(
    () => [
      ALL_TEAMS_OPTION,
      ...teamOptions.map((t) => ({ id: t.id, label: t.label })),
    ],
    [teamOptions],
  );

  const action = (
    <div className="flex items-center gap-[var(--sp-2xs)]">
      {showTeamFilter && teams.length > 1 && (
        <DropdownButton
          items={teams}
          onSelect={setSelectedTeam}
          leftIcon={Users}
          variant="secondary"
          size="sm"
          searchable
          searchPlaceholder="Buscar time..."
        >
          {selectedTeam?.label ?? "Todos os times"}
        </DropdownButton>
      )}
      <Button
        variant="tertiary"
        size="sm"
        leftIcon={ArrowsOutSimple}
        aria-label="Expandir"
        onClick={() => setExpanded(true)}
      />
    </div>
  );

  return (
    <>
      <Card padding="sm">
        <CardHeader title={title} action={action} />
        <CardBody>
          <GoalProgressBar
            label=""
            value={value}
            target={target}
            expected={expected}
            formattedValue={`${value}%`}
          />
          <p className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)] mt-[var(--sp-2xs)]">
            Esperado {expected}%
          </p>
        </CardBody>
      </Card>

      <Modal open={expanded} onClose={() => setExpanded(false)} size="lg">
        <ModalHeader title={title} onClose={() => setExpanded(false)}>
          {showTeamFilter && teams.length > 1 && (
            <DropdownButton
              items={teams}
              onSelect={setSelectedTeam}
              leftIcon={Users}
              variant="secondary"
              size="sm"
              searchable
              searchPlaceholder="Buscar time..."
            >
              {selectedTeam?.label ?? "Todos os times"}
            </DropdownButton>
          )}
        </ModalHeader>
        <ModalBody>
          <div className="pb-[var(--sp-sm)] border-b border-[var(--color-caramel-200)] mb-[var(--sp-sm)]">
            <GoalProgressBar
              label="Progresso geral"
              value={value}
              target={target}
              expected={expected}
              formattedValue={`${value}%`}
            />
            <p className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)] mt-[var(--sp-2xs)]">
              Esperado {expected}%
            </p>
          </div>

          {keyResults.length > 0 && (
            <div className="flex flex-col gap-[var(--sp-sm)]">
              <p className="[font-family:var(--font-heading)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)]">
                Key Results
              </p>
              {keyResults.map((kr) => (
                <div
                  key={kr.label}
                  className="flex flex-col gap-[var(--sp-2xs)] p-[var(--sp-xs)] rounded-[var(--radius-sm)] border border-[var(--color-caramel-200)]"
                >
                  <div className="flex items-center justify-between gap-[var(--sp-xs)]">
                    <span className="[font-family:var(--font-label)] text-[var(--text-sm)] text-[var(--color-neutral-900)] flex-1 min-w-0">
                      {kr.label}
                    </span>
                    <Badge color={STATUS_BADGE[kr.status].color} size="sm">
                      {STATUS_BADGE[kr.status].label}
                    </Badge>
                  </div>
                  <GoalProgressBar
                    label=""
                    value={kr.value}
                    target={kr.target}
                    expected={kr.expected}
                    formattedValue={`${kr.value}%`}
                  />
                  <span className="[font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
                    Responsável: {kr.owner}
                  </span>
                </div>
              ))}
            </div>
          )}

          {keyResults.length === 0 && (
            <p className="[font-family:var(--font-body)] text-[var(--text-sm)] text-[var(--color-neutral-500)] text-center py-[var(--sp-lg)] px-[var(--sp-md)]">
              Nenhum Key Result encontrado para este período.
            </p>
          )}
        </ModalBody>
      </Modal>
    </>
  );
}
