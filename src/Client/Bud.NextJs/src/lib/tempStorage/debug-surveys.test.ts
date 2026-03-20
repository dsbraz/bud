// ─── Teste de diagnóstico do fluxo de dados de pesquisa ─────────────────────
// Verifica a camada de dados pura (sem React context).
// NOTA: memberIds = [] aqui porque a junção de teamMembers com Team.members
// é feita pelo PeopleDataContext.buildTeamsView() no app React, não no raw store.

import { describe, it, expect, beforeEach } from "vitest";
import { loadSurveysSnapshot } from "./surveys-store";
import { aggregateSurveySubmissions } from "./survey-aggregation";
import { createWizardStateFromListItem } from "@/pages/surveys/utils/localSurveyAdapters";

describe("Survey data layer — seed integridade", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("seed tem 33 submissions (12 survey-10 + 9 survey-11 + 12 survey-12)", () => {
    const snapshot = loadSurveysSnapshot();
    expect(snapshot.submissions.length).toBe(33);
    const ids = new Set(snapshot.submissions.map((s) => s.surveyId));
    expect(ids.has("10")).toBe(true);
    expect(ids.has("11")).toBe(true);
    expect(ids.has("12")).toBe(true);
  });

  it("survey 10 tem template pulse com questões válidas", () => {
    const snapshot = loadSurveysSnapshot();
    const record = snapshot.records.find((r) => r.id === "10");
    expect(record).toBeDefined();
    expect(record?.listItem.type).toBe("pulse");

    const template = snapshot.templates.find((t) => t.id === record?.listItem.templateId);
    expect(template).toBeDefined();
    expect(template?.questions?.length).toBeGreaterThan(0);
    // IDs devem ser pulse-question-1-1, etc.
    expect(template?.questions?.[0]?.id).toBe("pulse-question-1-1");
  });

  it("survey 10 submissions usam os mesmos IDs de questão do template", () => {
    const snapshot = loadSurveysSnapshot();
    const record = snapshot.records.find((r) => r.id === "10")!;
    const template = snapshot.templates.find((t) => t.id === record.listItem.templateId);
    const wizardState = createWizardStateFromListItem(record.listItem, template);
    const subs = snapshot.submissions.filter((s) => s.surveyId === "10");

    expect(subs.length).toBe(12);
    expect(wizardState.questions.length).toBeGreaterThan(0);

    const result = aggregateSurveySubmissions(wizardState.questions, subs);
    const hasData = [...result.values()].some((a) => a.count > 0);
    expect(hasData).toBe(true);

    // Verifica que rating e likert têm counts
    const rating = result.get("pulse-question-1-1");
    expect(rating?.type).toBe("rating");
    expect(rating?.count).toBe(12);
  });

  it("survey 11 (health_check) tem 9 submissions com respostas válidas", () => {
    const snapshot = loadSurveysSnapshot();
    const record = snapshot.records.find((r) => r.id === "11")!;
    const template = snapshot.templates.find((t) => t.id === record.listItem.templateId);
    const wizardState = createWizardStateFromListItem(record.listItem, template);
    const subs = snapshot.submissions.filter((s) => s.surveyId === "11");

    expect(subs.length).toBe(9);
    const result = aggregateSurveySubmissions(wizardState.questions, subs);
    const hasData = [...result.values()].some((a) => a.count > 0);
    expect(hasData).toBe(true);
  });

  it("survey 12 (eNPS) tem 12 submissions com score calculável", () => {
    const snapshot = loadSurveysSnapshot();
    const record = snapshot.records.find((r) => r.id === "12")!;
    const template = snapshot.templates.find((t) => t.id === record.listItem.templateId);
    const wizardState = createWizardStateFromListItem(record.listItem, template);
    const subs = snapshot.submissions.filter((s) => s.surveyId === "12");

    expect(subs.length).toBe(12);
    const result = aggregateSurveySubmissions(wizardState.questions, subs);
    const npsAgg = result.get("enps-question-1-1");
    expect(npsAgg?.type).toBe("nps");
    expect(npsAgg?.count).toBe(12);
  });

  it("surveys 10, 11, 12 têm managerIds incluindo 'bs' (membro de team-produto)", () => {
    const snapshot = loadSurveysSnapshot();
    const survey10 = snapshot.records.find((r) => r.id === "10");
    const survey11 = snapshot.records.find((r) => r.id === "11");
    const survey12 = snapshot.records.find((r) => r.id === "12");

    expect(survey10?.listItem.managerIds).toContain("bs");
    expect(survey11?.listItem.managerIds).toContain("bs");
    expect(survey12?.listItem.managerIds).toContain("bs");
  });

  it("migration v3→v4 injeta surveys 10/11/12 em dados legados sem elas", () => {
    // Simula usuário com dados legados (schema v3, sem surveys 10/11/12)
    const legacyData = {
      schemaVersion: 3,
      records: [
        { id: "1", listItem: { id: "1", type: "pulse", status: "active", managerIds: ["ms"], ownerIds: ["ms"], tagIds: [], cycleId: null, name: "Pulse", category: "pesquisa", startDate: "2025-01-01", endDate: "2025-12-31", totalRecipients: 100, totalResponses: 80, completionRate: 80, templateId: null, createdAt: "2025-01-01T00:00:00Z" }, wizardState: null, createdAt: "2025-01-01T00:00:00Z", updatedAt: "2025-01-01T00:00:00Z" },
      ],
      templates: [],
      submissions: [],
    };
    localStorage.setItem("bud.saas.surveys-store:org-1", JSON.stringify(legacyData));

    const snapshot = loadSurveysSnapshot();

    // Deve ter os records legados + os novos seed records (10, 11, 12)
    const ids = snapshot.records.map((r) => r.id);
    expect(ids).toContain("1");   // legado preservado
    expect(ids).toContain("10");  // seed injetado
    expect(ids).toContain("11");  // seed injetado
    expect(ids).toContain("12");  // seed injetado
  });
});
