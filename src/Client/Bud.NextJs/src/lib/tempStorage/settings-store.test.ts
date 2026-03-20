/**
 * Tests for settings-store.ts
 *
 * Settings Store manages AI settings, data sources, custom tones, and knowledge docs.
 * Follows the same patterns as other stores (config, people, missions, surveys).
 */

import { describe, it, expect, beforeEach } from "vitest";
import {
  loadSettingsSnapshot,
  saveSettingsSnapshot,
  resetSettingsSnapshot,
  generateId,
  createDefaultAiSettings,
  TONE_PRESETS,
  DATA_SOURCE_CATALOG,
  type SettingsStoreSnapshot,
  type OrgAiSettings,
  type DataSourceRecord,
  type CustomToneRecord,
  type KnowledgeDocRecord,
  type ToneId,
  type ProactivityLevel,
  type TransparencyMode,
  type DataSourceStatus,
  type KnowledgeDocType,
} from "./settings-store";

const STORAGE_KEY = "bud.saas.settings-store";

// ─── Test Helpers ───

function getStoredSnapshot(): SettingsStoreSnapshot | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return null;
  return JSON.parse(raw) as SettingsStoreSnapshot;
}

function setStoredSnapshot(snapshot: Partial<SettingsStoreSnapshot>): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(snapshot));
}

// ─── Tests ───

describe("settings-store", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // Constants and Presets
  // ═══════════════════════════════════════════════════════════════════════════

  describe("TONE_PRESETS", () => {
    it("contains 5 tone presets", () => {
      expect(TONE_PRESETS).toHaveLength(5);
    });

    it("has all required tone IDs", () => {
      const ids = TONE_PRESETS.map((t) => t.id);
      expect(ids).toContain("profissional");
      expect(ids).toContain("direto");
      expect(ids).toContain("empatico");
      expect(ids).toContain("coach");
      expect(ids).toContain("energetico");
    });

    it("each preset has required properties", () => {
      for (const preset of TONE_PRESETS) {
        expect(preset).toHaveProperty("id");
        expect(preset).toHaveProperty("label");
        expect(preset).toHaveProperty("description");
        expect(preset).toHaveProperty("example");
        expect(preset).toHaveProperty("iconName");
        expect(preset.label.length).toBeGreaterThan(0);
        expect(preset.description.length).toBeGreaterThan(0);
        expect(preset.example.length).toBeGreaterThan(0);
      }
    });
  });

  describe("DATA_SOURCE_CATALOG", () => {
    it("contains 7 data source types", () => {
      expect(DATA_SOURCE_CATALOG).toHaveLength(7);
    });

    it("has all required data source types", () => {
      const types = DATA_SOURCE_CATALOG.map((d) => d.type);
      expect(types).toContain("notion");
      expect(types).toContain("confluence");
      expect(types).toContain("gdrive");
      expect(types).toContain("sharepoint");
      expect(types).toContain("jira");
      expect(types).toContain("sheets");
      expect(types).toContain("custom");
    });

    it("each catalog item has required properties", () => {
      for (const item of DATA_SOURCE_CATALOG) {
        expect(item).toHaveProperty("type");
        expect(item).toHaveProperty("name");
        expect(item).toHaveProperty("description");
        expect(item).toHaveProperty("tools");
        expect(item).toHaveProperty("requiresPro");
        expect(Array.isArray(item.tools)).toBe(true);
        expect(typeof item.requiresPro).toBe("boolean");
      }
    });

    it("only custom source requires Pro", () => {
      const proSources = DATA_SOURCE_CATALOG.filter((d) => d.requiresPro);
      expect(proSources).toHaveLength(1);
      expect(proSources[0]!.type).toBe("custom");
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // Utility Functions
  // ═══════════════════════════════════════════════════════════════════════════

  describe("generateId", () => {
    it("generates unique IDs with prefix", () => {
      const id1 = generateId("test");
      const id2 = generateId("test");

      expect(id1).toMatch(/^test-\d+-[a-z0-9]+$/);
      expect(id2).toMatch(/^test-\d+-[a-z0-9]+$/);
      expect(id1).not.toBe(id2);
    });

    it("handles different prefixes", () => {
      const id1 = generateId("doc");
      const id2 = generateId("tone");
      const id3 = generateId("ds");

      expect(id1).toMatch(/^doc-/);
      expect(id2).toMatch(/^tone-/);
      expect(id3).toMatch(/^ds-/);
    });
  });

  describe("createDefaultAiSettings", () => {
    it("creates settings with correct orgId", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.orgId).toBe("org-1");
    });

    it("creates settings with correct orgId for different orgs", () => {
      const settings1 = createDefaultAiSettings("org-1");
      const settings2 = createDefaultAiSettings("brq");

      expect(settings1.orgId).toBe("org-1");
      expect(settings2.orgId).toBe("brq");
    });

    it("uses profissional as default tone", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.toneId).toBe("profissional");
    });

    it("uses pt-BR as default language", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.language).toBe("pt-BR");
    });

    it("has working hours enabled by default", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.respectWorkingHours).toBe(true);
      expect(settings.workingHoursStart).toBe("08:00");
      expect(settings.workingHoursEnd).toBe("19:00");
    });

    it("uses default proactivity level", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.proactivityLevel).toBe("default");
    });

    it("has suggestion types configured with sensible defaults", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.suggestionTypes.oneOnOnePrep).toBe(true);
      expect(settings.suggestionTypes.coachingTips).toBe(true);
      expect(settings.suggestionTypes.teamAlerts).toBe(true);
      expect(settings.suggestionTypes.reviewDrafts).toBe(true);
      expect(settings.suggestionTypes.okrSuggestions).toBe(false);
      expect(settings.suggestionTypes.dailyBriefing).toBe(true);
    });

    it("uses always transparency mode", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.transparencyMode).toBe("always");
    });

    it("has bias detection enabled by default", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.biasDetectionEnabled).toBe(true);
    });

    it("has data sharing disabled by default", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.dataSharingEnabled).toBe(false);
    });

    it("has no usage limit by default", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.monthlyUsageLimit).toBeNull();
    });

    it("has custom instructions with default text", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.customInstructions).toContain("linguagem acessível");
      expect(settings.customInstructions).toContain("próximos passos");
    });

    it("has timestamps", () => {
      const settings = createDefaultAiSettings("org-1");
      expect(settings.createdAt).toBeDefined();
      expect(settings.updatedAt).toBeDefined();
      expect(new Date(settings.createdAt).getTime()).toBeGreaterThan(0);
      expect(new Date(settings.updatedAt).getTime()).toBeGreaterThan(0);
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // loadSettingsSnapshot
  // ═══════════════════════════════════════════════════════════════════════════

  describe("loadSettingsSnapshot", () => {
    describe("initial load (no stored data)", () => {
      it("returns seed snapshot when localStorage is empty", () => {
        const snapshot = loadSettingsSnapshot();
        expect(snapshot).toBeDefined();
        expect(snapshot.schemaVersion).toBe(2);
      });

      it("creates AI settings for default orgs", () => {
        const snapshot = loadSettingsSnapshot();
        expect(snapshot.aiSettingsByOrg["org-1"]).toBeDefined();
        expect(snapshot.aiSettingsByOrg["brq"]).toBeDefined();
        expect(snapshot.aiSettingsByOrg["initech"]).toBeDefined();
      });

      it("seeds data sources for org-1 only", () => {
        const snapshot = loadSettingsSnapshot();
        expect(snapshot.dataSourcesByOrg["org-1"]!.length).toBeGreaterThan(0);
        expect(snapshot.dataSourcesByOrg["brq"]).toEqual([]);
        expect(snapshot.dataSourcesByOrg["initech"]).toEqual([]);
      });

      it("seeds knowledge docs for org-1 only", () => {
        const snapshot = loadSettingsSnapshot();
        expect(snapshot.knowledgeDocsByOrg["org-1"]!.length).toBeGreaterThan(0);
        expect(snapshot.knowledgeDocsByOrg["brq"]).toEqual([]);
        expect(snapshot.knowledgeDocsByOrg["initech"]).toEqual([]);
      });

      it("initializes custom tones as empty arrays", () => {
        const snapshot = loadSettingsSnapshot();
        expect(snapshot.customTonesByOrg["org-1"]).toEqual([]);
        expect(snapshot.customTonesByOrg["brq"]).toEqual([]);
        expect(snapshot.customTonesByOrg["initech"]).toEqual([]);
      });

      it("persists seed to localStorage", () => {
        loadSettingsSnapshot();
        const stored = getStoredSnapshot();
        expect(stored).not.toBeNull();
        expect(stored!.schemaVersion).toBe(2);
      });
    });

    describe("load from existing data", () => {
      it("loads previously saved snapshot", () => {
        const customSettings: Partial<OrgAiSettings> = {
          orgId: "org-1",
          toneId: "coach",
          language: "en-US",
        };

        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": customSettings as OrgAiSettings },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.aiSettingsByOrg["org-1"]!.toneId).toBe("coach");
        expect(snapshot.aiSettingsByOrg["org-1"]!.language).toBe("en-US");
      });

      it("preserves custom tones", () => {
        const customTone: CustomToneRecord = {
          id: "tone-1",
          orgId: "org-1",
          label: "Casual",
          description: "Tom informal",
          example: "E aí, tudo bem?",
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        };

        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [customTone] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.customTonesByOrg["org-1"]).toHaveLength(1);
        expect(snapshot.customTonesByOrg["org-1"]![0]!.label).toBe("Casual");
      });
    });

    describe("migration from invalid/corrupt data", () => {
      it("returns seed for invalid JSON", () => {
        localStorage.setItem(STORAGE_KEY, "not valid json");
        const snapshot = loadSettingsSnapshot();
        expect(snapshot.schemaVersion).toBe(2);
        expect(snapshot.aiSettingsByOrg["org-1"]).toBeDefined();
      });

      it("returns seed for null stored value", () => {
        setStoredSnapshot(null as any);
        const snapshot = loadSettingsSnapshot();
        expect(snapshot.schemaVersion).toBe(2);
      });

      it("migrates old schema versions", () => {
        setStoredSnapshot({
          schemaVersion: 1,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.schemaVersion).toBe(2);

        // Should persist the migration
        const stored = getStoredSnapshot();
        expect(stored!.schemaVersion).toBe(2);
      });

      it("ensures default org exists after migration", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: {},
          dataSourcesByOrg: {},
          customTonesByOrg: {},
          knowledgeDocsByOrg: {},
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.aiSettingsByOrg["org-1"]).toBeDefined();
        expect(snapshot.dataSourcesByOrg["org-1"]).toBeDefined();
      });
    });

    describe("AI settings sanitization", () => {
      it("sanitizes invalid tone to default", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: {
            "org-1": {
              ...createDefaultAiSettings("org-1"),
              toneId: "invalid-tone" as ToneId,
            },
          },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.aiSettingsByOrg["org-1"]!.toneId).toBe("profissional");
      });

      it("sanitizes invalid proactivity level to default", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: {
            "org-1": {
              ...createDefaultAiSettings("org-1"),
              proactivityLevel: "invalid" as ProactivityLevel,
            },
          },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.aiSettingsByOrg["org-1"]!.proactivityLevel).toBe("default");
      });

      it("sanitizes invalid transparency mode to default", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: {
            "org-1": {
              ...createDefaultAiSettings("org-1"),
              transparencyMode: "invalid" as TransparencyMode,
            },
          },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.aiSettingsByOrg["org-1"]!.transparencyMode).toBe("always");
      });

      it("preserves valid boolean fields", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: {
            "org-1": {
              ...createDefaultAiSettings("org-1"),
              respectWorkingHours: false,
              biasDetectionEnabled: false,
              dataSharingEnabled: true,
            },
          },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        const settings = snapshot.aiSettingsByOrg["org-1"]!;
        expect(settings.respectWorkingHours).toBe(false);
        expect(settings.biasDetectionEnabled).toBe(false);
        expect(settings.dataSharingEnabled).toBe(true);
      });

      it("sanitizes non-boolean values to defaults", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: {
            "org-1": {
              ...createDefaultAiSettings("org-1"),
              respectWorkingHours: "yes" as any,
              biasDetectionEnabled: 1 as any,
            },
          },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        const settings = snapshot.aiSettingsByOrg["org-1"]!;
        expect(settings.respectWorkingHours).toBe(true); // default
        expect(settings.biasDetectionEnabled).toBe(true); // default
      });

      it("sanitizes suggestion types with partial data", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: {
            "org-1": {
              ...createDefaultAiSettings("org-1"),
              suggestionTypes: {
                oneOnOnePrep: false,
                // Missing other fields
              } as any,
            },
          },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        const types = snapshot.aiSettingsByOrg["org-1"]!.suggestionTypes;
        expect(types.oneOnOnePrep).toBe(false);
        expect(types.coachingTips).toBe(true); // default
        expect(types.teamAlerts).toBe(true); // default
      });

      it("handles null AI settings for an org", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: {
            "org-1": null as any,
          },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.aiSettingsByOrg["org-1"]).toBeDefined();
        expect(snapshot.aiSettingsByOrg["org-1"]!.toneId).toBe("profissional");
      });
    });

    describe("data source sanitization", () => {
      it("sanitizes data sources with missing fields", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: {
            "org-1": [
              {
                id: "ds-1",
                type: "notion",
                name: "My Notion",
                // Missing other fields
              } as any,
            ],
          },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        const ds = snapshot.dataSourcesByOrg["org-1"]![0]!;
        expect(ds.id).toBe("ds-1");
        expect(ds.type).toBe("notion");
        expect(ds.name).toBe("My Notion");
        expect(ds.description).toBe("");
        expect(ds.status).toBe("disconnected");
        expect(ds.enabled).toBe(false);
        expect(ds.tools).toEqual([]);
      });

      it("filters out data sources without required fields", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: {
            "org-1": [
              { id: "ds-1", type: "notion", name: "Valid" },
              { id: "ds-2" }, // Missing type and name
              { type: "gdrive", name: "No ID" }, // Missing id
            ] as any,
          },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.dataSourcesByOrg["org-1"]).toHaveLength(1);
        expect(snapshot.dataSourcesByOrg["org-1"]![0]!.id).toBe("ds-1");
      });

      it("sanitizes invalid data source type to custom", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: {
            "org-1": [
              {
                id: "ds-1",
                type: "invalid-type",
                name: "Unknown Source",
              } as any,
            ],
          },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.dataSourcesByOrg["org-1"]![0]!.type).toBe("custom");
      });

      it("sanitizes invalid data source status to disconnected", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: {
            "org-1": [
              {
                id: "ds-1",
                type: "notion",
                name: "Notion",
                status: "unknown" as DataSourceStatus,
              } as any,
            ],
          },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.dataSourcesByOrg["org-1"]![0]!.status).toBe("disconnected");
      });

      it("preserves valid data source tools array", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: {
            "org-1": [
              {
                id: "ds-1",
                type: "notion",
                name: "Notion",
                tools: ["Read", "Search", "Create"],
              } as any,
            ],
          },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.dataSourcesByOrg["org-1"]![0]!.tools).toEqual(["Read", "Search", "Create"]);
      });
    });

    describe("custom tone sanitization", () => {
      it("sanitizes custom tones with missing fields", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: {
            "org-1": [
              {
                id: "tone-1",
                label: "Custom",
                // Missing other fields
              } as any,
            ],
          },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        const tone = snapshot.customTonesByOrg["org-1"]![0]!;
        expect(tone.id).toBe("tone-1");
        expect(tone.label).toBe("Custom");
        expect(tone.description).toBe("");
        expect(tone.example).toBe("");
      });

      it("filters out custom tones without required fields", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: {
            "org-1": [
              { id: "tone-1", label: "Valid" },
              { id: "tone-2" }, // Missing label
              { label: "No ID" }, // Missing id
            ] as any,
          },
          knowledgeDocsByOrg: { "org-1": [] },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.customTonesByOrg["org-1"]).toHaveLength(1);
        expect(snapshot.customTonesByOrg["org-1"]![0]!.id).toBe("tone-1");
      });
    });

    describe("knowledge doc sanitization", () => {
      it("sanitizes knowledge docs with missing fields", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: {
            "org-1": [
              {
                id: "doc-1",
                name: "Manual",
                // Missing other fields
              } as any,
            ],
          },
        });

        const snapshot = loadSettingsSnapshot();
        const doc = snapshot.knowledgeDocsByOrg["org-1"]![0]!;
        expect(doc.id).toBe("doc-1");
        expect(doc.name).toBe("Manual");
        expect(doc.type).toBe("text"); // default
        expect(doc.size).toBeNull();
        expect(doc.content).toBeNull();
      });

      it("filters out knowledge docs without required fields", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: {
            "org-1": [
              { id: "doc-1", name: "Valid" },
              { id: "doc-2" }, // Missing name
              { name: "No ID" }, // Missing id
            ] as any,
          },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.knowledgeDocsByOrg["org-1"]).toHaveLength(1);
        expect(snapshot.knowledgeDocsByOrg["org-1"]![0]!.id).toBe("doc-1");
      });

      it("sanitizes invalid knowledge doc type to text", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: {
            "org-1": [
              {
                id: "doc-1",
                name: "Manual",
                type: "invalid" as KnowledgeDocType,
              } as any,
            ],
          },
        });

        const snapshot = loadSettingsSnapshot();
        expect(snapshot.knowledgeDocsByOrg["org-1"]![0]!.type).toBe("text");
      });

      it("preserves valid knowledge doc types", () => {
        setStoredSnapshot({
          schemaVersion: 2,
          updatedAt: new Date().toISOString(),
          aiSettingsByOrg: { "org-1": createDefaultAiSettings("org-1") },
          dataSourcesByOrg: { "org-1": [] },
          customTonesByOrg: { "org-1": [] },
          knowledgeDocsByOrg: {
            "org-1": [
              { id: "doc-1", name: "Doc 1", type: "file" },
              { id: "doc-2", name: "Doc 2", type: "text" },
              { id: "doc-3", name: "Doc 3", type: "url" },
            ] as any,
          },
        });

        const snapshot = loadSettingsSnapshot();
        const docs = snapshot.knowledgeDocsByOrg["org-1"]!;
        expect(docs[0]!.type).toBe("file");
        expect(docs[1]!.type).toBe("text");
        expect(docs[2]!.type).toBe("url");
      });
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // saveSettingsSnapshot
  // ═══════════════════════════════════════════════════════════════════════════

  describe("saveSettingsSnapshot", () => {
    it("persists snapshot to localStorage", () => {
      const initial = loadSettingsSnapshot();
      initial.aiSettingsByOrg["org-1"]!.toneId = "coach";

      saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      const stored = getStoredSnapshot();
      expect(stored!.aiSettingsByOrg["org-1"]!.toneId).toBe("coach");
    });

    it("updates schemaVersion", () => {
      const initial = loadSettingsSnapshot();

      saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      const stored = getStoredSnapshot();
      expect(stored!.schemaVersion).toBe(2);
    });

    it("updates updatedAt timestamp", () => {
      const before = new Date().toISOString();
      const initial = loadSettingsSnapshot();

      saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      const stored = getStoredSnapshot();
      expect(new Date(stored!.updatedAt).getTime()).toBeGreaterThanOrEqual(new Date(before).getTime());
    });

    it("returns the saved snapshot", () => {
      const initial = loadSettingsSnapshot();
      initial.aiSettingsByOrg["org-1"]!.language = "en-US";

      const result = saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      expect(result.aiSettingsByOrg["org-1"]!.language).toBe("en-US");
      expect(result.schemaVersion).toBe(2);
    });

    it("deep clones data to prevent mutations", () => {
      const initial = loadSettingsSnapshot();
      const customTone: CustomToneRecord = {
        id: "tone-1",
        orgId: "org-1",
        label: "Test",
        description: "Test tone",
        example: "Example",
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };
      initial.customTonesByOrg["org-1"] = [customTone];

      const result = saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      // Mutate the original
      customTone.label = "Mutated";

      // The saved snapshot should not be affected
      const stored = getStoredSnapshot();
      expect(stored!.customTonesByOrg["org-1"]![0]!.label).toBe("Test");
      expect(result.customTonesByOrg["org-1"]![0]!.label).toBe("Test");
    });

    it("can add data sources", () => {
      const initial = loadSettingsSnapshot();
      const newSource: DataSourceRecord = {
        id: "ds-new",
        orgId: "org-1",
        type: "confluence",
        name: "Team Wiki",
        description: "Internal documentation",
        status: "connected",
        enabled: true,
        url: "https://wiki.example.com",
        lastSync: new Date().toISOString(),
        accessSummary: "5 spaces",
        tools: ["Read", "Search"],
        connectedAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };

      initial.dataSourcesByOrg["org-1"]!.push(newSource);

      saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      const stored = getStoredSnapshot();
      const added = stored!.dataSourcesByOrg["org-1"]!.find((ds) => ds.id === "ds-new");
      expect(added).toBeDefined();
      expect(added!.name).toBe("Team Wiki");
    });

    it("can add knowledge docs", () => {
      const initial = loadSettingsSnapshot();
      const newDoc: KnowledgeDocRecord = {
        id: "doc-new",
        orgId: "org-1",
        name: "Company Handbook",
        type: "file",
        size: "5.2 MB",
        content: null,
        addedAt: new Date().toISOString().split("T")[0]!,
      };

      initial.knowledgeDocsByOrg["org-1"]!.push(newDoc);

      saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      const stored = getStoredSnapshot();
      const added = stored!.knowledgeDocsByOrg["org-1"]!.find((doc) => doc.id === "doc-new");
      expect(added).toBeDefined();
      expect(added!.name).toBe("Company Handbook");
    });

    it("can update suggestion types", () => {
      const initial = loadSettingsSnapshot();
      initial.aiSettingsByOrg["org-1"]!.suggestionTypes = {
        oneOnOnePrep: false,
        coachingTips: false,
        teamAlerts: true,
        reviewDrafts: false,
        okrSuggestions: true,
        dailyBriefing: false,
      };

      saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      const stored = getStoredSnapshot();
      const types = stored!.aiSettingsByOrg["org-1"]!.suggestionTypes;
      expect(types.oneOnOnePrep).toBe(false);
      expect(types.okrSuggestions).toBe(true);
      expect(types.dailyBriefing).toBe(false);
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // resetSettingsSnapshot
  // ═══════════════════════════════════════════════════════════════════════════

  describe("resetSettingsSnapshot", () => {
    it("resets to seed data", () => {
      // First, modify the snapshot
      const initial = loadSettingsSnapshot();
      initial.aiSettingsByOrg["org-1"]!.toneId = "energetico";
      initial.aiSettingsByOrg["org-1"]!.language = "es-ES";

      saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      // Verify the modification
      let stored = getStoredSnapshot();
      expect(stored!.aiSettingsByOrg["org-1"]!.toneId).toBe("energetico");

      // Reset
      const reset = resetSettingsSnapshot();

      // Verify reset
      expect(reset.aiSettingsByOrg["org-1"]!.toneId).toBe("profissional");
      expect(reset.aiSettingsByOrg["org-1"]!.language).toBe("pt-BR");

      stored = getStoredSnapshot();
      expect(stored!.aiSettingsByOrg["org-1"]!.toneId).toBe("profissional");
    });

    it("clears custom tones", () => {
      const initial = loadSettingsSnapshot();
      initial.customTonesByOrg["org-1"] = [
        {
          id: "tone-1",
          orgId: "org-1",
          label: "Custom",
          description: "desc",
          example: "ex",
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        },
      ];

      saveSettingsSnapshot({
        aiSettingsByOrg: initial.aiSettingsByOrg,
        dataSourcesByOrg: initial.dataSourcesByOrg,
        customTonesByOrg: initial.customTonesByOrg,
        knowledgeDocsByOrg: initial.knowledgeDocsByOrg,
      });

      const reset = resetSettingsSnapshot();
      expect(reset.customTonesByOrg["org-1"]).toEqual([]);
    });

    it("returns the seed snapshot", () => {
      const reset = resetSettingsSnapshot();
      expect(reset.schemaVersion).toBe(2);
      expect(reset.aiSettingsByOrg["org-1"]).toBeDefined();
      expect(reset.dataSourcesByOrg["org-1"]!.length).toBeGreaterThan(0);
      expect(reset.knowledgeDocsByOrg["org-1"]!.length).toBeGreaterThan(0);
    });

    it("persists reset to localStorage", () => {
      resetSettingsSnapshot();

      const stored = getStoredSnapshot();
      expect(stored!.schemaVersion).toBe(2);
      expect(stored!.aiSettingsByOrg["org-1"]!.toneId).toBe("profissional");
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // Seed Data Structure
  // ═══════════════════════════════════════════════════════════════════════════

  describe("seed data structure", () => {
    it("seeds 2 data sources for org-1", () => {
      const snapshot = loadSettingsSnapshot();
      expect(snapshot.dataSourcesByOrg["org-1"]).toHaveLength(2);
    });

    it("seeds notion and sheets data sources", () => {
      const snapshot = loadSettingsSnapshot();
      const types = snapshot.dataSourcesByOrg["org-1"]!.map((ds) => ds.type);
      expect(types).toContain("notion");
      expect(types).toContain("sheets");
    });

    it("seeds 3 knowledge docs for org-1", () => {
      const snapshot = loadSettingsSnapshot();
      expect(snapshot.knowledgeDocsByOrg["org-1"]).toHaveLength(3);
    });

    it("seeds knowledge docs with different types", () => {
      const snapshot = loadSettingsSnapshot();
      const docs = snapshot.knowledgeDocsByOrg["org-1"]!;
      const types = docs.map((d) => d.type);
      expect(types).toContain("file");
      expect(types).toContain("text");
    });

    it("data sources are connected and enabled", () => {
      const snapshot = loadSettingsSnapshot();
      for (const ds of snapshot.dataSourcesByOrg["org-1"]!) {
        expect(ds.status).toBe("connected");
        expect(ds.enabled).toBe(true);
      }
    });

    it("knowledge docs have addedAt dates in the past", () => {
      const snapshot = loadSettingsSnapshot();
      const now = new Date();
      for (const doc of snapshot.knowledgeDocsByOrg["org-1"]!) {
        const addedAt = new Date(doc.addedAt);
        expect(addedAt.getTime()).toBeLessThan(now.getTime());
      }
    });
  });
});
