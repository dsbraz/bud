import type { LocalCheckIn } from "@/lib/missions-store";

type CheckInSyncOperation = "create" | "update" | "delete";

interface SyncCheckInInput {
  operation: CheckInSyncOperation;
  checkIn: LocalCheckIn;
}

interface SyncCheckInResult {
  syncedAt: string;
}

const REQUEST_TIMEOUT_MS = 10000;

function getSyncEndpoint(): string | null {
  const raw = import.meta.env.VITE_CHECKIN_SYNC_URL as string | undefined;
  if (!raw) return null;
  const trimmed = raw.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function wait(ms: number): Promise<void> {
  return new Promise((resolve) => {
    globalThis.setTimeout(resolve, ms);
  });
}

function toPayload(checkIn: LocalCheckIn) {
  return {
    id: checkIn.id,
    keyResultId: checkIn.keyResultId,
    authorId: checkIn.authorId,
    value: checkIn.value,
    previousValue: checkIn.previousValue,
    confidence: checkIn.confidence,
    note: checkIn.note,
    mentions: checkIn.mentions,
    createdAt: checkIn.createdAt,
    clientMutationId: checkIn.clientMutationId,
    deletedAt: checkIn.deletedAt,
  };
}

async function requestWithTimeout(input: RequestInfo | URL, init: RequestInit, timeoutMs: number): Promise<Response> {
  const controller = new AbortController();
  const timerId = globalThis.setTimeout(() => {
    controller.abort();
  }, timeoutMs);

  try {
    return await fetch(input, {
      ...init,
      signal: controller.signal,
    });
  } finally {
    globalThis.clearTimeout(timerId);
  }
}

export async function syncCheckInOutboxOperation(input: SyncCheckInInput): Promise<SyncCheckInResult> {
  if (typeof navigator !== "undefined" && !navigator.onLine) {
    throw new Error("Sem conexão com a internet");
  }

  const endpoint = getSyncEndpoint();
  if (!endpoint) {
    if (!import.meta.env.DEV) {
      throw new Error("VITE_CHECKIN_SYNC_URL nao configurada");
    }
    await wait(140);
    return { syncedAt: new Date().toISOString() };
  }

  const method = input.operation === "create" ? "POST" : input.operation === "update" ? "PATCH" : "DELETE";
  const url = input.operation === "create"
    ? endpoint
    : `${endpoint.replace(/\/$/, "")}/${encodeURIComponent(input.checkIn.id)}`;

  const hasBody = input.operation !== "delete";
  const response = await requestWithTimeout(
    url,
    {
      method,
      headers: {
        "Content-Type": "application/json",
      },
      body: hasBody ? JSON.stringify(toPayload(input.checkIn)) : undefined,
    },
    REQUEST_TIMEOUT_MS,
  );

  if (!response.ok) {
    const responseText = await response.text();
    const message = responseText.trim().length > 0
      ? responseText
      : `HTTP ${response.status}`;
    throw new Error(`Falha ao sincronizar check-in: ${message}`);
  }

  return { syncedAt: new Date().toISOString() };
}
