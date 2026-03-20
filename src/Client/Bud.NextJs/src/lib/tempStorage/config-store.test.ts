import { describe, it, expect, beforeEach } from "vitest";
import {
  // ID creation utilities
  createTagIdFromName,
  createCompanyValueIdFromName,
  createCycleIdFromName,
  createRoleSlug,
  createRoleId,
  // Store functions
  loadConfigSnapshot,
  saveConfigSnapshot,
  resetConfigSnapshot,
  // Hierarchy helpers
  getChildOrganizations,
  getDescendantOrganizations,
  getParentOrganization,
  getAncestorOrganizations,
  getRootOrganizations,
  isDescendantOf,
  type CompanyProfile,
  type ConfigStoreSnapshot,
} from "./config-store";

// ─── ID Creation Utilities ───

describe("ID Creation Utilities", () => {
  describe("createTagIdFromName()", () => {
    it("creates id with tag- prefix", () => {
      expect(createTagIdFromName("Test Tag")).toBe("tag-test-tag");
    });

    it("removes accents", () => {
      expect(createTagIdFromName("Inovação")).toBe("tag-inovacao");
    });

    it("converts to lowercase", () => {
      expect(createTagIdFromName("UPPERCASE")).toBe("tag-uppercase");
    });

    it("replaces spaces with hyphens", () => {
      expect(createTagIdFromName("Multiple Words Here")).toBe(
        "tag-multiple-words-here"
      );
    });

    it("removes special characters", () => {
      expect(createTagIdFromName("Test@#$%Tag")).toBe("tag-test-tag");
    });

    it("handles empty string with timestamp fallback", () => {
      const result = createTagIdFromName("");
      expect(result).toMatch(/^tag-\d+$/);
    });
  });

  describe("createCompanyValueIdFromName()", () => {
    it("creates id with value- prefix", () => {
      expect(createCompanyValueIdFromName("Collaboration")).toBe(
        "value-collaboration"
      );
    });

    it("removes accents", () => {
      expect(createCompanyValueIdFromName("Transparência")).toBe(
        "value-transparencia"
      );
    });

    it("handles empty string with timestamp fallback", () => {
      const result = createCompanyValueIdFromName("");
      expect(result).toMatch(/^value-\d+$/);
    });
  });

  describe("createCycleIdFromName()", () => {
    it("creates id with cycle- prefix", () => {
      expect(createCycleIdFromName("Q1 2026")).toBe("cycle-q1-2026");
    });

    it("handles empty string with timestamp fallback", () => {
      const result = createCycleIdFromName("");
      expect(result).toMatch(/^cycle-\d+$/);
    });
  });

  describe("createRoleSlug()", () => {
    it("creates slug from name", () => {
      expect(createRoleSlug("Admin RH")).toBe("admin-rh");
    });

    it("removes accents", () => {
      expect(createRoleSlug("Gestão")).toBe("gestao");
    });

    it("handles empty string with timestamp fallback", () => {
      const result = createRoleSlug("");
      expect(result).toMatch(/^role-\d+$/);
    });
  });

  describe("createRoleId()", () => {
    it("creates role id from org and slug", () => {
      expect(createRoleId("org-1", "admin-rh")).toBe("role-org-1-admin-rh");
    });
  });
});

// ─── Store Functions ───

describe("Store Functions", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe("loadConfigSnapshot()", () => {
    it("returns seed data when localStorage is empty", () => {
      const snapshot = loadConfigSnapshot();

      expect(snapshot.schemaVersion).toBe(2);
      expect(snapshot.activeOrgId).toBe("org-1");
      expect(Object.keys(snapshot.organizationsById).length).toBeGreaterThan(0);
    });

    it("persists seed data to localStorage on first load", () => {
      loadConfigSnapshot();

      const stored = localStorage.getItem("bud.saas.config-store");
      expect(stored).not.toBeNull();
    });

    it("loads existing data from localStorage", () => {
      const customData: Partial<ConfigStoreSnapshot> = {
        schemaVersion: 2,
        activeOrgId: "custom-org",
        organizationsById: {
          "custom-org": {
            id: "custom-org",
            name: "Custom Org",
            legalName: "Custom Org Ltda",
            slug: "custom-org",
            cnpj: null,
            logoUrl: null,
            plan: "starter",
            timezone: "America/Sao_Paulo",
            language: "pt-BR",
            parentOrgId: null,
            orgType: "company",
            depth: 0,
            path: ["custom-org"],
          },
        },
        companyValuesByOrg: {},
        tagsByOrg: {},
        cyclesByOrg: {},
        rolesByOrg: {},
        permissions: [],
        legacyRoleSlugAliases: {},
        legacyCycleIdAliases: {},
        legacyTagIdAliases: {},
      };

      localStorage.setItem("bud.saas.config-store", JSON.stringify(customData));
      const snapshot = loadConfigSnapshot();

      expect(snapshot.activeOrgId).toBe("custom-org");
      expect(snapshot.organizationsById["custom-org"]).toBeDefined();
    });

    it("handles corrupted localStorage data", () => {
      localStorage.setItem("bud.saas.config-store", "not-valid-json");
      const snapshot = loadConfigSnapshot();

      // Should return seed data
      expect(snapshot.schemaVersion).toBe(2);
      expect(snapshot.activeOrgId).toBe("org-1");
    });

    it("migrates old schema versions", () => {
      const oldData = {
        schemaVersion: 1,
        activeOrgId: "org-1",
        organizationsById: {
          "org-1": {
            id: "org-1",
            name: "Test Org",
            slug: "test-org",
          },
        },
        companyValuesByOrg: {},
        tagsByOrg: {},
        cyclesByOrg: {},
        rolesByOrg: {},
      };

      localStorage.setItem("bud.saas.config-store", JSON.stringify(oldData));
      const snapshot = loadConfigSnapshot();

      expect(snapshot.schemaVersion).toBe(2);
      // Should have migrated org with new fields
      expect(snapshot.organizationsById["org-1"]?.depth).toBeDefined();
      expect(snapshot.organizationsById["org-1"]?.path).toBeDefined();
    });
  });

  describe("saveConfigSnapshot()", () => {
    it("saves data to localStorage", () => {
      const initial = loadConfigSnapshot();
      initial.activeOrgId = "new-org-id";

      // We need to ensure new-org-id exists in organizationsById
      initial.organizationsById["new-org-id"] = {
        id: "new-org-id",
        name: "New Org",
        legalName: "New Org Ltda",
        slug: "new-org",
        cnpj: null,
        logoUrl: null,
        plan: "starter",
        timezone: "America/Sao_Paulo",
        language: "pt-BR",
        parentOrgId: null,
        orgType: "company",
        depth: 0,
        path: ["new-org-id"],
      };

      saveConfigSnapshot(initial);

      const stored = localStorage.getItem("bud.saas.config-store");
      expect(stored).not.toBeNull();
      const parsed = JSON.parse(stored!);
      expect(parsed.activeOrgId).toBe("new-org-id");
    });

    it("updates schemaVersion and updatedAt", () => {
      const initial = loadConfigSnapshot();
      const saved = saveConfigSnapshot(initial);

      expect(saved.schemaVersion).toBe(2);
      expect(saved.updatedAt).toBeDefined();
    });

    it("preserves legacy aliases with defaults", () => {
      const initial = loadConfigSnapshot();
      initial.legacyRoleSlugAliases = { custom: "custom-role" };

      const saved = saveConfigSnapshot(initial);

      // Should have both default and custom aliases
      expect(saved.legacyRoleSlugAliases.custom).toBe("custom-role");
      expect(saved.legacyRoleSlugAliases.admin).toBe("admin-rh"); // default
    });
  });

  describe("resetConfigSnapshot()", () => {
    it("resets to seed data", () => {
      // First, save some custom data
      const initial = loadConfigSnapshot();
      initial.activeOrgId = "custom-org";
      initial.organizationsById["custom-org"] = {
        id: "custom-org",
        name: "Custom",
        legalName: null,
        slug: "custom",
        cnpj: null,
        logoUrl: null,
        plan: "starter",
        timezone: "America/Sao_Paulo",
        language: "pt-BR",
        parentOrgId: null,
        orgType: "company",
        depth: 0,
        path: ["custom-org"],
      };
      saveConfigSnapshot(initial);

      // Reset
      const reset = resetConfigSnapshot();

      expect(reset.activeOrgId).toBe("org-1");
      expect(reset.organizationsById["custom-org"]).toBeUndefined();
    });

    it("persists reset data to localStorage", () => {
      resetConfigSnapshot();

      const stored = localStorage.getItem("bud.saas.config-store");
      const parsed = JSON.parse(stored!);
      expect(parsed.activeOrgId).toBe("org-1");
    });
  });
});

// ─── Hierarchy Helpers ───

describe("Hierarchy Helpers", () => {
  // Setup test org hierarchy:
  // - holding-1 (holding)
  //   - company-1 (subsidiary)
  //     - bu-1 (business_unit)
  //   - company-2 (subsidiary)
  // - standalone-1 (company, no parent)
  const testOrgs: Record<string, CompanyProfile> = {
    "holding-1": {
      id: "holding-1",
      name: "Holding Corp",
      legalName: "Holding Corp S.A.",
      slug: "holding",
      cnpj: null,
      logoUrl: null,
      plan: "enterprise",
      timezone: "America/Sao_Paulo",
      language: "pt-BR",
      parentOrgId: null,
      orgType: "holding",
      depth: 0,
      path: ["holding-1"],
    },
    "company-1": {
      id: "company-1",
      name: "Company 1",
      legalName: "Company 1 Ltda",
      slug: "company-1",
      cnpj: null,
      logoUrl: null,
      plan: "professional",
      timezone: "America/Sao_Paulo",
      language: "pt-BR",
      parentOrgId: "holding-1",
      orgType: "subsidiary",
      depth: 1,
      path: ["holding-1", "company-1"],
    },
    "company-2": {
      id: "company-2",
      name: "Company 2",
      legalName: "Company 2 Ltda",
      slug: "company-2",
      cnpj: null,
      logoUrl: null,
      plan: "professional",
      timezone: "America/Sao_Paulo",
      language: "pt-BR",
      parentOrgId: "holding-1",
      orgType: "subsidiary",
      depth: 1,
      path: ["holding-1", "company-2"],
    },
    "bu-1": {
      id: "bu-1",
      name: "Business Unit 1",
      legalName: "BU1 Ltda",
      slug: "bu-1",
      cnpj: null,
      logoUrl: null,
      plan: "professional",
      timezone: "America/Sao_Paulo",
      language: "pt-BR",
      parentOrgId: "company-1",
      orgType: "business_unit",
      depth: 2,
      path: ["holding-1", "company-1", "bu-1"],
    },
    "standalone-1": {
      id: "standalone-1",
      name: "Standalone Corp",
      legalName: "Standalone Corp Ltda",
      slug: "standalone",
      cnpj: null,
      logoUrl: null,
      plan: "starter",
      timezone: "America/Sao_Paulo",
      language: "pt-BR",
      parentOrgId: null,
      orgType: "company",
      depth: 0,
      path: ["standalone-1"],
    },
  };

  describe("getChildOrganizations()", () => {
    it("returns direct children of holding", () => {
      const children = getChildOrganizations(testOrgs, "holding-1");
      expect(children).toHaveLength(2);
      expect(children.map((c) => c.id)).toContain("company-1");
      expect(children.map((c) => c.id)).toContain("company-2");
    });

    it("returns direct children of company", () => {
      const children = getChildOrganizations(testOrgs, "company-1");
      expect(children).toHaveLength(1);
      expect(children[0]?.id).toBe("bu-1");
    });

    it("returns empty array for leaf org", () => {
      const children = getChildOrganizations(testOrgs, "bu-1");
      expect(children).toHaveLength(0);
    });

    it("returns empty array for standalone org", () => {
      const children = getChildOrganizations(testOrgs, "standalone-1");
      expect(children).toHaveLength(0);
    });
  });

  describe("getDescendantOrganizations()", () => {
    it("returns all descendants of holding", () => {
      const descendants = getDescendantOrganizations(testOrgs, "holding-1");
      expect(descendants).toHaveLength(3);
      expect(descendants.map((d) => d.id)).toContain("company-1");
      expect(descendants.map((d) => d.id)).toContain("company-2");
      expect(descendants.map((d) => d.id)).toContain("bu-1");
    });

    it("returns descendants of company (grandchildren)", () => {
      const descendants = getDescendantOrganizations(testOrgs, "company-1");
      expect(descendants).toHaveLength(1);
      expect(descendants[0]?.id).toBe("bu-1");
    });

    it("returns empty array for leaf org", () => {
      const descendants = getDescendantOrganizations(testOrgs, "bu-1");
      expect(descendants).toHaveLength(0);
    });
  });

  describe("getParentOrganization()", () => {
    it("returns parent of subsidiary", () => {
      const parent = getParentOrganization(testOrgs, "company-1");
      expect(parent?.id).toBe("holding-1");
    });

    it("returns parent of business unit", () => {
      const parent = getParentOrganization(testOrgs, "bu-1");
      expect(parent?.id).toBe("company-1");
    });

    it("returns null for root org", () => {
      const parent = getParentOrganization(testOrgs, "holding-1");
      expect(parent).toBeNull();
    });

    it("returns null for standalone org", () => {
      const parent = getParentOrganization(testOrgs, "standalone-1");
      expect(parent).toBeNull();
    });

    it("returns null for non-existent org", () => {
      const parent = getParentOrganization(testOrgs, "non-existent");
      expect(parent).toBeNull();
    });
  });

  describe("getAncestorOrganizations()", () => {
    it("returns all ancestors of business unit", () => {
      const ancestors = getAncestorOrganizations(testOrgs, "bu-1");
      expect(ancestors).toHaveLength(2);
      expect(ancestors[0]?.id).toBe("company-1");
      expect(ancestors[1]?.id).toBe("holding-1");
    });

    it("returns parent only for subsidiary", () => {
      const ancestors = getAncestorOrganizations(testOrgs, "company-1");
      expect(ancestors).toHaveLength(1);
      expect(ancestors[0]?.id).toBe("holding-1");
    });

    it("returns empty array for root org", () => {
      const ancestors = getAncestorOrganizations(testOrgs, "holding-1");
      expect(ancestors).toHaveLength(0);
    });
  });

  describe("getRootOrganizations()", () => {
    it("returns orgs with no parent", () => {
      const roots = getRootOrganizations(testOrgs);
      expect(roots).toHaveLength(2);
      expect(roots.map((r) => r.id)).toContain("holding-1");
      expect(roots.map((r) => r.id)).toContain("standalone-1");
    });
  });

  describe("isDescendantOf()", () => {
    it("returns true for direct child", () => {
      expect(isDescendantOf(testOrgs, "company-1", "holding-1")).toBe(true);
    });

    it("returns true for grandchild", () => {
      expect(isDescendantOf(testOrgs, "bu-1", "holding-1")).toBe(true);
    });

    it("returns false for same org", () => {
      expect(isDescendantOf(testOrgs, "holding-1", "holding-1")).toBe(false);
    });

    it("returns false for unrelated orgs", () => {
      expect(isDescendantOf(testOrgs, "standalone-1", "holding-1")).toBe(false);
    });

    it("returns false for parent checking descendant", () => {
      expect(isDescendantOf(testOrgs, "holding-1", "company-1")).toBe(false);
    });

    it("returns false for non-existent org", () => {
      expect(isDescendantOf(testOrgs, "non-existent", "holding-1")).toBe(false);
    });
  });
});

// ─── Seed Data Validation ───

describe("Seed Data Validation", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("seed includes default organization", () => {
    const snapshot = loadConfigSnapshot();
    expect(snapshot.organizationsById["org-1"]).toBeDefined();
    expect(snapshot.organizationsById["org-1"]?.name).toBe(
      "Bud Tecnologia Ltda."
    );
  });

  it("seed includes company values", () => {
    const snapshot = loadConfigSnapshot();
    const values = snapshot.companyValuesByOrg["org-1"];
    expect(values).toBeDefined();
    expect(values?.length).toBeGreaterThan(0);
    expect(values?.some((v) => v.id === "value-inovacao")).toBe(true);
  });

  it("seed includes tags", () => {
    const snapshot = loadConfigSnapshot();
    const tags = snapshot.tagsByOrg["org-1"];
    expect(tags).toBeDefined();
    expect(tags?.length).toBeGreaterThan(0);
  });

  it("seed includes cycles with dynamic dates", () => {
    const snapshot = loadConfigSnapshot();
    const cycles = snapshot.cyclesByOrg["org-1"];
    expect(cycles).toBeDefined();
    expect(cycles?.length).toBeGreaterThan(0);

    // Should have active cycle
    const activeCycles = cycles?.filter((c) => c.status === "active");
    expect(activeCycles?.length).toBeGreaterThan(0);
  });

  it("seed includes roles with permissions", () => {
    const snapshot = loadConfigSnapshot();
    const roles = snapshot.rolesByOrg["org-1"];
    expect(roles).toBeDefined();
    expect(roles?.length).toBeGreaterThan(0);

    const adminRole = roles?.find((r) => r.slug === "super-admin");
    expect(adminRole).toBeDefined();
    expect(adminRole?.permissionIds.length).toBeGreaterThan(0);
  });

  it("seed includes permissions", () => {
    const snapshot = loadConfigSnapshot();
    expect(snapshot.permissions.length).toBeGreaterThan(0);
    expect(snapshot.permissions.some((p) => p.id === "people.view")).toBe(true);
  });

  it("seed includes legacy aliases", () => {
    const snapshot = loadConfigSnapshot();

    expect(snapshot.legacyRoleSlugAliases.admin).toBe("admin-rh");
    expect(snapshot.legacyRoleSlugAliases.manager).toBe("gestor");
    expect(snapshot.legacyCycleIdAliases["annual-2026"]).toBe("ano-2026");
    expect(snapshot.legacyTagIdAliases.strategy).toBe("tag-estrategia");
  });
});

// ─── Data Sanitization (via migration) ───

describe("Data Sanitization", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("sanitizes invalid organization data", () => {
    const invalidData = {
      schemaVersion: 2,
      activeOrgId: "test-org",
      organizationsById: {
        "test-org": {
          id: "test-org",
          name: "Test",
          slug: "test",
          // Missing many fields
        },
      },
      companyValuesByOrg: {},
      tagsByOrg: {},
      cyclesByOrg: {},
      rolesByOrg: {},
    };

    localStorage.setItem("bud.saas.config-store", JSON.stringify(invalidData));
    const snapshot = loadConfigSnapshot();

    // Should fill in defaults
    const org = snapshot.organizationsById["test-org"];
    expect(org?.plan).toBe("professional");
    expect(org?.timezone).toBe("America/Sao_Paulo");
    expect(org?.depth).toBe(0);
    expect(org?.path).toEqual(["test-org"]);
  });

  it("sanitizes invalid tag data", () => {
    const invalidData = {
      schemaVersion: 2,
      activeOrgId: "org-1",
      organizationsById: {
        "org-1": { id: "org-1", name: "Test", slug: "test" },
      },
      tagsByOrg: {
        "org-1": [
          { id: "tag-1", name: "Tag 1" }, // Missing color
          { name: "Invalid" }, // Missing id
        ],
      },
      companyValuesByOrg: {},
      cyclesByOrg: {},
      rolesByOrg: {},
    };

    localStorage.setItem("bud.saas.config-store", JSON.stringify(invalidData));
    const snapshot = loadConfigSnapshot();

    const tags = snapshot.tagsByOrg["org-1"];
    // Should have sanitized the valid tag
    const tag1 = tags?.find((t) => t.id === "tag-1");
    expect(tag1?.color).toBe("neutral"); // default
    // Invalid tag (missing id) should be filtered out
    expect(tags?.some((t) => t.name === "Invalid" && !t.id)).toBe(false);
  });

  it("sanitizes invalid cycle data", () => {
    const invalidData = {
      schemaVersion: 2,
      activeOrgId: "org-1",
      organizationsById: {
        "org-1": { id: "org-1", name: "Test", slug: "test" },
      },
      cyclesByOrg: {
        "org-1": [
          {
            id: "cycle-1",
            name: "Q1",
            startDate: "2026-01-01",
            endDate: "2026-03-31",
            // Missing type and status
          },
        ],
      },
      companyValuesByOrg: {},
      tagsByOrg: {},
      rolesByOrg: {},
    };

    localStorage.setItem("bud.saas.config-store", JSON.stringify(invalidData));
    const snapshot = loadConfigSnapshot();

    const cycles = snapshot.cyclesByOrg["org-1"];
    const cycle1 = cycles?.find((c) => c.id === "cycle-1");
    expect(cycle1?.type).toBe("custom"); // default
    expect(cycle1?.status).toBe("planning"); // default
  });

  it("falls back to seed data when arrays are empty", () => {
    const emptyData = {
      schemaVersion: 2,
      activeOrgId: "org-1",
      organizationsById: {
        "org-1": { id: "org-1", name: "Test", slug: "test" },
      },
      companyValuesByOrg: { "org-1": [] },
      tagsByOrg: { "org-1": [] },
      cyclesByOrg: { "org-1": [] },
      rolesByOrg: { "org-1": [] },
    };

    localStorage.setItem("bud.saas.config-store", JSON.stringify(emptyData));
    const snapshot = loadConfigSnapshot();

    // Should fall back to seed data since arrays are empty
    expect(snapshot.companyValuesByOrg["org-1"]?.length).toBeGreaterThan(0);
    expect(snapshot.tagsByOrg["org-1"]?.length).toBeGreaterThan(0);
    expect(snapshot.cyclesByOrg["org-1"]?.length).toBeGreaterThan(0);
    expect(snapshot.rolesByOrg["org-1"]?.length).toBeGreaterThan(0);
  });
});
