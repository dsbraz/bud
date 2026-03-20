import type { Gender, Team, TeamColor, TeamMember, User, UserStatus } from "@/types";

export interface PeopleUserRecord extends User {
  /**
   * Foreign key to ConfigRoleRecord.id (e.g., "role-org-1-gestor")
   * This is the primary reference for user's role.
   */
  roleId: string | null;
  /**
   * Role slug for backwards compatibility and display (e.g., "gestor", "colaborador")
   * @deprecated Use roleId for role resolution. This field is kept for legacy data migration.
   */
  roleType: string;
}

export interface PeopleStoreSnapshot {
  schemaVersion: number;
  updatedAt: string;
  currentUserId: string;
  usersById: Record<string, PeopleUserRecord>;
  teamsById: Record<string, Team>;
  teamMembers: TeamMember[];
  legacyUserIdAliases: Record<string, string>;
  legacyTeamIdAliases: Record<string, string>;
}

const STORAGE_KEY_PREFIX = "bud.saas.people-store";
const LEGACY_STORAGE_KEY = "bud.saas.people-store";
const STORE_SCHEMA_VERSION = 2;
const DEFAULT_ORG_ID = "org-1";

/**
 * Creates a role ID from org and slug.
 * Format: "role-{orgId}-{slug}"
 */
export function createRoleIdForOrg(orgId: string, roleSlug: string): string {
  return `role-${orgId}-${roleSlug}`;
}

function getStorageKey(orgId: string): string {
  return `${STORAGE_KEY_PREFIX}:${orgId}`;
}

const DEFAULT_USER_ALIASES: Record<string, string> = {
  "u-ms": "ms",
  "u-af": "af",
  "u-jm": "jm",
  "u-br": "br",
  "u-lo": "lo",
  "u-cs": "cs",
  "u-rm": "rm",
  chro: "ms",
  cto: "pa",
  cpo: "bs",
  cmo: "fr",
  "eng-lead": "cm",
  eng1: "lo",
  eng2: "md",
  eng3: "tb",
  eng4: "im",
  "infra-lead": "dm",
  infra1: "rv",
  pm1: "cr",
  pm2: "gf",
  "design-lead": "af",
  des1: "pr",
  des2: "vt",
  hr1: "rl",
  hr2: "ln",
  hr3: "bc",
  mkt1: "jc",
  mkt2: "ap",
};

const DEFAULT_TEAM_ALIASES: Record<string, string> = {
  rec: "team-recrutamento",
  eng: "team-engenharia",
  mkt: "team-marketing",
  ops: "team-operacoes",
  people: "team-people",
  "team-1": "team-engenharia",
  "team-2": "team-produto",
  "team-3": "team-design",
  "team-4": "team-marketing",
  "team-5": "team-vendas",
  "team-6": "team-cs",
  "team-7": "team-people",
  "team-8": "team-financeiro",
};

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") return structuredClone(value);
  return JSON.parse(JSON.stringify(value)) as T;
}

function slugify(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function teamColorByName(name: string): TeamColor {
  const normalized = slugify(name);
  if (normalized.includes("executivo")) return "wine";
  if (normalized.includes("engenharia")) return "orange";
  if (normalized.includes("produto")) return "caramel";
  if (normalized.includes("design")) return "success";
  if (normalized.includes("marketing")) return "warning";
  if (normalized.includes("rh") || normalized.includes("people")) return "neutral";
  return "neutral";
}

function createUser(input: {
  id: string;
  firstName: string;
  lastName: string;
  jobTitle: string;
  managerId: string | null;
  roleType: string;
  status?: UserStatus;
  nickname?: string | null;
  language?: string;
  gender?: Gender | null;
  birthDate?: string | null;
  orgId?: string;
}): PeopleUserRecord {
  const now = new Date().toISOString();
  const emailPrefix = `${slugify(input.firstName)}.${slugify(input.lastName)}`;
  const orgId = input.orgId ?? DEFAULT_ORG_ID;

  return {
    id: input.id,
    orgId,
    email: `${emailPrefix}@acme.com`,
    firstName: input.firstName,
    lastName: input.lastName,
    nickname: input.nickname ?? null,
    jobTitle: input.jobTitle,
    managerId: input.managerId,
    avatarUrl: null,
    initials: `${input.firstName[0] ?? ""}${input.lastName[0] ?? ""}`.toUpperCase(),
    birthDate: input.birthDate ?? null,
    gender: input.gender ?? null,
    language: input.language ?? "pt-br",
    phone: null,
    status: input.status ?? "active",
    invitedAt: null,
    activatedAt: null,
    lastLoginAt: null,
    authProvider: "email",
    authProviderId: null,
    createdAt: now,
    updatedAt: now,
    deletedAt: null,
    roleId: createRoleIdForOrg(orgId, input.roleType),
    roleType: input.roleType,
  };
}

function createTeam(input: {
  id: string;
  name: string;
  description: string;
  leaderId: string | null;
  color?: TeamColor;
}): Team {
  const now = new Date().toISOString();
  return {
    id: input.id,
    orgId: DEFAULT_ORG_ID,
    name: input.name,
    description: input.description,
    color: input.color ?? teamColorByName(input.name),
    leaderId: input.leaderId,
    parentTeamId: null,
    status: "active",
    createdAt: now,
    updatedAt: now,
    deletedAt: null,
  };
}

function createTeamMember(teamId: string, userId: string, roleInTeam: TeamMember["roleInTeam"]): TeamMember {
  return {
    teamId,
    userId,
    roleInTeam,
    joinedAt: new Date().toISOString(),
  };
}

function getSeedSnapshot(orgId = DEFAULT_ORG_ID): PeopleStoreSnapshot {
  const users: PeopleUserRecord[] = [
    createUser({ id: "ceo", firstName: "Roberto", lastName: "Nascimento", jobTitle: "CEO", managerId: null, roleType: "super-admin" }),
    createUser({ id: "pa", firstName: "Pedro", lastName: "Almeida", jobTitle: "CTO", managerId: "ceo", roleType: "super-admin", language: "en" }),
    createUser({ id: "bs", firstName: "Beatriz", lastName: "Santos", jobTitle: "CPO", managerId: "ceo", roleType: "super-admin" }),
    createUser({ id: "ms", firstName: "Maria", lastName: "Soares", jobTitle: "CHRO", managerId: "ceo", roleType: "admin-rh" }),
    createUser({ id: "fr", firstName: "Fernando", lastName: "Rodrigues", jobTitle: "CMO", managerId: "ceo", roleType: "gestor" }),
    createUser({ id: "cm", firstName: "Carlos", lastName: "Mendes", jobTitle: "Tech Lead", managerId: "pa", roleType: "gestor" }),
    createUser({ id: "lo", firstName: "Lucas", lastName: "Oliveira", jobTitle: "Eng. Software Sr", managerId: "cm", roleType: "colaborador" }),
    createUser({ id: "md", firstName: "Mariana", lastName: "Duarte", jobTitle: "Eng. Software", managerId: "cm", roleType: "colaborador" }),
    createUser({ id: "tb", firstName: "Thiago", lastName: "Barbosa", jobTitle: "Eng. Software Jr", managerId: "cm", roleType: "colaborador" }),
    createUser({ id: "im", firstName: "Isabela", lastName: "Moreira", jobTitle: "QA Engineer", managerId: "cm", roleType: "colaborador" }),
    createUser({ id: "dm", firstName: "Diego", lastName: "Martins", jobTitle: "Infra Lead", managerId: "pa", roleType: "gestor" }),
    createUser({ id: "rv", firstName: "Renata", lastName: "Vieira", jobTitle: "DevOps", managerId: "dm", roleType: "colaborador" }),
    createUser({ id: "cr", firstName: "Camila", lastName: "Rocha", jobTitle: "Product Manager", managerId: "bs", roleType: "gestor" }),
    createUser({ id: "gf", firstName: "Gustavo", lastName: "Fonseca", jobTitle: "Product Designer", managerId: "bs", roleType: "colaborador" }),
    createUser({ id: "af", firstName: "Ana", lastName: "Ferreira", jobTitle: "Design Lead", managerId: "bs", roleType: "gestor" }),
    createUser({ id: "pr", firstName: "Paula", lastName: "Ribeiro", jobTitle: "UI Designer", managerId: "af", roleType: "colaborador" }),
    createUser({ id: "vt", firstName: "Vinicius", lastName: "Teixeira", jobTitle: "UX Researcher", managerId: "af", roleType: "colaborador" }),
    createUser({ id: "rl", firstName: "Rafael", lastName: "Lima", jobTitle: "Analista RH Sr", managerId: "ms", roleType: "admin-rh" }),
    createUser({ id: "ln", firstName: "Larissa", lastName: "Nunes", jobTitle: "Analista RH", managerId: "ms", roleType: "colaborador" }),
    createUser({ id: "bc", firstName: "Bruno", lastName: "Cardoso", jobTitle: "Recrutador", managerId: "ms", roleType: "colaborador", status: "inactive" }),
    createUser({ id: "jc", firstName: "Juliana", lastName: "Costa", jobTitle: "Growth Analyst", managerId: "fr", roleType: "colaborador" }),
    createUser({ id: "ap", firstName: "Andre", lastName: "Peixoto", jobTitle: "Content Manager", managerId: "fr", roleType: "colaborador" }),
    createUser({ id: "jm", firstName: "Joao", lastName: "Martins", jobTitle: "People Ops", managerId: "ms", roleType: "gestor" }),
    createUser({ id: "br", firstName: "Beatriz", lastName: "Ramos", jobTitle: "Product Manager", managerId: "bs", roleType: "gestor" }),
    createUser({ id: "cs", firstName: "Carla", lastName: "Santos", jobTitle: "People Business Partner", managerId: "ms", roleType: "admin-rh" }),
    createUser({ id: "rm", firstName: "Rafael", lastName: "Mendes", jobTitle: "Engineering Manager", managerId: "pa", roleType: "gestor" }),
    createUser({ id: "gn", firstName: "Gabriel", lastName: "Nunes", jobTitle: "Sales Rep", managerId: "fr", roleType: "colaborador" }),
    createUser({ id: "fd", firstName: "Fernanda", lastName: "Dias", jobTitle: "CS Manager", managerId: "cs", roleType: "gestor" }),
    createUser({ id: "if", firstName: "Isabela", lastName: "Freitas", jobTitle: "Finance Analyst", managerId: "ceo", roleType: "colaborador" }),
  ];

  const teams: Team[] = [
    createTeam({ id: "team-executivo", name: "Executivo", description: "Equipe executiva e lideranca estrategica", leaderId: "ceo", color: "wine" }),
    createTeam({ id: "team-engenharia", name: "Engenharia", description: "Time de desenvolvimento e infraestrutura", leaderId: "pa", color: "orange" }),
    createTeam({ id: "team-produto", name: "Produto", description: "Product management e estrategia de produto", leaderId: "bs", color: "caramel" }),
    createTeam({ id: "team-design", name: "Design", description: "Design de produto e pesquisa", leaderId: "af", color: "success" }),
    createTeam({ id: "team-rh", name: "RH", description: "Recursos humanos e gestao de pessoas", leaderId: "ms", color: "neutral" }),
    createTeam({ id: "team-marketing", name: "Marketing", description: "Growth, conteudo e comunicacao", leaderId: "fr", color: "warning" }),
    createTeam({ id: "team-people", name: "People", description: "People operations e cultura", leaderId: "ms", color: "neutral" }),
    createTeam({ id: "team-vendas", name: "Vendas", description: "Time comercial", leaderId: "fr", color: "caramel" }),
    createTeam({ id: "team-cs", name: "Customer Success", description: "Relacao com clientes", leaderId: "fd", color: "warning" }),
    createTeam({ id: "team-financeiro", name: "Financeiro", description: "Gestao financeira", leaderId: "if", color: "neutral" }),
    createTeam({ id: "team-recrutamento", name: "Recrutamento e Selecao", description: "Aquisicao de talentos", leaderId: "rl", color: "neutral" }),
    createTeam({ id: "team-operacoes", name: "Operacoes", description: "Operacoes internas", leaderId: "dm", color: "neutral" }),
  ];

  const teamMembers: TeamMember[] = [
    createTeamMember("team-executivo", "ceo", "leader"),
    createTeamMember("team-executivo", "pa", "member"),
    createTeamMember("team-executivo", "bs", "member"),
    createTeamMember("team-executivo", "ms", "member"),
    createTeamMember("team-executivo", "fr", "member"),

    createTeamMember("team-engenharia", "pa", "leader"),
    createTeamMember("team-engenharia", "cm", "member"),
    createTeamMember("team-engenharia", "lo", "member"),
    createTeamMember("team-engenharia", "md", "member"),
    createTeamMember("team-engenharia", "tb", "member"),
    createTeamMember("team-engenharia", "im", "member"),
    createTeamMember("team-engenharia", "dm", "member"),
    createTeamMember("team-engenharia", "rv", "member"),
    createTeamMember("team-engenharia", "rm", "member"),

    createTeamMember("team-produto", "bs", "leader"),
    createTeamMember("team-produto", "cr", "member"),
    createTeamMember("team-produto", "gf", "member"),
    createTeamMember("team-produto", "jm", "member"),
    createTeamMember("team-produto", "br", "member"),

    createTeamMember("team-design", "af", "leader"),
    createTeamMember("team-design", "pr", "member"),
    createTeamMember("team-design", "vt", "member"),
    createTeamMember("team-design", "gf", "member"),

    createTeamMember("team-rh", "ms", "leader"),
    createTeamMember("team-rh", "rl", "member"),
    createTeamMember("team-rh", "ln", "member"),
    createTeamMember("team-rh", "bc", "member"),
    createTeamMember("team-rh", "cs", "member"),

    createTeamMember("team-marketing", "fr", "leader"),
    createTeamMember("team-marketing", "jc", "member"),
    createTeamMember("team-marketing", "ap", "member"),
    createTeamMember("team-marketing", "gn", "member"),

    createTeamMember("team-people", "ms", "leader"),
    createTeamMember("team-people", "cs", "member"),
    createTeamMember("team-people", "fd", "member"),

    createTeamMember("team-vendas", "fr", "leader"),
    createTeamMember("team-vendas", "gn", "member"),
    createTeamMember("team-vendas", "br", "member"),

    createTeamMember("team-cs", "fd", "leader"),
    createTeamMember("team-cs", "cs", "member"),

    createTeamMember("team-financeiro", "if", "leader"),

    createTeamMember("team-recrutamento", "rl", "leader"),
    createTeamMember("team-recrutamento", "ln", "member"),
    createTeamMember("team-recrutamento", "bc", "member"),

    createTeamMember("team-operacoes", "dm", "leader"),
    createTeamMember("team-operacoes", "rv", "member"),
  ];

  const usersByOrg = users.map((user) => ({ ...user, orgId }));
  const teamsByOrg = teams.map((team) => ({ ...team, orgId }));

  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: new Date().toISOString(),
    currentUserId: "ms",
    usersById: Object.fromEntries(usersByOrg.map((user) => [user.id, user])),
    teamsById: Object.fromEntries(teamsByOrg.map((team) => [team.id, team])),
    teamMembers,
    legacyUserIdAliases: DEFAULT_USER_ALIASES,
    legacyTeamIdAliases: DEFAULT_TEAM_ALIASES,
  };
}

function sanitizeUsers(raw: unknown, orgId = DEFAULT_ORG_ID): Record<string, PeopleUserRecord> {
  if (!raw || typeof raw !== "object") return {};

  const entries = Object.entries(raw as Record<string, unknown>)
    .filter(([, value]) => !!value && typeof value === "object")
    .map(([id, value]) => {
      const item = value as Partial<PeopleUserRecord>;
      if (!item.firstName || !item.lastName || !item.email) return null;
      
      const roleType = String(item.roleType ?? "colaborador");
      // Migrate legacy data: if roleId is missing, create it from roleType
      const roleId = item.roleId ?? createRoleIdForOrg(item.orgId ?? orgId, roleType);
      
      return [
        id,
        {
          ...cloneDeep(item),
          id,
          roleId,
          roleType,
        } as PeopleUserRecord,
      ] as const;
    })
    .filter((entry): entry is readonly [string, PeopleUserRecord] => !!entry);

  return Object.fromEntries(entries);
}

function sanitizeTeams(raw: unknown): Record<string, Team> {
  if (!raw || typeof raw !== "object") return {};

  const entries = Object.entries(raw as Record<string, unknown>)
    .filter(([, value]) => !!value && typeof value === "object")
    .map(([id, value]) => {
      const item = value as Partial<Team>;
      if (!item.name || !item.orgId) return null;
      return [
        id,
        {
          ...cloneDeep(item),
          id,
          status: item.status ?? "active",
          color: (item.color as TeamColor | undefined) ?? "neutral",
          createdAt: String(item.createdAt ?? new Date().toISOString()),
          updatedAt: String(item.updatedAt ?? new Date().toISOString()),
        } as Team,
      ] as const;
    })
    .filter((entry): entry is readonly [string, Team] => !!entry);

  return Object.fromEntries(entries);
}

function sanitizeTeamMembers(raw: unknown): TeamMember[] {
  if (!Array.isArray(raw)) return [];

  return raw
    .filter((item) => !!item && typeof item === "object")
    .map((item) => item as Partial<TeamMember>)
    .filter((item) => !!item.teamId && !!item.userId)
    .map((item) => ({
      teamId: String(item.teamId),
      userId: String(item.userId),
      roleInTeam: (item.roleInTeam as TeamMember["roleInTeam"] | undefined) ?? "member",
      joinedAt: String(item.joinedAt ?? new Date().toISOString()),
    }));
}

function mergeAliases(base: Record<string, string>, incoming: unknown): Record<string, string> {
  if (!incoming || typeof incoming !== "object") return base;
  const mappedEntries = Object.entries(incoming as Record<string, unknown>)
    .filter(([key, value]) => key.trim().length > 0 && typeof value === "string" && value.trim().length > 0)
    .map(([key, value]) => [key, value] as const);

  return {
    ...base,
    ...(Object.fromEntries(mappedEntries) as Record<string, string>),
  };
}

function migrateSnapshot(raw: Partial<PeopleStoreSnapshot> | null, orgId = DEFAULT_ORG_ID): PeopleStoreSnapshot {
  const seed = getSeedSnapshot(orgId);
  if (!raw || typeof raw !== "object") return seed;

  const usersById = sanitizeUsers(raw.usersById, orgId);
  const teamsById = sanitizeTeams(raw.teamsById);
  const teamMembers = sanitizeTeamMembers(raw.teamMembers);

  // Always merge seed entities: new users/teams/members added to seed appear automatically
  // for all users without requiring a schema version bump. User-created items are preserved.
  let resolvedUsers: Record<string, PeopleUserRecord>;
  if (Object.keys(usersById).length === 0) {
    resolvedUsers = seed.usersById;
  } else {
    const newSeedUsers = Object.fromEntries(
      Object.entries(seed.usersById).filter(([id]) => !usersById[id]),
    );
    resolvedUsers = { ...usersById, ...newSeedUsers };
  }

  let resolvedTeams: Record<string, Team>;
  if (Object.keys(teamsById).length === 0) {
    resolvedTeams = seed.teamsById;
  } else {
    const newSeedTeams = Object.fromEntries(
      Object.entries(seed.teamsById).filter(([id]) => !teamsById[id]),
    );
    resolvedTeams = { ...teamsById, ...newSeedTeams };
  }

  let resolvedMembers: TeamMember[];
  if (teamMembers.length === 0) {
    resolvedMembers = seed.teamMembers;
  } else {
    const memberKeys = new Set(teamMembers.map((m) => `${m.teamId}:${m.userId}`));
    const newSeedMembers = seed.teamMembers.filter(
      (m) => !memberKeys.has(`${m.teamId}:${m.userId}`),
    );
    resolvedMembers = [...teamMembers, ...newSeedMembers];
  }

  const filteredMembers = resolvedMembers.filter(
    (member) => !!resolvedUsers[member.userId] && !!resolvedTeams[member.teamId],
  );

  const currentUserId = raw.currentUserId && resolvedUsers[raw.currentUserId]
    ? raw.currentUserId
    : seed.currentUserId;

  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: typeof raw.updatedAt === "string" ? raw.updatedAt : seed.updatedAt,
    currentUserId,
    usersById: resolvedUsers,
    teamsById: resolvedTeams,
    teamMembers: filteredMembers,
    legacyUserIdAliases: mergeAliases(DEFAULT_USER_ALIASES, raw.legacyUserIdAliases),
    legacyTeamIdAliases: mergeAliases(DEFAULT_TEAM_ALIASES, raw.legacyTeamIdAliases),
  };
}

export function loadPeopleSnapshot(orgId = DEFAULT_ORG_ID): PeopleStoreSnapshot {
  if (typeof window === "undefined") return getSeedSnapshot(orgId);

  const storageKey = getStorageKey(orgId);
  const raw = window.localStorage.getItem(storageKey);
  if (!raw) {
    if (orgId === DEFAULT_ORG_ID) {
      const legacyRaw = window.localStorage.getItem(LEGACY_STORAGE_KEY);
      if (legacyRaw) {
        try {
          const parsedLegacy = JSON.parse(legacyRaw) as Partial<PeopleStoreSnapshot>;
          const migratedLegacy = migrateSnapshot(parsedLegacy, orgId);
          window.localStorage.setItem(storageKey, JSON.stringify(migratedLegacy));
          return migratedLegacy;
        } catch {
          // ignore legacy parse failures and recreate seed snapshot
        }
      }
    }

    const seed = getSeedSnapshot(orgId);
    window.localStorage.setItem(storageKey, JSON.stringify(seed));
    return seed;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<PeopleStoreSnapshot>;
    const migrated = migrateSnapshot(parsed, orgId);
    if ((parsed.schemaVersion ?? 0) !== STORE_SCHEMA_VERSION) {
      window.localStorage.setItem(storageKey, JSON.stringify(migrated));
    }
    return migrated;
  } catch {
    const seed = getSeedSnapshot(orgId);
    window.localStorage.setItem(storageKey, JSON.stringify(seed));
    return seed;
  }
}

export function savePeopleSnapshot(
  snapshot: Omit<PeopleStoreSnapshot, "schemaVersion" | "updatedAt">,
  orgId = DEFAULT_ORG_ID,
): PeopleStoreSnapshot {
  const next: PeopleStoreSnapshot = {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: new Date().toISOString(),
    currentUserId: snapshot.currentUserId,
    usersById: cloneDeep(snapshot.usersById),
    teamsById: cloneDeep(snapshot.teamsById),
    teamMembers: cloneDeep(snapshot.teamMembers),
    legacyUserIdAliases: {
      ...DEFAULT_USER_ALIASES,
      ...cloneDeep(snapshot.legacyUserIdAliases),
    },
    legacyTeamIdAliases: {
      ...DEFAULT_TEAM_ALIASES,
      ...cloneDeep(snapshot.legacyTeamIdAliases),
    },
  };

  if (typeof window !== "undefined") {
    window.localStorage.setItem(getStorageKey(orgId), JSON.stringify(next));
  }

  return next;
}

export function resetPeopleSnapshot(orgId = DEFAULT_ORG_ID): PeopleStoreSnapshot {
  const seed = getSeedSnapshot(orgId);
  if (typeof window !== "undefined") {
    window.localStorage.setItem(getStorageKey(orgId), JSON.stringify(seed));
  }
  return seed;
}

export function createTeamIdFromName(name: string): string {
  return `team-${slugify(name) || Date.now().toString()}`;
}

/**
 * Extracts the role slug from a roleId.
 * Format: "role-{orgId}-{slug}" => returns "slug"
 */
export function extractRoleSlugFromId(roleId: string | null): string {
  if (!roleId) return "colaborador";
  const parts = roleId.split("-");
  // Format: role-org-1-gestor => ["role", "org", "1", "gestor"]
  // We want the last part after the orgId
  if (parts.length < 3) return "colaborador";
  // Find the slug part (everything after "role-{orgId}-")
  const prefix = "role-";
  if (!roleId.startsWith(prefix)) return "colaborador";
  const withoutPrefix = roleId.slice(prefix.length);
  // Find the last hyphen-separated segment
  const lastHyphen = withoutPrefix.lastIndexOf("-");
  if (lastHyphen === -1) return withoutPrefix || "colaborador";
  return withoutPrefix.slice(lastHyphen + 1) || "colaborador";
}
