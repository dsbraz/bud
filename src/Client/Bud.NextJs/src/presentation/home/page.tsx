"use client";

import { useState } from "react";
import { Fire, Plus } from "@phosphor-icons/react";

import { Badge, Button, Tooltip } from "@mdonangelo/bud-ds";
import { PageHeader } from "@/presentation/layout/page-header";

import { MissionsCard } from "./components/MissionsCard";
import { EngagementCard } from "./components/EngagementCard";
import { ActivitiesCard } from "./components/ActivitiesCard";
import { TeamHealthCard } from "./components/TeamHealthCard";
import { WidgetBuilder } from "./components/WidgetBuilder";
import { useHomeMissionReadModel } from "./hooks/useHomeMissionReadModel";
import { useUser } from "@auth0/nextjs-auth0";

export function HomeComponent() {
  const { user } = useUser();
  const greeting = getGreeting();

  const readModel = useHomeMissionReadModel();

  const [widgetBuilderOpen, setWidgetBuilderOpen] = useState(false);

  return (
    <div className="flex flex-col gap-6 w-full">
      <PageHeader title={`${greeting}, ${user?.name}!`}>
        <Tooltip content="Check-in semanal: 7 semanas consecutivas. Próximo badge: 12 semanas (faltam 5)">
          <Badge color="orange" size="sm" leftIcon={Fire}>
            7 sem.
          </Badge>
        </Tooltip>
      </PageHeader>

      <div className="grid grid-cols-[repeat(auto-fit,minmax(340px,1fr))] gap-[var(--sp-2xs)] items-start md:grid-cols-[repeat(auto-fit,minmax(280px,1fr))]">
        <div className="flex flex-col gap-[var(--sp-2xs)]">
          <MissionsCard
            title={readModel.annual.label}
            value={readModel.annual.value}
            expected={readModel.annual.expected}
            target={readModel.annual.target}
            keyResults={readModel.annual.keyResults}
          />
          <MissionsCard
            title={readModel.quarter.label}
            value={readModel.quarter.value}
            expected={readModel.quarter.expected}
            target={readModel.quarter.target}
            keyResults={readModel.quarter.keyResults}
            showTeamFilter
          />
          <EngagementCard />
          <Button
            variant="secondary"
            size="sm"
            leftIcon={Plus}
            onClick={() => setWidgetBuilderOpen(true)}
          >
            Adicionar widget
          </Button>
        </div>

        <div className="flex flex-col gap-[var(--sp-2xs)]">
          <ActivitiesCard activities={readModel.activities} />
          <TeamHealthCard />
        </div>
      </div>

      <WidgetBuilder
        open={widgetBuilderOpen}
        onClose={() => setWidgetBuilderOpen(false)}
      />
    </div>
  );
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return "Bom dia";
  if (hour < 18) return "Boa tarde";
  return "Boa noite";
}
