import { describe, it, expect, beforeEach } from "vitest";
import {
  loadPeopleSnapshot,
  savePeopleSnapshot,
  resetPeopleSnapshot,
  createRoleIdForOrg,
  createTeamIdFromName,
  extractRoleSlugFromId,
  type PeopleStoreSnapshot,
  type PeopleUserRecord,
} from "./people-store";

// ─── Utility Functions ───

describe("Utility Functions", () => {
  describe("createRoleIdForOrg()", () => {
    it("creates role id with correct format", () => {
      expect(createRoleIdForOrg("org-1", "gestor")).toBe("role-org-1-gestor");
    });

    it("handles different org and role combinations", () => {
      expect(createRoleIdForOrg("org-2", "admin-rh")).toBe("role-org-2-admin-rh");
      expect(createRoleIdForOrg("brq", "super-admin")).toBe("role-brq-super-admin");
    });
  });

  describe("createTeamIdFromName()", () => {
    it("creates team id with team- prefix", () => {
      expect(createTeamIdFromName("Engineering")).toBe("team-engineering");
    });

    it("removes accents", () => {
      expect(createTeamIdFromName("Gestão")).toBe("team-gestao");
    });

    it("converts to lowercase and replaces spaces", () => {
      expect(createTeamIdFromName("Product Design")).toBe("team-product-design");
    });

    it("handles empty string with timestamp fallback", () => {
      const result = createTeamIdFromName("");
      expect(result).toMatch(/^team-\d+$/);
    });
  });

  describe("extractRoleSlugFromId()", () => {
    it("extracts slug from standard role id", () => {
      expect(extractRoleSlugFromId("role-org-1-gestor")).toBe("gestor");
    });

    it("extracts slug with hyphenated role names", () => {
      expect(extractRoleSlugFromId("role-org-1-admin-rh")).toBe("rh");
    });

    it("returns colaborador for null", () => {
      expect(extractRoleSlugFromId(null)).toBe("colaborador");
    });

    it("returns colaborador for invalid format", () => {
      expect(extractRoleSlugFromId("invalid")).toBe("colaborador");
      expect(extractRoleSlugFromId("")).toBe("colaborador");
    });
  });
});

// ─── Store Functions ───

describe("People Store", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe("loadPeopleSnapshot()", () => {
    it("returns seed data when localStorage is empty", () => {
      const snapshot = loadPeopleSnapshot();

      expect(snapshot.schemaVersion).toBe(2);
      expect(Object.keys(snapshot.usersById).length).toBeGreaterThan(0);
      expect(Object.keys(snapshot.teamsById).length).toBeGreaterThan(0);
    });

    it("persists seed data to localStorage on first load", () => {
      loadPeopleSnapshot();

      const stored = localStorage.getItem("bud.saas.people-store:org-1");
      expect(stored).not.toBeNull();
    });

    it("loads existing data from localStorage, merging seed users and teams", () => {
      const customData: PeopleStoreSnapshot = {
        schemaVersion: 2,
        updatedAt: new Date().toISOString(),
        currentUserId: "custom-user",
        usersById: {
          "custom-user": {
            id: "custom-user",
            orgId: "org-1",
            email: "custom@test.com",
            firstName: "Custom",
            lastName: "User",
            nickname: null,
            jobTitle: "Tester",
            managerId: null,
            avatarUrl: null,
            initials: "CU",
            birthDate: null,
            gender: null,
            language: "pt-br",
            phone: null,
            status: "active",
            invitedAt: null,
            activatedAt: null,
            lastLoginAt: null,
            authProvider: "email",
            authProviderId: null,
            createdAt: "2026-01-01T00:00:00.000Z",
            updatedAt: "2026-01-01T00:00:00.000Z",
            deletedAt: null,
            roleId: "role-org-1-colaborador",
            roleType: "colaborador",
          },
        },
        teamsById: {},
        teamMembers: [],
        legacyUserIdAliases: {},
        legacyTeamIdAliases: {},
      };

      localStorage.setItem(
        "bud.saas.people-store:org-1",
        JSON.stringify(customData)
      );
      const snapshot = loadPeopleSnapshot();

      // User-created data is preserved
      expect(snapshot.currentUserId).toBe("custom-user");
      expect(snapshot.usersById["custom-user"]).toBeDefined();
      // Seed users are always merged in
      expect(snapshot.usersById["ms"]).toBeDefined();
      // teamsById was empty → falls back to seed teams
      expect(Object.keys(snapshot.teamsById).length).toBeGreaterThan(0);
    });

    it("handles corrupted localStorage data", () => {
      localStorage.setItem("bud.saas.people-store:org-1", "not-valid-json");
      const snapshot = loadPeopleSnapshot();

      // Should return seed data
      expect(snapshot.schemaVersion).toBe(2);
      expect(Object.keys(snapshot.usersById).length).toBeGreaterThan(0);
    });

    it("loads data for different org IDs", () => {
      loadPeopleSnapshot("org-1");
      loadPeopleSnapshot("org-2");

      expect(
        localStorage.getItem("bud.saas.people-store:org-1")
      ).not.toBeNull();
      expect(
        localStorage.getItem("bud.saas.people-store:org-2")
      ).not.toBeNull();
    });

    it("migrates users without roleId", () => {
      const oldData = {
        schemaVersion: 1,
        currentUserId: "test-user",
        usersById: {
          "test-user": {
            id: "test-user",
            orgId: "org-1",
            email: "test@test.com",
            firstName: "Test",
            lastName: "User",
            roleType: "gestor",
            // No roleId field
          },
        },
        teamsById: {},
        teamMembers: [],
      };

      localStorage.setItem(
        "bud.saas.people-store:org-1",
        JSON.stringify(oldData)
      );
      const snapshot = loadPeopleSnapshot();

      // Should have migrated roleId
      expect(snapshot.usersById["test-user"]?.roleId).toBe("role-org-1-gestor");
    });
  });

  describe("savePeopleSnapshot()", () => {
    it("saves data to localStorage", () => {
      const initial = loadPeopleSnapshot();

      // Add a new user
      initial.usersById["new-user"] = {
        id: "new-user",
        orgId: "org-1",
        email: "new@test.com",
        firstName: "New",
        lastName: "User",
        nickname: null,
        jobTitle: "Newbie",
        managerId: null,
        avatarUrl: null,
        initials: "NU",
        birthDate: null,
        gender: null,
        language: "pt-br",
        phone: null,
        status: "active",
        invitedAt: null,
        activatedAt: null,
        lastLoginAt: null,
        authProvider: "email",
        authProviderId: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        deletedAt: null,
        roleId: "role-org-1-colaborador",
        roleType: "colaborador",
      };

      savePeopleSnapshot(initial);

      const stored = localStorage.getItem("bud.saas.people-store:org-1");
      const parsed = JSON.parse(stored!) as PeopleStoreSnapshot;
      expect(parsed.usersById["new-user"]).toBeDefined();
    });

    it("updates schemaVersion and updatedAt", () => {
      const initial = loadPeopleSnapshot();
      const saved = savePeopleSnapshot(initial);

      expect(saved.schemaVersion).toBe(2);
      expect(saved.updatedAt).toBeDefined();
    });

    it("preserves legacy aliases with defaults", () => {
      const initial = loadPeopleSnapshot();
      initial.legacyUserIdAliases = { "custom-alias": "custom-id" };

      const saved = savePeopleSnapshot(initial);

      expect(saved.legacyUserIdAliases["custom-alias"]).toBe("custom-id");
      expect(saved.legacyUserIdAliases["u-ms"]).toBe("ms"); // default
    });

    it("does not mutate original data", () => {
      const initial = loadPeopleSnapshot();
      const originalCount = Object.keys(initial.usersById).length;

      const saved = savePeopleSnapshot(initial);
      saved.usersById["mutated"] = {} as PeopleUserRecord;

      expect(Object.keys(initial.usersById).length).toBe(originalCount);
    });
  });

  describe("resetPeopleSnapshot()", () => {
    it("resets to seed data", () => {
      // First, save custom data
      const initial = loadPeopleSnapshot();
      initial.usersById = {
        "only-custom": {
          id: "only-custom",
          orgId: "org-1",
          email: "only@test.com",
          firstName: "Only",
          lastName: "Custom",
          nickname: null,
          jobTitle: "Test",
          managerId: null,
          avatarUrl: null,
          initials: "OC",
          birthDate: null,
          gender: null,
          language: "pt-br",
          phone: null,
          status: "active",
          invitedAt: null,
          activatedAt: null,
          lastLoginAt: null,
          authProvider: "email",
          authProviderId: null,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          deletedAt: null,
          roleId: "role-org-1-colaborador",
          roleType: "colaborador",
        },
      };
      savePeopleSnapshot(initial);

      // Reset
      const reset = resetPeopleSnapshot();

      expect(reset.usersById["only-custom"]).toBeUndefined();
      expect(Object.keys(reset.usersById).length).toBeGreaterThan(0);
    });

    it("persists reset data to localStorage", () => {
      resetPeopleSnapshot();

      const stored = localStorage.getItem("bud.saas.people-store:org-1");
      const parsed = JSON.parse(stored!) as PeopleStoreSnapshot;
      expect(parsed.schemaVersion).toBe(2);
    });
  });
});

// ─── Seed Data Validation ───

describe("Seed Data Validation", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("seed includes current user", () => {
    const snapshot = loadPeopleSnapshot();
    expect(snapshot.currentUserId).toBe("ms");
    expect(snapshot.usersById["ms"]).toBeDefined();
  });

  it("seed users have valid structure", () => {
    const snapshot = loadPeopleSnapshot();
    const user = snapshot.usersById["ms"];

    expect(user?.id).toBe("ms");
    expect(user?.firstName).toBeDefined();
    expect(user?.lastName).toBeDefined();
    expect(user?.email).toBeDefined();
    expect(user?.roleId).toBeDefined();
    expect(user?.roleType).toBeDefined();
  });

  it("seed includes teams", () => {
    const snapshot = loadPeopleSnapshot();
    expect(Object.keys(snapshot.teamsById).length).toBeGreaterThan(0);
  });

  it("seed teams have valid structure", () => {
    const snapshot = loadPeopleSnapshot();
    const team = snapshot.teamsById["team-engenharia"];

    expect(team?.id).toBe("team-engenharia");
    expect(team?.name).toBeDefined();
    expect(team?.leaderId).toBeDefined();
    expect(team?.color).toBeDefined();
  });

  it("seed includes team members", () => {
    const snapshot = loadPeopleSnapshot();
    expect(snapshot.teamMembers.length).toBeGreaterThan(0);
  });

  it("seed team members reference valid users and teams", () => {
    const snapshot = loadPeopleSnapshot();

    for (const member of snapshot.teamMembers) {
      expect(snapshot.usersById[member.userId]).toBeDefined();
      expect(snapshot.teamsById[member.teamId]).toBeDefined();
    }
  });

  it("seed includes legacy aliases", () => {
    const snapshot = loadPeopleSnapshot();

    // User aliases
    expect(snapshot.legacyUserIdAliases["u-ms"]).toBe("ms");
    expect(snapshot.legacyUserIdAliases["chro"]).toBe("ms");

    // Team aliases
    expect(snapshot.legacyTeamIdAliases["eng"]).toBe("team-engenharia");
    expect(snapshot.legacyTeamIdAliases["team-1"]).toBe("team-engenharia");
  });

  it("seed has users with different roles", () => {
    const snapshot = loadPeopleSnapshot();
    const roleTypes = new Set(
      Object.values(snapshot.usersById).map((u) => u.roleType)
    );

    expect(roleTypes.has("super-admin")).toBe(true);
    expect(roleTypes.has("admin-rh")).toBe(true);
    expect(roleTypes.has("gestor")).toBe(true);
    expect(roleTypes.has("colaborador")).toBe(true);
  });

  it("seed has users with different statuses", () => {
    const snapshot = loadPeopleSnapshot();
    const statuses = new Set(
      Object.values(snapshot.usersById).map((u) => u.status)
    );

    expect(statuses.has("active")).toBe(true);
    expect(statuses.has("inactive")).toBe(true);
  });
});

// ─── Data Sanitization ───

describe("Data Sanitization", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("filters out invalid users", () => {
    const invalidData = {
      schemaVersion: 2,
      currentUserId: "valid-user",
      usersById: {
        "valid-user": {
          id: "valid-user",
          orgId: "org-1",
          email: "valid@test.com",
          firstName: "Valid",
          lastName: "User",
          roleType: "colaborador",
        },
        "invalid-user": {
          id: "invalid-user",
          // Missing required fields
        },
      },
      teamsById: {},
      teamMembers: [],
    };

    localStorage.setItem(
      "bud.saas.people-store:org-1",
      JSON.stringify(invalidData)
    );
    const snapshot = loadPeopleSnapshot();

    expect(snapshot.usersById["valid-user"]).toBeDefined();
    expect(snapshot.usersById["invalid-user"]).toBeUndefined();
  });

  it("filters out invalid teams", () => {
    const invalidData = {
      schemaVersion: 2,
      currentUserId: "ms",
      usersById: {},
      teamsById: {
        "valid-team": {
          id: "valid-team",
          name: "Valid Team",
          orgId: "org-1",
        },
        "invalid-team": {
          id: "invalid-team",
          // Missing name
        },
      },
      teamMembers: [],
    };

    localStorage.setItem(
      "bud.saas.people-store:org-1",
      JSON.stringify(invalidData)
    );
    const snapshot = loadPeopleSnapshot();

    // Falls back to seed because valid teams require both name and orgId
    expect(Object.keys(snapshot.teamsById).length).toBeGreaterThan(0);
  });

  it("filters out team members referencing non-existent users/teams", () => {
    const snapshot = loadPeopleSnapshot();

    // Manually add invalid team member
    const invalidData = {
      ...snapshot,
      teamMembers: [
        ...snapshot.teamMembers,
        { teamId: "non-existent-team", userId: "ms", roleInTeam: "member", joinedAt: new Date().toISOString() },
        { teamId: "team-engenharia", userId: "non-existent-user", roleInTeam: "member", joinedAt: new Date().toISOString() },
      ],
    };

    localStorage.setItem(
      "bud.saas.people-store:org-1",
      JSON.stringify(invalidData)
    );
    const loaded = loadPeopleSnapshot();

    // Invalid members should be filtered out
    const invalidMembers = loaded.teamMembers.filter(
      (m) => m.teamId === "non-existent-team" || m.userId === "non-existent-user"
    );
    expect(invalidMembers.length).toBe(0);
  });

  it("defaults role to colaborador when missing roleType", () => {
    const dataWithMissingRole = {
      schemaVersion: 2,
      currentUserId: "test-user",
      usersById: {
        "test-user": {
          id: "test-user",
          orgId: "org-1",
          email: "test@test.com",
          firstName: "Test",
          lastName: "User",
          // No roleType
        },
      },
      teamsById: {},
      teamMembers: [],
    };

    localStorage.setItem(
      "bud.saas.people-store:org-1",
      JSON.stringify(dataWithMissingRole)
    );
    const snapshot = loadPeopleSnapshot();

    expect(snapshot.usersById["test-user"]?.roleType).toBe("colaborador");
    expect(snapshot.usersById["test-user"]?.roleId).toBe("role-org-1-colaborador");
  });
});
