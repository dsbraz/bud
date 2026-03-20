export type OrgType = "holding" | "company" | "subsidiary" | "business_unit";

export type OrgPlan = "free" | "starter" | "professional" | "enterprise";

export type OrgMemberRole = "admin" | "viewer" | "operator";

export type OrgMemberScope = "direct" | "descendants" | "all";

export interface Organization {
  id: string;
  parentOrgId: string | null;
  orgType: OrgType;
  depth: number;
  path: string[];
  name: string;
  slug: string;
  logoUrl: string | null;
  plan: OrgPlan;
  evaluationFramework: string;
  okrCadence: string;
  cascadeModel: string;
  inheritConfigs: boolean;
  timezone: string;
  language: string;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
}

export interface OrgMember {
  userId: string;
  orgId: string;
  role: OrgMemberRole;
  scope: OrgMemberScope;
  grantedBy: string | null;
  grantedAt: string;
}
