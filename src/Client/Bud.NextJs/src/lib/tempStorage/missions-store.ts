import type { Mission, CheckIn } from "@/types";
import {
  MOCK_MISSIONS,
  MOCK_CHECKIN_HISTORY,
} from "@/lib/tempStorage/missions";

export type CheckInSyncStatus = "pending" | "synced" | "failed";

export interface LocalCheckIn extends CheckIn {
  clientMutationId: string;
  syncStatus: CheckInSyncStatus;
  syncedAt: string | null;
  error: string | null;
  deletedAt: string | null;
}

export interface CheckInOutboxItem {
  id: string;
  checkInId: string;
  keyResultId: string;
  operation: "create" | "update" | "delete";
  createdAt: string;
  attempts: number;
  status: "pending" | "processing" | "failed";
  lastError: string | null;
  nextRetryAt: string | null;
}

export interface MissionsStoreSnapshot {
  schemaVersion: number;
  updatedAt: string;
  missions: Mission[];
  checkInsById: Record<string, LocalCheckIn>;
  checkInIdsByKr: Record<string, string[]>;
  checkInOutbox: CheckInOutboxItem[];
}

const STORAGE_KEY_PREFIX = "bud.saas.missions-store";
const LEGACY_STORAGE_KEY = "bud.saas.missions-store";
const STORE_SCHEMA_VERSION = 4;
const DEFAULT_ORG_ID = "org-1";

function getStorageKey(orgId: string): string {
  return `${STORAGE_KEY_PREFIX}:${orgId}`;
}

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") {
    return structuredClone(value);
  }
  return JSON.parse(JSON.stringify(value)) as T;
}

function toLocalCheckIn(checkIn: CheckIn): LocalCheckIn {
  return {
    ...checkIn,
    clientMutationId: `seed-${checkIn.id}`,
    syncStatus: "synced",
    syncedAt: checkIn.createdAt,
    error: null,
    deletedAt: null,
  };
}

function normalizeCheckIns(history: Record<string, CheckIn[]>): {
  checkInsById: Record<string, LocalCheckIn>;
  checkInIdsByKr: Record<string, string[]>;
} {
  const checkInsById: Record<string, LocalCheckIn> = {};
  const checkInIdsByKr: Record<string, string[]> = {};

  for (const [keyResultId, items] of Object.entries(history)) {
    const sorted = [...items].sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    );
    const ids: string[] = [];
    for (const item of sorted) {
      checkInsById[item.id] = toLocalCheckIn(item);
      ids.push(item.id);
    }
    checkInIdsByKr[keyResultId] = ids;
  }

  return { checkInsById, checkInIdsByKr };
}

function normalizeFromLocalCheckIns(
  byId: Record<string, LocalCheckIn>,
  byKr: Record<string, string[]>,
): {
  checkInsById: Record<string, LocalCheckIn>;
  checkInIdsByKr: Record<string, string[]>;
} {
  const checkInsById: Record<string, LocalCheckIn> = {};
  const checkInIdsByKr: Record<string, string[]> = {};

  for (const [id, item] of Object.entries(byId)) {
    checkInsById[id] = {
      ...item,
      clientMutationId: item.clientMutationId || `legacy-${id}`,
      syncStatus: item.syncStatus || "synced",
      syncedAt: item.syncedAt ?? item.createdAt,
      error: item.error ?? null,
      deletedAt: item.deletedAt ?? null,
    };
  }

  for (const [keyResultId, ids] of Object.entries(byKr)) {
    const filtered = ids.filter((id) => !!checkInsById[id]);
    const unique = Array.from(new Set(filtered));
    unique.sort((a, b) => {
      const aDate = new Date(checkInsById[a]?.createdAt ?? 0).getTime();
      const bDate = new Date(checkInsById[b]?.createdAt ?? 0).getTime();
      return bDate - aDate;
    });
    checkInIdsByKr[keyResultId] = unique;
  }

  for (const [id, item] of Object.entries(checkInsById)) {
    const keyResultId = item.keyResultId;
    if (!checkInIdsByKr[keyResultId]) {
      checkInIdsByKr[keyResultId] = [id];
      continue;
    }
    if (!checkInIdsByKr[keyResultId].includes(id)) {
      checkInIdsByKr[keyResultId].push(id);
    }
  }

  for (const keyResultId of Object.keys(checkInIdsByKr)) {
    const ids = checkInIdsByKr[keyResultId];
    if (!ids) continue;
    ids.sort((a, b) => {
      const aDate = new Date(checkInsById[a]?.createdAt ?? 0).getTime();
      const bDate = new Date(checkInsById[b]?.createdAt ?? 0).getTime();
      return bDate - aDate;
    });
  }

  return { checkInsById, checkInIdsByKr };
}

function buildHistoryMap(
  checkInsById: Record<string, LocalCheckIn>,
  checkInIdsByKr: Record<string, string[]>,
): Record<string, CheckIn[]> {
  const history: Record<string, CheckIn[]> = {};
  for (const [keyResultId, ids] of Object.entries(checkInIdsByKr)) {
    const rows = ids
      .map((id) => checkInsById[id])
      .filter((item): item is LocalCheckIn => !!item)
      .filter((item) => item.deletedAt === null)
      .map((item) => ({
        id: item.id,
        keyResultId: item.keyResultId,
        authorId: item.authorId,
        value: item.value,
        previousValue: item.previousValue,
        confidence: item.confidence,
        note: item.note,
        mentions: item.mentions,
        createdAt: item.createdAt,
        author: item.author,
      }));

    if (rows.length > 0) {
      history[keyResultId] = rows;
    }
  }
  return history;
}

function mapKeyResultOrg(
  keyResult: NonNullable<Mission["keyResults"]>[number],
  orgId: string,
): NonNullable<Mission["keyResults"]>[number] {
  return {
    ...keyResult,
    orgId,
    children: keyResult.children?.map((child) => mapKeyResultOrg(child, orgId)),
  };
}

function mapMissionOrg(mission: Mission, orgId: string): Mission {
  return {
    ...mission,
    orgId,
    keyResults: mission.keyResults?.map((keyResult) =>
      mapKeyResultOrg(keyResult, orgId),
    ),
    children: mission.children?.map((child) => mapMissionOrg(child, orgId)),
  };
}

function getSeedSnapshot(orgId = DEFAULT_ORG_ID): MissionsStoreSnapshot {
  const normalized = normalizeCheckIns(cloneDeep(MOCK_CHECKIN_HISTORY));
  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: new Date().toISOString(),
    missions: cloneDeep(MOCK_MISSIONS).map((mission) =>
      mapMissionOrg(mission, orgId),
    ),
    checkInsById: normalized.checkInsById,
    checkInIdsByKr: normalized.checkInIdsByKr,
    checkInOutbox: [],
  };
}

function migrateSnapshot(
  raw: Partial<MissionsStoreSnapshot> | null,
  orgId = DEFAULT_ORG_ID,
): MissionsStoreSnapshot {
  const seed = getSeedSnapshot(orgId);
  const legacyRaw = raw as Partial<MissionsStoreSnapshot> & {
    checkInHistory?: Record<string, CheckIn[]>;
  };

  if (!raw || typeof raw !== "object") {
    return seed;
  }

  // On schema upgrade, reset missions to seed so new demo data is visible.
  // User check-ins (stored separately) are preserved.
  const isSchemaUpgrade = (raw.schemaVersion ?? 0) < STORE_SCHEMA_VERSION;
  const missions =
    !isSchemaUpgrade && Array.isArray(raw.missions)
      ? (raw.missions as Mission[])
      : seed.missions;

  const hasNormalized =
    raw.checkInsById &&
    typeof raw.checkInsById === "object" &&
    raw.checkInIdsByKr &&
    typeof raw.checkInIdsByKr === "object";

  const hasLegacyHistory =
    legacyRaw.checkInHistory && typeof legacyRaw.checkInHistory === "object";

  const normalized = hasNormalized
    ? normalizeFromLocalCheckIns(
        raw.checkInsById as Record<string, LocalCheckIn>,
        raw.checkInIdsByKr as Record<string, string[]>,
      )
    : hasLegacyHistory
      ? normalizeCheckIns(legacyRaw.checkInHistory as Record<string, CheckIn[]>)
      : {
          checkInsById: seed.checkInsById,
          checkInIdsByKr: seed.checkInIdsByKr,
        };

  const checkInOutbox = Array.isArray(raw.checkInOutbox)
    ? (raw.checkInOutbox as CheckInOutboxItem[]).map((item) => ({
        ...item,
        status: item.status === "processing" ? "pending" : item.status,
        attempts: typeof item.attempts === "number" ? item.attempts : 0,
        lastError: item.lastError ?? null,
        nextRetryAt: item.nextRetryAt ?? null,
      }))
    : [];

  return {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt:
      typeof raw.updatedAt === "string" ? raw.updatedAt : seed.updatedAt,
    missions,
    checkInsById: normalized.checkInsById,
    checkInIdsByKr: normalized.checkInIdsByKr,
    checkInOutbox,
  };
}

export function loadMissionsSnapshot(
  orgId = DEFAULT_ORG_ID,
): MissionsStoreSnapshot {
  if (typeof window === "undefined") {
    return getSeedSnapshot(orgId);
  }

  const storageKey = getStorageKey(orgId);
  const raw = window.localStorage.getItem(storageKey);
  if (!raw) {
    if (orgId === DEFAULT_ORG_ID) {
      const legacyRaw = window.localStorage.getItem(LEGACY_STORAGE_KEY);
      if (legacyRaw) {
        try {
          const parsedLegacy = JSON.parse(
            legacyRaw,
          ) as Partial<MissionsStoreSnapshot>;
          const migratedLegacy = migrateSnapshot(parsedLegacy, orgId);
          window.localStorage.setItem(
            storageKey,
            JSON.stringify(migratedLegacy),
          );
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
    const parsed = JSON.parse(raw) as Partial<MissionsStoreSnapshot>;
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

export function saveMissionsSnapshot(
  snapshot: Omit<MissionsStoreSnapshot, "updatedAt" | "schemaVersion">,
  orgId = DEFAULT_ORG_ID,
): MissionsStoreSnapshot {
  const next: MissionsStoreSnapshot = {
    schemaVersion: STORE_SCHEMA_VERSION,
    updatedAt: new Date().toISOString(),
    missions: cloneDeep(snapshot.missions),
    checkInsById: cloneDeep(snapshot.checkInsById),
    checkInIdsByKr: cloneDeep(snapshot.checkInIdsByKr),
    checkInOutbox: cloneDeep(snapshot.checkInOutbox),
  };

  if (typeof window !== "undefined") {
    window.localStorage.setItem(getStorageKey(orgId), JSON.stringify(next));
  }

  return next;
}

export function resetMissionsSnapshot(
  orgId = DEFAULT_ORG_ID,
): MissionsStoreSnapshot {
  const seed = getSeedSnapshot(orgId);
  if (typeof window !== "undefined") {
    window.localStorage.setItem(getStorageKey(orgId), JSON.stringify(seed));
  }
  return seed;
}

export function snapshotToCheckInHistory(
  snapshot: MissionsStoreSnapshot,
): Record<string, CheckIn[]> {
  return buildHistoryMap(snapshot.checkInsById, snapshot.checkInIdsByKr);
}
