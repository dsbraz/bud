"use client";

import { useState, useMemo } from "react";
import {
  Card,
  CardHeader,
  CardBody,
  DropdownButton,
  Button,
  Chart,
  Modal,
  ModalHeader,
  ModalBody,
  Badge,
  GoalProgressBar,
  AvatarLabelGroup,
  ChartTooltipContent,
} from "@mdonangelo/bud-ds";
import type { DropdownItem } from "@mdonangelo/bud-ds";
import {
  TrendUp,
  TrendDown,
  Users,
  ArrowsOutSimple,
} from "@phosphor-icons/react";
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from "recharts";
import { useEngagementReadModel } from "../hooks/useEngagementReadModel";

export function EngagementCard() {
  const { metrics, teamMembers, teamOptions, weeklyData } =
    useEngagementReadModel();
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
    if (selectedTeamId === "all") return teamMembers;
    // In real implementation, would filter by team membership
    return teamMembers;
  }, [teamMembers, selectedTeamId]);

  const TrendIcon = metrics.trendDirection === "up" ? TrendUp : TrendDown;

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
        <CardHeader title="Engajamento" action={action} />
        <CardBody>
          <div className="flex flex-col items-center gap-[var(--sp-xs)]">
            <Chart value={metrics.overall} variant="half" size={120} />
            <div className="flex items-center gap-[var(--sp-3xs)] text-[var(--color-green-700)]">
              <span className="[font-family:var(--font-label)] text-[var(--text-xs)] font-semibold text-[var(--color-neutral-900)]">
                Engajamento {metrics.trendDirection === "up" ? "subiu" : "caiu"}{" "}
                {metrics.trend}% essa semana
              </span>
              <TrendIcon size={16} />
            </div>
            <p className="[font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-500)] text-center max-w-[280px]">
              Para melhorar, apoie o time a atualizar as missões e responder os
              formulários
            </p>
          </div>
        </CardBody>
      </Card>

      <Modal open={expanded} onClose={() => setExpanded(false)} size="lg">
        <ModalHeader title="Engajamento" onClose={() => setExpanded(false)}>
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
          <div className="grid grid-cols-3 max-[480px]:grid-cols-1 gap-[var(--sp-sm)] max-[480px]:gap-[var(--sp-xs)] pb-[var(--sp-sm)] border-b border-[var(--color-caramel-200)]">
            <div className="flex flex-col items-center gap-[var(--sp-2xs)]">
              <Chart value={metrics.overall} variant="half" size={100} />
              <span className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
                Engajamento geral
              </span>
            </div>
            <div className="flex flex-col items-center gap-[var(--sp-2xs)]">
              <Chart
                value={metrics.missionsUpdated}
                variant="half"
                size={100}
              />
              <span className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
                Missões atualizadas
              </span>
            </div>
            <div className="flex flex-col items-center gap-[var(--sp-2xs)]">
              <Chart
                value={metrics.surveyParticipation}
                variant="half"
                size={100}
              />
              <span className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
                Participação em pesquisas
              </span>
            </div>
          </div>

          <div className="mt-[var(--sp-sm)] pb-[var(--sp-sm)] border-b border-[var(--color-caramel-200)]">
            <p className="flex items-center gap-[var(--sp-2xs)] [font-family:var(--font-heading)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)] mb-[var(--sp-xs)]">
              Evolução semanal
            </p>
            <div className="w-full">
              <ResponsiveContainer width="100%" height={240}>
                <LineChart data={weeklyData}>
                  <CartesianGrid
                    stroke="var(--color-caramel-200)"
                    strokeDasharray="3 3"
                    vertical={false}
                  />
                  <XAxis
                    dataKey="week"
                    tick={{
                      fontFamily: "var(--font-label)",
                      fontSize: 12,
                      fill: "var(--color-neutral-500)",
                    }}
                    axisLine={false}
                    tickLine={false}
                  />
                  <YAxis
                    domain={[0, 100]}
                    tick={{
                      fontFamily: "var(--font-label)",
                      fontSize: 12,
                      fill: "var(--color-neutral-500)",
                    }}
                    axisLine={false}
                    tickLine={false}
                    tickFormatter={(v) => `${v}%`}
                  />
                  <Tooltip
                    content={
                      <ChartTooltipContent valueFormatter={(v) => `${v}%`} />
                    }
                    animationDuration={150}
                    animationEasing="ease-out"
                  />
                  <Line
                    type="monotone"
                    dataKey="engajamento"
                    name="Engajamento"
                    stroke="var(--color-chart-1)"
                    strokeWidth={2}
                    dot={false}
                  />
                  <Line
                    type="monotone"
                    dataKey="missoes"
                    name="Missões"
                    stroke="var(--color-chart-2)"
                    strokeWidth={2}
                    dot={false}
                  />
                  <Line
                    type="monotone"
                    dataKey="pulso"
                    name="Pulso"
                    stroke="var(--color-chart-3)"
                    strokeWidth={2}
                    dot={false}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div className="mt-[var(--sp-sm)] flex flex-col gap-[var(--sp-2xs)]">
            <p className="flex items-center gap-[var(--sp-2xs)] [font-family:var(--font-heading)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)] mb-[var(--sp-xs)]">
              Detalhamento por pessoa
              <Badge color="neutral" size="sm">
                {filteredMembers.length} pessoas
              </Badge>
            </p>
            {filteredMembers.map((person) => (
              <div
                key={person.id}
                className="flex flex-col gap-[var(--sp-2xs)] p-[var(--sp-xs)] rounded-[var(--radius-sm)] border border-[var(--color-caramel-200)]"
              >
                <div className="flex items-center justify-between gap-[var(--sp-xs)]">
                  <AvatarLabelGroup
                    size="sm"
                    initials={person.initials}
                    name={person.name}
                    supportingText={person.role}
                  />
                  <Badge
                    color={person.trendDirection === "up" ? "success" : "error"}
                    size="sm"
                    rightIcon={
                      person.trendDirection === "up" ? TrendUp : TrendDown
                    }
                  >
                    {person.trendDirection === "up" ? "+" : "-"}
                    {person.trend}%
                  </Badge>
                </div>
                <GoalProgressBar
                  label=""
                  value={person.value}
                  target={100}
                  expected={70}
                  formattedValue={`${person.value}%`}
                />
              </div>
            ))}
          </div>
        </ModalBody>
      </Modal>
    </>
  );
}
