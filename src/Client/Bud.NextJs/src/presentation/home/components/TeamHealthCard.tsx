"use client";

import { useState, useMemo } from "react";
import {
  Card,
  CardHeader,
  CardBody,
  Badge,
  Button,
  Avatar,
  Modal,
  ModalHeader,
  ModalBody,
  GoalProgressBar,
  DropdownButton,
} from "@mdonangelo/bud-ds";
import type { DropdownItem } from "@mdonangelo/bud-ds";
import {
  CheckCircle,
  WarningCircle,
  XCircle,
  Users,
  ArrowsOutSimple,
  CaretRight,
} from "@phosphor-icons/react";
import {
  useTeamHealthReadModel,
  type TeamMemberHealth,
} from "../hooks/useTeamHealthReadModel";

interface HealthSummary {
  count: number;
  label: string;
  status: "good" | "attention" | "critical";
}

const statusIcon = {
  good: CheckCircle,
  attention: WarningCircle,
  critical: XCircle,
};

const statusBadgeColor = {
  good: "success" as const,
  attention: "warning" as const,
  critical: "error" as const,
};

const statusBadgeLabel = {
  good: "Ok",
  attention: "Atenção",
  critical: "Urgente",
};

const summaryBgClass = {
  good: "bg-[var(--color-green-50)]",
  attention: "bg-[var(--color-yellow-50)]",
  critical: "bg-[var(--color-red-50)]",
};

const summaryIconClass = {
  good: "text-[var(--color-green-600)]",
  attention: "text-[var(--color-yellow-600)]",
  critical: "text-[var(--color-red-600)]",
};

const summaryCountClass = {
  good: "text-[var(--color-green-800)]",
  attention: "text-[var(--color-yellow-800)]",
  critical: "text-[var(--color-red-800)]",
};

function SummaryCards({ data }: { data: HealthSummary[] }) {
  return (
    <div className="grid grid-cols-3 max-[480px]:grid-cols-1 gap-[var(--sp-2xs)] mb-[var(--sp-sm)]">
      {data.map((s) => {
        const Icon = statusIcon[s.status];
        return (
          <div
            key={s.status}
            className={`flex flex-col gap-[var(--sp-2xs)] p-[var(--sp-xs)] rounded-[var(--radius-sm)] border border-[var(--color-caramel-300)] ${summaryBgClass[s.status]}`}
          >
            <div className="flex items-center gap-[var(--sp-2xs)]">
              <Icon size={20} className={summaryIconClass[s.status]} />
              <span
                className={`[font-family:var(--font-heading)] text-[var(--text-lg)] font-semibold leading-[1.2] ${summaryCountClass[s.status]}`}
              >
                {s.count}
              </span>
            </div>
            <span className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-900)]">
              {s.label}
            </span>
          </div>
        );
      })}
    </div>
  );
}

function MemberRow({ member }: { member: TeamMemberHealth }) {
  const detail = `Missões: ${member.missions}% · Check-in: ${member.lastCheckinLabel}`;

  return (
    <li className="flex items-center gap-[var(--sp-xs)] py-[var(--sp-xs)] border-b border-[var(--color-caramel-200)] cursor-pointer transition-colors duration-[120ms] last:border-b-0 hover:bg-[var(--color-caramel-50)]">
      <Avatar initials={member.initials} size="sm" />
      <div className="flex-1 min-w-0 flex flex-col gap-[2px]">
        <span className="[font-family:var(--font-label)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)]">
          {member.name}
        </span>
        <span className="[font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
          {detail}
        </span>
      </div>
      <Badge color={statusBadgeColor[member.status]} size="sm">
        {statusBadgeLabel[member.status]}
      </Badge>
      <CaretRight
        size={16}
        className="shrink-0 text-[var(--color-neutral-400)]"
      />
    </li>
  );
}

export function TeamHealthCard() {
  const { summary, members, teamOptions } = useTeamHealthReadModel();
  const [expanded, setExpanded] = useState(false);
  const [selectedTeamId, setSelectedTeamId] = useState("all");

  // Convert teamOptions to DropdownItem format
  const teams: DropdownItem[] = useMemo(
    () => teamOptions.map((t) => ({ id: t.id, label: t.label })),
    [teamOptions],
  );

  const selectedTeam = teams.find((t) => t.id === selectedTeamId) ?? teams[0];

  // Filter members by team (if not "all")
  const filteredMembers = useMemo(() => {
    if (selectedTeamId === "all") return members;
    // In real implementation, would filter by team membership
    return members;
  }, [members, selectedTeamId]);

  // Build summaries from real data
  const summaries: HealthSummary[] = useMemo(
    () => [
      {
        count: summary.good,
        label: "Boa performance",
        status: "good" as const,
      },
      {
        count: summary.attention,
        label: "Demandam atenção",
        status: "attention" as const,
      },
      {
        count: summary.critical,
        label: "Em estado crítico",
        status: "critical" as const,
      },
    ],
    [summary],
  );

  // Preview: show first 4 members
  const previewMembers = filteredMembers.slice(0, 4);

  const action = (
    <div className="flex items-center gap-[var(--sp-2xs)]">
      <DropdownButton
        items={teams}
        onSelect={(item) => setSelectedTeamId(item.id)}
        leftIcon={Users}
        variant="secondary"
        size="sm"
        searchable
        searchPlaceholder="Buscar time..."
      >
        {selectedTeam?.label}
      </DropdownButton>
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
        <CardHeader title="Saúde do time" action={action} />
        <CardBody>
          <SummaryCards data={summaries} />
          <ul className="list-none flex flex-col">
            {previewMembers.map((member) => (
              <MemberRow key={member.id} member={member} />
            ))}
          </ul>
        </CardBody>
      </Card>

      <Modal open={expanded} onClose={() => setExpanded(false)} size="lg">
        <ModalHeader title="Saúde do time" onClose={() => setExpanded(false)}>
          <DropdownButton
            items={teams}
            onSelect={(item) => setSelectedTeamId(item.id)}
            leftIcon={Users}
            variant="secondary"
            size="sm"
            searchable
            searchPlaceholder="Buscar time..."
          >
            {selectedTeam?.label}
          </DropdownButton>
        </ModalHeader>
        <ModalBody>
          <SummaryCards data={summaries} />

          {filteredMembers.filter((m) => m.status === "critical").length >
            0 && (
            <div className="mt-[var(--sp-sm)]">
              <p className="[font-family:var(--font-heading)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)] mb-[var(--sp-2xs)]">
                Em estado crítico
              </p>
              {filteredMembers
                .filter((m) => m.status === "critical")
                .map((member) => (
                  <div
                    key={member.id}
                    className="flex flex-col gap-[var(--sp-2xs)] p-[var(--sp-xs)] rounded-[var(--radius-sm)] border border-[var(--color-caramel-200)] mb-[var(--sp-2xs)]"
                  >
                    <div className="flex items-center gap-[var(--sp-xs)]">
                      <Avatar initials={member.initials} size="sm" />
                      <div className="flex-1 min-w-0 flex flex-col gap-[2px]">
                        <span className="[font-family:var(--font-label)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)]">
                          {member.name}
                        </span>
                        <span className="[font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
                          Último check-in: {member.lastCheckinLabel}
                        </span>
                      </div>
                      <Badge color="error" size="sm">
                        Urgente
                      </Badge>
                    </div>
                    <GoalProgressBar
                      label="Progresso das missões"
                      value={member.missions}
                      target={100}
                      expected={member.missionsExpected}
                      formattedValue={`${member.missions}%`}
                    />
                  </div>
                ))}
            </div>
          )}

          {filteredMembers.filter((m) => m.status === "attention").length >
            0 && (
            <div className="mt-[var(--sp-sm)]">
              <p className="[font-family:var(--font-heading)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)] mb-[var(--sp-2xs)]">
                Demandam atenção
              </p>
              {filteredMembers
                .filter((m) => m.status === "attention")
                .map((member) => (
                  <div
                    key={member.id}
                    className="flex flex-col gap-[var(--sp-2xs)] p-[var(--sp-xs)] rounded-[var(--radius-sm)] border border-[var(--color-caramel-200)] mb-[var(--sp-2xs)]"
                  >
                    <div className="flex items-center gap-[var(--sp-xs)]">
                      <Avatar initials={member.initials} size="sm" />
                      <div className="flex-1 min-w-0 flex flex-col gap-[2px]">
                        <span className="[font-family:var(--font-label)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)]">
                          {member.name}
                        </span>
                        <span className="[font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
                          Último check-in: {member.lastCheckinLabel}
                        </span>
                      </div>
                      <Badge color="warning" size="sm">
                        Atenção
                      </Badge>
                    </div>
                    <GoalProgressBar
                      label="Progresso das missões"
                      value={member.missions}
                      target={100}
                      expected={member.missionsExpected}
                      formattedValue={`${member.missions}%`}
                    />
                  </div>
                ))}
            </div>
          )}

          {filteredMembers.filter((m) => m.status === "good").length > 0 && (
            <div className="mt-[var(--sp-sm)]">
              <p className="[font-family:var(--font-heading)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)] mb-[var(--sp-2xs)]">
                Boa performance
              </p>
              <ul className="list-none flex flex-col">
                {filteredMembers
                  .filter((m) => m.status === "good")
                  .map((member) => (
                    <MemberRow key={member.id} member={member} />
                  ))}
              </ul>
            </div>
          )}
        </ModalBody>
      </Modal>
    </>
  );
}
