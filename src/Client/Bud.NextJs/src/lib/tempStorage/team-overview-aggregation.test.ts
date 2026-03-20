import { describe, it, expect } from "vitest";
import {
  aggregateTeamMissions,
  aggregateTeamSurveys,
} from "./team-overview-aggregation";
import type { Mission } from "@/types/mission";
import type { SurveyListItemData } from "./surveys-store";

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeMission(overrides: Partial<Mission> = {}): Mission {
  return {
    id: "m1",
    orgId: "org-1",
    cycleId: null,
    parentId: null,
    depth: 0,
    path: ["m1"],
    title: "Missão Teste",
    description: null,
    ownerId: "user-1",
    teamId: "team-1",
    status: "active",
    visibility: "public",
    progress: 50,
    kanbanStatus: "doing",
    sortOrder: 0,
    dueDate: null,
    completedAt: null,
    createdAt: "2025-01-01T00:00:00Z",
    updatedAt: "2025-01-01T00:00:00Z",
    deletedAt: null,
    ...overrides,
  };
}

function makeSurvey(overrides: Partial<SurveyListItemData> = {}): SurveyListItemData {
  return {
    id: "s1",
    templateId: null,
    name: "Pulse",
    type: "pulse",
    category: "pesquisa",
    status: "active",
    startDate: "2025-01-01",
    endDate: "2025-12-31",
    ownerIds: [],
    managerIds: ["user-1"],
    tagIds: [],
    cycleId: null,
    totalRecipients: 10,
    totalResponses: 8,
    completionRate: 80,
    createdAt: "2025-01-01T00:00:00Z",
    ...overrides,
  };
}

// ── aggregateTeamMissions ─────────────────────────────────────────────────────

describe("aggregateTeamMissions", () => {
  it("conta missões ativas corretamente", () => {
    const missions = [
      makeMission({ id: "m1", status: "active", progress: 60 }),
      makeMission({ id: "m2", status: "active", progress: 80 }),
      makeMission({ id: "m3", status: "completed", progress: 100 }),
    ];
    const result = aggregateTeamMissions(missions, "team-1", []);
    expect(result.totalActive).toBe(2);
    expect(result.completedCount).toBe(1);
  });

  it("calcula progresso médio apenas das ativas", () => {
    const missions = [
      makeMission({ id: "m1", status: "active", progress: 40 }),
      makeMission({ id: "m2", status: "active", progress: 80 }),
    ];
    const result = aggregateTeamMissions(missions, "team-1", []);
    expect(result.avgProgress).toBe(60);
  });

  it("inclui missões por ownerId mesmo sem teamId", () => {
    const missions = [
      makeMission({ id: "m1", teamId: null, ownerId: "user-99", status: "active" }),
    ];
    const result = aggregateTeamMissions(missions, "team-1", ["user-99"]);
    expect(result.totalActive).toBe(1);
  });

  it("exclui missões canceladas", () => {
    const missions = [makeMission({ status: "cancelled" })];
    const result = aggregateTeamMissions(missions, "team-1", []);
    expect(result.totalActive).toBe(0);
    expect(result.missions).toHaveLength(0);
  });

  it("exclui missões deletadas (deletedAt != null)", () => {
    const missions = [makeMission({ deletedAt: "2025-01-01T00:00:00Z" })];
    const result = aggregateTeamMissions(missions, "team-1", []);
    expect(result.missions).toHaveLength(0);
  });

  it("marca missão como atRisk quando progresso baixo e prazo próximo", () => {
    const due = new Date(Date.now() + 10 * 24 * 60 * 60 * 1000).toISOString(); // 10 dias
    const missions = [makeMission({ progress: 20, dueDate: due, status: "active" })];
    const result = aggregateTeamMissions(missions, "team-1", []);
    expect(result.missions[0]?.isAtRisk).toBe(true);
    expect(result.atRiskCount).toBe(1);
  });

  it("não marca como atRisk missão com progresso >= 70", () => {
    const due = new Date(Date.now() + 5 * 24 * 60 * 60 * 1000).toISOString();
    const missions = [makeMission({ progress: 75, dueDate: due, status: "active" })];
    const result = aggregateTeamMissions(missions, "team-1", []);
    expect(result.missions[0]?.isAtRisk).toBe(false);
  });

  it("retorna totalActive=0 e avgProgress=0 para lista vazia", () => {
    const result = aggregateTeamMissions([], "team-1", []);
    expect(result.totalActive).toBe(0);
    expect(result.avgProgress).toBe(0);
  });
});

// ── aggregateTeamSurveys ──────────────────────────────────────────────────────

describe("aggregateTeamSurveys", () => {
  it("retorna apenas surveys com managers do time", () => {
    const surveys = [
      makeSurvey({ id: "s1", managerIds: ["user-1"] }),
      makeSurvey({ id: "s2", managerIds: ["user-999"] }),
    ];
    const result = aggregateTeamSurveys(surveys, ["user-1"]);
    expect(result).toHaveLength(1);
    expect(result[0]?.surveyId).toBe("s1");
  });

  it("exclui surveys arquivadas", () => {
    const surveys = [makeSurvey({ status: "archived", managerIds: ["user-1"] })];
    const result = aggregateTeamSurveys(surveys, ["user-1"]);
    expect(result).toHaveLength(0);
  });

  it("retorna array vazio quando memberIds está vazio", () => {
    const surveys = [makeSurvey()];
    const result = aggregateTeamSurveys(surveys, []);
    expect(result).toHaveLength(0);
  });

  it("mapeia campos corretamente", () => {
    const surveys = [
      makeSurvey({
        id: "s1",
        name: "Health Check",
        type: "health_check",
        totalResponses: 7,
        totalRecipients: 10,
        completionRate: 70,
        managerIds: ["user-1"],
      }),
    ];
    const result = aggregateTeamSurveys(surveys, ["user-1"]);
    expect(result[0]).toMatchObject({
      surveyId: "s1",
      surveyName: "Health Check",
      surveyType: "health_check",
      responseCount: 7,
      totalRecipients: 10,
      completionRate: 70,
    });
  });
});
