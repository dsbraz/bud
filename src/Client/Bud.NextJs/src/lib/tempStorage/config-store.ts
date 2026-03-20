import type {
  Cycle,
  CycleStatus,
  CycleType,
  DataScope,
  Permission,
  Role,
  RoleType,
  Tag,
} from "@/types";
import { generateRelativeCycles, type CycleDefinition } from "./seed-utils";

export type OrgType = "holding" | "company" | "subsidiary" | "business_unit";

export interface CompanyProfile {
  id: string;
  name: string;
  legalName: string | null;
  slug: string;
  cnpj: string | null;
  logoUrl: string | null;
  plan: string;
  timezone: string;
  language: string;
  /** Parent organization ID for hierarchy support */
  parentOrgId: string | null;
  /** Organization type in hierarchy */
  orgType: OrgType;
  /** Depth in hierarchy tree (0 = root) */
  depth: number;
  /** Path of org IDs from root to this org (e.g., ["holding-1", "company-1", "this-org"]) */
  path: string[];
}

export interface CompanyValueRecord {
  id: string;
  orgId: string;
  name: string;
  description: string;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
}

export interface ConfigRoleRecord extends Omit<Role, "permissions"> {
  slug: string;
  permissionIds: string[];
}

export interface ConfigStoreSnapshot {
  schemaVersion: number;
  updatedAt: string;
  activeOrgId: string;
  organizationsById: Record<string, CompanyProfile>;
  companyValuesByOrg: Record<string, CompanyValueRecord[]>;
  tagsByOrg: Record<string, Tag[]>;
  cyclesByOrg: Record<string, Cycle[]>;
  rolesByOrg: Record<string, ConfigRoleRecord[]>;
  permissions: Permission[];
  legacyRoleSlugAliases: Record<string, string>;
  legacyCycleIdAliases: Record<string, string>;
  legacyTagIdAliases: Record<string, string>;
}

const STORAGE_KEY = "bud.saas.config-store";
const STORE_SCHEMA_VERSION = 2;

const DEFAULT_ACTIVE_ORG_ID = "org-1";

const DEFAULT_ROLE_ALIASES: Record<string, string> = {
  super_admin: "super-admin",
  superadmin: "super-admin",
  admin: "admin-rh",
  admin_rh: "admin-rh",
  manager: "gestor",
  collaborator: "colaborador",
  viewer: "visualizador",
};

const DEFAULT_CYCLE_ALIASES: Record<string, string> = {
  "annual-2026": "ano-2026",
};

const DEFAULT_TAG_ALIASES: Record<string, string> = {
  strategy: "tag-estrategia",
  growth: "tag-crescimento",
  retention: "tag-retencao",
  culture: "tag-cultura",
  innovation: "tag-inovacao",
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

function nowIso(): string {
  return new Date().toISOString();
}

export function createTagIdFromName(name: string): string {
  const slug = slugify(name);
  return `tag-${slug || Date.now().toString()}`;
}

export function createCompanyValueIdFromName(name: string): string {
  const slug = slugify(name);
  return `value-${slug || Date.now().toString()}`;
}

export function createCycleIdFromName(name: string): string {
  const slug = slugify(name);
  return `cycle-${slug || Date.now().toString()}`;
}

export function createRoleSlug(name: string): string {
  const normalized = slugify(name);
  return normalized || `role-${Date.now().toString()}`;
}

export function createRoleId(orgId: string, slug: string): string {
  return `role-${orgId}-${slug}`;
}

function createPermission(input: {
  id: string;
  group: Permission["group"];
  label: string;
  description: string;
}): Permission {
  return {
    id: input.id,
    group: input.group,
    label: input.label,
    description: input.description,
  };
}

function buildPermissions(): Permission[] {
  return [
    createPermission({ id: "people.view", group: "people", label: "Visualizar", description: "Ver informações de colaboradores" }),
    createPermission({ id: "people.create", group: "people", label: "Criar", description: "Adicionar novos colaboradores" }),
    createPermission({ id: "people.edit", group: "people", label: "Editar", description: "Alterar dados de colaboradores" }),
    createPermission({ id: "people.deactivate", group: "people", label: "Desativar", description: "Desativar contas de colaboradores" }),
    createPermission({ id: "missions.view", group: "missions", label: "Visualizar", description: "Ver missões e OKRs" }),
    createPermission({ id: "missions.create", group: "missions", label: "Criar", description: "Criar novas missões e OKRs" }),
    createPermission({ id: "missions.edit", group: "missions", label: "Editar", description: "Alterar missões e OKRs existentes" }),
    createPermission({ id: "missions.delete", group: "missions", label: "Excluir", description: "Remover missões e OKRs" }),
    createPermission({ id: "missions.assign", group: "missions", label: "Atribuir", description: "Atribuir missões a colaboradores" }),
    createPermission({ id: "surveys.view", group: "surveys", label: "Visualizar", description: "Ver pesquisas disponíveis" }),
    createPermission({ id: "surveys.create", group: "surveys", label: "Criar", description: "Criar novas pesquisas" }),
    createPermission({ id: "surveys.edit", group: "surveys", label: "Editar", description: "Alterar pesquisas existentes" }),
    createPermission({ id: "surveys.publish", group: "surveys", label: "Publicar", description: "Publicar pesquisas para respondentes" }),
    createPermission({ id: "surveys.results", group: "surveys", label: "Ver resultados", description: "Acessar resultados e análises" }),
    createPermission({ id: "settings.access", group: "settings", label: "Acessar", description: "Visualizar configurações da plataforma" }),
    createPermission({ id: "settings.edit", group: "settings", label: "Editar", description: "Alterar configurações da plataforma" }),
  ];
}

function createOrganization(input: {
  id: string;
  name: string;
  legalName?: string;
  slug: string;
  cnpj?: string;
  logoUrl: string | null;
  plan?: string;
  parentOrgId?: string | null;
  orgType?: OrgType;
}): CompanyProfile {
  const parentOrgId = input.parentOrgId ?? null;
  const orgType = input.orgType ?? (parentOrgId ? "subsidiary" : "company");
  
  // Calculate depth and path based on parent (simplified - in real app would look up parent)
  const depth = parentOrgId ? 1 : 0;
  const path = parentOrgId ? [parentOrgId, input.id] : [input.id];

  return {
    id: input.id,
    name: input.name,
    legalName: input.legalName ?? input.name,
    slug: input.slug,
    cnpj: input.cnpj ?? null,
    logoUrl: input.logoUrl,
    plan: input.plan ?? "professional",
    timezone: "America/Sao_Paulo",
    language: "pt-BR",
    parentOrgId,
    orgType,
    depth,
    path,
  };
}

function createCompanyValue(orgId: string, input: { id: string; name: string; description: string }): CompanyValueRecord {
  const now = nowIso();
  return {
    id: input.id,
    orgId,
    name: input.name,
    description: input.description,
    createdAt: now,
    updatedAt: now,
    deletedAt: null,
  };
}

function createTag(orgId: string, input: { id: string; name: string; color: string }): Tag {
  const now = nowIso();
  return {
    id: input.id,
    orgId,
    name: input.name,
    color: input.color,
    createdAt: now,
    updatedAt: now,
    deletedAt: null,
  };
}

function createCycle(
  orgId: string,
  input: {
    id: string;
    name: string;
    type: CycleType;
    startDate: string;
    endDate: string;
    status: CycleStatus;
  },
): Cycle {
  const now = nowIso();
  return {
    id: input.id,
    orgId,
    name: input.name,
    type: input.type,
    startDate: input.startDate,
    endDate: input.endDate,
    status: input.status,
    okrDefinitionDeadline: null,
    midReviewDate: null,
    createdAt: now,
    updatedAt: now,
  };
}

function createRole(
  orgId: string,
  input: {
    slug: string;
    name: string;
    description: string;
    type: RoleType;
    isDefault: boolean;
    scope: DataScope;
    permissionIds: string[];
  },
): ConfigRoleRecord {
  const now = nowIso();
  return {
    id: createRoleId(orgId, input.slug),
    orgId,
    slug: input.slug,
    name: input.name,
    description: input.description,
    type: input.type,
    isDefault: input.isDefault,
    scope: input.scope,
    permissionIds: input.permissionIds,
    createdAt: now,
    updatedAt: now,
  };
}

function buildOrgDefaults(orgId: string): {
  values: CompanyValueRecord[];
  tags: Tag[];
  cycles: Cycle[];
  roles: ConfigRoleRecord[];
} {
  const values = [
    createCompanyValue(orgId, {
      id: "value-inovacao",
      name: "Inovação",
      description: "Buscamos soluções criativas e desafiamos o status quo em tudo que fazemos.",
    }),
    createCompanyValue(orgId, {
      id: "value-colaboracao",
      name: "Colaboração",
      description: "Acreditamos que resultados extraordinários vêm do trabalho em equipe.",
    }),
    createCompanyValue(orgId, {
      id: "value-transparencia",
      name: "Transparência",
      description: "Comunicamos com honestidade e clareza, construindo confiança mútua.",
    }),
    createCompanyValue(orgId, {
      id: "value-foco-cliente",
      name: "Foco no cliente",
      description: "Todas as nossas decisões começam pelo impacto no cliente.",
    }),
    createCompanyValue(orgId, {
      id: "value-excelencia",
      name: "Excelência",
      description: "Entregamos com qualidade e buscamos melhorar continuamente.",
    }),
    createCompanyValue(orgId, {
      id: "value-integridade",
      name: "Integridade",
      description: "Agimos com ética, responsabilidade e coerência em todas as relações.",
    }),
  ];

  const tags = [
    createTag(orgId, { id: "tag-engenharia", name: "Engenharia", color: "orange" }),
    createTag(orgId, { id: "tag-design", name: "Design", color: "wine" }),
    createTag(orgId, { id: "tag-marketing", name: "Marketing", color: "success" }),
    createTag(orgId, { id: "tag-projeto-alpha", name: "Projeto Alpha", color: "caramel" }),
    createTag(orgId, { id: "tag-lideranca", name: "Liderança", color: "neutral" }),
    createTag(orgId, { id: "tag-comunicacao", name: "Comunicação", color: "warning" }),
    createTag(orgId, { id: "tag-estrategia", name: "Estratégia", color: "wine" }),
    createTag(orgId, { id: "tag-crescimento", name: "Crescimento", color: "orange" }),
    createTag(orgId, { id: "tag-retencao", name: "Retenção", color: "success" }),
    createTag(orgId, { id: "tag-cultura", name: "Cultura", color: "caramel" }),
    createTag(orgId, { id: "tag-inovacao", name: "Inovação", color: "neutral" }),
  ];

  // Generate cycles dynamically relative to current date
  const relativeCycles = generateRelativeCycles();
  const cycles = relativeCycles.map((def: CycleDefinition) =>
    createCycle(orgId, {
      id: def.id,
      name: def.name,
      type: def.type as CycleType,
      startDate: def.startDate,
      endDate: def.endDate,
      status: def.status as CycleStatus,
    }),
  );

  const allPermissionIds = buildPermissions().map((permission) => permission.id);
  const roles = [
    createRole(orgId, {
      slug: "super-admin",
      name: "Super Admin",
      description: "Acesso total ao sistema",
      type: "system",
      isDefault: false,
      scope: "org",
      permissionIds: allPermissionIds,
    }),
    createRole(orgId, {
      slug: "admin-rh",
      name: "Admin RH",
      description: "Gestão de pessoas e configurações",
      type: "system",
      isDefault: false,
      scope: "org",
      permissionIds: allPermissionIds.filter((permissionId) => permissionId !== "settings.edit"),
    }),
    createRole(orgId, {
      slug: "gestor",
      name: "Gestor",
      description: "Gestão do time direto",
      type: "system",
      isDefault: false,
      scope: "team",
      permissionIds: [
        "people.view",
        "missions.view",
        "missions.create",
        "missions.edit",
        "missions.assign",
        "surveys.view",
        "surveys.results",
      ],
    }),
    createRole(orgId, {
      slug: "colaborador",
      name: "Colaborador",
      description: "Acesso padrão para colaboradores",
      type: "system",
      isDefault: true,
      scope: "self",
      permissionIds: ["people.view", "missions.view", "surveys.view"],
    }),
    createRole(orgId, {
      slug: "visualizador",
      name: "Visualizador",
      description: "Acesso somente leitura",
      type: "system",
      isDefault: false,
      scope: "org",
      permissionIds: ["people.view", "missions.view", "surveys.view"],
    }),
  ];

  return { values, tags, cycles, roles };
}

export function getSeedSnapshot(): ConfigStoreSnapshot {
  // Example hierarchy:
  // - BRQ (holding)
  //   - BRQ Digital (subsidiary)
  //   - BRQ Consulting (subsidiary)
  // - Bud Tecnologia (standalone company)
  // - Initech (standalone company)
  
  const organizations = [
    // Standalone companies
    createOrganization({
      id: "org-1",
      name: "Bud Tecnologia Ltda.",
      legalName: "Bud Tecnologia Ltda.",
      slug: "bud-tecnologia",
      cnpj: "12.345.678/0001-99",
      logoUrl: "/logos/bud-org.svg",
      plan: "professional",
      orgType: "company",
    }),
    // Holding example
    createOrganization({
      id: "brq",
      name: "BRQ S.A",
      legalName: "BRQ Soluções em Tecnologia S.A.",
      slug: "brq",
      cnpj: "89.123.456/0001-12",
      logoUrl: "/logos/brq.svg",
      plan: "enterprise",
      orgType: "holding",
    }),
    // Subsidiaries of BRQ
    createOrganization({
      id: "brq-digital",
      name: "BRQ Digital",
      legalName: "BRQ Digital Solutions Ltda.",
      slug: "brq-digital",
      cnpj: "89.123.456/0002-93",
      logoUrl: "/logos/brq.svg",
      plan: "enterprise",
      parentOrgId: "brq",
      orgType: "subsidiary",
    }),
    createOrganization({
      id: "brq-consulting",
      name: "BRQ Consulting",
      legalName: "BRQ Consultoria em TI Ltda.",
      slug: "brq-consulting",
      cnpj: "89.123.456/0003-74",
      logoUrl: "/logos/brq.svg",
      plan: "enterprise",
      parentOrgId: "brq",
      orgType: "subsidiary",
    }),
    // Another standalone company
    createOrganization({
      id: "initech",
      name: "Initech Tecnologia",
      legalName: "Initech Tecnologia Ltda.",
      slug: "initech",
      cnpj: "67.890.123/0001-45",
      logoUrl: "https://ui-avatars.com/api/?name=IT&background=0EA5E9&color=fff&size=48&font-size=0.4&bold=true",
      plan: "starter",
      orgType: "company",
    }),
  ];

  const companyValuesByOrg: Record<string, CompanyValueRecord[]> = {};
  const tagsByOrg: Record<string, Tag[]> = {};
  const cyclesByOrg: Record<string, Cycle[]> = {};
  const rolesByOrg: Record<string, ConfigRoleRecord[]> = {};

  organizations.forEach((organization) => {
    const defaults = buildOrgDefaults(organization.id);
    companyValuesByOrg[organization.id] = defaults.values;
    tagsByOrg[organization.id] = defaults.tags;
    cyclesByOrg[organization.id] = defaults.cycles;
    rolesByOrg[organization.id] = defaults.roles;
  });

  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: nowIso(),
    activeOrgId: DEFAULT_ACTIVE_ORG_ID,
    organizationsById: Object.fromEntries(organizations.map((organization) => [organization.id, organization])),
    companyValuesByOrg,
    tagsByOrg,
    cyclesByOrg,
    rolesByOrg,
    permissions: buildPermissions(),
    legacyRoleSlugAliases: DEFAULT_ROLE_ALIASES,
    legacyCycleIdAliases: DEFAULT_CYCLE_ALIASES,
    legacyTagIdAliases: DEFAULT_TAG_ALIASES,
  };
}

function sanitizeOrganization(raw: unknown): CompanyProfile | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Partial<CompanyProfile>;
  if (!item.id || !item.name || !item.slug) return null;

  const id = String(item.id);
  const parentOrgId = item.parentOrgId ? String(item.parentOrgId) : null;
  const orgType = (item.orgType as OrgType) ?? (parentOrgId ? "subsidiary" : "company");
  const depth = typeof item.depth === "number" ? item.depth : (parentOrgId ? 1 : 0);
  const path = Array.isArray(item.path) 
    ? item.path.map(String) 
    : (parentOrgId ? [parentOrgId, id] : [id]);

  return {
    id,
    name: String(item.name),
    legalName: item.legalName ? String(item.legalName) : null,
    slug: String(item.slug),
    cnpj: item.cnpj ? String(item.cnpj) : null,
    logoUrl: item.logoUrl ? String(item.logoUrl) : null,
    plan: item.plan ? String(item.plan) : "professional",
    timezone: item.timezone ? String(item.timezone) : "America/Sao_Paulo",
    language: item.language ? String(item.language) : "pt-BR",
    parentOrgId,
    orgType,
    depth,
    path,
  };
}

function sanitizeCompanyValue(raw: unknown, orgId: string): CompanyValueRecord | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Partial<CompanyValueRecord>;
  if (!item.id || !item.name) return null;

  const now = nowIso();
  return {
    id: String(item.id),
    orgId,
    name: String(item.name),
    description: String(item.description ?? ""),
    createdAt: String(item.createdAt ?? now),
    updatedAt: String(item.updatedAt ?? now),
    deletedAt: item.deletedAt ? String(item.deletedAt) : null,
  };
}

function sanitizeTag(raw: unknown, orgId: string): Tag | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Partial<Tag>;
  if (!item.id || !item.name) return null;

  const now = nowIso();
  return {
    id: String(item.id),
    orgId,
    name: String(item.name),
    color: String(item.color ?? "neutral"),
    createdAt: String(item.createdAt ?? now),
    updatedAt: String(item.updatedAt ?? now),
    deletedAt: item.deletedAt ? String(item.deletedAt) : null,
  };
}

function sanitizeCycle(raw: unknown, orgId: string): Cycle | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Partial<Cycle>;
  if (!item.id || !item.name || !item.startDate || !item.endDate) return null;

  const now = nowIso();
  return {
    id: String(item.id),
    orgId,
    name: String(item.name),
    type: (item.type as CycleType | undefined) ?? "custom",
    startDate: String(item.startDate),
    endDate: String(item.endDate),
    status: (item.status as CycleStatus | undefined) ?? "planning",
    okrDefinitionDeadline: item.okrDefinitionDeadline ? String(item.okrDefinitionDeadline) : null,
    midReviewDate: item.midReviewDate ? String(item.midReviewDate) : null,
    createdAt: String(item.createdAt ?? now),
    updatedAt: String(item.updatedAt ?? now),
  };
}

function sanitizeRole(raw: unknown, orgId: string): ConfigRoleRecord | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Partial<ConfigRoleRecord>;
  if (!item.id || !item.name) return null;

  const now = nowIso();
  const slug = String(item.slug ?? createRoleSlug(item.name));

  return {
    id: String(item.id),
    orgId,
    slug,
    name: String(item.name),
    description: item.description ? String(item.description) : null,
    type: (item.type as RoleType | undefined) ?? "custom",
    isDefault: Boolean(item.isDefault),
    scope: (item.scope as DataScope | undefined) ?? "self",
    permissionIds: Array.isArray(item.permissionIds)
      ? item.permissionIds.map((permissionId) => String(permissionId))
      : [],
    createdAt: String(item.createdAt ?? now),
    updatedAt: String(item.updatedAt ?? now),
  };
}

function mergeAliases(base: Record<string, string>, incoming: unknown): Record<string, string> {
  if (!incoming || typeof incoming !== "object") return base;
  const validEntries = Object.entries(incoming as Record<string, unknown>)
    .filter(([key, value]) => key.trim().length > 0 && typeof value === "string" && value.trim().length > 0);

  return {
    ...base,
    ...(Object.fromEntries(validEntries) as Record<string, string>),
  };
}

function migrateSnapshot(raw: Partial<ConfigStoreSnapshot> | null): ConfigStoreSnapshot {
  const seed = getSeedSnapshot();
  if (!raw || typeof raw !== "object") return seed;

  const organizationsById: Record<string, CompanyProfile> = {};
  Object.entries(raw.organizationsById ?? {}).forEach(([orgId, value]) => {
    const organization = sanitizeOrganization({ id: orgId, ...(value as object) });
    if (!organization) return;
    organizationsById[organization.id] = organization;
  });

  // Always merge seed organizations: new orgs added to seed appear automatically
  // for all users without requiring a schema version bump. User-created orgs are preserved.
  Object.entries(seed.organizationsById).forEach(([seedOrgId, seedOrg]) => {
    if (!organizationsById[seedOrgId]) {
      organizationsById[seedOrgId] = seedOrg;
    }
  });

  const companyValuesByOrg: Record<string, CompanyValueRecord[]> = {};
  const tagsByOrg: Record<string, Tag[]> = {};
  const cyclesByOrg: Record<string, Cycle[]> = {};
  const rolesByOrg: Record<string, ConfigRoleRecord[]> = {};

  Object.keys(organizationsById).forEach((orgId) => {
    const seedValues = seed.companyValuesByOrg[orgId] ?? [];
    const seedTags = seed.tagsByOrg[orgId] ?? [];
    const seedCycles = seed.cyclesByOrg[orgId] ?? [];
    const seedRoles = seed.rolesByOrg[orgId] ?? [];

    const rawValues = Array.isArray(raw.companyValuesByOrg?.[orgId])
      ? raw.companyValuesByOrg?.[orgId] ?? []
      : [];
    const nextValues = rawValues
      .map((value) => sanitizeCompanyValue(value, orgId))
      .filter((value): value is CompanyValueRecord => !!value);
    // Always merge: user values preserved, new seed values injected by ID
    const userValueIds = new Set(nextValues.map((v) => v.id));
    const newSeedValues = seedValues.filter((v) => !userValueIds.has(v.id));
    companyValuesByOrg[orgId] = nextValues.length === 0 ? seedValues : [...nextValues, ...newSeedValues];

    const rawTags = Array.isArray(raw.tagsByOrg?.[orgId])
      ? raw.tagsByOrg?.[orgId] ?? []
      : [];
    const nextTags = rawTags
      .map((tag) => sanitizeTag(tag, orgId))
      .filter((tag): tag is Tag => !!tag);
    // Always merge: user tags preserved, new seed tags injected by ID
    const userTagIds = new Set(nextTags.map((t) => t.id));
    const newSeedTags = seedTags.filter((t) => !userTagIds.has(t.id));
    tagsByOrg[orgId] = nextTags.length === 0 ? seedTags : [...nextTags, ...newSeedTags];

    const rawCycles = Array.isArray(raw.cyclesByOrg?.[orgId])
      ? raw.cyclesByOrg?.[orgId] ?? []
      : [];
    const nextCycles = rawCycles
      .map((cycle) => sanitizeCycle(cycle, orgId))
      .filter((cycle): cycle is Cycle => !!cycle);
    // Seed cycles are date-relative, always regenerated fresh.
    // User-created custom cycles (IDs not in seed) are preserved.
    const seedCycleIds = new Set(seedCycles.map((c) => c.id));
    const userCustomCycles = nextCycles.filter((c) => !seedCycleIds.has(c.id));
    cyclesByOrg[orgId] = [...seedCycles, ...userCustomCycles];

    const rawRoles = Array.isArray(raw.rolesByOrg?.[orgId])
      ? raw.rolesByOrg?.[orgId] ?? []
      : [];
    const nextRoles = rawRoles
      .map((role) => sanitizeRole(role, orgId))
      .filter((role): role is ConfigRoleRecord => !!role);
    // System roles always from seed (source of truth for permissions).
    // User-created custom roles are preserved.
    const seedRoleIds = new Set(seedRoles.map((r) => r.id));
    const userCustomRoles = nextRoles.filter((r) => !seedRoleIds.has(r.id));
    rolesByOrg[orgId] = [...seedRoles, ...userCustomRoles];
  });

  const activeOrgId = raw.activeOrgId && organizationsById[raw.activeOrgId]
    ? raw.activeOrgId
    : Object.keys(organizationsById)[0] ?? DEFAULT_ACTIVE_ORG_ID;

  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: typeof raw.updatedAt === "string" ? raw.updatedAt : seed.updatedAt,
    activeOrgId,
    organizationsById,
    companyValuesByOrg,
    tagsByOrg,
    cyclesByOrg,
    rolesByOrg,
    // Permissions are system-defined — always authoritative from seed.
    permissions: seed.permissions,
    legacyRoleSlugAliases: mergeAliases(DEFAULT_ROLE_ALIASES, raw.legacyRoleSlugAliases),
    legacyCycleIdAliases: mergeAliases(DEFAULT_CYCLE_ALIASES, raw.legacyCycleIdAliases),
    legacyTagIdAliases: mergeAliases(DEFAULT_TAG_ALIASES, raw.legacyTagIdAliases),
  };
}

export function loadConfigSnapshot(): ConfigStoreSnapshot {
  if (typeof window === "undefined") {
    return getSeedSnapshot();
  }

  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    const seed = getSeedSnapshot();
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(seed));
    return seed;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<ConfigStoreSnapshot>;
    const migrated = migrateSnapshot(parsed);
    if ((parsed.schemaVersion ?? 0) !== STORE_SCHEMA_VERSION) {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(migrated));
    }
    return migrated;
  } catch {
    const seed = getSeedSnapshot();
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(seed));
    return seed;
  }
}

export function saveConfigSnapshot(
  snapshot: Omit<
    ConfigStoreSnapshot,
    "schemaVersion" | "updatedAt"
  >,
): ConfigStoreSnapshot {
  const next: ConfigStoreSnapshot = {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: nowIso(),
    activeOrgId: snapshot.activeOrgId,
    organizationsById: cloneDeep(snapshot.organizationsById),
    companyValuesByOrg: cloneDeep(snapshot.companyValuesByOrg),
    tagsByOrg: cloneDeep(snapshot.tagsByOrg),
    cyclesByOrg: cloneDeep(snapshot.cyclesByOrg),
    rolesByOrg: cloneDeep(snapshot.rolesByOrg),
    permissions: cloneDeep(snapshot.permissions),
    legacyRoleSlugAliases: {
      ...DEFAULT_ROLE_ALIASES,
      ...cloneDeep(snapshot.legacyRoleSlugAliases),
    },
    legacyCycleIdAliases: {
      ...DEFAULT_CYCLE_ALIASES,
      ...cloneDeep(snapshot.legacyCycleIdAliases),
    },
    legacyTagIdAliases: {
      ...DEFAULT_TAG_ALIASES,
      ...cloneDeep(snapshot.legacyTagIdAliases),
    },
  };

  if (typeof window !== "undefined") {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  }

  return next;
}

export function resetConfigSnapshot(): ConfigStoreSnapshot {
  const seed = getSeedSnapshot();
  if (typeof window !== "undefined") {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(seed));
  }
  return seed;
}

// ===== Organization Hierarchy Helpers =====

/**
 * Get child organizations for a given parent org
 */
export function getChildOrganizations(
  organizationsById: Record<string, CompanyProfile>,
  parentOrgId: string,
): CompanyProfile[] {
  return Object.values(organizationsById).filter(
    (org) => org.parentOrgId === parentOrgId,
  );
}

/**
 * Get all descendant organizations (children, grandchildren, etc.)
 */
export function getDescendantOrganizations(
  organizationsById: Record<string, CompanyProfile>,
  parentOrgId: string,
): CompanyProfile[] {
  const descendants: CompanyProfile[] = [];
  const queue = getChildOrganizations(organizationsById, parentOrgId);

  while (queue.length > 0) {
    const org = queue.shift()!;
    descendants.push(org);
    queue.push(...getChildOrganizations(organizationsById, org.id));
  }

  return descendants;
}

/**
 * Get the parent organization, if any
 */
export function getParentOrganization(
  organizationsById: Record<string, CompanyProfile>,
  orgId: string,
): CompanyProfile | null {
  const org = organizationsById[orgId];
  if (!org?.parentOrgId) return null;
  return organizationsById[org.parentOrgId] ?? null;
}

/**
 * Get all ancestor organizations (parent, grandparent, etc.)
 */
export function getAncestorOrganizations(
  organizationsById: Record<string, CompanyProfile>,
  orgId: string,
): CompanyProfile[] {
  const ancestors: CompanyProfile[] = [];
  let current = organizationsById[orgId];

  while (current?.parentOrgId) {
    const parent = organizationsById[current.parentOrgId];
    if (!parent) break;
    ancestors.push(parent);
    current = parent;
  }

  return ancestors;
}

/**
 * Get root organizations (holdings or standalone companies with no parent)
 */
export function getRootOrganizations(
  organizationsById: Record<string, CompanyProfile>,
): CompanyProfile[] {
  return Object.values(organizationsById).filter(
    (org) => org.parentOrgId === null,
  );
}

/**
 * Check if orgId is a descendant of ancestorOrgId
 */
export function isDescendantOf(
  organizationsById: Record<string, CompanyProfile>,
  orgId: string,
  ancestorOrgId: string,
): boolean {
  const org = organizationsById[orgId];
  if (!org) return false;
  return org.path.includes(ancestorOrgId) && org.id !== ancestorOrgId;
}
