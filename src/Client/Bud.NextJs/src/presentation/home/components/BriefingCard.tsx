import Link from "next/link";
import { Lightning, ArrowRight } from "@phosphor-icons/react";
import { Badge, Accordion, AccordionItem } from "@mdonangelo/bud-ds";

import { useBriefingReadModel } from "../hooks/useBriefingReadModel";
import { formatWeekdayDate } from "@/lib/tempStorage/date-format";

const indicatorColor: Record<string, string> = {
  high: "bg-[var(--color-error-500)]",
  positive: "bg-[var(--color-success-500)]",
};

const iconColor: Record<string, string> = {
  high: "bg-[var(--color-error-100)] text-[var(--color-error-600)]",
  positive: "bg-[var(--color-success-100)] text-[var(--color-success-600)]",
};

export function BriefingCard() {
  const briefingItems = useBriefingReadModel();

  const formatted = formatWeekdayDate();

  // Count urgent items for the badge
  const urgentCount = briefingItems.filter((i) => i.priority === "high").length;

  return (
    <Accordion header>
      <AccordionItem
        icon={Lightning}
        title="Briefing do dia"
        description={formatted}
        action={
          urgentCount > 0 ? (
            <Badge color="error" size="sm">
              {urgentCount} urgente{urgentCount > 1 ? "s" : ""}
            </Badge>
          ) : (
            <Badge color="success" size="sm">
              Em dia
            </Badge>
          )
        }
        defaultOpen
      >
        <div className="flex flex-col bg-[var(--color-neutral-0)] rounded-[var(--radius-sm)] overflow-hidden">
          {briefingItems.map((item) => {
            const Icon = item.icon;
            const hasAction = !!item.action?.href;

            const itemClass =
              "group flex items-center gap-[var(--sp-sm)] p-[var(--sp-sm)] no-underline text-inherit transition-colors duration-[150ms] ease-[ease] cursor-pointer relative hover:bg-[var(--color-caramel-50)] [&:not(:last-child)]:border-b [&:not(:last-child)]:border-[var(--color-caramel-100)]";

            const itemContent = (
              <>
                <span
                  className={`absolute left-0 top-0 bottom-0 w-[var(--sp-3xs)] rounded-[0_var(--radius-2xs)_var(--radius-2xs)_0] ${indicatorColor[item.priority] ?? "bg-transparent"}`}
                />
                <span
                  className={`shrink-0 flex items-center justify-center w-[var(--sp-xl)] h-[var(--sp-xl)] rounded-[var(--radius-xs)] ${iconColor[item.priority] ?? "bg-[var(--color-caramel-100)] text-[var(--color-neutral-600)]"}`}
                >
                  <Icon size={16} />
                </span>
                <span className="flex-1 [font-family:var(--font-body)] text-[var(--text-sm)] text-[var(--color-neutral-700)] leading-[1.4] min-w-0">
                  {item.text}
                </span>
                {hasAction && (
                  <span className="shrink-0 flex items-center gap-[var(--sp-3xs)] [font-family:var(--font-label)] text-[var(--text-xs)] text-[var(--color-neutral-500)] opacity-0 -translate-x-[var(--sp-3xs)] transition-[opacity,transform,color] duration-[150ms] ease-[ease] group-hover:opacity-100 group-hover:translate-x-0 group-hover:text-[var(--color-orange-600)]">
                    {item.action!.label}
                    <ArrowRight size={14} />
                  </span>
                )}
              </>
            );

            if (hasAction) {
              return (
                <Link
                  key={item.id}
                  href={item.action!.href!}
                  className={itemClass}
                >
                  {itemContent}
                </Link>
              );
            }

            return (
              <div key={item.id} className={itemClass}>
                {itemContent}
              </div>
            );
          })}
        </div>
      </AccordionItem>
    </Accordion>
  );
}
