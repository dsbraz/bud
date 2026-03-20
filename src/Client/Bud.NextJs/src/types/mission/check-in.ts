export type ConfidenceLevel =
  | "high"
  | "medium"
  | "low"
  | "barrier"
  | "deprioritized";

export interface CheckIn {
  id: string;
  keyResultId: string;
  authorId: string;
  value: string;
  previousValue: string | null;
  confidence: ConfidenceLevel | null;
  note: string | null;
  mentions: string[] | null;
  createdAt: string;
  /** Preenchido em queries com join */
  author?: {
    id: string;
    firstName: string;
    lastName: string;
    initials: string | null;
  };
}
