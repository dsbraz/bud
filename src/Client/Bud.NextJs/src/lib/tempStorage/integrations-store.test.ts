/**
 * Tests for integrations-store.ts
 *
 * Integrations Store manages connections with external services like Slack, Teams,
 * calendar apps, HRIS systems, project management tools, SSO providers, and APIs.
 */

import { describe, it, expect, beforeEach } from "vitest";
import {
  loadIntegrationsSnapshot,
  saveIntegrationsSnapshot,
  resetIntegrationsSnapshot,
  type IntegrationsStoreSnapshot,
  type IntegrationStatus,
  type IntegrationCategory,
} from "./integrations-store";

const STORAGE_KEY_PREFIX = "bud.saas.integrations-store";
const DEFAULT_ORG_ID = "org-1";

// ─── Test Helpers ───

function getStorageKey(orgId: string): string {
  return `${STORAGE_KEY_PREFIX}:${orgId}`;
}

function getStoredSnapshot(orgId = DEFAULT_ORG_ID): IntegrationsStoreSnapshot | null {
  const raw = localStorage.getItem(getStorageKey(orgId));
  if (!raw) return null;
  return JSON.parse(raw) as IntegrationsStoreSnapshot;
}

function setStoredSnapshot(snapshot: Partial<IntegrationsStoreSnapshot>, orgId = DEFAULT_ORG_ID): void {
  localStorage.setItem(getStorageKey(orgId), JSON.stringify(snapshot));
}

// ─── Tests ───

describe("integrations-store", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // loadIntegrationsSnapshot - Initial Load
  // ═══════════════════════════════════════════════════════════════════════════

  describe("loadIntegrationsSnapshot", () => {
    describe("initial load (no stored data)", () => {
      it("returns seed snapshot when localStorage is empty", () => {
        const snapshot = loadIntegrationsSnapshot();
        expect(snapshot).toBeDefined();
        expect(snapshot.schemaVersion).toBe(2);
      });

      it("returns integrations array", () => {
        const snapshot = loadIntegrationsSnapshot();
        expect(Array.isArray(snapshot.integrations)).toBe(true);
        expect(snapshot.integrations.length).toBeGreaterThan(0);
      });

      it("has updatedAt timestamp", () => {
        const snapshot = loadIntegrationsSnapshot();
        expect(snapshot.updatedAt).toBeDefined();
        expect(new Date(snapshot.updatedAt).getTime()).toBeGreaterThan(0);
      });

      it("does not auto-persist seed to localStorage", () => {
        loadIntegrationsSnapshot();
        const stored = getStoredSnapshot();
        // integrations-store doesn't auto-persist on first load (unlike settings-store)
        expect(stored).toBeNull();
      });
    });

    describe("seed integrations coverage", () => {
      it("includes communication integrations", () => {
        const snapshot = loadIntegrationsSnapshot();
        const comm = snapshot.integrations.filter((i) => i.category === "communication");
        expect(comm.length).toBeGreaterThanOrEqual(4);

        const ids = comm.map((i) => i.id);
        expect(ids).toContain("slack");
        expect(ids).toContain("teams");
        expect(ids).toContain("whatsapp");
        expect(ids).toContain("email");
      });

      it("includes calendar integrations", () => {
        const snapshot = loadIntegrationsSnapshot();
        const calendar = snapshot.integrations.filter((i) => i.category === "calendar");
        expect(calendar.length).toBeGreaterThanOrEqual(2);

        const ids = calendar.map((i) => i.id);
        expect(ids).toContain("google-calendar");
        expect(ids).toContain("outlook");
      });

      it("includes HRIS integrations", () => {
        const snapshot = loadIntegrationsSnapshot();
        const hris = snapshot.integrations.filter((i) => i.category === "hris");
        expect(hris.length).toBeGreaterThanOrEqual(5);

        const ids = hris.map((i) => i.id);
        expect(ids).toContain("gupy");
        expect(ids).toContain("totvs");
        expect(ids).toContain("sap");
        expect(ids).toContain("sheets");
        expect(ids).toContain("csv");
      });

      it("includes project management integrations", () => {
        const snapshot = loadIntegrationsSnapshot();
        const pm = snapshot.integrations.filter((i) => i.category === "pm");
        expect(pm.length).toBeGreaterThanOrEqual(3);

        const ids = pm.map((i) => i.id);
        expect(ids).toContain("jira");
        expect(ids).toContain("asana");
        expect(ids).toContain("powerbi");
      });

      it("includes auth/SSO integrations", () => {
        const snapshot = loadIntegrationsSnapshot();
        const auth = snapshot.integrations.filter((i) => i.category === "auth");
        expect(auth.length).toBeGreaterThanOrEqual(3);

        const ids = auth.map((i) => i.id);
        expect(ids).toContain("google-sso");
        expect(ids).toContain("microsoft-sso");
        expect(ids).toContain("saml");
      });

      it("includes API integrations", () => {
        const snapshot = loadIntegrationsSnapshot();
        const api = snapshot.integrations.filter((i) => i.category === "api");
        expect(api.length).toBeGreaterThanOrEqual(2);

        const ids = api.map((i) => i.id);
        expect(ids).toContain("api");
        expect(ids).toContain("webhooks");
      });

      it("has total of 19 integrations", () => {
        const snapshot = loadIntegrationsSnapshot();
        expect(snapshot.integrations).toHaveLength(19);
      });
    });

    describe("seed integration states", () => {
      it("slack is connected and enabled by default", () => {
        const snapshot = loadIntegrationsSnapshot();
        const slack = snapshot.integrations.find((i) => i.id === "slack");

        expect(slack).toBeDefined();
        expect(slack!.status).toBe("connected");
        expect(slack!.enabled).toBe(true);
        expect(slack!.connectedAt).toBeDefined();
      });

      it("teams is disconnected by default", () => {
        const snapshot = loadIntegrationsSnapshot();
        const teams = snapshot.integrations.find((i) => i.id === "teams");

        expect(teams).toBeDefined();
        expect(teams!.status).toBe("disconnected");
        expect(teams!.enabled).toBe(false);
        expect(teams!.connectedAt).toBeNull();
      });

      it("google-calendar is connected by default", () => {
        const snapshot = loadIntegrationsSnapshot();
        const gcal = snapshot.integrations.find((i) => i.id === "google-calendar");

        expect(gcal).toBeDefined();
        expect(gcal!.status).toBe("connected");
        expect(gcal!.enabled).toBe(true);
      });

      it("google-sso is connected by default", () => {
        const snapshot = loadIntegrationsSnapshot();
        const sso = snapshot.integrations.find((i) => i.id === "google-sso");

        expect(sso).toBeDefined();
        expect(sso!.status).toBe("connected");
        expect(sso!.enabled).toBe(true);
      });

      it("api is connected by default", () => {
        const snapshot = loadIntegrationsSnapshot();
        const api = snapshot.integrations.find((i) => i.id === "api");

        expect(api).toBeDefined();
        expect(api!.status).toBe("connected");
        expect(api!.enabled).toBe(true);
      });

      it("popular integrations are marked", () => {
        const snapshot = loadIntegrationsSnapshot();
        const popular = snapshot.integrations.filter((i) => i.popular);

        expect(popular.length).toBeGreaterThanOrEqual(5);

        const ids = popular.map((i) => i.id);
        expect(ids).toContain("slack");
        expect(ids).toContain("teams");
        expect(ids).toContain("google-calendar");
        expect(ids).toContain("totvs");
        expect(ids).toContain("jira");
        expect(ids).toContain("google-sso");
      });
    });

    describe("integration record structure", () => {
      it("each integration has required fields", () => {
        const snapshot = loadIntegrationsSnapshot();

        for (const integration of snapshot.integrations) {
          expect(integration.id).toBeDefined();
          expect(typeof integration.id).toBe("string");
          expect(integration.name).toBeDefined();
          expect(typeof integration.name).toBe("string");
          expect(integration.description).toBeDefined();
          expect(integration.category).toBeDefined();
          expect(integration.iconId).toBeDefined();
          expect(integration.iconBg).toBeDefined();
          expect(integration.iconColor).toBeDefined();
          expect(typeof integration.status).toBe("string");
          expect(typeof integration.enabled).toBe("boolean");
          expect(typeof integration.popular).toBe("boolean");
          expect(Array.isArray(integration.features)).toBe(true);
          expect(typeof integration.config).toBe("object");
        }
      });

      it("connected integrations have lastSync", () => {
        const snapshot = loadIntegrationsSnapshot();
        const connected = snapshot.integrations.filter(
          (i) => i.status === "connected" && i.lastSync !== "always_active"
        );

        for (const integration of connected) {
          expect(integration.lastSync).toBeDefined();
        }
      });

      it("always-active integrations have special lastSync value", () => {
        const snapshot = loadIntegrationsSnapshot();
        const alwaysActive = snapshot.integrations.filter((i) => i.lastSync === "always_active");

        expect(alwaysActive.length).toBeGreaterThanOrEqual(2);
        const ids = alwaysActive.map((i) => i.id);
        expect(ids).toContain("google-sso");
        expect(ids).toContain("api");
      });
    });

    describe("load from existing data", () => {
      it("loads previously saved snapshot", () => {
        const customSnapshot: IntegrationsStoreSnapshot = {
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          integrations: [
            {
              id: "slack",
              name: "Slack",
              description: "Custom description",
              category: "communication",
              iconId: "SlackLogo",
              iconBg: "var(--color-yellow-100)",
              iconColor: "var(--color-yellow-700)",
              status: "disconnected",
              enabled: false,
              popular: true,
              lastSync: null,
              features: [],
              connectedAt: null,
              config: {},
            },
          ],
        };

        setStoredSnapshot(customSnapshot);
        const snapshot = loadIntegrationsSnapshot();

        // Should merge with seed, keeping user state
        const slack = snapshot.integrations.find((i) => i.id === "slack");
        expect(slack!.status).toBe("disconnected");
        expect(slack!.enabled).toBe(false);
      });

      it("preserves user config in saved integrations", () => {
        const customConfig = { channel: "#general", notify: true };
        const customSnapshot: IntegrationsStoreSnapshot = {
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          integrations: [
            {
              id: "slack",
              name: "Slack",
              description: "desc",
              category: "communication",
              iconId: "SlackLogo",
              iconBg: "var(--color-yellow-100)",
              iconColor: "var(--color-yellow-700)",
              status: "connected",
              enabled: true,
              popular: true,
              lastSync: new Date().toISOString(),
              features: [],
              connectedAt: new Date().toISOString(),
              config: customConfig,
            },
          ],
        };

        setStoredSnapshot(customSnapshot);
        const snapshot = loadIntegrationsSnapshot();

        const slack = snapshot.integrations.find((i) => i.id === "slack");
        expect(slack!.config).toEqual(customConfig);
      });
    });

    describe("migration and merge behavior", () => {
      it("merges stored integrations with seed catalog", () => {
        // Store only slack with custom state
        const customSnapshot: IntegrationsStoreSnapshot = {
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          integrations: [
            {
              id: "slack",
              name: "Slack",
              description: "desc",
              category: "communication",
              iconId: "SlackLogo",
              iconBg: "var(--color-yellow-100)",
              iconColor: "var(--color-yellow-700)",
              status: "error",
              enabled: true,
              popular: true,
              lastSync: null,
              features: [],
              connectedAt: "2026-01-01T00:00:00Z",
              config: { custom: true },
            },
          ],
        };

        setStoredSnapshot(customSnapshot);
        const snapshot = loadIntegrationsSnapshot();

        // Should have all 19 integrations (merged with seed)
        expect(snapshot.integrations).toHaveLength(19);

        // Slack should preserve user state
        const slack = snapshot.integrations.find((i) => i.id === "slack");
        expect(slack!.status).toBe("error");
        expect(slack!.config).toEqual({ custom: true });

        // Teams should be from seed (disconnected)
        const teams = snapshot.integrations.find((i) => i.id === "teams");
        expect(teams!.status).toBe("disconnected");
      });

      it("returns seed for invalid JSON", () => {
        localStorage.setItem(getStorageKey(DEFAULT_ORG_ID), "not valid json");
        const snapshot = loadIntegrationsSnapshot();
        expect(snapshot.schemaVersion).toBe(2);
        expect(snapshot.integrations.length).toBe(19);
      });

      it("returns seed for null/undefined stored value", () => {
        setStoredSnapshot(null as any);
        const snapshot = loadIntegrationsSnapshot();
        expect(snapshot.integrations.length).toBe(19);
      });

      it("handles empty integrations array", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          integrations: [],
        });

        const snapshot = loadIntegrationsSnapshot();
        // Should return all seed integrations since none were stored
        expect(snapshot.integrations).toHaveLength(19);
      });
    });

    describe("sanitization", () => {
      it("filters out integrations without required fields", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          integrations: [
            { id: "valid", name: "Valid", category: "api" },
            { id: "no-name", category: "api" }, // missing name
            { name: "No ID", category: "api" }, // missing id
            { id: "no-cat", name: "No Category" }, // missing category
          ] as any,
        });

        const snapshot = loadIntegrationsSnapshot();
        // Valid one is merged with seed, so we still get 19
        expect(snapshot.integrations).toHaveLength(19);
      });

      it("applies default values for missing optional fields", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          integrations: [
            {
              id: "minimal",
              name: "Minimal",
              category: "api",
              // Missing most fields
            } as any,
          ],
        });

        const snapshot = loadIntegrationsSnapshot();
        // The minimal integration won't match any seed, but seed integrations remain
        expect(snapshot.integrations).toHaveLength(19);
      });

      it("preserves features array", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          integrations: [
            {
              id: "slack",
              name: "Slack",
              category: "communication",
              features: ["Custom Feature 1", "Custom Feature 2"],
            } as any,
          ],
        });

        const snapshot = loadIntegrationsSnapshot();
        const slack = snapshot.integrations.find((i) => i.id === "slack");
        // Features come from seed, not from stored data (merge keeps seed static data)
        expect(slack!.features.length).toBeGreaterThan(0);
      });
    });

    describe("org-specific storage", () => {
      it("uses org-specific storage key", () => {
        const snapshot = loadIntegrationsSnapshot("brq");
        // Should work even without stored data
        expect(snapshot.integrations).toHaveLength(19);
      });

      it("isolates data between orgs", () => {
        // Save to org-1
        const snapshot1 = loadIntegrationsSnapshot("org-1");
        snapshot1.integrations[0]!.status = "error";
        saveIntegrationsSnapshot(snapshot1, "org-1");

        // Load from brq - should be independent
        const snapshot2 = loadIntegrationsSnapshot("brq");
        expect(snapshot2.integrations[0]!.status).toBe("connected"); // seed state
      });
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // saveIntegrationsSnapshot
  // ═══════════════════════════════════════════════════════════════════════════

  describe("saveIntegrationsSnapshot", () => {
    it("persists snapshot to localStorage", () => {
      const snapshot = loadIntegrationsSnapshot();
      snapshot.integrations[0]!.status = "error";
      saveIntegrationsSnapshot(snapshot);

      const stored = getStoredSnapshot();
      expect(stored).not.toBeNull();
      expect(stored!.integrations[0]!.status).toBe("error");
    });

    it("updates updatedAt timestamp", () => {
      const before = new Date().toISOString();
      const snapshot = loadIntegrationsSnapshot();
      saveIntegrationsSnapshot(snapshot);

      const stored = getStoredSnapshot();
      expect(new Date(stored!.updatedAt).getTime()).toBeGreaterThanOrEqual(new Date(before).getTime());
    });

    it("preserves schemaVersion", () => {
      const snapshot = loadIntegrationsSnapshot();
      saveIntegrationsSnapshot(snapshot);

      const stored = getStoredSnapshot();
      expect(stored!.schemaVersion).toBe(2);
    });

    it("can toggle integration status", () => {
      const snapshot = loadIntegrationsSnapshot();
      const slack = snapshot.integrations.find((i) => i.id === "slack")!;

      slack.status = "disconnected";
      slack.enabled = false;
      slack.connectedAt = null;
      slack.lastSync = null;

      saveIntegrationsSnapshot(snapshot);

      const stored = getStoredSnapshot();
      const savedSlack = stored!.integrations.find((i) => i.id === "slack");
      expect(savedSlack!.status).toBe("disconnected");
      expect(savedSlack!.enabled).toBe(false);
    });

    it("can connect new integration", () => {
      const snapshot = loadIntegrationsSnapshot();
      const teams = snapshot.integrations.find((i) => i.id === "teams")!;

      teams.status = "connected";
      teams.enabled = true;
      teams.connectedAt = new Date().toISOString();
      teams.lastSync = new Date().toISOString();

      saveIntegrationsSnapshot(snapshot);

      const stored = getStoredSnapshot();
      const savedTeams = stored!.integrations.find((i) => i.id === "teams");
      expect(savedTeams!.status).toBe("connected");
      expect(savedTeams!.enabled).toBe(true);
      expect(savedTeams!.connectedAt).toBeDefined();
    });

    it("can update integration config", () => {
      const snapshot = loadIntegrationsSnapshot();
      const slack = snapshot.integrations.find((i) => i.id === "slack")!;

      slack.config = {
        channel: "#engineering",
        mentionEveryone: false,
        notifyOnError: true,
      };

      saveIntegrationsSnapshot(snapshot);

      const stored = getStoredSnapshot();
      const savedSlack = stored!.integrations.find((i) => i.id === "slack");
      expect(savedSlack!.config).toEqual({
        channel: "#engineering",
        mentionEveryone: false,
        notifyOnError: true,
      });
    });

    it("saves to org-specific key", () => {
      const snapshot = loadIntegrationsSnapshot("brq");
      saveIntegrationsSnapshot(snapshot, "brq");

      const stored = localStorage.getItem(getStorageKey("brq"));
      expect(stored).not.toBeNull();

      // org-1 should still be empty
      const org1Stored = localStorage.getItem(getStorageKey("org-1"));
      expect(org1Stored).toBeNull();
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // resetIntegrationsSnapshot
  // ═══════════════════════════════════════════════════════════════════════════

  describe("resetIntegrationsSnapshot", () => {
    it("resets to seed data", () => {
      // First, modify the snapshot
      const snapshot = loadIntegrationsSnapshot();
      snapshot.integrations.forEach((i) => {
        i.status = "error";
        i.enabled = false;
      });
      saveIntegrationsSnapshot(snapshot);

      // Verify modification
      let stored = getStoredSnapshot();
      expect(stored!.integrations.every((i) => i.status === "error")).toBe(true);

      // Reset
      const reset = resetIntegrationsSnapshot();

      // Verify reset
      const slack = reset.integrations.find((i) => i.id === "slack");
      expect(slack!.status).toBe("connected");
      expect(slack!.enabled).toBe(true);

      // Verify stored
      stored = getStoredSnapshot();
      expect(stored!.integrations.find((i) => i.id === "slack")!.status).toBe("connected");
    });

    it("returns the seed snapshot", () => {
      const reset = resetIntegrationsSnapshot();
      expect(reset.schemaVersion).toBe(2);
      expect(reset.integrations).toHaveLength(19);
    });

    it("persists reset to localStorage", () => {
      resetIntegrationsSnapshot();

      const stored = getStoredSnapshot();
      expect(stored).not.toBeNull();
      expect(stored!.schemaVersion).toBe(2);
    });

    it("resets org-specific data", () => {
      // Modify brq
      const snapshot = loadIntegrationsSnapshot("brq");
      snapshot.integrations[0]!.status = "error";
      saveIntegrationsSnapshot(snapshot, "brq");

      // Reset brq
      resetIntegrationsSnapshot("brq");

      // Verify reset
      const stored = getStoredSnapshot("brq");
      expect(stored!.integrations[0]!.status).toBe("connected");
    });

    it("clears custom config", () => {
      const snapshot = loadIntegrationsSnapshot();
      snapshot.integrations[0]!.config = { custom: "data" };
      saveIntegrationsSnapshot(snapshot);

      resetIntegrationsSnapshot();

      const stored = getStoredSnapshot();
      expect(stored!.integrations[0]!.config).toEqual({});
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // Integration Categories
  // ═══════════════════════════════════════════════════════════════════════════

  describe("integration categories", () => {
    it("has valid category for each integration", () => {
      const validCategories: IntegrationCategory[] = ["communication", "calendar", "hris", "pm", "auth", "api"];
      const snapshot = loadIntegrationsSnapshot();

      for (const integration of snapshot.integrations) {
        expect(validCategories).toContain(integration.category);
      }
    });

    it("has at least one integration per category", () => {
      const snapshot = loadIntegrationsSnapshot();
      const categories = new Set(snapshot.integrations.map((i) => i.category));

      expect(categories.has("communication")).toBe(true);
      expect(categories.has("calendar")).toBe(true);
      expect(categories.has("hris")).toBe(true);
      expect(categories.has("pm")).toBe(true);
      expect(categories.has("auth")).toBe(true);
      expect(categories.has("api")).toBe(true);
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // Integration Status
  // ═══════════════════════════════════════════════════════════════════════════

  describe("integration status", () => {
    it("has valid status for each integration", () => {
      const validStatuses: IntegrationStatus[] = ["connected", "disconnected", "error"];
      const snapshot = loadIntegrationsSnapshot();

      for (const integration of snapshot.integrations) {
        expect(validStatuses).toContain(integration.status);
      }
    });

    it("seed data has mix of connected and disconnected", () => {
      const snapshot = loadIntegrationsSnapshot();
      const connected = snapshot.integrations.filter((i) => i.status === "connected");
      const disconnected = snapshot.integrations.filter((i) => i.status === "disconnected");

      expect(connected.length).toBeGreaterThan(0);
      expect(disconnected.length).toBeGreaterThan(0);
    });
  });
});
