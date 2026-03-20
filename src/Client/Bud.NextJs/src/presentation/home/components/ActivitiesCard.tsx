"use client";

import { useState } from "react";
import {
  Table,
  ChartDonut,
  ChatCircleDots,
  Lightning,
  CaretRight,
  ArrowsOutSimple,
  UserCircle,
  CalendarCheck,
  Megaphone,
} from "@phosphor-icons/react";

import {
  Card,
  CardHeader,
  CardBody,
  Badge,
  Button,
  Modal,
  ModalHeader,
  ModalBody,
} from "@mdonangelo/bud-ds";

interface Activity {
  icon: React.ComponentType<{ size?: number }>;
  title: string;
  subtitle: string;
  urgent?: boolean;
  category: string;
}

export interface ActivityInput {
  title: string;
  subtitle: string;
  urgent?: boolean;
  category: string;
}

const baseActivities: ActivityInput[] = [
  {
    title: "Responder formulário: Retrospectiva Semanal",
    subtitle: "Data limite para responder: 10/03/2026",
    category: "Pesquisas",
  },
  {
    title: "Atualizar missão: Reduzir churn do produto de Crédito Imobiliário",
    subtitle: "Última atualização 15/01/2026",
    urgent: true,
    category: "Missões",
  },
  {
    title: "Comentar nos check-ins da semana",
    subtitle: "O seu time atualizou 23 missões essa semana",
    category: "Check-ins",
  },
  {
    title: "Assistente Bud A.I: relatório de insights da semana",
    subtitle: "Entenda a performance do seu time essa semana",
    category: "IA",
  },
];

const baseAllActivities: ActivityInput[] = [
  ...baseActivities,
  {
    title: "Aprovar OKRs do Q2 2026",
    subtitle: "3 times aguardando aprovação",
    category: "Missões",
  },
  {
    title: "Revisar PDI de Ana Ferreira",
    subtitle: "Plano atualizado em 05/03/2026",
    category: "Pessoas",
  },
  {
    title: "Preparar pauta da 1:1 com Lucas Oliveira",
    subtitle: "Reunião agendada para 11/03/2026 às 14h",
    category: "1:1",
  },
  {
    title: "Dar reconhecimento: Beatriz Ramos — entrega do módulo de pesquisas",
    subtitle: "Sugestão do assistente Bud A.I",
    category: "Reconhecimento",
  },
  {
    title: "Responder formulário: Pesquisa de clima organizacional",
    subtitle: "Data limite para responder: 15/03/2026",
    category: "Pesquisas",
  },
  {
    title: "Atualizar missão: Implementar integração com Slack e Teams",
    subtitle: "Última atualização 28/02/2026",
    urgent: true,
    category: "Missões",
  },
];

function iconByCategory(
  category: string,
): React.ComponentType<{ size?: number }> {
  switch (category) {
    case "Pesquisas":
      return Table;
    case "Missões":
      return ChartDonut;
    case "Check-ins":
      return ChatCircleDots;
    case "IA":
      return Lightning;
    case "Pessoas":
      return UserCircle;
    case "1:1":
      return CalendarCheck;
    case "Reconhecimento":
      return Megaphone;
    default:
      return Table;
  }
}

function toActivity(input: ActivityInput): Activity {
  return {
    ...input,
    icon: iconByCategory(input.category),
  };
}

function ActivityItem({ activity }: { activity: Activity }) {
  return (
    <li className="flex items-center gap-[var(--sp-xs)] py-[var(--sp-xs)] border-b border-[var(--color-caramel-200)] cursor-pointer transition-colors duration-[120ms] last:border-b-0 hover:bg-[var(--color-caramel-50)]">
      <div className="shrink-0 flex items-center justify-center w-10 h-10 rounded-[var(--radius-sm)] bg-[var(--color-caramel-50)] border border-[var(--color-caramel-200)] text-[var(--color-neutral-600)]">
        <activity.icon size={16} />
      </div>
      <div className="flex-1 min-w-0 flex flex-col gap-[var(--sp-3xs)]">
        <p className="[font-family:var(--font-label)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)]">
          {activity.title}
        </p>
        <p
          className={
            activity.urgent
              ? "[font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-red-500)]"
              : "[font-family:var(--font-body)] text-[var(--text-xs)] text-[var(--color-neutral-500)]"
          }
        >
          {activity.subtitle}
        </p>
      </div>
      <CaretRight
        size={16}
        className="shrink-0 text-[var(--color-neutral-400)]"
      />
    </li>
  );
}

export function ActivitiesCard({
  activities: activitiesInput,
}: {
  activities?: ActivityInput[];
}) {
  const [expanded, setExpanded] = useState(false);

  const activities = (
    activitiesInput && activitiesInput.length > 0
      ? activitiesInput
      : baseActivities
  ).map(toActivity);
  const allActivities = (
    activitiesInput && activitiesInput.length > 0
      ? activitiesInput
      : baseAllActivities
  ).map(toActivity);

  const urgentCount = allActivities.filter((a) => a.urgent).length;

  const action = (
    <div className="flex items-center gap-[var(--sp-2xs)]">
      <Badge color="neutral" size="sm">
        02/03 à 06/03
      </Badge>
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
        <CardHeader title="Minhas atividades" action={action} />
        <CardBody>
          <ul className="list-none flex flex-col">
            {activities.map((activity, i) => (
              <ActivityItem key={i} activity={activity} />
            ))}
          </ul>
        </CardBody>
      </Card>

      <Modal open={expanded} onClose={() => setExpanded(false)} size="lg">
        <ModalHeader
          title="Minhas atividades"
          onClose={() => setExpanded(false)}
        >
          <Badge color="neutral" size="sm">
            02/03 à 06/03
          </Badge>
        </ModalHeader>
        <ModalBody>
          <div className="grid grid-cols-3 max-[480px]:grid-cols-1 gap-[var(--sp-2xs)] pb-[var(--sp-sm)] border-b border-[var(--color-caramel-200)] mb-[var(--sp-sm)]">
            <div className="flex flex-col items-center gap-[var(--sp-3xs)] p-[var(--sp-xs)] rounded-[var(--radius-sm)] bg-[var(--color-caramel-50)]">
              <span className="[font-family:var(--font-heading)] text-[var(--text-lg)] font-semibold text-[var(--color-neutral-900)]">
                {allActivities.length}
              </span>
              <span className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
                Pendentes
              </span>
            </div>
            <div className="flex flex-col items-center gap-[var(--sp-3xs)] p-[var(--sp-xs)] rounded-[var(--radius-sm)] bg-[var(--color-caramel-50)]">
              <span className="[font-family:var(--font-heading)] text-[var(--text-lg)] font-semibold text-[var(--color-red-600)]">
                {urgentCount}
              </span>
              <span className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
                Urgentes
              </span>
            </div>
            <div className="flex flex-col items-center gap-[var(--sp-3xs)] p-[var(--sp-xs)] rounded-[var(--radius-sm)] bg-[var(--color-caramel-50)]">
              <span className="[font-family:var(--font-heading)] text-[var(--text-lg)] font-semibold text-[var(--color-neutral-900)]">
                3
              </span>
              <span className="[font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)]">
                Concluídas hoje
              </span>
            </div>
          </div>

          <div className="mb-[var(--sp-sm)]">
            <p className="[font-family:var(--font-heading)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)] mb-[var(--sp-2xs)]">
              Urgentes
            </p>
            <ul className="list-none flex flex-col">
              {allActivities
                .filter((a) => a.urgent)
                .map((activity, i) => (
                  <ActivityItem key={`urgent-${i}`} activity={activity} />
                ))}
            </ul>
          </div>

          <div className="mb-[var(--sp-sm)]">
            <p className="[font-family:var(--font-heading)] text-[var(--text-sm)] font-semibold text-[var(--color-neutral-900)] mb-[var(--sp-2xs)]">
              Todas as atividades
            </p>
            <ul className="list-none flex flex-col">
              {allActivities
                .filter((a) => !a.urgent)
                .map((activity, i) => (
                  <ActivityItem key={`all-${i}`} activity={activity} />
                ))}
            </ul>
          </div>
        </ModalBody>
      </Modal>
    </>
  );
}
