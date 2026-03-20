"use client";

// ─── ActivityDataContext ──────────────────────────────────────────────────────
// Context para rastreamento de atividades dos usuários.
// Persiste atividades no localStorage e expõe queries.
//
// TODO: migrate @/lib/activity-store and @/types/activity from bud-2-saas

import {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from "react";

import type { UserActivity } from "@/types/activity/activity";

import {
  loadActivitySnapshot,
  saveActivitySnapshot,
  addActivity,
  getActivitiesForUser,
  type ActivityStoreSnapshot,
} from "@/lib/tempStorage/activity-store";

// ── Tipos do context ──────────────────────────────────────────────────────────

interface ActivityDataContextValue {
  /** Todas as atividades carregadas */
  activities: UserActivity[];

  /** Registra uma nova atividade */
  logActivity: (activity: Omit<UserActivity, "id" | "createdAt">) => void;

  /** Retorna atividades de um usuário nos últimos N dias */
  getActivitiesForUserId: (userId: string, days?: number) => UserActivity[];
}

// ── Context ───────────────────────────────────────────────────────────────────

const ActivityDataContext = createContext<ActivityDataContextValue | null>(
  null,
);

// ── Provider ──────────────────────────────────────────────────────────────────

interface ActivityDataProviderProps {
  children: ReactNode;
}

export function ActivityDataProvider({ children }: ActivityDataProviderProps) {
  const [snapshot, setSnapshot] = useState<ActivityStoreSnapshot>(() =>
    loadActivitySnapshot(),
  );

  const logActivity = useCallback(
    (activity: Omit<UserActivity, "id" | "createdAt">) => {
      setSnapshot((prev) => {
        const next = addActivity(prev, activity);
        saveActivitySnapshot(next);
        return next;
      });
    },
    [],
  );

  const getActivitiesForUserId = useCallback(
    (userId: string, days = 30) =>
      getActivitiesForUser(snapshot.activities, userId, days),
    [snapshot.activities],
  );

  return (
    <ActivityDataContext.Provider
      value={{
        activities: snapshot.activities,
        logActivity,
        getActivitiesForUserId,
      }}
    >
      {children}
    </ActivityDataContext.Provider>
  );
}

export function useActivityData(): ActivityDataContextValue {
  const ctx = useContext(ActivityDataContext);
  if (!ctx) {
    throw new Error(
      "useActivityData deve ser usado dentro de ActivityDataProvider",
    );
  }
  return ctx;
}
