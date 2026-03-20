import { describe, it, expect } from "vitest";
import {
  aggregateQuestion,
  aggregateSurveySubmissions,
  calculateSurveyOverallScore,
  calculateScoreTrend,
} from "./survey-aggregation";
import type { WizardQuestion } from "@/types/survey";
import type { SurveySubmissionRecord } from "./surveys-store";

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeQuestion(overrides: Partial<WizardQuestion> = {}): WizardQuestion {
  return {
    id: "q1",
    sectionId: null,
    type: "text_short",
    text: "Pergunta",
    isRequired: false,
    ...overrides,
  };
}

function makeSubmission(answers: Record<string, unknown>): SurveySubmissionRecord {
  return {
    id: `sub-${Math.random()}`,
    surveyId: "survey-1",
    respondentKey: "device-x",
    answers,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    submittedAt: new Date().toISOString(),
  };
}

// ── aggregateQuestion ─────────────────────────────────────────────────────────

describe("aggregateQuestion — likert", () => {
  const q = makeQuestion({ type: "likert", scaleMin: 1, scaleMax: 5 });

  it("calcula média corretamente", () => {
    const result = aggregateQuestion(q, [3, 4, 5]);
    expect(result.type).toBe("likert");
    if (result.type === "likert") {
      expect(result.avg).toBeCloseTo(4);
      expect(result.count).toBe(3);
    }
  });

  it("ignora valores fora da escala", () => {
    const result = aggregateQuestion(q, [0, 6, 3]);
    if (result.type === "likert") {
      expect(result.count).toBe(1);
      expect(result.avg).toBe(3);
    }
  });

  it("retorna count=0 para respostas vazias", () => {
    const result = aggregateQuestion(q, []);
    if (result.type === "likert") {
      expect(result.count).toBe(0);
      expect(result.avg).toBe(0);
    }
  });

  it("distribui votos corretamente", () => {
    const result = aggregateQuestion(q, [1, 1, 3, 5]);
    if (result.type === "likert") {
      expect(result.distribution[0]).toBe(2); // índice 0 = valor 1
      expect(result.distribution[2]).toBe(1); // índice 2 = valor 3
      expect(result.distribution[4]).toBe(1); // índice 4 = valor 5
    }
  });

  it("usa scaleMin/scaleMax customizados", () => {
    const qCustom = makeQuestion({ type: "likert", scaleMin: 0, scaleMax: 10 });
    const result = aggregateQuestion(qCustom, [0, 5, 10]);
    if (result.type === "likert") {
      expect(result.avg).toBeCloseTo(5);
      expect(result.scaleMin).toBe(0);
      expect(result.scaleMax).toBe(10);
    }
  });
});

describe("aggregateQuestion — nps", () => {
  const q = makeQuestion({ type: "nps" });

  it("calcula NPS com promotores e detratores", () => {
    // 2 promotores (9, 10), 1 passivo (8), 2 detratores (3, 5)
    const result = aggregateQuestion(q, [9, 10, 8, 3, 5]);
    expect(result.type).toBe("nps");
    if (result.type === "nps") {
      expect(result.count).toBe(5);
      expect(result.score).toBe(0); // (2-2)/5 * 100 = 0
      expect(result.promoters).toBeCloseTo(0.4);
      expect(result.detractors).toBeCloseTo(0.4);
      expect(result.passives).toBeCloseTo(0.2);
    }
  });

  it("NPS positivo quando maioria são promotores", () => {
    const result = aggregateQuestion(q, [10, 10, 10, 7, 2]);
    if (result.type === "nps") {
      expect(result.score).toBeGreaterThan(0);
    }
  });

  it("ignora valores fora de 0-10", () => {
    const result = aggregateQuestion(q, [-1, 11, 9]);
    if (result.type === "nps") {
      expect(result.count).toBe(1);
    }
  });
});

describe("aggregateQuestion — rating", () => {
  const q = makeQuestion({ type: "rating", ratingMax: 5 });

  it("calcula média de estrelas", () => {
    const result = aggregateQuestion(q, [4, 5, 3]);
    expect(result.type).toBe("rating");
    if (result.type === "rating") {
      expect(result.avg).toBeCloseTo(4);
      expect(result.max).toBe(5);
    }
  });

  it("distribui votos por estrela", () => {
    const result = aggregateQuestion(q, [1, 1, 5]);
    if (result.type === "rating") {
      expect(result.distribution[0]).toBe(2); // 1 estrela
      expect(result.distribution[4]).toBe(1); // 5 estrelas
    }
  });
});

describe("aggregateQuestion — multiple_choice", () => {
  const q = makeQuestion({
    type: "multiple_choice",
    options: [
      { id: "a", label: "Opção A" },
      { id: "b", label: "Opção B" },
    ],
  });

  it("conta distribuição de opções", () => {
    const result = aggregateQuestion(q, ["a", "a", "b"]);
    expect(result.type).toBe("multiple_choice");
    if (result.type === "multiple_choice") {
      expect(result.distribution["a"]?.count).toBe(2);
      expect(result.distribution["b"]?.count).toBe(1);
      expect(result.distribution["a"]?.percent).toBeCloseTo(2 / 3);
    }
  });

  it("ignora opções inválidas", () => {
    const result = aggregateQuestion(q, ["a", "invalid"]);
    if (result.type === "multiple_choice") {
      expect(result.distribution["a"]?.count).toBe(1);
      expect(result.distribution["invalid"]).toBeUndefined();
    }
  });
});

describe("aggregateQuestion — yes_no", () => {
  const q = makeQuestion({ type: "yes_no" });

  it("conta sim e não", () => {
    const result = aggregateQuestion(q, [true, true, false]);
    expect(result.type).toBe("yes_no");
    if (result.type === "yes_no") {
      expect(result.yesCount).toBe(2);
      expect(result.noCount).toBe(1);
      expect(result.yesPercent).toBeCloseTo(2 / 3);
    }
  });

  it("aceita string 'yes'/'no'", () => {
    const result = aggregateQuestion(q, ["yes", "no", "yes"]);
    if (result.type === "yes_no") {
      expect(result.yesCount).toBe(2);
      expect(result.noCount).toBe(1);
    }
  });
});

describe("aggregateQuestion — checkbox", () => {
  const q = makeQuestion({
    type: "checkbox",
    options: [
      { id: "x", label: "X" },
      { id: "y", label: "Y" },
    ],
  });

  it("conta múltiplas seleções por resposta", () => {
    const result = aggregateQuestion(q, [["x", "y"], ["x"]]);
    expect(result.type).toBe("checkbox");
    if (result.type === "checkbox") {
      expect(result.distribution["x"]?.count).toBe(2);
      expect(result.distribution["y"]?.count).toBe(1);
    }
  });
});

describe("aggregateQuestion — ranking", () => {
  const q = makeQuestion({
    type: "ranking",
    options: [
      { id: "a", label: "A" },
      { id: "b", label: "B" },
    ],
  });

  it("calcula rank médio", () => {
    // Respondente 1: a=1, b=2; Respondente 2: b=1, a=2
    const result = aggregateQuestion(q, [["a", "b"], ["b", "a"]]);
    expect(result.type).toBe("ranking");
    if (result.type === "ranking") {
      expect(result.avgRanks["a"]?.avgRank).toBeCloseTo(1.5);
      expect(result.avgRanks["b"]?.avgRank).toBeCloseTo(1.5);
    }
  });
});

describe("aggregateQuestion — text_short", () => {
  const q = makeQuestion({ type: "text_short" });

  it("coleta respostas de texto", () => {
    const result = aggregateQuestion(q, ["Ótimo", "Bom", ""]);
    expect(result.type).toBe("text_short");
    if (result.type === "text_short") {
      expect(result.responses).toHaveLength(2); // ignora vazio
      expect(result.count).toBe(2);
    }
  });
});

// ── aggregateSurveySubmissions ─────────────────────────────────────────────────

describe("aggregateSurveySubmissions", () => {
  it("agrega múltiplas perguntas de múltiplas submissions", () => {
    const questions: WizardQuestion[] = [
      makeQuestion({ id: "q-likert", type: "likert", scaleMin: 1, scaleMax: 5 }),
      makeQuestion({ id: "q-nps", type: "nps" }),
    ];

    const submissions = [
      makeSubmission({ "q-likert": 4, "q-nps": 9 }),
      makeSubmission({ "q-likert": 5, "q-nps": 10 }),
    ];

    const result = aggregateSurveySubmissions(questions, submissions);

    expect(result.size).toBe(2);
    const likert = result.get("q-likert");
    expect(likert?.type).toBe("likert");
    if (likert?.type === "likert") {
      expect(likert.avg).toBeCloseTo(4.5);
      expect(likert.count).toBe(2);
    }
  });

  it("retorna Map vazio para lista vazia de submissions", () => {
    const questions: WizardQuestion[] = [makeQuestion({ type: "likert" })];
    const result = aggregateSurveySubmissions(questions, []);
    const agg = result.get("q1");
    if (agg?.type === "likert") {
      expect(agg.count).toBe(0);
    }
  });
});

// ── calculateSurveyOverallScore ────────────────────────────────────────────────

describe("calculateSurveyOverallScore", () => {
  it("normaliza likert 1-5 com avg=4 para 75%", () => {
    const q = makeQuestion({ type: "likert", scaleMin: 1, scaleMax: 5 });
    const agg = aggregateQuestion(q, [4, 4]);
    const map = new Map([["q1", agg]]);
    const score = calculateSurveyOverallScore(map, [q]);
    expect(score).toBe(75); // (4-1)/(5-1)*100 = 75
  });

  it("normaliza rating 1-5 com avg=5 para 100%", () => {
    const q = makeQuestion({ type: "rating", ratingMax: 5 });
    const agg = aggregateQuestion(q, [5, 5]);
    const map = new Map([["q1", agg]]);
    const score = calculateSurveyOverallScore(map, [q]);
    expect(score).toBe(100);
  });

  it("normaliza NPS de -100..100 para 0..100", () => {
    const q = makeQuestion({ type: "nps" });
    const agg = aggregateQuestion(q, [10, 10, 10]); // NPS = 100
    const map = new Map([["q1", agg]]);
    const score = calculateSurveyOverallScore(map, [q]);
    expect(score).toBe(100); // (100+100)/2
  });

  it("yes_no com 100% sim retorna 100", () => {
    const q = makeQuestion({ type: "yes_no" });
    const agg = aggregateQuestion(q, [true, true]);
    const map = new Map([["q1", agg]]);
    const score = calculateSurveyOverallScore(map, [q]);
    expect(score).toBe(100);
  });

  it("retorna null se não há perguntas pontuáveis", () => {
    const q = makeQuestion({ type: "text_short" });
    const agg = aggregateQuestion(q, ["texto"]);
    const map = new Map([["q1", agg]]);
    const score = calculateSurveyOverallScore(map, [q]);
    expect(score).toBeNull();
  });

  it("calcula média entre múltiplas perguntas pontuáveis", () => {
    const q1 = makeQuestion({ id: "q1", type: "yes_no" });
    const q2 = makeQuestion({ id: "q2", type: "yes_no" });
    const agg1 = aggregateQuestion(q1, [true, true]); // 100%
    const agg2 = aggregateQuestion(q2, [false, false]); // 0%
    const map = new Map([["q1", agg1], ["q2", agg2]]);
    const score = calculateSurveyOverallScore(map, [q1, q2]);
    expect(score).toBe(50);
  });
});

// ── calculateScoreTrend ───────────────────────────────────────────────────────

describe("calculateScoreTrend", () => {
  it("retorna 'up' quando score aumentou mais de 2", () => {
    expect(calculateScoreTrend(80, 70)).toBe("up");
  });

  it("retorna 'down' quando score caiu mais de 2", () => {
    expect(calculateScoreTrend(70, 80)).toBe("down");
  });

  it("retorna 'stable' para variação <= 2", () => {
    expect(calculateScoreTrend(72, 70)).toBe("stable");
    expect(calculateScoreTrend(70, 72)).toBe("stable");
  });

  it("retorna null se algum valor é null", () => {
    expect(calculateScoreTrend(null, 70)).toBeNull();
    expect(calculateScoreTrend(70, null)).toBeNull();
    expect(calculateScoreTrend(null, null)).toBeNull();
  });
});
