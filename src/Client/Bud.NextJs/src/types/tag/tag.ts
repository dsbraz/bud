export interface Tag {
  id: string;
  orgId: string;
  name: string;
  color: string;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
  /** Calculado em queries */
  linkedItems?: number;
}
