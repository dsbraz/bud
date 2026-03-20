/**
 * Seed Utils — Dynamic date-relative seed data utilities
 *
 * Provides helper functions to generate seed data that is automatically
 * relative to the current date, ensuring the demo data always feels "fresh"
 * regardless of when the user accesses the application.
 */

// ─── Date Utilities ───

/**
 * Get today's date at midnight in local timezone
 */
export function today(): Date {
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  return now;
}

/**
 * Add days to a date
 */
export function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

/**
 * Add weeks to a date
 */
export function addWeeks(date: Date, weeks: number): Date {
  return addDays(date, weeks * 7);
}

/**
 * Add months to a date
 */
export function addMonths(date: Date, months: number): Date {
  const result = new Date(date);
  result.setMonth(result.getMonth() + months);
  return result;
}

/**
 * Get the start of the current quarter
 */
export function startOfQuarter(date: Date): Date {
  const quarter = Math.floor(date.getMonth() / 3);
  return new Date(date.getFullYear(), quarter * 3, 1);
}

/**
 * Get the end of the current quarter
 */
export function endOfQuarter(date: Date): Date {
  const quarter = Math.floor(date.getMonth() / 3);
  const endMonth = (quarter + 1) * 3;
  return new Date(date.getFullYear(), endMonth, 0);
}

/**
 * Get the start of the current semester
 */
export function startOfSemester(date: Date): Date {
  const semester = Math.floor(date.getMonth() / 6);
  return new Date(date.getFullYear(), semester * 6, 1);
}

/**
 * Get the end of the current semester
 */
export function endOfSemester(date: Date): Date {
  const semester = Math.floor(date.getMonth() / 6);
  const endMonth = (semester + 1) * 6;
  return new Date(date.getFullYear(), endMonth, 0);
}

/**
 * Get the start of the current year
 */
export function startOfYear(date: Date): Date {
  return new Date(date.getFullYear(), 0, 1);
}

/**
 * Get the end of the current year
 */
export function endOfYear(date: Date): Date {
  return new Date(date.getFullYear(), 11, 31);
}

/**
 * Get the Monday of the current week
 */
export function startOfWeek(date: Date): Date {
  const result = new Date(date);
  const day = result.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  result.setDate(result.getDate() + diff);
  result.setHours(0, 0, 0, 0);
  return result;
}

/**
 * Format date as ISO string (YYYY-MM-DD)
 */
export function toIsoDate(date: Date): string {
  return date.toISOString().split("T")[0]!;
}

/**
 * Format date as ISO datetime string
 */
export function toIsoDateTime(date: Date): string {
  return date.toISOString();
}

/**
 * Get the current quarter number (1-4)
 */
export function getQuarter(date: Date): number {
  return Math.floor(date.getMonth() / 3) + 1;
}

/**
 * Get the current semester number (1-2)
 */
export function getSemester(date: Date): number {
  return Math.floor(date.getMonth() / 6) + 1;
}

/**
 * Calculate progress through a period (0-100)
 */
export function periodProgress(start: Date, end: Date, current: Date = today()): number {
  const totalMs = end.getTime() - start.getTime();
  const elapsedMs = current.getTime() - start.getTime();
  
  if (totalMs <= 0) return 100;
  if (elapsedMs <= 0) return 0;
  if (elapsedMs >= totalMs) return 100;
  
  return Math.round((elapsedMs / totalMs) * 100);
}

/**
 * Get expected progress percentage based on time elapsed in period
 */
export function expectedProgress(start: Date, end: Date, current: Date = today()): number {
  return periodProgress(start, end, current);
}

// ─── Cycle Generation ───

export interface CycleDefinition {
  id: string;
  name: string;
  type: "quarterly" | "semi_annual" | "annual" | "custom";
  startDate: string;
  endDate: string;
  status: "planning" | "active" | "ended";
}

/**
 * Generate cycles relative to current date
 */
export function generateRelativeCycles(): CycleDefinition[] {
  const now = today();
  const year = now.getFullYear();
  const quarter = getQuarter(now);
  const semester = getSemester(now);

  const cycles: CycleDefinition[] = [];

  // Previous quarter (ended)
  const prevQuarter = quarter === 1 ? 4 : quarter - 1;
  const prevQuarterYear = quarter === 1 ? year - 1 : year;
  const prevQuarterStart = new Date(prevQuarterYear, (prevQuarter - 1) * 3, 1);
  const prevQuarterEnd = new Date(prevQuarterYear, prevQuarter * 3, 0);
  cycles.push({
    id: `q${prevQuarter}-${prevQuarterYear}`,
    name: `Q${prevQuarter} ${prevQuarterYear}`,
    type: "quarterly",
    startDate: toIsoDate(prevQuarterStart),
    endDate: toIsoDate(prevQuarterEnd),
    status: "ended",
  });

  // Current quarter (active)
  const currentQuarterStart = startOfQuarter(now);
  const currentQuarterEnd = endOfQuarter(now);
  cycles.push({
    id: `q${quarter}-${year}`,
    name: `Q${quarter} ${year}`,
    type: "quarterly",
    startDate: toIsoDate(currentQuarterStart),
    endDate: toIsoDate(currentQuarterEnd),
    status: "active",
  });

  // Next quarter (planning)
  const nextQuarter = quarter === 4 ? 1 : quarter + 1;
  const nextQuarterYear = quarter === 4 ? year + 1 : year;
  const nextQuarterStart = new Date(nextQuarterYear, (nextQuarter - 1) * 3, 1);
  const nextQuarterEnd = new Date(nextQuarterYear, nextQuarter * 3, 0);
  cycles.push({
    id: `q${nextQuarter}-${nextQuarterYear}`,
    name: `Q${nextQuarter} ${nextQuarterYear}`,
    type: "quarterly",
    startDate: toIsoDate(nextQuarterStart),
    endDate: toIsoDate(nextQuarterEnd),
    status: "planning",
  });

  // Remaining quarters of the year (if any)
  for (let q = nextQuarter + 1; q <= 4; q++) {
    if (q > 4) break;
    const qStart = new Date(year, (q - 1) * 3, 1);
    const qEnd = new Date(year, q * 3, 0);
    cycles.push({
      id: `q${q}-${year}`,
      name: `Q${q} ${year}`,
      type: "quarterly",
      startDate: toIsoDate(qStart),
      endDate: toIsoDate(qEnd),
      status: "planning",
    });
  }

  // Current semester
  const currentSemesterStart = startOfSemester(now);
  const currentSemesterEnd = endOfSemester(now);
  cycles.push({
    id: `s${semester}-${year}`,
    name: `${semester === 1 ? "1" : "2"}o Semestre ${year}`,
    type: "semi_annual",
    startDate: toIsoDate(currentSemesterStart),
    endDate: toIsoDate(currentSemesterEnd),
    status: "active",
  });

  // Next semester if applicable
  if (semester === 1) {
    const nextSemesterStart = new Date(year, 6, 1);
    const nextSemesterEnd = new Date(year, 11, 31);
    cycles.push({
      id: `s2-${year}`,
      name: `2o Semestre ${year}`,
      type: "semi_annual",
      startDate: toIsoDate(nextSemesterStart),
      endDate: toIsoDate(nextSemesterEnd),
      status: "planning",
    });
  }

  // Current year
  cycles.push({
    id: `ano-${year}`,
    name: `Ano ${year}`,
    type: "annual",
    startDate: toIsoDate(startOfYear(now)),
    endDate: toIsoDate(endOfYear(now)),
    status: "active",
  });

  return cycles;
}

/**
 * Get the current active quarter cycle ID
 */
export function getCurrentQuarterId(): string {
  const now = today();
  return `q${getQuarter(now)}-${now.getFullYear()}`;
}

/**
 * Get the current year cycle ID
 */
export function getCurrentYearId(): string {
  return `ano-${today().getFullYear()}`;
}

// ─── Check-in Date Generation ───

export interface CheckInDateSet {
  /** Most recent check-in (this week) */
  recent: Date;
  /** Previous check-in (last week) */
  previous: Date;
  /** Older check-in (2 weeks ago) */
  older: Date;
  /** Oldest check-in (3 weeks ago) */
  oldest: Date;
  /** Very old check-in (4 weeks ago) */
  veryOld: Date;
}

/**
 * Generate check-in dates relative to current date
 * All check-ins fall on weekdays (Mon-Fri)
 */
export function generateCheckInDates(): CheckInDateSet {
  const now = today();
  const weekStart = startOfWeek(now);
  
  // Helper to get a random weekday (Mon-Fri) in a given week
  const weekdayInWeek = (weekMonday: Date, preferredDay: number = 3): Date => {
    // preferredDay: 0=Mon, 1=Tue, ..., 4=Fri
    const day = Math.min(Math.max(preferredDay, 0), 4);
    return addDays(weekMonday, day);
  };

  return {
    recent: weekdayInWeek(weekStart, 2), // Wednesday this week
    previous: weekdayInWeek(addWeeks(weekStart, -1), 3), // Thursday last week
    older: weekdayInWeek(addWeeks(weekStart, -2), 2), // Wednesday 2 weeks ago
    oldest: weekdayInWeek(addWeeks(weekStart, -3), 4), // Friday 3 weeks ago
    veryOld: weekdayInWeek(addWeeks(weekStart, -4), 1), // Tuesday 4 weeks ago
  };
}

// ─── Survey Date Generation ───

export interface SurveyDateSet {
  /** Active survey ending soon (this week) */
  endingSoon: { start: Date; end: Date };
  /** Active survey mid-cycle */
  activeMid: { start: Date; end: Date };
  /** Recently closed survey */
  recentlyClosed: { start: Date; end: Date };
  /** Scheduled survey (starts next week) */
  scheduled: { start: Date; end: Date };
  /** Archived survey (last month) */
  archived: { start: Date; end: Date };
}

/**
 * Generate survey dates relative to current date
 */
export function generateSurveyDates(): SurveyDateSet {
  const now = today();
  const weekStart = startOfWeek(now);

  return {
    endingSoon: {
      start: addWeeks(weekStart, -2),
      end: addDays(weekStart, 4), // Friday this week
    },
    activeMid: {
      start: addWeeks(weekStart, -1),
      end: addWeeks(weekStart, 2),
    },
    recentlyClosed: {
      start: addWeeks(weekStart, -4),
      end: addWeeks(weekStart, -2),
    },
    scheduled: {
      start: addWeeks(weekStart, 1),
      end: addWeeks(weekStart, 3),
    },
    archived: {
      start: addMonths(now, -2),
      end: addMonths(now, -1),
    },
  };
}

// ─── Progress Simulation ───

/**
 * Calculate simulated progress based on time and a "performance factor"
 * @param startDate Start of the period
 * @param endDate End of the period
 * @param performanceFactor 0.5 = behind, 1.0 = on track, 1.2 = ahead
 * @param variance Random variance to add (0-1)
 */
export function simulateProgress(
  startDate: Date,
  endDate: Date,
  performanceFactor: number = 1.0,
  variance: number = 0.1,
): number {
  const expected = expectedProgress(startDate, endDate);
  const factor = performanceFactor + (variance * 2 - variance); // Simple deterministic adjustment
  const progress = Math.round(expected * factor);
  return Math.max(0, Math.min(100, progress));
}

/**
 * Determine status based on progress vs expected
 */
export function determineStatus(
  progress: number,
  expected: number,
): "on_track" | "attention" | "off_track" | "completed" {
  if (progress >= 100) return "completed";
  if (progress >= expected - 5) return "on_track";
  if (progress >= expected - 20) return "attention";
  return "off_track";
}

// ─── ID Utilities ───

/**
 * Generate a deterministic ID from components
 */
export function makeId(...parts: (string | number)[]): string {
  return parts.map(String).join("-");
}

/**
 * Hash a string to a number (for deterministic "random" selection)
 */
export function hashString(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = ((hash << 5) - hash) + char;
    hash = hash & hash;
  }
  return Math.abs(hash);
}

/**
 * Pick an item from array using deterministic "random" based on seed
 */
export function pickFromArray<T>(array: T[], seed: string): T {
  const index = hashString(seed) % array.length;
  return array[index]!;
}

// ─── Deterministic Check-in Generation ───

export interface GeneratedCheckIn {
  date: Date;
  dateIso: string;
  weekday: number; // 0=Sun, 1=Mon, ..., 6=Sat
  weekOfCycle: number;
}

/**
 * Generate deterministic check-in dates for a given KR
 * Uses the KR ID as seed to ensure same dates on every access
 * 
 * @param krId The Key Result ID (used as seed for determinism)
 * @param count Number of check-ins to generate (default 4)
 * @param maxWeeksBack How far back to go (default 4 weeks)
 */
export function generateCheckInsForKR(
  krId: string,
  count: number = 4,
  maxWeeksBack: number = 4,
): GeneratedCheckIn[] {
  const now = today();
  const weekStart = startOfWeek(now);
  const hash = hashString(krId);
  
  const checkIns: GeneratedCheckIn[] = [];
  
  for (let i = 0; i < count; i++) {
    // Deterministic week offset based on hash + index
    const weekOffset = -((hash + i * 7) % maxWeeksBack);
    const targetWeekStart = addWeeks(weekStart, weekOffset);
    
    // Deterministic weekday (Mon-Fri) based on hash + index
    const weekdayOffset = ((hash + i * 3) % 5); // 0-4 = Mon-Fri
    const checkInDate = addDays(targetWeekStart, weekdayOffset);
    
    // Skip if in the future
    if (checkInDate > now) continue;
    
    checkIns.push({
      date: checkInDate,
      dateIso: toIsoDateTime(checkInDate),
      weekday: checkInDate.getDay(),
      weekOfCycle: Math.abs(weekOffset),
    });
  }
  
  // Sort by date descending (most recent first)
  checkIns.sort((a, b) => b.date.getTime() - a.date.getTime());
  
  return checkIns;
}

/**
 * Generate a deterministic progress value based on seed
 * Ensures consistent values across page loads
 */
export function deterministicProgress(seed: string, min: number = 20, max: number = 95): number {
  const hash = hashString(seed);
  const range = max - min;
  return min + (hash % range);
}

/**
 * Generate deterministic "created at" date for an entity
 * Based on cycle start + deterministic offset
 */
export function deterministicCreatedAt(entityId: string, cycleStart: Date): string {
  const hash = hashString(entityId);
  const offsetDays = hash % 14; // Within first 2 weeks of cycle
  return toIsoDateTime(addDays(cycleStart, offsetDays));
}

/**
 * Generate deterministic "updated at" date (recent, within last 2 weeks)
 */
export function deterministicUpdatedAt(entityId: string): string {
  const now = today();
  const hash = hashString(entityId);
  const daysAgo = hash % 14; // Within last 2 weeks
  return toIsoDateTime(addDays(now, -daysAgo));
}

/**
 * Get current cycle info for seed data generation
 */
export function getCurrentCycleInfo(): {
  quarterId: string;
  yearId: string;
  quarterStart: Date;
  quarterEnd: Date;
  yearStart: Date;
  yearEnd: Date;
} {
  const now = today();
  return {
    quarterId: getCurrentQuarterId(),
    yearId: getCurrentYearId(),
    quarterStart: startOfQuarter(now),
    quarterEnd: endOfQuarter(now),
    yearStart: startOfYear(now),
    yearEnd: endOfYear(now),
  };
}
