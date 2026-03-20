"use client";

import {
  useState,
  useEffect,
  useCallback,
  type ReactNode,
  useRef,
} from "react";
import { useRouter } from "next/navigation";
import {
  List,
  House,
  Target,
  ListChecks,
  Users,
  GearSix,
  CalendarCheck,
  Trophy,
  WarningCircle,
} from "@phosphor-icons/react";

import {
  PageHeader as DsPageHeader,
  SearchButton,
  NotificationButton,
  Button,
  Card,
  CardBody,
  CommandPalette,
  NotificationPanel,
} from "@mdonangelo/bud-ds";
import type { CommandGroup, NotificationItem } from "@mdonangelo/bud-ds";

import { useSidebar } from "@/contexts/SidebarContext";

const COMMAND_GROUPS: CommandGroup[] = [
  {
    label: "Navegação",
    items: [
      { id: "/home", label: "Início", icon: House },
      { id: "/missions", label: "Missões", icon: Target },
      { id: "/surveys", label: "Pesquisas", icon: ListChecks },
      { id: "/my-team", label: "Meu Time", icon: Users },
      { id: "/settings", label: "Configurações", icon: GearSix },
    ],
  },
];

const NOTIFICATIONS: NotificationItem[] = [
  {
    id: "1",
    icon: WarningCircle,
    title: "Engajamento em queda",
    description: "Ana Ferreira apresenta queda de 23% no engajamento.",
    time: "há 2h",
    unread: true,
  },
  {
    id: "2",
    icon: CalendarCheck,
    title: "1:1 com Lucas às 14h",
    description: "Pontos sugeridos: OKR de retenção e feedback.",
    time: "há 3h",
    unread: true,
  },
  {
    id: "3",
    icon: Trophy,
    title: "Reconhecimentos em alta",
    description: "Sua equipe deu 12 reconhecimentos esta semana (+40%).",
    time: "há 5h",
    unread: false,
  },
];

interface PageHeaderProps {
  title: string;
  description?: string;
  children?: ReactNode;
}

export function PageHeader({ title, description, children }: PageHeaderProps) {
  const router = useRouter();
  const { isMobile, openSidebar } = useSidebar();

  const [searchOpen, setSearchOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);

  const notifBtnRef = useRef<HTMLButtonElement>(null);

  const hasUnread = NOTIFICATIONS.some((n) => n.unread);

  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === "k") {
      e.preventDefault();
      setSearchOpen(true);
    }
  }, []);

  useEffect(() => {
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [handleKeyDown]);

  return (
    <div>
      <div className="flex items-stretch gap-2">
        {isMobile && (
          <Card padding="sm" className="flex items-center shrink-0">
            <CardBody>
              <Button
                variant="tertiary"
                size="md"
                leftIcon={List}
                aria-label="Abrir menu"
                onClick={openSidebar}
              />
            </CardBody>
          </Card>
        )}
        <DsPageHeader title={title} className="flex-1 min-w-0">
          {children}
          <SearchButton onClick={() => setSearchOpen(true)} />
          <NotificationButton
            ref={notifBtnRef}
            hasUnread={hasUnread}
            onClick={() => setNotifOpen((v) => !v)}
          />
        </DsPageHeader>
      </div>

      {description && (
        <p className="text-sm text-muted-foreground mt-1 px-1">{description}</p>
      )}

      <CommandPalette
        open={searchOpen}
        onClose={() => setSearchOpen(false)}
        onSelect={(id) => {
          setSearchOpen(false);
          router.push(id);
        }}
        groups={COMMAND_GROUPS}
        placeholder="Buscar páginas, pessoas, OKRs..."
      />

      <NotificationPanel
        open={notifOpen}
        onClose={() => setNotifOpen(false)}
        anchorRef={notifBtnRef}
        notifications={NOTIFICATIONS}
        onClickItem={() => setNotifOpen(false)}
        onMarkAllRead={() => {}}
        onViewAll={() => setNotifOpen(false)}
      />
    </div>
  );
}
