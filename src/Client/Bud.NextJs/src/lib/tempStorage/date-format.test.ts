import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import {
  formatDateBR,
  formatDateShort,
  formatDateTimeBR,
  formatRelativeTime,
  formatMonthYear,
  formatMonthYearCapitalized,
  formatDateRange,
  formatWeekdayDate,
  todayIso,
  nowIso,
} from "./date-format";

// ─── formatDateBR ───

describe("formatDateBR()", () => {
  it("formats ISO date to Brazilian format", () => {
    // Use noon UTC to avoid timezone edge cases
    const result = formatDateBR("2026-03-15T12:00:00.000Z");
    expect(result).toMatch(/15\/03\/2026/);
  });

  it("formats ISO datetime to Brazilian format", () => {
    const result = formatDateBR("2026-03-15T12:00:00.000Z");
    expect(result).toMatch(/\d{2}\/\d{2}\/\d{4}/);
  });

  it("returns empty string for null", () => {
    expect(formatDateBR(null)).toBe("");
  });

  it("returns empty string for undefined", () => {
    expect(formatDateBR(undefined)).toBe("");
  });

  it("returns empty string for invalid date", () => {
    expect(formatDateBR("not-a-date")).toBe("");
  });

  it("returns empty string for empty string", () => {
    expect(formatDateBR("")).toBe("");
  });
});

// ─── formatDateShort ───

describe("formatDateShort()", () => {
  it("formats date to DD/MM", () => {
    // Use noon UTC to avoid timezone edge cases
    expect(formatDateShort("2026-03-15T12:00:00.000Z")).toBe("15/03");
  });

  it("pads single digit day", () => {
    expect(formatDateShort("2026-03-05T12:00:00.000Z")).toBe("05/03");
  });

  it("pads single digit month", () => {
    expect(formatDateShort("2026-01-15T12:00:00.000Z")).toBe("15/01");
  });

  it("returns empty string for invalid date", () => {
    expect(formatDateShort("invalid")).toBe("");
  });
});

// ─── formatDateTimeBR ───

describe("formatDateTimeBR()", () => {
  it("formats datetime to Brazilian format with time", () => {
    // Note: The output depends on timezone, so we test the pattern
    const result = formatDateTimeBR("2026-03-15T10:30:00.000Z");
    expect(result).toMatch(/\d{2}\/\d{2}\/\d{4},?\s+\d{2}:\d{2}/);
  });

  it("returns empty string for invalid datetime", () => {
    expect(formatDateTimeBR("invalid")).toBe("");
  });
});

// ─── formatRelativeTime ───

describe("formatRelativeTime()", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-03-15T12:00:00.000Z"));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns 'Agora' for time less than 1 minute ago", () => {
    const result = formatRelativeTime("2026-03-15T11:59:30.000Z");
    expect(result).toBe("Agora");
  });

  it("returns singular minute format", () => {
    const result = formatRelativeTime("2026-03-15T11:59:00.000Z");
    expect(result).toBe("Há 1 minuto");
  });

  it("returns plural minutes format", () => {
    const result = formatRelativeTime("2026-03-15T11:45:00.000Z");
    expect(result).toBe("Há 15 minutos");
  });

  it("returns singular hour format", () => {
    const result = formatRelativeTime("2026-03-15T11:00:00.000Z");
    expect(result).toBe("Há 1 hora");
  });

  it("returns plural hours format", () => {
    const result = formatRelativeTime("2026-03-15T09:00:00.000Z");
    expect(result).toBe("Há 3 horas");
  });

  it("returns singular day format", () => {
    const result = formatRelativeTime("2026-03-14T12:00:00.000Z");
    expect(result).toBe("Há 1 dia");
  });

  it("returns plural days format", () => {
    const result = formatRelativeTime("2026-03-12T12:00:00.000Z");
    expect(result).toBe("Há 3 dias");
  });

  it("returns full date for dates older than 7 days", () => {
    const result = formatRelativeTime("2026-03-01T12:00:00.000Z");
    expect(result).toBe("01/03/2026");
  });

  it("returns empty string for null", () => {
    expect(formatRelativeTime(null)).toBe("");
  });

  it("returns empty string for undefined", () => {
    expect(formatRelativeTime(undefined)).toBe("");
  });

  it("returns empty string for invalid date", () => {
    expect(formatRelativeTime("invalid")).toBe("");
  });
});

// ─── formatMonthYear ───

describe("formatMonthYear()", () => {
  it("formats date to month and year in Portuguese", () => {
    const result = formatMonthYear("2026-03-15");
    expect(result).toMatch(/março.*2026/i);
  });

  it("accepts Date object", () => {
    const result = formatMonthYear(new Date(2026, 2, 15));
    expect(result).toMatch(/março.*2026/i);
  });

  it("returns empty string for invalid date", () => {
    expect(formatMonthYear("invalid")).toBe("");
  });
});

// ─── formatMonthYearCapitalized ───

describe("formatMonthYearCapitalized()", () => {
  it("capitalizes first letter of month", () => {
    const result = formatMonthYearCapitalized("2026-03-15");
    expect(result.charAt(0)).toBe(result.charAt(0).toUpperCase());
    expect(result).toMatch(/Março/i);
  });

  it("accepts Date object", () => {
    const result = formatMonthYearCapitalized(new Date(2026, 0, 15));
    expect(result).toMatch(/Janeiro/i);
  });

  it("returns empty string for invalid date", () => {
    expect(formatMonthYearCapitalized("invalid")).toBe("");
  });
});

// ─── formatDateRange ───

describe("formatDateRange()", () => {
  it("formats date range with both dates", () => {
    // Use noon UTC to avoid timezone edge cases
    const result = formatDateRange(
      "2026-01-01T12:00:00.000Z",
      "2026-03-31T12:00:00.000Z"
    );
    expect(result).toBe("01/01/2026 — 31/03/2026");
  });

  it("formats range with only start date", () => {
    const result = formatDateRange("2026-01-01T12:00:00.000Z", null);
    expect(result).toBe("A partir de 01/01/2026");
  });

  it("formats range with only end date", () => {
    const result = formatDateRange(null, "2026-03-31T12:00:00.000Z");
    expect(result).toBe("Até 31/03/2026");
  });

  it("returns empty string when both dates are null", () => {
    expect(formatDateRange(null, null)).toBe("");
  });
});

// ─── formatWeekdayDate ───

describe("formatWeekdayDate()", () => {
  it("formats date with weekday, day and month", () => {
    // Sunday, March 15, 2026
    const result = formatWeekdayDate(new Date(2026, 2, 15));
    expect(result).toMatch(/domingo/i);
    expect(result).toMatch(/15/);
    expect(result).toMatch(/março/i);
  });

  it("uses current date when no argument provided", () => {
    const result = formatWeekdayDate();
    // Should contain a weekday name in Portuguese
    expect(result).toMatch(
      /(segunda|terça|quarta|quinta|sexta|sábado|domingo)/i
    );
  });
});

// ─── todayIso ───

describe("todayIso()", () => {
  it("returns ISO date format YYYY-MM-DD", () => {
    const result = todayIso();
    expect(result).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it("returns current date", () => {
    const result = todayIso();
    const now = new Date();
    const expected = now.toISOString().split("T")[0];
    expect(result).toBe(expected);
  });
});

// ─── nowIso ───

describe("nowIso()", () => {
  it("returns full ISO datetime format", () => {
    const result = nowIso();
    expect(result).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
  });

  it("returns current timestamp", () => {
    const before = new Date().getTime();
    const result = nowIso();
    const after = new Date().getTime();

    const resultTime = new Date(result).getTime();
    expect(resultTime).toBeGreaterThanOrEqual(before);
    expect(resultTime).toBeLessThanOrEqual(after);
  });
});
