import { useState, useRef, useCallback, useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  Card,
  Badge,
  Button,
  SearchButton,
  NotificationButton,
  AssistantButton,
  CommandPalette,
  NotificationPanel,
} from "@mdonangelo/bud-ds";
import type { CommandGroup, NotificationItem } from "@mdonangelo/bud-ds";
import {
  House,
  Target,
  ListChecks,
  Users,
  GearSix,
  CalendarCheck,
  Trophy,
  WarningCircle,
  Eye,
} from "@phosphor-icons/react";
import { useAssistant } from "@/contexts/AssistantContext";
import { useSurveysData } from "@/contexts/SurveysDataContext";
import { useWizard } from "../SurveyWizardContext";
import { SurveyPreviewModal } from "./SurveyPreviewModal";
import styles from "./WizardTopBar.module.css";

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

export function WizardTopBar() {
  const { state } = useWizard();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { open: assistantOpen, toggle: toggleAssistant } = useAssistant();
  const { getSurveyTemplateById, getSurveyTemplateByType } = useSurveysData();

  const [searchOpen, setSearchOpen] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(false);
  const notifBtnRef = useRef<HTMLButtonElement>(null);
  const [notifOpen, setNotifOpen] = useState(false);

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

  const templateId = searchParams.get("templateId");
  const template = templateId
    ? getSurveyTemplateById(templateId)
    : state.type
      ? getSurveyTemplateByType(state.type)
      : null;
  const hasUnread = NOTIFICATIONS.some((n) => n.unread);
  const displayName = state.name || "Nome da pesquisa...";
  const canPreview = state.questions.length > 0;

  return (
    <>
      <Card padding="none">
        <div className={styles.topBar}>
          <div className={styles.left}>
            <span className={styles.title}>{displayName}</span>
            {template && (
              <Badge color="wine" size="sm">
                Template {template.name}
              </Badge>
            )}
            {canPreview && (
              <Button
                variant="secondary"
                size="sm"
                leftIcon={Eye}
                onClick={() => setPreviewOpen(true)}
              >
                Visualizar
              </Button>
            )}
          </div>

          <div className={styles.right}>
            <SearchButton onClick={() => setSearchOpen(true)} />
            <NotificationButton
              ref={notifBtnRef}
              hasUnread={hasUnread}
              onClick={() => setNotifOpen((v) => !v)}
            />
            <AssistantButton
              active={assistantOpen}
              onClick={toggleAssistant}
            />
          </div>
        </div>
      </Card>

      <CommandPalette
        open={searchOpen}
        onClose={() => setSearchOpen(false)}
        onSelect={(id: string) => {
          setSearchOpen(false);
          navigate(id);
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

      <SurveyPreviewModal
        open={previewOpen}
        onClose={() => setPreviewOpen(false)}
      />
    </>
  );
}
