import { describe, it, expect } from "vitest";
import {
  // Date Utilities
  today,
  addDays,
  addWeeks,
  addMonths,
  startOfQuarter,
  endOfQuarter,
  startOfSemester,
  endOfSemester,
  startOfYear,
  endOfYear,
  startOfWeek,
  toIsoDate,
  toIsoDateTime,
  getQuarter,
  getSemester,
  periodProgress,
  expectedProgress,
  // Cycle Generation
  generateRelativeCycles,
  getCurrentQuarterId,
  getCurrentYearId,
  // Check-in Generation
  generateCheckInDates,
  generateCheckInsForKR,
  // Survey Generation
  generateSurveyDates,
  // Progress Simulation
  simulateProgress,
  determineStatus,
  // ID Utilities
  makeId,
  hashString,
  pickFromArray,
  // Deterministic Generation
  deterministicProgress,
  deterministicCreatedAt,
  deterministicUpdatedAt,
  getCurrentCycleInfo,
} from "./seed-utils";

// ─── Date Utilities ───

describe("Date Utilities", () => {
  describe("today()", () => {
    it("returns current date at midnight", () => {
      const result = today();
      expect(result.getHours()).toBe(0);
      expect(result.getMinutes()).toBe(0);
      expect(result.getSeconds()).toBe(0);
      expect(result.getMilliseconds()).toBe(0);
    });

    it("returns today's date", () => {
      const result = today();
      const now = new Date();
      expect(result.getFullYear()).toBe(now.getFullYear());
      expect(result.getMonth()).toBe(now.getMonth());
      expect(result.getDate()).toBe(now.getDate());
    });
  });

  describe("addDays()", () => {
    it("adds positive days correctly", () => {
      const date = new Date(2026, 0, 15); // Jan 15, 2026
      const result = addDays(date, 10);
      expect(result.getDate()).toBe(25);
    });

    it("subtracts days with negative value", () => {
      const date = new Date(2026, 0, 15);
      const result = addDays(date, -10);
      expect(result.getDate()).toBe(5);
    });

    it("handles month rollover", () => {
      const date = new Date(2026, 0, 25); // Jan 25
      const result = addDays(date, 10);
      expect(result.getMonth()).toBe(1); // February
      expect(result.getDate()).toBe(4);
    });

    it("does not mutate original date", () => {
      const date = new Date(2026, 0, 15);
      const originalTime = date.getTime();
      addDays(date, 10);
      expect(date.getTime()).toBe(originalTime);
    });
  });

  describe("addWeeks()", () => {
    it("adds weeks correctly", () => {
      const date = new Date(2026, 0, 1);
      const result = addWeeks(date, 2);
      expect(result.getDate()).toBe(15);
    });

    it("subtracts weeks with negative value", () => {
      const date = new Date(2026, 0, 15);
      const result = addWeeks(date, -2);
      expect(result.getDate()).toBe(1);
    });
  });

  describe("addMonths()", () => {
    it("adds months correctly", () => {
      const date = new Date(2026, 0, 15); // Jan 15
      const result = addMonths(date, 3);
      expect(result.getMonth()).toBe(3); // April
      expect(result.getDate()).toBe(15);
    });

    it("handles year rollover", () => {
      const date = new Date(2026, 10, 15); // Nov 15
      const result = addMonths(date, 3);
      expect(result.getFullYear()).toBe(2027);
      expect(result.getMonth()).toBe(1); // February
    });

    it("does not mutate original date", () => {
      const date = new Date(2026, 0, 15);
      const originalTime = date.getTime();
      addMonths(date, 3);
      expect(date.getTime()).toBe(originalTime);
    });
  });

  describe("startOfQuarter()", () => {
    it("returns Jan 1 for Q1 dates", () => {
      const result = startOfQuarter(new Date(2026, 1, 15)); // Feb 15
      expect(result.getMonth()).toBe(0);
      expect(result.getDate()).toBe(1);
    });

    it("returns Apr 1 for Q2 dates", () => {
      const result = startOfQuarter(new Date(2026, 4, 20)); // May 20
      expect(result.getMonth()).toBe(3);
      expect(result.getDate()).toBe(1);
    });

    it("returns Jul 1 for Q3 dates", () => {
      const result = startOfQuarter(new Date(2026, 7, 10)); // Aug 10
      expect(result.getMonth()).toBe(6);
      expect(result.getDate()).toBe(1);
    });

    it("returns Oct 1 for Q4 dates", () => {
      const result = startOfQuarter(new Date(2026, 11, 25)); // Dec 25
      expect(result.getMonth()).toBe(9);
      expect(result.getDate()).toBe(1);
    });
  });

  describe("endOfQuarter()", () => {
    it("returns Mar 31 for Q1 dates", () => {
      const result = endOfQuarter(new Date(2026, 1, 15)); // Feb 15
      expect(result.getMonth()).toBe(2);
      expect(result.getDate()).toBe(31);
    });

    it("returns Jun 30 for Q2 dates", () => {
      const result = endOfQuarter(new Date(2026, 4, 20)); // May 20
      expect(result.getMonth()).toBe(5);
      expect(result.getDate()).toBe(30);
    });

    it("returns Sep 30 for Q3 dates", () => {
      const result = endOfQuarter(new Date(2026, 7, 10)); // Aug 10
      expect(result.getMonth()).toBe(8);
      expect(result.getDate()).toBe(30);
    });

    it("returns Dec 31 for Q4 dates", () => {
      const result = endOfQuarter(new Date(2026, 11, 25)); // Dec 25
      expect(result.getMonth()).toBe(11);
      expect(result.getDate()).toBe(31);
    });
  });

  describe("startOfSemester()", () => {
    it("returns Jan 1 for S1 dates", () => {
      const result = startOfSemester(new Date(2026, 3, 15)); // Apr 15
      expect(result.getMonth()).toBe(0);
      expect(result.getDate()).toBe(1);
    });

    it("returns Jul 1 for S2 dates", () => {
      const result = startOfSemester(new Date(2026, 9, 15)); // Oct 15
      expect(result.getMonth()).toBe(6);
      expect(result.getDate()).toBe(1);
    });
  });

  describe("endOfSemester()", () => {
    it("returns Jun 30 for S1 dates", () => {
      const result = endOfSemester(new Date(2026, 3, 15)); // Apr 15
      expect(result.getMonth()).toBe(5);
      expect(result.getDate()).toBe(30);
    });

    it("returns Dec 31 for S2 dates", () => {
      const result = endOfSemester(new Date(2026, 9, 15)); // Oct 15
      expect(result.getMonth()).toBe(11);
      expect(result.getDate()).toBe(31);
    });
  });

  describe("startOfYear()", () => {
    it("returns Jan 1 of the same year", () => {
      const result = startOfYear(new Date(2026, 5, 15));
      expect(result.getMonth()).toBe(0);
      expect(result.getDate()).toBe(1);
      expect(result.getFullYear()).toBe(2026);
    });
  });

  describe("endOfYear()", () => {
    it("returns Dec 31 of the same year", () => {
      const result = endOfYear(new Date(2026, 5, 15));
      expect(result.getMonth()).toBe(11);
      expect(result.getDate()).toBe(31);
      expect(result.getFullYear()).toBe(2026);
    });
  });

  describe("startOfWeek()", () => {
    it("returns Monday for a Wednesday", () => {
      const wednesday = new Date(2026, 2, 18); // Wed Mar 18, 2026
      const result = startOfWeek(wednesday);
      expect(result.getDay()).toBe(1); // Monday
      expect(result.getDate()).toBe(16);
    });

    it("returns same Monday for a Monday", () => {
      const monday = new Date(2026, 2, 16); // Mon Mar 16, 2026
      const result = startOfWeek(monday);
      expect(result.getDay()).toBe(1);
      expect(result.getDate()).toBe(16);
    });

    it("returns previous Monday for a Sunday", () => {
      const sunday = new Date(2026, 2, 22); // Sun Mar 22, 2026
      const result = startOfWeek(sunday);
      expect(result.getDay()).toBe(1);
      expect(result.getDate()).toBe(16);
    });

    it("sets time to midnight", () => {
      const date = new Date(2026, 2, 18, 15, 30, 45);
      const result = startOfWeek(date);
      expect(result.getHours()).toBe(0);
      expect(result.getMinutes()).toBe(0);
      expect(result.getSeconds()).toBe(0);
    });
  });

  describe("toIsoDate()", () => {
    it("formats date as YYYY-MM-DD", () => {
      const date = new Date(2026, 2, 15);
      expect(toIsoDate(date)).toBe("2026-03-15");
    });

    it("pads single digit months and days", () => {
      const date = new Date(2026, 0, 5); // Jan 5
      expect(toIsoDate(date)).toBe("2026-01-05");
    });
  });

  describe("toIsoDateTime()", () => {
    it("returns ISO datetime string", () => {
      const date = new Date(2026, 2, 15, 10, 30, 0);
      const result = toIsoDateTime(date);
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
    });
  });

  describe("getQuarter()", () => {
    it("returns 1 for Jan-Mar", () => {
      expect(getQuarter(new Date(2026, 0, 15))).toBe(1);
      expect(getQuarter(new Date(2026, 1, 15))).toBe(1);
      expect(getQuarter(new Date(2026, 2, 15))).toBe(1);
    });

    it("returns 2 for Apr-Jun", () => {
      expect(getQuarter(new Date(2026, 3, 15))).toBe(2);
      expect(getQuarter(new Date(2026, 4, 15))).toBe(2);
      expect(getQuarter(new Date(2026, 5, 15))).toBe(2);
    });

    it("returns 3 for Jul-Sep", () => {
      expect(getQuarter(new Date(2026, 6, 15))).toBe(3);
      expect(getQuarter(new Date(2026, 7, 15))).toBe(3);
      expect(getQuarter(new Date(2026, 8, 15))).toBe(3);
    });

    it("returns 4 for Oct-Dec", () => {
      expect(getQuarter(new Date(2026, 9, 15))).toBe(4);
      expect(getQuarter(new Date(2026, 10, 15))).toBe(4);
      expect(getQuarter(new Date(2026, 11, 15))).toBe(4);
    });
  });

  describe("getSemester()", () => {
    it("returns 1 for Jan-Jun", () => {
      expect(getSemester(new Date(2026, 0, 15))).toBe(1);
      expect(getSemester(new Date(2026, 5, 15))).toBe(1);
    });

    it("returns 2 for Jul-Dec", () => {
      expect(getSemester(new Date(2026, 6, 15))).toBe(2);
      expect(getSemester(new Date(2026, 11, 15))).toBe(2);
    });
  });

  describe("periodProgress()", () => {
    it("returns 0 at the start of period", () => {
      const start = new Date(2026, 0, 1);
      const end = new Date(2026, 2, 31);
      const current = new Date(2026, 0, 1);
      expect(periodProgress(start, end, current)).toBe(0);
    });

    it("returns 100 at the end of period", () => {
      const start = new Date(2026, 0, 1);
      const end = new Date(2026, 2, 31);
      const current = new Date(2026, 2, 31);
      expect(periodProgress(start, end, current)).toBe(100);
    });

    it("returns ~50 at the middle of period", () => {
      const start = new Date(2026, 0, 1);
      const end = new Date(2026, 0, 31);
      const current = new Date(2026, 0, 16);
      const result = periodProgress(start, end, current);
      expect(result).toBeGreaterThan(45);
      expect(result).toBeLessThan(55);
    });

    it("returns 100 if current is past end", () => {
      const start = new Date(2026, 0, 1);
      const end = new Date(2026, 0, 31);
      const current = new Date(2026, 2, 15);
      expect(periodProgress(start, end, current)).toBe(100);
    });

    it("returns 0 if current is before start", () => {
      const start = new Date(2026, 2, 1);
      const end = new Date(2026, 2, 31);
      const current = new Date(2026, 0, 15);
      expect(periodProgress(start, end, current)).toBe(0);
    });

    it("returns 100 if start equals end", () => {
      const date = new Date(2026, 0, 15);
      expect(periodProgress(date, date, date)).toBe(100);
    });
  });

  describe("expectedProgress()", () => {
    it("is an alias for periodProgress", () => {
      const start = new Date(2026, 0, 1);
      const end = new Date(2026, 2, 31);
      const current = new Date(2026, 1, 15);
      expect(expectedProgress(start, end, current)).toBe(
        periodProgress(start, end, current)
      );
    });
  });
});

// ─── Cycle Generation ───

describe("Cycle Generation", () => {
  describe("generateRelativeCycles()", () => {
    it("generates multiple cycles", () => {
      const cycles = generateRelativeCycles();
      expect(cycles.length).toBeGreaterThan(0);
    });

    it("includes current quarter as active", () => {
      const cycles = generateRelativeCycles();
      const activeCycles = cycles.filter((c) => c.status === "active");
      expect(activeCycles.length).toBeGreaterThan(0);

      const quarterCycles = activeCycles.filter((c) => c.type === "quarterly");
      expect(quarterCycles.length).toBe(1);
    });

    it("includes previous quarter as ended", () => {
      const cycles = generateRelativeCycles();
      const endedQuarters = cycles.filter(
        (c) => c.type === "quarterly" && c.status === "ended"
      );
      expect(endedQuarters.length).toBeGreaterThanOrEqual(1);
    });

    it("includes at least one planning quarter", () => {
      const cycles = generateRelativeCycles();
      const planningQuarters = cycles.filter(
        (c) => c.type === "quarterly" && c.status === "planning"
      );
      expect(planningQuarters.length).toBeGreaterThanOrEqual(1);
    });

    it("includes annual cycle", () => {
      const cycles = generateRelativeCycles();
      const annual = cycles.find((c) => c.type === "annual");
      expect(annual).toBeDefined();
      expect(annual?.status).toBe("active");
    });

    it("includes semester cycles", () => {
      const cycles = generateRelativeCycles();
      const semesters = cycles.filter((c) => c.type === "semi_annual");
      expect(semesters.length).toBeGreaterThanOrEqual(1);
    });

    it("has valid date format for all cycles", () => {
      const cycles = generateRelativeCycles();
      const dateRegex = /^\d{4}-\d{2}-\d{2}$/;

      for (const cycle of cycles) {
        expect(cycle.startDate).toMatch(dateRegex);
        expect(cycle.endDate).toMatch(dateRegex);
      }
    });
  });

  describe("getCurrentQuarterId()", () => {
    it("returns format qN-YYYY", () => {
      const result = getCurrentQuarterId();
      expect(result).toMatch(/^q[1-4]-\d{4}$/);
    });

    it("matches current quarter and year", () => {
      const now = new Date();
      const expectedQuarter = Math.floor(now.getMonth() / 3) + 1;
      const expectedYear = now.getFullYear();
      expect(getCurrentQuarterId()).toBe(`q${expectedQuarter}-${expectedYear}`);
    });
  });

  describe("getCurrentYearId()", () => {
    it("returns format ano-YYYY", () => {
      const result = getCurrentYearId();
      expect(result).toMatch(/^ano-\d{4}$/);
    });

    it("matches current year", () => {
      const expectedYear = new Date().getFullYear();
      expect(getCurrentYearId()).toBe(`ano-${expectedYear}`);
    });
  });
});

// ─── Check-in Generation ───

describe("Check-in Generation", () => {
  describe("generateCheckInDates()", () => {
    it("returns all date slots", () => {
      const dates = generateCheckInDates();
      expect(dates.recent).toBeInstanceOf(Date);
      expect(dates.previous).toBeInstanceOf(Date);
      expect(dates.older).toBeInstanceOf(Date);
      expect(dates.oldest).toBeInstanceOf(Date);
      expect(dates.veryOld).toBeInstanceOf(Date);
    });

    it("dates are in correct chronological order", () => {
      const dates = generateCheckInDates();
      expect(dates.recent.getTime()).toBeGreaterThan(dates.previous.getTime());
      expect(dates.previous.getTime()).toBeGreaterThan(dates.older.getTime());
      expect(dates.older.getTime()).toBeGreaterThan(dates.oldest.getTime());
      expect(dates.oldest.getTime()).toBeGreaterThan(dates.veryOld.getTime());
    });

    it("all dates are on weekdays (Mon-Fri)", () => {
      const dates = generateCheckInDates();
      for (const key of Object.keys(dates) as (keyof typeof dates)[]) {
        const day = dates[key].getDay();
        expect(day).toBeGreaterThanOrEqual(1);
        expect(day).toBeLessThanOrEqual(5);
      }
    });
  });

  describe("generateCheckInsForKR()", () => {
    it("generates requested number of check-ins", () => {
      const checkIns = generateCheckInsForKR("kr-test-1", 4);
      expect(checkIns.length).toBeLessThanOrEqual(4);
    });

    it("returns deterministic results for same ID", () => {
      const checkIns1 = generateCheckInsForKR("kr-test-123");
      const checkIns2 = generateCheckInsForKR("kr-test-123");

      expect(checkIns1.length).toBe(checkIns2.length);
      for (let i = 0; i < checkIns1.length; i++) {
        expect(checkIns1[i]?.dateIso).toBe(checkIns2[i]?.dateIso);
      }
    });

    it("returns different results for different IDs", () => {
      const checkIns1 = generateCheckInsForKR("kr-alpha");
      const checkIns2 = generateCheckInsForKR("kr-beta");

      // At least one date should differ
      const dates1 = checkIns1.map((c) => c.dateIso).sort();
      const dates2 = checkIns2.map((c) => c.dateIso).sort();
      expect(dates1).not.toEqual(dates2);
    });

    it("all check-ins are in the past", () => {
      const now = new Date();
      const checkIns = generateCheckInsForKR("kr-test");

      for (const checkIn of checkIns) {
        expect(checkIn.date.getTime()).toBeLessThanOrEqual(now.getTime());
      }
    });

    it("check-ins are sorted by date descending", () => {
      const checkIns = generateCheckInsForKR("kr-test", 4);

      for (let i = 0; i < checkIns.length - 1; i++) {
        const current = checkIns[i]!;
        const next = checkIns[i + 1]!;
        expect(current.date.getTime()).toBeGreaterThanOrEqual(
          next.date.getTime()
        );
      }
    });
  });
});

// ─── Survey Generation ───

describe("Survey Generation", () => {
  describe("generateSurveyDates()", () => {
    it("returns all survey date sets", () => {
      const dates = generateSurveyDates();
      expect(dates.endingSoon).toBeDefined();
      expect(dates.activeMid).toBeDefined();
      expect(dates.recentlyClosed).toBeDefined();
      expect(dates.scheduled).toBeDefined();
      expect(dates.archived).toBeDefined();
    });

    it("each survey has start before end", () => {
      const dates = generateSurveyDates();

      for (const key of Object.keys(dates) as (keyof typeof dates)[]) {
        const survey = dates[key];
        expect(survey.start.getTime()).toBeLessThan(survey.end.getTime());
      }
    });

    it("endingSoon ends this week", () => {
      const dates = generateSurveyDates();
      const now = new Date();
      const endOfWeek = new Date(now);
      endOfWeek.setDate(endOfWeek.getDate() + 7);

      expect(dates.endingSoon.end.getTime()).toBeLessThan(endOfWeek.getTime());
    });

    it("scheduled starts in the future", () => {
      const dates = generateSurveyDates();
      const now = new Date();

      expect(dates.scheduled.start.getTime()).toBeGreaterThan(now.getTime());
    });

    it("archived is in the past", () => {
      const dates = generateSurveyDates();
      const now = new Date();

      expect(dates.archived.end.getTime()).toBeLessThan(now.getTime());
    });
  });
});

// ─── Progress Simulation ───

describe("Progress Simulation", () => {
  describe("simulateProgress()", () => {
    it("returns value between 0 and 100", () => {
      const start = new Date(2026, 0, 1);
      const end = new Date(2026, 2, 31);
      const result = simulateProgress(start, end);

      expect(result).toBeGreaterThanOrEqual(0);
      expect(result).toBeLessThanOrEqual(100);
    });

    it("higher performance factor increases progress", () => {
      const start = new Date(2026, 0, 1);
      const end = new Date(2026, 11, 31);

      const lowProgress = simulateProgress(start, end, 0.5, 0);
      const highProgress = simulateProgress(start, end, 1.5, 0);

      expect(highProgress).toBeGreaterThan(lowProgress);
    });
  });

  describe("determineStatus()", () => {
    it("returns completed for 100% progress", () => {
      expect(determineStatus(100, 50)).toBe("completed");
      expect(determineStatus(100, 100)).toBe("completed");
    });

    it("returns on_track when progress >= expected - 5", () => {
      expect(determineStatus(50, 50)).toBe("on_track");
      expect(determineStatus(48, 50)).toBe("on_track");
      expect(determineStatus(45, 50)).toBe("on_track");
    });

    it("returns attention when progress is 6-20 behind", () => {
      expect(determineStatus(44, 50)).toBe("attention");
      expect(determineStatus(35, 50)).toBe("attention");
      expect(determineStatus(30, 50)).toBe("attention");
    });

    it("returns off_track when progress is >20 behind", () => {
      expect(determineStatus(29, 50)).toBe("off_track");
      expect(determineStatus(10, 50)).toBe("off_track");
      expect(determineStatus(0, 50)).toBe("off_track");
    });
  });
});

// ─── ID Utilities ───

describe("ID Utilities", () => {
  describe("makeId()", () => {
    it("joins parts with hyphen", () => {
      expect(makeId("a", "b", "c")).toBe("a-b-c");
    });

    it("converts numbers to strings", () => {
      expect(makeId("item", 1, "sub", 2)).toBe("item-1-sub-2");
    });

    it("handles single part", () => {
      expect(makeId("single")).toBe("single");
    });

    it("handles empty parts", () => {
      expect(makeId("a", "", "b")).toBe("a--b");
    });
  });

  describe("hashString()", () => {
    it("returns positive number", () => {
      expect(hashString("test")).toBeGreaterThanOrEqual(0);
    });

    it("returns same hash for same input", () => {
      const hash1 = hashString("consistent-test");
      const hash2 = hashString("consistent-test");
      expect(hash1).toBe(hash2);
    });

    it("returns different hash for different input", () => {
      const hash1 = hashString("input-a");
      const hash2 = hashString("input-b");
      expect(hash1).not.toBe(hash2);
    });

    it("handles empty string", () => {
      expect(hashString("")).toBe(0);
    });

    it("handles long strings", () => {
      const longString = "a".repeat(1000);
      const result = hashString(longString);
      expect(typeof result).toBe("number");
      expect(result).toBeGreaterThanOrEqual(0);
    });
  });

  describe("pickFromArray()", () => {
    it("returns an item from the array", () => {
      const array = ["a", "b", "c", "d", "e"];
      const result = pickFromArray(array, "seed");
      expect(array).toContain(result);
    });

    it("returns same item for same seed", () => {
      const array = [1, 2, 3, 4, 5];
      const result1 = pickFromArray(array, "same-seed");
      const result2 = pickFromArray(array, "same-seed");
      expect(result1).toBe(result2);
    });

    it("distributes across array with different seeds", () => {
      const array = ["a", "b", "c", "d", "e"];
      const results = new Set<string>();

      for (let i = 0; i < 100; i++) {
        results.add(pickFromArray(array, `seed-${i}`));
      }

      // Should pick multiple different items
      expect(results.size).toBeGreaterThan(1);
    });
  });
});

// ─── Deterministic Generation ───

describe("Deterministic Generation", () => {
  describe("deterministicProgress()", () => {
    it("returns value within specified range", () => {
      const result = deterministicProgress("test-seed", 20, 80);
      expect(result).toBeGreaterThanOrEqual(20);
      expect(result).toBeLessThan(80);
    });

    it("returns same value for same seed", () => {
      const result1 = deterministicProgress("consistent-seed");
      const result2 = deterministicProgress("consistent-seed");
      expect(result1).toBe(result2);
    });

    it("uses default range when not specified", () => {
      const result = deterministicProgress("test");
      expect(result).toBeGreaterThanOrEqual(20);
      expect(result).toBeLessThan(95);
    });
  });

  describe("deterministicCreatedAt()", () => {
    it("returns ISO datetime string", () => {
      const cycleStart = new Date(2026, 0, 1);
      const result = deterministicCreatedAt("entity-1", cycleStart);
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
    });

    it("returns date within 2 weeks of cycle start", () => {
      const cycleStart = new Date(2026, 0, 1);
      const result = deterministicCreatedAt("entity-1", cycleStart);
      const resultDate = new Date(result);
      const maxDate = new Date(cycleStart);
      maxDate.setDate(maxDate.getDate() + 14);

      expect(resultDate.getTime()).toBeGreaterThanOrEqual(cycleStart.getTime());
      expect(resultDate.getTime()).toBeLessThanOrEqual(maxDate.getTime());
    });

    it("returns same value for same entity ID", () => {
      const cycleStart = new Date(2026, 0, 1);
      const result1 = deterministicCreatedAt("entity-x", cycleStart);
      const result2 = deterministicCreatedAt("entity-x", cycleStart);
      expect(result1).toBe(result2);
    });
  });

  describe("deterministicUpdatedAt()", () => {
    it("returns ISO datetime string", () => {
      const result = deterministicUpdatedAt("entity-1");
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
    });

    it("returns date within last 2 weeks", () => {
      const now = new Date();
      const twoWeeksAgo = new Date(now);
      twoWeeksAgo.setDate(twoWeeksAgo.getDate() - 14);

      const result = deterministicUpdatedAt("entity-1");
      const resultDate = new Date(result);

      expect(resultDate.getTime()).toBeGreaterThanOrEqual(
        twoWeeksAgo.getTime()
      );
      expect(resultDate.getTime()).toBeLessThanOrEqual(now.getTime());
    });

    it("returns same value for same entity ID", () => {
      const result1 = deterministicUpdatedAt("entity-y");
      const result2 = deterministicUpdatedAt("entity-y");
      expect(result1).toBe(result2);
    });
  });

  describe("getCurrentCycleInfo()", () => {
    it("returns all required properties", () => {
      const info = getCurrentCycleInfo();

      expect(info.quarterId).toBeDefined();
      expect(info.yearId).toBeDefined();
      expect(info.quarterStart).toBeInstanceOf(Date);
      expect(info.quarterEnd).toBeInstanceOf(Date);
      expect(info.yearStart).toBeInstanceOf(Date);
      expect(info.yearEnd).toBeInstanceOf(Date);
    });

    it("quarterId matches getCurrentQuarterId", () => {
      const info = getCurrentCycleInfo();
      expect(info.quarterId).toBe(getCurrentQuarterId());
    });

    it("yearId matches getCurrentYearId", () => {
      const info = getCurrentCycleInfo();
      expect(info.yearId).toBe(getCurrentYearId());
    });

    it("quarter dates are within year dates", () => {
      const info = getCurrentCycleInfo();

      expect(info.quarterStart.getTime()).toBeGreaterThanOrEqual(
        info.yearStart.getTime()
      );
      expect(info.quarterEnd.getTime()).toBeLessThanOrEqual(
        info.yearEnd.getTime()
      );
    });
  });
});
