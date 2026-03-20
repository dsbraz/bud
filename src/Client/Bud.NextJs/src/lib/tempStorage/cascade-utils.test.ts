import { describe, it, expect } from "vitest";
import type { Mission } from "@/types";
import {
  getUserMissionStats,
  getTeamMissionStats,
  checkUserDeletion,
  checkTeamDeletion,
  transferUserOwnership,
  unlinkTeamFromMissions,
} from "./cascade-utils";

// ─── Test Fixtures ───

function createMission(overrides: Partial<Mission> = {}): Mission {
  return {
    id: "mission-1",
    orgId: "org-1",
    cycleId: "q1-2026",
    parentId: null,
    depth: 0,
    path: ["mission-1"],
    title: "Test Mission",
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
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-15T00:00:00.000Z",
    deletedAt: null,
    ...overrides,
  };
}

function createKeyResult(ownerId: string, status: "on_track" | "attention" | "off_track" | "completed" = "on_track") {
  return {
    id: `kr-${Math.random().toString(36).slice(2, 7)}`,
    orgId: "org-1",
    missionId: "mission-1",
    parentKrId: null,
    ownerId,
    title: "Test KR",
    description: null,
    goalType: "reach" as const,
    unit: "percent" as const,
    unitLabel: null,
    measurementMode: "manual" as const,
    startValue: "0",
    currentValue: "50",
    targetValue: "100",
    lowThreshold: null,
    highThreshold: null,
    expectedValue: null,
    status,
    progress: 50,
    periodLabel: null,
    periodStart: null,
    periodEnd: null,
    sortOrder: 0,
    linkedMissionId: null,
    linkedSurveyId: null,
    externalSource: null,
    externalConfig: null,
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-15T00:00:00.000Z",
    deletedAt: null,
    tasks: [] as ReturnType<typeof createTask>[],
  };
}

function createTask(ownerId: string) {
  return {
    id: `task-${Math.random().toString(36).slice(2, 7)}`,
    missionId: "mission-1" as string | null,
    keyResultId: null as string | null,
    title: "Test Task",
    description: null,
    ownerId,
    dueDate: null,
    isDone: false,
    sortOrder: 0,
    completedAt: null,
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-15T00:00:00.000Z",
  };
}

// ─── getUserMissionStats ───

describe("getUserMissionStats()", () => {
  it("returns zero counts for empty missions array", () => {
    const result = getUserMissionStats([], "user-1");
    expect(result).toEqual({
      ownedMissions: 0,
      ownedKeyResults: 0,
      ownedTasks: 0,
    });
  });

  it("counts owned missions", () => {
    const missions = [
      createMission({ id: "m1", ownerId: "user-1" }),
      createMission({ id: "m2", ownerId: "user-1" }),
      createMission({ id: "m3", ownerId: "user-2" }),
    ];
    const result = getUserMissionStats(missions, "user-1");
    expect(result.ownedMissions).toBe(2);
  });

  it("counts owned key results", () => {
    const missions = [
      createMission({
        id: "m1",
        ownerId: "user-2",
        keyResults: [
          createKeyResult("user-1"),
          createKeyResult("user-1"),
          createKeyResult("user-2"),
        ],
      }),
    ];
    const result = getUserMissionStats(missions, "user-1");
    expect(result.ownedKeyResults).toBe(2);
  });

  it("counts owned tasks on missions", () => {
    const missions = [
      createMission({
        id: "m1",
        ownerId: "user-2",
        tasks: [createTask("user-1"), createTask("user-1"), createTask("user-2")],
      }),
    ];
    const result = getUserMissionStats(missions, "user-1");
    expect(result.ownedTasks).toBe(2);
  });

  it("counts owned tasks on key results", () => {
    const kr = createKeyResult("user-2");
    kr.tasks = [createTask("user-1"), createTask("user-2")];

    const missions = [
      createMission({
        id: "m1",
        ownerId: "user-2",
        keyResults: [kr],
      }),
    ];
    const result = getUserMissionStats(missions, "user-1");
    expect(result.ownedTasks).toBe(1);
  });

  it("recurses into child missions", () => {
    const missions = [
      createMission({
        id: "m1",
        ownerId: "user-2",
        children: [
          createMission({ id: "m2", ownerId: "user-1" }),
          createMission({
            id: "m3",
            ownerId: "user-2",
            children: [createMission({ id: "m4", ownerId: "user-1" })],
          }),
        ],
      }),
    ];
    const result = getUserMissionStats(missions, "user-1");
    expect(result.ownedMissions).toBe(2);
  });
});

// ─── getTeamMissionStats ───

describe("getTeamMissionStats()", () => {
  it("returns zero for empty missions array", () => {
    const result = getTeamMissionStats([], "team-1");
    expect(result.linkedMissions).toBe(0);
  });

  it("counts linked missions", () => {
    const missions = [
      createMission({ id: "m1", teamId: "team-1" }),
      createMission({ id: "m2", teamId: "team-1" }),
      createMission({ id: "m3", teamId: "team-2" }),
    ];
    const result = getTeamMissionStats(missions, "team-1");
    expect(result.linkedMissions).toBe(2);
  });

  it("recurses into child missions", () => {
    const missions = [
      createMission({
        id: "m1",
        teamId: "team-2",
        children: [
          createMission({ id: "m2", teamId: "team-1" }),
          createMission({ id: "m3", teamId: "team-1" }),
        ],
      }),
    ];
    const result = getTeamMissionStats(missions, "team-1");
    expect(result.linkedMissions).toBe(2);
  });
});

// ─── checkUserDeletion ───

describe("checkUserDeletion()", () => {
  it("allows deletion when user has no references", () => {
    const missions = [createMission({ ownerId: "user-2" })];
    const result = checkUserDeletion(missions, "user-1", false, false);

    expect(result.canDelete).toBe(true);
    expect(result.canSoftDelete).toBe(true);
    expect(result.blockers).toHaveLength(0);
    expect(result.warnings).toHaveLength(0);
  });

  it("always allows soft delete", () => {
    const missions = [createMission({ ownerId: "user-1", status: "active" })];
    const result = checkUserDeletion(missions, "user-1", true, true);

    expect(result.canSoftDelete).toBe(true);
  });

  it("warns about owned missions", () => {
    const missions = [createMission({ ownerId: "user-1", status: "completed" })];
    const result = checkUserDeletion(missions, "user-1", false, false);

    expect(result.warnings).toContain("1 missão(ões) atribuída(s)");
  });

  it("warns about owned key results", () => {
    const missions = [
      createMission({
        ownerId: "user-2",
        keyResults: [createKeyResult("user-1", "completed")],
      }),
    ];
    const result = checkUserDeletion(missions, "user-1", false, false);

    expect(result.warnings).toContain("1 key result(s) atribuído(s)");
  });

  it("warns about owned tasks", () => {
    const missions = [
      createMission({
        ownerId: "user-2",
        tasks: [createTask("user-1")],
      }),
    ];
    const result = checkUserDeletion(missions, "user-1", false, false);

    expect(result.warnings).toContain("1 tarefa(s) atribuída(s)");
  });

  it("warns when user is team leader", () => {
    const result = checkUserDeletion([], "user-1", true, false);
    expect(result.warnings).toContain("É líder de time");
  });

  it("warns when user is manager", () => {
    const result = checkUserDeletion([], "user-1", false, true);
    expect(result.warnings).toContain("É gestor de outros colaboradores");
  });

  it("blocks deletion for active missions", () => {
    const missions = [createMission({ ownerId: "user-1", status: "active" })];
    const result = checkUserDeletion(missions, "user-1", false, false);

    expect(result.canDelete).toBe(false);
    expect(result.blockers).toContain(
      "1 missão(ões) ativa(s) - reatribua antes de excluir"
    );
  });

  it("blocks deletion for active key results", () => {
    const missions = [
      createMission({
        ownerId: "user-2",
        keyResults: [createKeyResult("user-1", "on_track")],
      }),
    ];
    const result = checkUserDeletion(missions, "user-1", false, false);

    expect(result.canDelete).toBe(false);
    expect(result.blockers).toContain(
      "1 key result(s) em andamento - reatribua antes de excluir"
    );
  });
});

// ─── checkTeamDeletion ───

describe("checkTeamDeletion()", () => {
  it("allows deletion when team has no references", () => {
    const missions = [createMission({ teamId: "team-2" })];
    const result = checkTeamDeletion(missions, "team-1", false, 0);

    expect(result.canDelete).toBe(true);
    expect(result.canSoftDelete).toBe(true);
    expect(result.blockers).toHaveLength(0);
    expect(result.warnings).toHaveLength(0);
  });

  it("warns about linked missions", () => {
    const missions = [createMission({ teamId: "team-1", status: "completed" })];
    const result = checkTeamDeletion(missions, "team-1", false, 0);

    expect(result.warnings).toContain("1 missão(ões) vinculada(s)");
  });

  it("warns about member count", () => {
    const result = checkTeamDeletion([], "team-1", false, 5);
    expect(result.warnings).toContain("5 membro(s) no time");
  });

  it("blocks deletion when team has child teams", () => {
    const result = checkTeamDeletion([], "team-1", true, 0);

    expect(result.canDelete).toBe(false);
    expect(result.canSoftDelete).toBe(false);
    expect(result.blockers).toContain(
      "Time possui subtimes - remova ou reatribua os subtimes primeiro"
    );
  });

  it("blocks deletion when team has active missions", () => {
    const missions = [createMission({ teamId: "team-1", status: "active" })];
    const result = checkTeamDeletion(missions, "team-1", false, 0);

    expect(result.canDelete).toBe(false);
    expect(result.blockers).toContain(
      "1 missão(ões) ativa(s) vinculada(s) - desvincule antes de excluir"
    );
  });
});

// ─── transferUserOwnership ───

describe("transferUserOwnership()", () => {
  it("transfers mission ownership", () => {
    const missions = [
      createMission({ id: "m1", ownerId: "user-1" }),
      createMission({ id: "m2", ownerId: "user-2" }),
    ];

    const { missions: updated, transferred } = transferUserOwnership(
      missions,
      "user-1",
      "user-3"
    );

    expect(updated[0]?.ownerId).toBe("user-3");
    expect(updated[1]?.ownerId).toBe("user-2");
    expect(transferred.missions).toBe(1);
  });

  it("transfers key result ownership", () => {
    const missions = [
      createMission({
        id: "m1",
        ownerId: "user-2",
        keyResults: [createKeyResult("user-1"), createKeyResult("user-2")],
      }),
    ];

    const { missions: updated, transferred } = transferUserOwnership(
      missions,
      "user-1",
      "user-3"
    );

    expect(updated[0]?.keyResults?.[0]?.ownerId).toBe("user-3");
    expect(updated[0]?.keyResults?.[1]?.ownerId).toBe("user-2");
    expect(transferred.keyResults).toBe(1);
  });

  it("transfers task ownership", () => {
    const missions = [
      createMission({
        id: "m1",
        ownerId: "user-2",
        tasks: [createTask("user-1"), createTask("user-2")],
      }),
    ];

    const { missions: updated, transferred } = transferUserOwnership(
      missions,
      "user-1",
      "user-3"
    );

    expect(updated[0]?.tasks?.[0]?.ownerId).toBe("user-3");
    expect(updated[0]?.tasks?.[1]?.ownerId).toBe("user-2");
    expect(transferred.tasks).toBe(1);
  });

  it("recurses into child missions", () => {
    const missions = [
      createMission({
        id: "m1",
        ownerId: "user-2",
        children: [createMission({ id: "m2", ownerId: "user-1" })],
      }),
    ];

    const { missions: updated, transferred } = transferUserOwnership(
      missions,
      "user-1",
      "user-3"
    );

    expect(updated[0]?.children?.[0]?.ownerId).toBe("user-3");
    expect(transferred.missions).toBe(1);
  });

  it("does not mutate original array", () => {
    const missions = [createMission({ id: "m1", ownerId: "user-1" })];
    const originalOwnerId = missions[0]?.ownerId;

    transferUserOwnership(missions, "user-1", "user-3");

    expect(missions[0]?.ownerId).toBe(originalOwnerId);
  });

  it("returns zero counts when nothing to transfer", () => {
    const missions = [createMission({ ownerId: "user-2" })];

    const { transferred } = transferUserOwnership(missions, "user-1", "user-3");

    expect(transferred.missions).toBe(0);
    expect(transferred.keyResults).toBe(0);
    expect(transferred.tasks).toBe(0);
  });
});

// ─── unlinkTeamFromMissions ───

describe("unlinkTeamFromMissions()", () => {
  it("unlinks team from missions", () => {
    const missions = [
      createMission({ id: "m1", teamId: "team-1" }),
      createMission({ id: "m2", teamId: "team-2" }),
    ];

    const { missions: updated, unlinked } = unlinkTeamFromMissions(
      missions,
      "team-1"
    );

    expect(updated[0]?.teamId).toBeNull();
    expect(updated[1]?.teamId).toBe("team-2");
    expect(unlinked).toBe(1);
  });

  it("recurses into child missions", () => {
    const missions = [
      createMission({
        id: "m1",
        teamId: "team-2",
        children: [
          createMission({ id: "m2", teamId: "team-1" }),
          createMission({ id: "m3", teamId: "team-1" }),
        ],
      }),
    ];

    const { missions: updated, unlinked } = unlinkTeamFromMissions(
      missions,
      "team-1"
    );

    expect(updated[0]?.children?.[0]?.teamId).toBeNull();
    expect(updated[0]?.children?.[1]?.teamId).toBeNull();
    expect(unlinked).toBe(2);
  });

  it("does not mutate original array", () => {
    const missions = [createMission({ id: "m1", teamId: "team-1" })];
    const originalTeamId = missions[0]?.teamId;

    unlinkTeamFromMissions(missions, "team-1");

    expect(missions[0]?.teamId).toBe(originalTeamId);
  });

  it("returns zero when nothing to unlink", () => {
    const missions = [createMission({ teamId: "team-2" })];

    const { unlinked } = unlinkTeamFromMissions(missions, "team-1");

    expect(unlinked).toBe(0);
  });
});
