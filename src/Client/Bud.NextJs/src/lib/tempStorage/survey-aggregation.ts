// ─── Survey Aggregation ──────────────────────────────────────────────────────
// Funções puras para agregar respostas de pesquisa por tipo de pergunta.
// Não depende de React — pode ser usada em tests unitários diretamente.

import type { WizardQuestion } from "@/types/survey";
import type { SurveySubmissionRecord } from "@/lib/surveys-store";

// ── Tipos de resultado agregado ──────────────────────────────────────────────

export interface AggregatedLikert {
  type: "likert";
  avg: number;
  scaleMin: number;
  scaleMax: number;
  distribution: number[]; // índice 0 = scaleMin, índice N = scaleMax
  count: number;
}

export interface AggregatedNps {
  type: "nps";
  score: number; // -100 a 100
  promoters: number; // 0-1
  passives: number; // 0-1
  detractors: number; // 0-1
  count: number;
}

export interface AggregatedRating {
  type: "rating";
  avg: number;
  max: number;
  distribution: number[]; // índice 0 = 1 estrela, índice N = max estrelas
  count: number;
}

export interface AggregatedDistribution {
  type: "multiple_choice" | "checkbox" | "dropdown";
  distribution: Record<string, { label: string; count: number; percent: number }>;
  count: number;
}

export interface AggregatedYesNo {
  type: "yes_no";
  yesCount: number;
  noCount: number;
  yesPercent: number;
  count: number;
}

export interface AggregatedRanking {
  type: "ranking";
  avgRanks: Record<string, { label: string; avgRank: number }>;
  count: number;
}

export interface AggregatedText {
  type: "text_short" | "text_long";
  responses: string[];
  count: number;
}

export interface AggregatedDate {
  type: "date";
  values: string[];
  count: number;
}

export type AggregatedQuestion =
  | AggregatedLikert
  | AggregatedNps
  | AggregatedRating
  | AggregatedDistribution
  | AggregatedYesNo
  | AggregatedRanking
  | AggregatedText
  | AggregatedDate;

// ── Funções de agregação por tipo ─────────────────────────────────────────────

function aggregateLikert(question: WizardQuestion, answers: unknown[]): AggregatedLikert {
  const min = question.scaleMin ?? 1;
  const max = question.scaleMax ?? 5;
  const range = max - min + 1;
  const distribution = Array(range).fill(0) as number[];
  let total = 0;
  let count = 0;

  for (const raw of answers) {
    const val = typeof raw === "number" ? raw : Number(raw);
    if (!Number.isFinite(val) || val < min || val > max) continue;
    distribution[val - min] = (distribution[val - min] ?? 0) + 1;
    total += val;
    count++;
  }

  return {
    type: "likert",
    avg: count > 0 ? total / count : 0,
    scaleMin: min,
    scaleMax: max,
    distribution,
    count,
  };
}

function aggregateNps(answers: unknown[]): AggregatedNps {
  let promoters = 0;
  let passives = 0;
  let detractors = 0;
  let count = 0;

  for (const raw of answers) {
    const val = typeof raw === "number" ? raw : Number(raw);
    if (!Number.isFinite(val) || val < 0 || val > 10) continue;
    count++;
    if (val >= 9) promoters++;
    else if (val >= 7) passives++;
    else detractors++;
  }

  return {
    type: "nps",
    score: count > 0 ? Math.round(((promoters - detractors) / count) * 100) : 0,
    promoters: count > 0 ? promoters / count : 0,
    passives: count > 0 ? passives / count : 0,
    detractors: count > 0 ? detractors / count : 0,
    count,
  };
}

function aggregateRating(question: WizardQuestion, answers: unknown[]): AggregatedRating {
  const max = question.ratingMax ?? 5;
  const distribution = Array(max).fill(0) as number[];
  let total = 0;
  let count = 0;

  for (const raw of answers) {
    const val = typeof raw === "number" ? raw : Number(raw);
    if (!Number.isFinite(val) || val < 1 || val > max) continue;
    distribution[val - 1] = (distribution[val - 1] ?? 0) + 1;
    total += val;
    count++;
  }

  return {
    type: "rating",
    avg: count > 0 ? total / count : 0,
    max,
    distribution,
    count,
  };
}

function aggregateDistribution(
  question: WizardQuestion,
  type: "multiple_choice" | "checkbox" | "dropdown",
  answers: unknown[],
): AggregatedDistribution {
  const optionMap: Record<string, string> = {};
  for (const opt of question.options ?? []) {
    optionMap[opt.id] = opt.label;
  }

  const counts: Record<string, number> = {};
  let count = 0;

  for (const raw of answers) {
    const ids = Array.isArray(raw) ? raw : [raw];
    for (const id of ids) {
      if (typeof id !== "string" || !optionMap[id]) continue;
      counts[id] = (counts[id] ?? 0) + 1;
    }
    count++;
  }

  const total = Object.values(counts).reduce((s, n) => s + n, 0);
  const distribution: AggregatedDistribution["distribution"] = {};
  for (const [id, cnt] of Object.entries(counts)) {
    distribution[id] = {
      label: optionMap[id] ?? id,
      count: cnt,
      percent: total > 0 ? cnt / total : 0,
    };
  }

  return { type, distribution, count };
}

function aggregateYesNo(answers: unknown[]): AggregatedYesNo {
  let yesCount = 0;
  let noCount = 0;

  for (const raw of answers) {
    if (raw === true || raw === "yes" || raw === 1) yesCount++;
    else if (raw === false || raw === "no" || raw === 0) noCount++;
  }

  const count = yesCount + noCount;
  return {
    type: "yes_no",
    yesCount,
    noCount,
    yesPercent: count > 0 ? yesCount / count : 0,
    count,
  };
}

function aggregateRanking(question: WizardQuestion, answers: unknown[]): AggregatedRanking {
  const optionMap: Record<string, string> = {};
  for (const opt of question.options ?? []) {
    optionMap[opt.id] = opt.label;
  }

  const rankSums: Record<string, number> = {};
  const rankCounts: Record<string, number> = {};
  let count = 0;

  for (const raw of answers) {
    if (!Array.isArray(raw)) continue;
    raw.forEach((id, index) => {
      if (typeof id !== "string") return;
      rankSums[id] = (rankSums[id] ?? 0) + (index + 1);
      rankCounts[id] = (rankCounts[id] ?? 0) + 1;
    });
    count++;
  }

  const avgRanks: AggregatedRanking["avgRanks"] = {};
  for (const id of Object.keys(optionMap)) {
    avgRanks[id] = {
      label: optionMap[id] ?? id,
      avgRank: rankCounts[id] && rankSums[id] !== undefined ? rankSums[id] / rankCounts[id] : Infinity,
    };
  }

  return { type: "ranking", avgRanks, count };
}

// ── Função principal ──────────────────────────────────────────────────────────

/**
 * Agrega as respostas brutas para uma única pergunta.
 */
export function aggregateQuestion(
  question: WizardQuestion,
  rawAnswers: unknown[],
): AggregatedQuestion {
  switch (question.type) {
    case "likert":
      return aggregateLikert(question, rawAnswers);
    case "nps":
      return aggregateNps(rawAnswers);
    case "rating":
      return aggregateRating(question, rawAnswers);
    case "multiple_choice":
    case "dropdown":
      return aggregateDistribution(question, question.type, rawAnswers);
    case "checkbox":
      return aggregateDistribution(question, "checkbox", rawAnswers);
    case "yes_no":
      return aggregateYesNo(rawAnswers);
    case "ranking":
      return aggregateRanking(question, rawAnswers);
    case "text_short":
    case "text_long": {
      const responses = rawAnswers
        .filter((r): r is string => typeof r === "string" && r.trim().length > 0);
      return { type: question.type, responses, count: responses.length };
    }
    case "date": {
      const values = rawAnswers.filter((r): r is string => typeof r === "string");
      return { type: "date", values, count: values.length };
    }
    default:
      return { type: "text_short", responses: [], count: 0 };
  }
}

/**
 * Agrega todas as submissions de uma pesquisa, retornando um Map
 * questionId → AggregatedQuestion.
 */
export function aggregateSurveySubmissions(
  questions: WizardQuestion[],
  submissions: SurveySubmissionRecord[],
): Map<string, AggregatedQuestion> {
  const result = new Map<string, AggregatedQuestion>();

  for (const question of questions) {
    const rawAnswers = submissions
      .map((s) => s.answers[question.id])
      .filter((a) => a !== undefined && a !== null && a !== "");

    result.set(question.id, aggregateQuestion(question, rawAnswers));
  }

  return result;
}

/**
 * Calcula um score geral de 0–100 para uma pesquisa a partir das agregações.
 * Usa apenas perguntas com escala numérica natural (likert, nps, rating, yes_no).
 * Retorna null se não houver perguntas pontuáveis.
 */
export function calculateSurveyOverallScore(
  aggregations: Map<string, AggregatedQuestion>,
  questions: WizardQuestion[],
): number | null {
  const scores: number[] = [];

  for (const question of questions) {
    const agg = aggregations.get(question.id);
    if (!agg || agg.count === 0) continue;

    if (agg.type === "likert") {
      const range = agg.scaleMax - agg.scaleMin;
      if (range > 0) {
        scores.push(((agg.avg - agg.scaleMin) / range) * 100);
      }
    } else if (agg.type === "rating") {
      if (agg.max > 0) {
        scores.push((agg.avg / agg.max) * 100);
      }
    } else if (agg.type === "nps") {
      // NPS vai de -100 a 100; normalizar para 0-100
      scores.push((agg.score + 100) / 2);
    } else if (agg.type === "yes_no") {
      scores.push(agg.yesPercent * 100);
    }
  }

  if (scores.length === 0) return null;
  return Math.round(scores.reduce((s, n) => s + n, 0) / scores.length);
}

/**
 * Calcula a tendência comparando dois scores (positivo = melhora, negativo = piora).
 */
export function calculateScoreTrend(
  current: number | null,
  previous: number | null,
): "up" | "down" | "stable" | null {
  if (current === null || previous === null) return null;
  const diff = current - previous;
  if (diff > 2) return "up";
  if (diff < -2) return "down";
  return "stable";
}
