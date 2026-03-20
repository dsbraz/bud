import { describe, it, expect, beforeEach } from "vitest";
import {
  loadSurveysSnapshot,
  saveSurveysSnapshot,
  resetSurveysSnapshot,
  type SurveysStoreSnapshot,
} from "./surveys-store";

describe("Surveys Store", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe("loadSurveysSnapshot()", () => {
    it("returns seed data when localStorage is empty", () => {
      const snapshot = loadSurveysSnapshot();

      expect(snapshot.schemaVersion).toBe(5);
      expect(snapshot.records.length).toBeGreaterThan(0);
      expect(snapshot.templates.length).toBeGreaterThan(0);
    });

    it("persists seed data to localStorage on first load", () => {
      loadSurveysSnapshot();

      const stored = localStorage.getItem("bud.saas.surveys-store:org-1");
      expect(stored).not.toBeNull();
    });

    it("loads existing data from localStorage, merging seed records", () => {
      const customData: SurveysStoreSnapshot = {
        schemaVersion: 5,
        updatedAt: new Date().toISOString(),
        records: [
          {
            id: "custom-survey",
            listItem: {
              id: "custom-survey",
              templateId: null,
              name: "Custom Survey",
              type: "pulse",
              category: "pesquisa",
              status: "draft",
              startDate: "2026-01-01",
              endDate: "2026-01-31",
              ownerIds: ["ms"],
              managerIds: [],
              tagIds: [],
              cycleId: null,
              totalRecipients: 0,
              totalResponses: 0,
              completionRate: 0,
              createdAt: "2026-01-01T00:00:00.000Z",
            },
            wizardState: null,
            createdAt: "2026-01-01T00:00:00.000Z",
            updatedAt: "2026-01-01T00:00:00.000Z",
          },
        ],
        templates: [],
        submissions: [],
      };

      localStorage.setItem(
        "bud.saas.surveys-store:org-1",
        JSON.stringify(customData)
      );
      const snapshot = loadSurveysSnapshot();

      // User record is preserved; seed records are also merged in
      expect(snapshot.records.some((r) => r.id === "custom-survey")).toBe(true);
      // Seed records are always injected, so total > 1
      expect(snapshot.records.length).toBeGreaterThan(1);
      // Seed submissions are always present
      expect(snapshot.submissions.length).toBe(33);
    });

    it("handles corrupted localStorage data", () => {
      localStorage.setItem("bud.saas.surveys-store:org-1", "not-valid-json");
      const snapshot = loadSurveysSnapshot();

      // Should return seed data
      expect(snapshot.schemaVersion).toBe(5);
      expect(snapshot.records.length).toBeGreaterThan(0);
    });

    it("migrates old schema versions", () => {
      const oldData = {
        schemaVersion: 1,
        records: [
          {
            id: "old-survey",
            listItem: {
              id: "old-survey",
              name: "Old Survey",
              type: "pulse",
              category: "pesquisa",
              status: "completed",
              startDate: "2026-01-01",
              endDate: "2026-01-15",
              ownerIds: ["ms"],
              managerIds: [],
              tagIds: [],
              totalRecipients: 100,
              totalResponses: 80,
              completionRate: 80,
              createdAt: "2026-01-01T00:00:00.000Z",
            },
            wizardState: null,
            createdAt: "2026-01-01T00:00:00.000Z",
            updatedAt: "2026-01-01T00:00:00.000Z",
          },
        ],
        templates: [],
        submissions: [],
      };

      localStorage.setItem(
        "bud.saas.surveys-store:org-1",
        JSON.stringify(oldData)
      );
      const snapshot = loadSurveysSnapshot();

      expect(snapshot.schemaVersion).toBe(5);
      // Old records are always preserved and seed records are merged in
      expect(snapshot.records.some((r) => r.id === "old-survey")).toBe(true);
      // Seed submissions are always injected regardless of schema version
      expect(snapshot.submissions.length).toBe(33);
    });
  });

  describe("saveSurveysSnapshot()", () => {
    it("saves data to localStorage", () => {
      const initial = loadSurveysSnapshot();
      initial.records.push({
        id: "new-survey",
        listItem: {
          id: "new-survey",
          templateId: null,
          name: "New Survey",
          type: "enps",
          category: "pesquisa",
          status: "draft",
          startDate: "2026-02-01",
          endDate: "2026-02-28",
          ownerIds: ["ms"],
          managerIds: [],
          tagIds: [],
          cycleId: null,
          totalRecipients: 0,
          totalResponses: 0,
          completionRate: 0,
          createdAt: new Date().toISOString(),
        },
        wizardState: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      });

      saveSurveysSnapshot(initial);

      const stored = localStorage.getItem("bud.saas.surveys-store:org-1");
      const parsed = JSON.parse(stored!) as SurveysStoreSnapshot;
      expect(parsed.records.some((r) => r.id === "new-survey")).toBe(true);
    });

    it("updates schemaVersion and updatedAt", () => {
      const initial = loadSurveysSnapshot();
      const saved = saveSurveysSnapshot(initial);

      expect(saved.schemaVersion).toBe(5);
      expect(saved.updatedAt).toBeDefined();
    });
  });

  describe("resetSurveysSnapshot()", () => {
    it("resets to seed data", () => {
      // First, save custom data
      const initial = loadSurveysSnapshot();
      initial.records = [
        {
          id: "custom-only",
          listItem: {
            id: "custom-only",
            templateId: null,
            name: "Custom Only",
            type: "pulse",
            category: "pesquisa",
            status: "draft",
            startDate: "2026-01-01",
            endDate: "2026-01-31",
            ownerIds: [],
            managerIds: [],
            tagIds: [],
            cycleId: null,
            totalRecipients: 0,
            totalResponses: 0,
            completionRate: 0,
            createdAt: new Date().toISOString(),
          },
          wizardState: null,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        },
      ];
      saveSurveysSnapshot(initial);

      // Reset
      const reset = resetSurveysSnapshot();

      expect(reset.records.some((r) => r.id === "custom-only")).toBe(false);
      expect(reset.records.length).toBeGreaterThan(0);
    });
  });
});

// ─── Seed Data Validation ───

describe("Surveys Seed Data Validation", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("seed includes surveys with different statuses", () => {
    const snapshot = loadSurveysSnapshot();
    const statuses = new Set(snapshot.records.map((r) => r.listItem.status));

    expect(statuses.size).toBeGreaterThan(1);
  });

  it("seed includes surveys with different types", () => {
    const snapshot = loadSurveysSnapshot();
    const types = new Set(snapshot.records.map((r) => r.listItem.type));

    expect(types.size).toBeGreaterThan(1);
  });

  it("seed surveys have valid structure", () => {
    const snapshot = loadSurveysSnapshot();
    const record = snapshot.records[0];

    expect(record?.id).toBeDefined();
    expect(record?.listItem.name).toBeDefined();
    expect(record?.listItem.startDate).toBeDefined();
    expect(record?.listItem.endDate).toBeDefined();
    expect(record?.listItem.ownerIds).toBeDefined();
  });

  it("seed includes templates", () => {
    const snapshot = loadSurveysSnapshot();
    expect(snapshot.templates.length).toBeGreaterThan(0);
  });

  it("seed templates have valid structure", () => {
    const snapshot = loadSurveysSnapshot();
    const template = snapshot.templates[0];

    expect(template?.id).toBeDefined();
    expect(template?.name).toBeDefined();
    expect(template?.type).toBeDefined();
    expect(template?.sections).toBeDefined();
    expect(template?.questions).toBeDefined();
  });

  it("seed templates are marked as system templates", () => {
    const snapshot = loadSurveysSnapshot();
    const systemTemplates = snapshot.templates.filter((t) => t.isSystem);

    expect(systemTemplates.length).toBeGreaterThan(0);
  });

  it("seed submissions include team-produto survey responses", () => {
    const snapshot = loadSurveysSnapshot();
    // 12 submissions for survey 10 (Pulse) + 9 for survey 11 (Health Check) + 12 for survey 12 (eNPS)
    expect(snapshot.submissions.length).toBe(33);
    const surveyIds = new Set(snapshot.submissions.map((s) => s.surveyId));
    expect(surveyIds.has("10")).toBe(true);
    expect(surveyIds.has("11")).toBe(true);
    expect(surveyIds.has("12")).toBe(true);
  });
});
