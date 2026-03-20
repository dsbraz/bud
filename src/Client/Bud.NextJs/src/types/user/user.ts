export type UserStatus = "active" | "inactive" | "invited" | "suspended";

export type AuthProvider = "email" | "google" | "microsoft" | "saml";

export type Gender = "feminino" | "masculino" | "nao-binario" | "prefiro-nao-dizer";

export interface User {
  id: string;
  orgId: string;
  email: string;
  firstName: string;
  lastName: string;
  initials: string | null;
  jobTitle: string | null;
  managerId: string | null;
  avatarUrl: string | null;
  nickname: string | null;
  birthDate: string | null;
  gender: string | null;
  phone: string | null;
  language: string;
  status: UserStatus;
  invitedAt: string | null;
  activatedAt: string | null;
  lastLoginAt: string | null;
  authProvider: AuthProvider;
  authProviderId: string | null;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
}
