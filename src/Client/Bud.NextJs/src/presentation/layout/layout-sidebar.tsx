import { useState, useRef, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import type { CompanyProfile } from "@/lib/tempStorage/config-store";

import {
  Sidebar,
  SidebarHeader,
  SidebarOrgSwitcher,
  SidebarDivider,
  SidebarNav,
  SidebarGroup,
  SidebarItem,
  SidebarSubItem,
  SidebarFooter,
  SidebarUser,
  Avatar,
  Popover,
} from "@mdonangelo/bud-ds";
import type { PopoverItem } from "@mdonangelo/bud-ds";
import {
  House,
  Target,
  ListChecks,
  GearSix,
  Lifebuoy,
  UsersThree,
  UserCircle,
  SignOut,
  Plus,
} from "@phosphor-icons/react";
import { BudLogo, BudLogoMark } from "@/components/BudLogo";
import { PlanSelectionModal } from "@/components/PlanSelectionModal";
import { useSavedViews } from "@/contexts/SavedViewsContext";
import { useConfigData } from "@/contexts/ConfigDataContext";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

interface AppSidebarProps {
  collapsed: boolean;
  onToggleCollapse: () => void;
  mobileOpen?: boolean;
  onMobileClose?: () => void;
}

const USER_MENU: PopoverItem[] = [
  { id: "profile", label: "Minha conta", icon: UserCircle },
  { id: "logout", label: "Sair", icon: SignOut, danger: true },
];

export function AppSidebar({
  collapsed,
  onToggleCollapse,
  mobileOpen,
  onMobileClose,
}: AppSidebarProps) {
  // Hooks
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  // TODO: rebuild context into real requests to backend
  const { views } = useSavedViews();
  const { activeOrganization, setActiveOrgId } = useConfigData();

  const { data: organizations = [] } = useQuery<CompanyProfile[]>({
    queryKey: ["organizations"],
    queryFn: async () => {
      const res = await fetch("/api/organizations");
      return res.json() as Promise<CompanyProfile[]>;
    },
  });

  const activeViewId = searchParams.get("view");

  // Refs e states
  const orgRef = useRef<HTMLButtonElement>(null);
  const userRef = useRef<HTMLButtonElement>(null);

  const [userOpen, setUserOpen] = useState(false);
  const [orgOpen, setOrgOpen] = useState(false);
  const [planModalOpen, setPlanModalOpen] = useState(false);

  const orgItems = organizations.map((org) => ({
    id: org.id,
    label: org.name,
    image:
      org.logoUrl ??
      "https://ui-avatars.com/api/?name=" +
        encodeURIComponent(org.name) +
        "&background=0EA5E9&color=fff&size=48&font-size=0.4&bold=true",
  }));

  const orgMenuItems: PopoverItem[] = [
    ...orgItems.map((org) => ({
      id: org.id,
      label: org.label,
      image: org.image,
      onClick: () => {
        setActiveOrgId(org.id);
        setOrgOpen(false);
      },
    })),
    {
      id: "add-org",
      label: "Adicionar empresa",
      icon: Plus,
      divider: true,
      onClick: () => {
        setOrgOpen(false);
        setPlanModalOpen(true);
      },
    },
  ];

  function isActive(path: string) {
    return pathname === path || pathname.startsWith(path + "/");
  }

  const activeSection = "/" + (pathname.split("/")[1] ?? "");

  return (
    <>
      <Sidebar
        collapsed={collapsed}
        onCollapse={onToggleCollapse}
        mobileOpen={mobileOpen}
        onMobileClose={onMobileClose}
      >
        <SidebarHeader>
          {collapsed && !mobileOpen ? (
            <BudLogoMark
              height={22}
              style={{ cursor: "pointer" }}
              onClick={() => router.push("/home")}
            />
          ) : (
            <BudLogo
              height={28}
              style={{ cursor: "pointer" }}
              onClick={() => router.push("/home")}
            />
          )}
        </SidebarHeader>

        <SidebarOrgSwitcher
          ref={orgRef}
          image={activeOrganization?.logoUrl ?? orgItems[0]?.image}
          label={activeOrganization?.name ?? orgItems[0]?.label ?? ""}
          onClick={() => setOrgOpen((v) => !v)}
        />
        <Popover
          items={orgMenuItems}
          open={orgOpen}
          onClose={() => setOrgOpen(false)}
          anchorRef={orgRef}
        />

        <SidebarDivider />

        <SidebarNav>
          {/* ── Performance e Engajamento ── */}
          <SidebarGroup label="Performance e Engajamento">
            <SidebarItem
              icon={House}
              label="Início"
              active={isActive("/home")}
              onClick={() => router.push("/home")}
            />
            <SidebarItem
              key={`missions-${activeSection}`}
              icon={Target}
              label="Missões"
              active={isActive("/missions")}
              defaultExpanded={isActive("/missions")}
            >
              <SidebarSubItem
                active={
                  isActive("/missions") &&
                  !isActive("/missions/mine") &&
                  !activeViewId
                }
                onClick={() => router.push("/missions")}
              >
                Todas as missões
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/missions/mine")}
                onClick={() => router.push("/missions/mine")}
              >
                Minhas missões
              </SidebarSubItem>
              {views
                .filter((v) => v.module === "missions")
                .map((v) => (
                  <SidebarSubItem
                    key={v.id}
                    active={activeViewId === v.id}
                    onClick={() => router.push(`/missions?view=${v.id}`)}
                  >
                    {v.name}
                  </SidebarSubItem>
                ))}
              <SidebarSubItem onClick={() => router.push("/missions")}>
                + Criar visualização
              </SidebarSubItem>
            </SidebarItem>
            <SidebarItem
              key={`surveys-${activeSection}`}
              icon={ListChecks}
              label="Pesquisas"
              active={isActive("/surveys")}
              defaultExpanded={
                pathname === "/surveys" || pathname.startsWith("/surveys/")
              }
            >
              <SidebarSubItem
                active={pathname === "/surveys" && !searchParams.get("view")}
                onClick={() => router.push("/surveys")}
              >
                Todas as pesquisas
              </SidebarSubItem>
              {views
                .filter((v) => v.module === "surveys")
                .map((v) => (
                  <SidebarSubItem
                    key={v.id}
                    active={activeViewId === v.id}
                    onClick={() => router.push(`/surveys?view=${v.id}`)}
                  >
                    {v.name}
                  </SidebarSubItem>
                ))}
              <SidebarSubItem onClick={() => router.push("/surveys")}>
                + Criar visualização
              </SidebarSubItem>
            </SidebarItem>
            <SidebarItem
              key={`my-team-${activeSection}`}
              icon={UsersThree}
              label="Meu time"
              active={isActive("/my-team")}
              defaultExpanded={isActive("/my-team")}
            >
              <SidebarSubItem
                active={isActive("/my-team/overview")}
                onClick={() => router.push("/my-team/overview")}
              >
                Visão geral
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/my-team/org-chart")}
                onClick={() => router.push("/my-team/org-chart")}
              >
                Organograma
              </SidebarSubItem>
            </SidebarItem>
          </SidebarGroup>

          {/* ── Configurações ── */}
          <SidebarGroup label="Configurações">
            <SidebarItem
              key={`settings-${activeSection}`}
              icon={GearSix}
              label="Configurações"
              active={isActive("/settings")}
              defaultExpanded={isActive("/settings")}
            >
              <SidebarSubItem
                active={isActive("/settings/company")}
                onClick={() => router.push("/settings/company")}
              >
                Dados da empresa
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/settings/users")}
                onClick={() => router.push("/settings/users")}
              >
                Usuários
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/settings/teams")}
                onClick={() => router.push("/settings/teams")}
              >
                Times
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/settings/org-structure")}
                onClick={() => router.push("/settings/org-structure")}
              >
                Estrutura organizacional
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/settings/tags")}
                onClick={() => router.push("/settings/tags")}
              >
                Tags e organizadores
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/settings/cycles")}
                onClick={() => router.push("/settings/cycles")}
              >
                Ciclos
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/settings/survey-templates")}
                onClick={() => router.push("/settings/survey-templates")}
              >
                Templates de pesquisa
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/settings/ai")}
                onClick={() => router.push("/settings/ai")}
              >
                Inteligência artificial
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/settings/integrations")}
                onClick={() => router.push("/settings/integrations")}
              >
                Integrações
              </SidebarSubItem>
              <SidebarSubItem
                active={isActive("/settings/roles")}
                onClick={() => router.push("/settings/roles")}
              >
                Tipos de usuário
              </SidebarSubItem>
            </SidebarItem>
            <SidebarItem
              icon={Lifebuoy}
              label="Ajuda"
              active={isActive("/help")}
              onClick={() => router.push("/help")}
            />
          </SidebarGroup>
        </SidebarNav>

        <SidebarFooter>
          <SidebarUser
            ref={userRef}
            name="Maria Soares"
            role="HR Manager"
            avatar={<Avatar initials="MS" size="sm" />}
            onClick={() => setUserOpen((v) => !v)}
          />
          <Popover
            items={USER_MENU}
            open={userOpen}
            onClose={() => setUserOpen(false)}
            anchorRef={userRef}
          />
        </SidebarFooter>
      </Sidebar>

      <PlanSelectionModal
        open={planModalOpen}
        onClose={() => setPlanModalOpen(false)}
      />
    </>
  );
}
