"use client";

// TODO: migrate @/types (Mission, CheckIn) and @/lib/missions-store, @/lib/checkin-sync from bud-2-saas

import {
  createContext,
  useCallback,
  useEffect,
  useContext,
  useMemo,
  useRef,
  useState,
  type Dispatch,
  type ReactNode,
  type SetStateAction,
} from "react";
import type { Mission, CheckIn } from "@/types";
import {
  loadMissionsSnapshot,
  resetMissionsSnapshot,
  saveMissionsSnapshot,
  snapshotToCheckInHistory,
  type LocalCheckIn,
  type CheckInOutboxItem,
  type MissionsStoreSnapshot,
} from "@/lib/tempStorage/missions-store";
import { useConfigData } from "@/contexts/ConfigDataContext";
import { useActivityData } from "@/contexts/ActivityDataContext";
import { syncCheckInOutboxOperation } from "@/lib/tempStorage/checkin-sync";

type CheckInOperation = "create" | "update" | "delete";

interface CreateCheckInInput {
  keyResultId: string;
  authorId: string;
  value: string;
  previousValue: string | null;
  confidence: CheckIn["confidence"];
  note: string | null;
  mentions: string[] | null;
  author?: CheckIn["author"];
  createdAt?: string;
}

interface UpdateCheckInInput {
  value?: string;
  previousValue?: string | null;
  confidence?: CheckIn["confidence"];
  note?: string | null;
  mentions?: string[] | null;
}

interface CheckInSyncMeta {
  syncStatus: LocalCheckIn["syncStatus"];
  error: string | null;
  nextRetryAt: string | null;
}

interface MissionsDataContextValue {
  missions: Mission[];
  setMissions: Dispatch<SetStateAction<Mission[]>>;
  checkInHistory: Record<string, CheckIn[]>;
  getCheckInsByKeyResult: (keyResultId: string) => CheckIn[];
  getCheckInSyncMeta: (checkInId: string) => CheckInSyncMeta | null;
  createCheckIn: (input: CreateCheckInInput) => CheckIn;
  updateCheckIn: (
    checkInId: string,
    patch: UpdateCheckInInput,
  ) => CheckIn | null;
  deleteCheckIn: (checkInId: string) => void;
  retryCheckInSync: (checkInId: string) => void;
  resetToSeed: () => void;
  updatedAt: string;
}

const MissionsDataContext = createContext<MissionsDataContextValue | null>(
  null,
);

function toPublicCheckIn(local: LocalCheckIn): CheckIn {
  return {
    id: local.id,
    keyResultId: local.keyResultId,
    authorId: local.authorId,
    value: local.value,
    previousValue: local.previousValue,
    confidence: local.confidence,
    note: local.note,
    mentions: local.mentions,
    createdAt: local.createdAt,
    author: local.author,
  };
}

function createOutboxItem(
  operation: CheckInOperation,
  checkInId: string,
  keyResultId: string,
): CheckInOutboxItem {
  return {
    id: `outbox-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    checkInId,
    keyResultId,
    operation,
    createdAt: new Date().toISOString(),
    attempts: 0,
    status: "pending",
    lastError: null,
    nextRetryAt: null,
  };
}

type OutboxState = "none" | "create" | "update" | "delete" | "noop";

function reduceOutboxState(
  current: OutboxState,
  nextOperation: CheckInOperation,
): OutboxState {
  switch (current) {
    case "none":
      return nextOperation;
    case "create":
      if (nextOperation === "update") return "create";
      if (nextOperation === "delete") return "noop";
      return "create";
    case "update":
      if (nextOperation === "delete") return "delete";
      return "update";
    case "delete":
      return "delete";
    case "noop":
      return nextOperation;
    default:
      return nextOperation;
  }
}

function enqueueOutboxOperation(
  outbox: CheckInOutboxItem[],
  operation: CheckInOperation,
  checkInId: string,
  keyResultId: string,
): { nextOutbox: CheckInOutboxItem[]; shouldDropLocalCheckIn: boolean } {
  const processingForId = outbox.filter(
    (item) => item.checkInId === checkInId && item.status === "processing",
  );
  const queuedForId = outbox
    .filter(
      (item) => item.checkInId === checkInId && item.status !== "processing",
    )
    .sort(
      (a, b) =>
        new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
    );

  let effectiveState: OutboxState = "none";
  for (const item of queuedForId) {
    effectiveState = reduceOutboxState(effectiveState, item.operation);
  }
  effectiveState = reduceOutboxState(effectiveState, operation);

  const withoutQueuedForId = outbox.filter(
    (item) => !(item.checkInId === checkInId && item.status !== "processing"),
  );

  if (effectiveState === "none" || effectiveState === "noop") {
    return {
      nextOutbox: withoutQueuedForId,
      shouldDropLocalCheckIn:
        effectiveState === "noop" && processingForId.length === 0,
    };
  }

  const effectiveOperation: CheckInOperation = effectiveState;
  return {
    nextOutbox: [
      createOutboxItem(effectiveOperation, checkInId, keyResultId),
      ...withoutQueuedForId,
    ],
    shouldDropLocalCheckIn: false,
  };
}

function pickNextOutboxItem(
  outbox: CheckInOutboxItem[],
): CheckInOutboxItem | null {
  const now = Date.now();
  const eligible = outbox
    .filter(
      (item) =>
        item.status === "pending" ||
        (item.status === "failed" &&
          (!item.nextRetryAt || new Date(item.nextRetryAt).getTime() <= now)),
    )
    .sort(
      (a, b) =>
        new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
    );

  return eligible[0] ?? null;
}

function resolveRetryDelayMs(attempts: number): number {
  const capped = Math.min(Math.max(attempts, 1), 6);
  return Math.min(30000, 500 * 2 ** (capped - 1));
}

function getNextRetryDelay(outbox: CheckInOutboxItem[]): number | null {
  const now = Date.now();
  const pendingRetryTimes = outbox
    .filter((item) => item.status === "failed" && item.nextRetryAt)
    .map((item) => new Date(item.nextRetryAt ?? "").getTime())
    .filter((value) => !Number.isNaN(value));

  if (pendingRetryTimes.length === 0) return null;
  const nearest = Math.min(...pendingRetryTimes);
  return Math.max(0, nearest - now);
}

export function MissionsDataProvider({ children }: { children: ReactNode }) {
  const { activeOrgId } = useConfigData();
  const { logActivity } = useActivityData();
  const [snapshot, setSnapshot] = useState<MissionsStoreSnapshot>(() =>
    loadMissionsSnapshot(activeOrgId),
  );
  const snapshotRef = useRef(snapshot);
  const syncInFlightRef = useRef(false);

  useEffect(() => {
    setSnapshot(loadMissionsSnapshot(activeOrgId));
  }, [activeOrgId]);

  useEffect(() => {
    snapshotRef.current = snapshot;
  }, [snapshot]);

  const setMissions = useCallback<Dispatch<SetStateAction<Mission[]>>>(
    (updater) => {
      setSnapshot((prev) => {
        const nextMissions =
          typeof updater === "function"
            ? (updater as (prevState: Mission[]) => Mission[])(prev.missions)
            : updater;
        return saveMissionsSnapshot(
          {
            missions: nextMissions,
            checkInsById: prev.checkInsById,
            checkInIdsByKr: prev.checkInIdsByKr,
            checkInOutbox: prev.checkInOutbox,
          },
          activeOrgId,
        );
      });
    },
    [activeOrgId],
  );

  const getCheckInsByKeyResult = useCallback(
    (keyResultId: string): CheckIn[] => {
      const ids = snapshot.checkInIdsByKr[keyResultId] ?? [];
      return ids
        .map((id) => snapshot.checkInsById[id])
        .filter((item): item is LocalCheckIn => !!item)
        .filter((item) => item.deletedAt === null)
        .map((item) => toPublicCheckIn(item));
    },
    [snapshot.checkInIdsByKr, snapshot.checkInsById],
  );

  const getCheckInSyncMeta = useCallback(
    (checkInId: string): CheckInSyncMeta | null => {
      const local = snapshot.checkInsById[checkInId];
      if (!local) return null;

      const relatedFailed = snapshot.checkInOutbox.find(
        (item) => item.checkInId === checkInId && item.status === "failed",
      );

      return {
        syncStatus: local.syncStatus,
        error: local.error ?? relatedFailed?.lastError ?? null,
        nextRetryAt: relatedFailed?.nextRetryAt ?? null,
      };
    },
    [snapshot.checkInsById, snapshot.checkInOutbox],
  );

  const createCheckIn = useCallback(
    (input: CreateCheckInInput): CheckIn => {
      const createdAt = input.createdAt ?? new Date().toISOString();
      const id = `ck-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;

      const nextLocal: LocalCheckIn = {
        id,
        keyResultId: input.keyResultId,
        authorId: input.authorId,
        value: input.value,
        previousValue: input.previousValue,
        confidence: input.confidence,
        note: input.note,
        mentions: input.mentions,
        createdAt,
        author: input.author,
        clientMutationId: `mutation-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
        syncStatus: "pending",
        syncedAt: null,
        error: null,
        deletedAt: null,
      };

      setSnapshot((prev) => {
        const nextIds = [id, ...(prev.checkInIdsByKr[input.keyResultId] ?? [])];
        const deduped = Array.from(new Set(nextIds));
        const outboxResult = enqueueOutboxOperation(
          prev.checkInOutbox,
          "create",
          id,
          input.keyResultId,
        );

        return saveMissionsSnapshot(
          {
            missions: prev.missions,
            checkInsById: { ...prev.checkInsById, [id]: nextLocal },
            checkInIdsByKr: {
              ...prev.checkInIdsByKr,
              [input.keyResultId]: deduped,
            },
            checkInOutbox: outboxResult.nextOutbox,
          },
          activeOrgId,
        );
      });

      logActivity({
        userId: input.authorId,
        type: "checkin_create",
        entityId: input.keyResultId,
        entityType: "checkin",
        metadata: { keyResultId: input.keyResultId },
      });

      return toPublicCheckIn(nextLocal);
    },
    [activeOrgId, logActivity],
  );

  const updateCheckIn = useCallback(
    (checkInId: string, patch: UpdateCheckInInput): CheckIn | null => {
      const existing = snapshot.checkInsById[checkInId];
      if (!existing || existing.deletedAt !== null) return null;

      const updated: LocalCheckIn = {
        ...existing,
        value: patch.value ?? existing.value,
        previousValue: patch.previousValue ?? existing.previousValue,
        confidence: patch.confidence ?? existing.confidence,
        note: patch.note ?? existing.note,
        mentions: patch.mentions ?? existing.mentions,
        syncStatus: "pending",
        syncedAt: null,
        error: null,
      };

      setSnapshot((prev) => {
        const before = prev.checkInsById[checkInId];
        if (!before || before.deletedAt !== null) return prev;

        return saveMissionsSnapshot(
          {
            missions: prev.missions,
            checkInsById: { ...prev.checkInsById, [checkInId]: updated },
            checkInIdsByKr: prev.checkInIdsByKr,
            checkInOutbox: enqueueOutboxOperation(
              prev.checkInOutbox,
              "update",
              checkInId,
              before.keyResultId,
            ).nextOutbox,
          },
          activeOrgId,
        );
      });

      return toPublicCheckIn(updated);
    },
    [snapshot.checkInsById, activeOrgId],
  );

  const deleteCheckIn = useCallback(
    (checkInId: string) => {
      setSnapshot((prev) => {
        const existing = prev.checkInsById[checkInId];
        if (!existing || existing.deletedAt !== null) return prev;

        const updated: LocalCheckIn = {
          ...existing,
          deletedAt: new Date().toISOString(),
          syncStatus: "pending",
          syncedAt: null,
          error: null,
        };

        const outboxResult = enqueueOutboxOperation(
          prev.checkInOutbox,
          "delete",
          checkInId,
          existing.keyResultId,
        );

        if (outboxResult.shouldDropLocalCheckIn) {
          const nextById = { ...prev.checkInsById };
          delete nextById[checkInId];
          const nextIds = (
            prev.checkInIdsByKr[existing.keyResultId] ?? []
          ).filter((id) => id !== checkInId);

          return saveMissionsSnapshot(
            {
              missions: prev.missions,
              checkInsById: nextById,
              checkInIdsByKr: {
                ...prev.checkInIdsByKr,
                [existing.keyResultId]: nextIds,
              },
              checkInOutbox: outboxResult.nextOutbox,
            },
            activeOrgId,
          );
        }

        return saveMissionsSnapshot(
          {
            missions: prev.missions,
            checkInsById: { ...prev.checkInsById, [checkInId]: updated },
            checkInIdsByKr: prev.checkInIdsByKr,
            checkInOutbox: outboxResult.nextOutbox,
          },
          activeOrgId,
        );
      });
    },
    [activeOrgId],
  );

  const processOutbox = useCallback(async () => {
    if (syncInFlightRef.current) return;
    if (typeof navigator !== "undefined" && !navigator.onLine) return;

    const currentSnapshot = snapshotRef.current;
    const nextItem = pickNextOutboxItem(currentSnapshot.checkInOutbox);
    if (!nextItem) return;

    const currentCheckIn =
      currentSnapshot.checkInsById[nextItem.checkInId] ?? null;
    if (!currentCheckIn) {
      setSnapshot((prev) =>
        saveMissionsSnapshot(
          {
            missions: prev.missions,
            checkInsById: prev.checkInsById,
            checkInIdsByKr: prev.checkInIdsByKr,
            checkInOutbox: prev.checkInOutbox.filter(
              (item) => item.id !== nextItem.id,
            ),
          },
          activeOrgId,
        ),
      );
      return;
    }

    syncInFlightRef.current = true;
    try {
      setSnapshot((prev) => {
        const existing = prev.checkInsById[currentCheckIn.id];
        if (!existing) return prev;

        return saveMissionsSnapshot(
          {
            missions: prev.missions,
            checkInsById: {
              ...prev.checkInsById,
              [currentCheckIn.id]: {
                ...existing,
                syncStatus: "pending",
                error: null,
              },
            },
            checkInIdsByKr: prev.checkInIdsByKr,
            checkInOutbox: prev.checkInOutbox.map((item) =>
              item.id === nextItem.id
                ? {
                    ...item,
                    status: "processing",
                    lastError: null,
                    nextRetryAt: null,
                  }
                : item,
            ),
          },
          activeOrgId,
        );
      });

      try {
        const result = await syncCheckInOutboxOperation({
          operation: nextItem.operation,
          checkIn: currentCheckIn,
        });

        setSnapshot((prev) => {
          const existing = prev.checkInsById[nextItem.checkInId];
          const remainingOutbox = prev.checkInOutbox.filter(
            (item) => item.id !== nextItem.id,
          );

          if (!existing) {
            return saveMissionsSnapshot(
              {
                missions: prev.missions,
                checkInsById: prev.checkInsById,
                checkInIdsByKr: prev.checkInIdsByKr,
                checkInOutbox: remainingOutbox,
              },
              activeOrgId,
            );
          }

          const hasMoreOpsForCheckIn = remainingOutbox.some(
            (item) => item.checkInId === existing.id,
          );

          if (
            nextItem.operation === "delete" &&
            existing.deletedAt !== null &&
            !hasMoreOpsForCheckIn
          ) {
            const nextById = { ...prev.checkInsById };
            delete nextById[existing.id];
            const nextIds = (
              prev.checkInIdsByKr[existing.keyResultId] ?? []
            ).filter((id) => id !== existing.id);

            return saveMissionsSnapshot(
              {
                missions: prev.missions,
                checkInsById: nextById,
                checkInIdsByKr: {
                  ...prev.checkInIdsByKr,
                  [existing.keyResultId]: nextIds,
                },
                checkInOutbox: remainingOutbox,
              },
              activeOrgId,
            );
          }

          return saveMissionsSnapshot(
            {
              missions: prev.missions,
              checkInsById: {
                ...prev.checkInsById,
                [existing.id]: {
                  ...existing,
                  syncStatus: hasMoreOpsForCheckIn ? "pending" : "synced",
                  syncedAt: hasMoreOpsForCheckIn
                    ? existing.syncedAt
                    : result.syncedAt,
                  error: null,
                },
              },
              checkInIdsByKr: prev.checkInIdsByKr,
              checkInOutbox: remainingOutbox,
            },
            activeOrgId,
          );
        });
      } catch (error) {
        const message =
          error instanceof Error
            ? error.message
            : "Falha ao sincronizar check-in";

        setSnapshot((prev) => {
          const failedItem = prev.checkInOutbox.find(
            (item) => item.id === nextItem.id,
          );
          if (!failedItem) return prev;

          const nextAttempts = failedItem.attempts + 1;
          const nextRetryAt = new Date(
            Date.now() + resolveRetryDelayMs(nextAttempts),
          ).toISOString();
          const existing = prev.checkInsById[nextItem.checkInId];

          return saveMissionsSnapshot(
            {
              missions: prev.missions,
              checkInsById: existing
                ? {
                    ...prev.checkInsById,
                    [existing.id]: {
                      ...existing,
                      syncStatus: "failed",
                      error: message,
                    },
                  }
                : prev.checkInsById,
              checkInIdsByKr: prev.checkInIdsByKr,
              checkInOutbox: prev.checkInOutbox.map((item) =>
                item.id === nextItem.id
                  ? {
                      ...item,
                      status: "failed",
                      attempts: nextAttempts,
                      lastError: message,
                      nextRetryAt,
                    }
                  : item,
              ),
            },
            activeOrgId,
          );
        });
      }
    } finally {
      syncInFlightRef.current = false;
    }
  }, [activeOrgId]);

  const retryCheckInSync = useCallback(
    (checkInId: string) => {
      setSnapshot((prev) => {
        const hasFailedEntry = prev.checkInOutbox.some(
          (item) => item.checkInId === checkInId && item.status === "failed",
        );
        if (!hasFailedEntry) return prev;

        const existing = prev.checkInsById[checkInId];

        return saveMissionsSnapshot(
          {
            missions: prev.missions,
            checkInsById: existing
              ? {
                  ...prev.checkInsById,
                  [checkInId]: {
                    ...existing,
                    syncStatus: "pending",
                    error: null,
                  },
                }
              : prev.checkInsById,
            checkInIdsByKr: prev.checkInIdsByKr,
            checkInOutbox: prev.checkInOutbox.map((item) =>
              item.checkInId === checkInId && item.status === "failed"
                ? {
                    ...item,
                    status: "pending",
                    lastError: null,
                    nextRetryAt: null,
                  }
                : item,
            ),
          },
          activeOrgId,
        );
      });
    },
    [activeOrgId],
  );

  useEffect(() => {
    void processOutbox();
  }, [snapshot.checkInOutbox, processOutbox]);

  useEffect(() => {
    if (typeof window === "undefined") return;

    const handleOnline = () => {
      setSnapshot((prev) => {
        const hasFailed = prev.checkInOutbox.some(
          (item) => item.status === "failed",
        );
        if (!hasFailed) return prev;

        const nextById: Record<string, LocalCheckIn> = {};
        for (const [id, checkIn] of Object.entries(prev.checkInsById)) {
          nextById[id] =
            checkIn.syncStatus === "failed"
              ? { ...checkIn, syncStatus: "pending", error: null }
              : checkIn;
        }

        return saveMissionsSnapshot(
          {
            missions: prev.missions,
            checkInsById: nextById,
            checkInIdsByKr: prev.checkInIdsByKr,
            checkInOutbox: prev.checkInOutbox.map((item) =>
              item.status === "failed"
                ? {
                    ...item,
                    status: "pending",
                    lastError: null,
                    nextRetryAt: null,
                  }
                : item,
            ),
          },
          activeOrgId,
        );
      });
      void processOutbox();
    };

    window.addEventListener("online", handleOnline);
    return () => window.removeEventListener("online", handleOnline);
  }, [processOutbox, activeOrgId]);

  useEffect(() => {
    if (typeof window === "undefined") return;

    const delay = getNextRetryDelay(snapshot.checkInOutbox);
    if (delay === null) return;

    const timerId = window.setTimeout(() => {
      void processOutbox();
    }, delay + 10);

    return () => window.clearTimeout(timerId);
  }, [snapshot.checkInOutbox, processOutbox]);

  const resetToSeed = useCallback(() => {
    setSnapshot(resetMissionsSnapshot(activeOrgId));
  }, [activeOrgId]);

  const checkInHistory = useMemo(
    () => snapshotToCheckInHistory(snapshot),
    [snapshot],
  );

  const value = useMemo<MissionsDataContextValue>(
    () => ({
      missions: snapshot.missions,
      setMissions,
      checkInHistory,
      getCheckInsByKeyResult,
      getCheckInSyncMeta,
      createCheckIn,
      updateCheckIn,
      deleteCheckIn,
      retryCheckInSync,
      resetToSeed,
      updatedAt: snapshot.updatedAt,
    }),
    [
      snapshot.missions,
      snapshot.updatedAt,
      setMissions,
      checkInHistory,
      getCheckInsByKeyResult,
      getCheckInSyncMeta,
      createCheckIn,
      updateCheckIn,
      deleteCheckIn,
      retryCheckInSync,
      resetToSeed,
    ],
  );

  return (
    <MissionsDataContext.Provider value={value}>
      {children}
    </MissionsDataContext.Provider>
  );
}

export function useMissionsData() {
  const ctx = useContext(MissionsDataContext);
  if (!ctx) {
    throw new Error("useMissionsData must be used within MissionsDataProvider");
  }
  return ctx;
}
