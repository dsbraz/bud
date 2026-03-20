export type RoleType = "system" | "custom";

export type DataScope = "self" | "team" | "org";

export type PermissionGroup = "people" | "missions" | "surveys" | "settings";

export interface Permission {
  id: string;
  group: PermissionGroup;
  label: string;
  description: string | null;
}

export interface Role {
  id: string;
  orgId: string | null;
  slug?: string;
  name: string;
  description: string | null;
  type: RoleType;
  isDefault: boolean;
  scope: DataScope;
  createdAt: string;
  updatedAt: string;
  permissionIds?: string[];
  /** Preenchido em queries com join */
  permissions?: Permission[];
}
