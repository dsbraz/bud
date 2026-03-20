import type {
  SurveyCategory,
  SurveyType,
  SurveyWizardState,
  WizardQuestion,
  WizardSection,
} from "@/types/survey/survey";
import { formatDateBR, todayIso } from "@/lib/tempStorage/date-format";
import type {
  SurveyResultData,
  QuestionResult,
  QuestionResultData,
  IndividualResponse,
  ResultSection,
  ChoiceData,
  LikertData,
  NpsData,
  RankingData,
  TextData,
  YesNoData,
  HrReviewData,
  CalibrationData,
  CalibrationParticipant,
  HeatmapEntry,
  AttentionPoint,
  TrendPoint,
  ActionItem,
} from "@/presentation/surveys/results/types";
import type { SurveyRendererData } from "@/presentation/surveys/components/SurveyRenderer";
import type {
  SurveyListItemData,
  SurveyLocalRecord,
  SurveySubmissionRecord,
  SurveyTemplateRecord,
} from "@/lib/tempStorage/surveys-store";
import { initialWizardState } from "@/presentation/surveys/create/wizardReducer";
import { getTemplateByType } from "@/presentation/surveys/templates/surveyTemplates";

const SAMPLE_NAMES = [
  { name: "Ana Ferreira", initials: "AF", department: "Engenharia" },
  { name: "Joao Martins", initials: "JM", department: "Produto" },
  { name: "Beatriz Ramos", initials: "BR", department: "Design" },
  { name: "Lucas Oliveira", initials: "LO", department: "Marketing" },
  { name: "Carla Santos", initials: "CS", department: "People" },
];

/** Departments used for HR review heatmaps and calibration */
const HR_DEPARTMENTS = [
  "Engenharia",
  "Produto",
  "Design",
  "Marketing",
  "Vendas",
  "People",
];

/** People for calibration mock data - realistic Brazilian names */
const CALIBRATION_PEOPLE = [
  { name: "Ana Carolina Silva", initials: "AC" },
  { name: "Bruno Santos", initials: "BS" },
  { name: "Camila Oliveira", initials: "CO" },
  { name: "Daniel Ferreira", initials: "DF" },
  { name: "Elena Costa", initials: "EC" },
  { name: "Felipe Almeida", initials: "FA" },
  { name: "Gabriela Lima", initials: "GL" },
  { name: "Henrique Souza", initials: "HS" },
  { name: "Isabela Rodrigues", initials: "IR" },
  { name: "Joao Pedro Martins", initials: "JP" },
  { name: "Karla Mendes", initials: "KM" },
  { name: "Lucas Barbosa", initials: "LB" },
  { name: "Mariana Nascimento", initials: "MN" },
  { name: "Natalia Pereira", initials: "NP" },
  { name: "Otavio Ribeiro", initials: "OR" },
  { name: "Patricia Campos", initials: "PC" },
  { name: "Rafael Teixeira", initials: "RT" },
  { name: "Sofia Cardoso", initials: "SC" },
];

/** Bias alerts that may appear during calibration - undefined means no alert */
const BIAS_ALERTS: (string | undefined)[] = [
  undefined,
  undefined,
  undefined,
  "Efeito halo detectado",
  "Vies de leniencia",
  "Vies de recencia",
  undefined,
  "Gap significativo auto vs. gestor",
];

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") return structuredClone(value);
  return JSON.parse(JSON.stringify(value)) as T;
}

function parseBrDateToIso(value: string): string | null {
  if (!value) return null;
  const [day, month, year] = value.split("/").map((part) => Number(part));
  if (!day || !month || !year) return null;
  const date = new Date(year, month - 1, day);
  return date.toISOString();
}

function questionIdsFromTemplate(
  type: SurveyType,
  templateRecord?: SurveyTemplateRecord | null,
): { sections: WizardSection[]; questions: WizardQuestion[] } {
  if (
    templateRecord &&
    templateRecord.sections.length > 0 &&
    templateRecord.questions.length > 0
  ) {
    return {
      sections: cloneDeep(templateRecord.sections),
      questions: cloneDeep(templateRecord.questions),
    };
  }

  const template = getTemplateByType(type);
  if (!template) {
    return { sections: [], questions: [] };
  }

  const sections: WizardSection[] = [];
  const questions: WizardQuestion[] = [];

  template.sections.forEach((section, sectionIndex) => {
    const sectionId = `section-${type}-${sectionIndex + 1}`;
    sections.push({
      id: sectionId,
      title: section.title,
      description: section.description,
    });

    section.questions.forEach((question, questionIndex) => {
      const questionId = `question-${type}-${sectionIndex + 1}-${questionIndex + 1}`;
      questions.push({
        id: questionId,
        sectionId,
        type: question.type,
        text: question.text,
        isRequired: question.isRequired,
        options: question.options,
        scaleMin: question.scaleMin,
        scaleMax: question.scaleMax,
        scaleLabels: question.scaleLabels,
        ratingMax: question.ratingMax,
      });
    });
  });

  return {
    sections,
    questions,
  };
}

export function estimateParticipantsFromWizard(
  state: SurveyWizardState,
): number {
  let total = 0;
  if (state.scope.scopeType === "company") total = 150;
  else if (state.scope.scopeType === "team")
    total = state.scope.teamIds.length * 12;
  else total = state.scope.userIds.length;
  return Math.max(0, total - state.excludedUserIds.length);
}

export function wizardStateToSurveyListItem(
  state: SurveyWizardState,
  options: {
    surveyId: string;
    templateId: string | null;
    status: SurveyListItemData["status"];
    createdAt: string;
    totalResponses?: number;
    fallbackType?: SurveyType;
    fallbackCategory?: SurveyCategory;
  },
): SurveyListItemData {
  const totalRecipients = estimateParticipantsFromWizard(state);
  const totalResponses = options.totalResponses ?? 0;
  const completionRate =
    totalRecipients > 0
      ? Math.min(100, Math.round((totalResponses / totalRecipients) * 100))
      : 0;

  return {
    id: options.surveyId,
    templateId: options.templateId,
    name: state.name.trim() || "Pesquisa sem titulo",
    type: state.type ?? options.fallbackType ?? "custom",
    category: state.category ?? options.fallbackCategory ?? "pesquisa",
    status: options.status,
    startDate: state.startDate ?? "",
    endDate: state.endDate ?? "",
    ownerIds: Array.from(new Set(state.ownerIds)),
    managerIds: Array.from(new Set(state.managerIds)),
    tagIds: Array.from(new Set(state.tagIds)),
    cycleId: state.cycleId,
    totalRecipients,
    totalResponses,
    completionRate,
    createdAt: options.createdAt,
  };
}

export function createWizardStateFromListItem(
  item: SurveyListItemData,
  templateRecord?: SurveyTemplateRecord | null,
): SurveyWizardState {
  const base = cloneDeep(initialWizardState);
  const template = getTemplateByType(item.type);
  const mapped = questionIdsFromTemplate(item.type, templateRecord);
  const defaultConfig: {
    isAnonymous: boolean;
    recurrence?: string | null;
    aiPrefillOkrs?: boolean;
    aiPrefillFeedback?: boolean;
    aiBiasDetection?: boolean;
  } = templateRecord?.defaultConfig ??
    template?.defaultConfig ?? { isAnonymous: true };

  return {
    ...base,
    step: 1,
    type: item.type,
    category: item.category,
    name: item.name,
    description: templateRecord?.subtitle ?? template?.subtitle ?? "",
    ownerIds: Array.from(new Set(item.ownerIds ?? [])),
    managerIds: Array.from(new Set(item.managerIds ?? [])),
    tagIds: Array.from(new Set(item.tagIds ?? [])),
    cycleId: item.cycleId ?? null,
    sections: mapped.sections,
    questions: mapped.questions,
    isAnonymous: defaultConfig.isAnonymous ?? true,
    recurrence:
      (defaultConfig.recurrence as SurveyWizardState["recurrence"]) ?? null,
    aiPrefillOkrs: defaultConfig.aiPrefillOkrs ?? false,
    aiPrefillFeedback: defaultConfig.aiPrefillFeedback ?? false,
    aiBiasDetection: defaultConfig.aiBiasDetection ?? false,
    perspectives: item.category === "ciclo" ? base.perspectives : [],
    startDate: parseBrDateToIso(item.startDate),
    endDate: parseBrDateToIso(item.endDate),
  };
}

export function wizardStateToRendererData(
  state: SurveyWizardState,
): SurveyRendererData {
  return {
    name: state.name || "Pesquisa sem titulo",
    description: state.description || undefined,
    isAnonymous: state.isAnonymous,
    sections: state.sections,
    questions: state.questions,
  };
}

function distribute(total: number, buckets: number): number[] {
  if (buckets <= 0) return [];
  if (total <= 0) return Array.from({ length: buckets }, () => 0);

  const base = Math.floor(total / buckets);
  const remainder = total % buckets;
  return Array.from(
    { length: buckets },
    (_, index) => base + (index < remainder ? 1 : 0),
  );
}

function buildIndividualResponses(
  question: WizardQuestion,
  totalResponses: number,
): IndividualResponse[] {
  const total = Math.min(totalResponses, SAMPLE_NAMES.length);
  if (total === 0) return [];

  return Array.from({ length: total }, (_, index) => {
    const person =
      SAMPLE_NAMES[index % SAMPLE_NAMES.length] ?? SAMPLE_NAMES[0]!;
    const base: IndividualResponse = {
      id: `${question.id}-resp-${index + 1}`,
      name: person.name,
      initials: person.initials,
      department: person.department,
      answeredAt: todayIso(),
    };

    if (question.type === "likert" || question.type === "rating") {
      return {
        ...base,
        numericValue: Math.min(question.scaleMax ?? 5, 3 + (index % 3)),
      };
    }
    if (question.type === "nps") {
      return { ...base, numericValue: 7 + (index % 4) };
    }
    if (question.type === "yes_no") {
      return { ...base, textValue: index % 2 === 0 ? "Sim" : "Nao" };
    }

    if (question.type === "multiple_choice" || question.type === "dropdown") {
      const label =
        question.options?.[index % (question.options.length || 1)]?.label ??
        "Opcao 1";
      return { ...base, textValue: label };
    }

    if (question.type === "checkbox") {
      const options = question.options ?? [];
      const first = options[0]?.label ?? "Opcao 1";
      const second = options[1]?.label ?? "Opcao 2";
      return { ...base, textValue: `${first}, ${second}` };
    }

    if (question.type === "ranking") {
      const ranking = (question.options ?? [])
        .map((option) => option.label)
        .join(" > ");
      return { ...base, textValue: ranking || "Sem ordenacao" };
    }

    return {
      ...base,
      textValue: "Resposta registrada na pesquisa local.",
    };
  });
}

function buildQuestionData(
  question: WizardQuestion,
  totalResponses: number,
): QuestionResult["data"] {
  if (question.type === "likert" || question.type === "rating") {
    const scaleMax =
      question.type === "rating"
        ? (question.ratingMax ?? 5)
        : (question.scaleMax ?? 5);
    const counts = distribute(totalResponses, scaleMax);
    const distribution = counts.map((count, index) => ({
      label:
        question.type === "rating"
          ? `${index + 1} estrela${index > 0 ? "s" : ""}`
          : `${index + 1}`,
      count,
      percent:
        totalResponses > 0 ? Math.round((count / totalResponses) * 100) : 0,
    }));
    const weighted = distribution.reduce(
      (acc, item, index) => acc + item.count * (index + 1),
      0,
    );
    const average =
      totalResponses > 0 ? Number((weighted / totalResponses).toFixed(1)) : 0;
    const data: LikertData = { distribution, average };
    return data;
  }

  if (question.type === "nps") {
    const distribution = distribute(totalResponses, 11).map((count, value) => ({
      value,
      count,
    }));
    const detractorsCount = distribution
      .slice(0, 7)
      .reduce((acc, item) => acc + item.count, 0);
    const passivesCount = distribution
      .slice(7, 9)
      .reduce((acc, item) => acc + item.count, 0);
    const promotersCount = distribution
      .slice(9)
      .reduce((acc, item) => acc + item.count, 0);
    const score =
      totalResponses > 0
        ? Math.round(
            ((promotersCount - detractorsCount) / totalResponses) * 100,
          )
        : 0;
    const data: NpsData = {
      score,
      promoters:
        totalResponses > 0
          ? Math.round((promotersCount / totalResponses) * 100)
          : 0,
      passives:
        totalResponses > 0
          ? Math.round((passivesCount / totalResponses) * 100)
          : 0,
      detractors:
        totalResponses > 0
          ? Math.round((detractorsCount / totalResponses) * 100)
          : 0,
      distribution,
    };
    return data;
  }

  if (
    question.type === "multiple_choice" ||
    question.type === "dropdown" ||
    question.type === "checkbox"
  ) {
    const options =
      question.options && question.options.length > 0
        ? question.options
        : [
            { id: "opt-1", label: "Opcao 1" },
            { id: "opt-2", label: "Opcao 2" },
          ];
    const counts = distribute(totalResponses, options.length);
    const data: ChoiceData = {
      options: options.map((option, index) => ({
        count: counts[index] ?? 0,
        label: option.label,
        percent:
          totalResponses > 0
            ? Math.round(((counts[index] ?? 0) / totalResponses) * 100)
            : 0,
      })),
    };
    return data;
  }

  if (question.type === "yes_no") {
    const yes = Math.round(totalResponses * 0.6);
    const no = Math.max(0, totalResponses - yes);
    const data: YesNoData = {
      yes,
      no,
      yesPercent:
        totalResponses > 0 ? Math.round((yes / totalResponses) * 100) : 0,
      noPercent:
        totalResponses > 0 ? Math.round((no / totalResponses) * 100) : 0,
    };
    return data;
  }

  if (question.type === "ranking") {
    const options =
      question.options && question.options.length > 0
        ? question.options
        : [
            { id: "rank-1", label: "Item 1" },
            { id: "rank-2", label: "Item 2" },
            { id: "rank-3", label: "Item 3" },
          ];
    const data: RankingData = {
      items: options.map((option, index) => ({
        label: option.label,
        avgPosition: index + 1,
        count: totalResponses,
      })),
    };
    return data;
  }

  const responses =
    totalResponses > 0
      ? Array.from(
          { length: Math.min(totalResponses, 5) },
          () => "Resposta registrada na pesquisa local.",
        )
      : [];
  const data: TextData = {
    responses,
    totalCount: totalResponses,
  };
  return data;
}

function buildSectionsResult(
  state: SurveyWizardState,
  totalResponses: number,
): SurveyResultData["sections"] {
  const sections = state.sections;
  const questions = state.questions;

  if (questions.length === 0) {
    return [];
  }

  if (sections.length === 0) {
    return [
      {
        title: "Questionario",
        questions: questions.map((question) => ({
          questionId: question.id,
          questionText: question.text,
          questionType: question.type,
          responseCount: totalResponses,
          data: buildQuestionData(question, totalResponses),
          individualResponses: buildIndividualResponses(
            question,
            totalResponses,
          ),
        })),
      },
    ];
  }

  const result: SurveyResultData["sections"] = sections.map((section) => {
    const sectionQuestions = questions.filter(
      (question) => question.sectionId === section.id,
    );
    const mappedQuestions: QuestionResult[] = sectionQuestions.map(
      (question) => ({
        questionId: question.id,
        questionText: question.text,
        questionType: question.type,
        responseCount: totalResponses,
        data: buildQuestionData(question, totalResponses),
        individualResponses: buildIndividualResponses(question, totalResponses),
      }),
    );
    return {
      title: section.title,
      questions: mappedQuestions,
    };
  });

  const noSectionQuestions = questions.filter(
    (question) => question.sectionId === null,
  );
  if (noSectionQuestions.length > 0) {
    result.push({
      title: "Perguntas gerais",
      questions: noSectionQuestions.map((question) => ({
        questionId: question.id,
        questionText: question.text,
        questionType: question.type,
        responseCount: totalResponses,
        data: buildQuestionData(question, totalResponses),
        individualResponses: buildIndividualResponses(question, totalResponses),
      })),
    });
  }

  return result;
}

function isAnswerFilled(value: unknown): boolean {
  if (value === null || value === undefined) return false;
  if (typeof value === "string") return value.trim().length > 0;
  if (Array.isArray(value)) return value.length > 0;
  return true;
}

function formatSubmissionDate(iso: string): string {
  const formatted = formatDateBR(iso);
  return formatted || formatDateBR(todayIso());
}

function resolveOptionLabel(question: WizardQuestion, value: string): string {
  return (
    question.options?.find((option) => option.id === value)?.label ?? value
  );
}

function toIndividualResponsesFromSubmissions(
  question: WizardQuestion,
  submissions: SurveySubmissionRecord[],
): IndividualResponse[] {
  const answered = submissions.filter((submission) =>
    isAnswerFilled(submission.answers[question.id]),
  );

  return answered.map((submission, index) => {
    const rawValue = submission.answers[question.id];
    const base: IndividualResponse = {
      id: `${question.id}-${submission.id}`,
      name: `Respondente ${index + 1}`,
      initials: `R${String(index + 1).padStart(2, "0")}`,
      department: "Anônimo",
      answeredAt: formatSubmissionDate(submission.submittedAt),
    };

    if (typeof rawValue === "number") {
      return {
        ...base,
        numericValue: rawValue,
        textValue: String(rawValue),
      };
    }

    if (Array.isArray(rawValue)) {
      if (rawValue.length > 0 && typeof rawValue[0] === "object") {
        const rankingLabel = (
          rawValue as Array<{ label?: string; id?: string }>
        )
          .map((item, idx) => {
            const label = item.label ?? item.id ?? `Item ${idx + 1}`;
            return `${idx + 1}º ${label}`;
          })
          .join(", ");

        return {
          ...base,
          textValue: rankingLabel,
        };
      }

      const labels = (rawValue as string[])
        .map((item) => resolveOptionLabel(question, item))
        .join(", ");
      return {
        ...base,
        textValue: labels,
      };
    }

    if (typeof rawValue === "string") {
      if (question.type === "yes_no") {
        return {
          ...base,
          textValue:
            rawValue === "yes" ? "Sim" : rawValue === "no" ? "Nao" : rawValue,
        };
      }

      return {
        ...base,
        textValue: resolveOptionLabel(question, rawValue),
      };
    }

    return {
      ...base,
      textValue: "Resposta registrada",
    };
  });
}

function buildQuestionDataFromSubmissions(
  question: WizardQuestion,
  submissions: SurveySubmissionRecord[],
): QuestionResultData {
  const answers = submissions
    .map((submission) => submission.answers[question.id])
    .filter((value) => isAnswerFilled(value));
  const responseCount = answers.length;

  if (question.type === "likert" || question.type === "rating") {
    const max =
      question.type === "rating"
        ? (question.ratingMax ?? 5)
        : (question.scaleMax ?? 5);
    const counts = Array.from({ length: max }, () => 0);
    answers.forEach((value) => {
      if (typeof value !== "number") return;
      const index = Math.round(value) - 1;
      if (index >= 0 && index < max) {
        const currentCount = counts[index] ?? 0;
        counts[index] = currentCount + 1;
      }
    });

    const distribution = counts.map((count, index) => ({
      label:
        question.type === "rating"
          ? `${index + 1} estrela${index > 0 ? "s" : ""}`
          : `${index + 1}`,
      count,
      percent:
        responseCount > 0 ? Math.round((count / responseCount) * 100) : 0,
    }));
    const weighted = counts.reduce(
      (sum, count, index) => sum + count * (index + 1),
      0,
    );

    const data: LikertData = {
      distribution,
      average:
        responseCount > 0 ? Number((weighted / responseCount).toFixed(1)) : 0,
    };
    return data;
  }

  if (question.type === "nps") {
    const distribution = Array.from({ length: 11 }, (_, value) => ({
      value,
      count: 0,
    }));
    answers.forEach((value) => {
      if (typeof value !== "number") return;
      const rounded = Math.round(value);
      if (rounded >= 0 && rounded <= 10) {
        distribution[rounded]!.count += 1;
      }
    });

    const detractorsCount = distribution
      .slice(0, 7)
      .reduce((acc, item) => acc + item.count, 0);
    const passivesCount = distribution
      .slice(7, 9)
      .reduce((acc, item) => acc + item.count, 0);
    const promotersCount = distribution
      .slice(9)
      .reduce((acc, item) => acc + item.count, 0);

    const data: NpsData = {
      score:
        responseCount > 0
          ? Math.round(
              ((promotersCount - detractorsCount) / responseCount) * 100,
            )
          : 0,
      promoters:
        responseCount > 0
          ? Math.round((promotersCount / responseCount) * 100)
          : 0,
      passives:
        responseCount > 0
          ? Math.round((passivesCount / responseCount) * 100)
          : 0,
      detractors:
        responseCount > 0
          ? Math.round((detractorsCount / responseCount) * 100)
          : 0,
      distribution,
    };
    return data;
  }

  if (question.type === "multiple_choice" || question.type === "dropdown") {
    const options = question.options ?? [];
    const countsByOption = new Map(options.map((option) => [option.id, 0]));
    answers.forEach((value) => {
      if (typeof value !== "string") return;
      countsByOption.set(value, (countsByOption.get(value) ?? 0) + 1);
    });

    const data: ChoiceData = {
      options: options.map((option) => {
        const count = countsByOption.get(option.id) ?? 0;
        return {
          label: option.label,
          count,
          percent:
            responseCount > 0 ? Math.round((count / responseCount) * 100) : 0,
        };
      }),
    };
    return data;
  }

  if (question.type === "checkbox") {
    const options = question.options ?? [];
    const countsByOption = new Map(options.map((option) => [option.id, 0]));

    answers.forEach((value) => {
      if (!Array.isArray(value)) return;
      (value as string[]).forEach((optionId) => {
        countsByOption.set(optionId, (countsByOption.get(optionId) ?? 0) + 1);
      });
    });

    const data: ChoiceData = {
      options: options.map((option) => {
        const count = countsByOption.get(option.id) ?? 0;
        return {
          label: option.label,
          count,
          percent:
            responseCount > 0 ? Math.round((count / responseCount) * 100) : 0,
        };
      }),
    };
    return data;
  }

  if (question.type === "yes_no") {
    let yes = 0;
    let no = 0;
    answers.forEach((value) => {
      if (value === "yes") yes += 1;
      if (value === "no") no += 1;
    });

    const data: YesNoData = {
      yes,
      no,
      yesPercent:
        responseCount > 0 ? Math.round((yes / responseCount) * 100) : 0,
      noPercent: responseCount > 0 ? Math.round((no / responseCount) * 100) : 0,
    };
    return data;
  }

  if (question.type === "ranking") {
    const options = question.options ?? [];
    const metricsById = new Map(
      options.map((option, index) => [option.id, { sum: index + 1, count: 0 }]),
    );

    answers.forEach((value) => {
      if (!Array.isArray(value)) return;

      (value as Array<{ id?: string }>).forEach((item, index) => {
        if (!item?.id) return;
        const metrics = metricsById.get(item.id) ?? { sum: 0, count: 0 };
        metrics.sum += index + 1;
        metrics.count += 1;
        metricsById.set(item.id, metrics);
      });
    });

    const data: RankingData = {
      items: options
        .map((option, index) => {
          const metrics = metricsById.get(option.id) ?? {
            sum: index + 1,
            count: 0,
          };
          return {
            label: option.label,
            avgPosition:
              metrics.count > 0
                ? Number((metrics.sum / metrics.count).toFixed(1))
                : index + 1,
            count: metrics.count,
          };
        })
        .sort((a, b) => a.avgPosition - b.avgPosition),
    };
    return data;
  }

  const textResponses = answers
    .map((value) => {
      if (typeof value === "string") return value;
      if (typeof value === "number") return String(value);
      if (Array.isArray(value))
        return value.map((entry) => String(entry)).join(", ");
      if (value && typeof value === "object") return JSON.stringify(value);
      return "";
    })
    .filter((value) => value.trim().length > 0);

  const data: TextData = {
    responses: textResponses.slice(0, 8),
    totalCount: textResponses.length,
  };
  return data;
}

function buildSectionsResultFromSubmissions(
  state: SurveyWizardState,
  submissions: SurveySubmissionRecord[],
): SurveyResultData["sections"] {
  const sections = state.sections;
  const questions = state.questions;

  if (questions.length === 0) {
    return [];
  }

  const mapQuestion = (question: WizardQuestion): QuestionResult => {
    const responseCount = submissions.filter((submission) =>
      isAnswerFilled(submission.answers[question.id]),
    ).length;
    return {
      questionId: question.id,
      questionText: question.text,
      questionType: question.type,
      responseCount,
      data: buildQuestionDataFromSubmissions(question, submissions),
      individualResponses: toIndividualResponsesFromSubmissions(
        question,
        submissions,
      ),
    };
  };

  if (sections.length === 0) {
    return [
      {
        title: "Questionario",
        questions: questions.map(mapQuestion),
      },
    ];
  }

  const result: SurveyResultData["sections"] = sections.map((section) => {
    const sectionQuestions = questions.filter(
      (question) => question.sectionId === section.id,
    );
    return {
      title: section.title,
      questions: sectionQuestions.map(mapQuestion),
    };
  });

  const noSectionQuestions = questions.filter(
    (question) => question.sectionId === null,
  );
  if (noSectionQuestions.length > 0) {
    result.push({
      title: "Perguntas gerais",
      questions: noSectionQuestions.map(mapQuestion),
    });
  }

  return result;
}

/* ——————————————————————————————————————————————————————————————————————————
   Calibration data generator
   Used for ciclo surveys (performance, 360_feedback) to show 9-box grid
   and participant calibration status
   —————————————————————————————————————————————————————————————————————————— */

function generateCalibration(): CalibrationData {
  const participants: CalibrationParticipant[] = CALIBRATION_PEOPLE.map(
    (person, index) => {
      // Generate realistic scores with some variance
      const selfScore = Math.round((2.5 + Math.random() * 2.5) * 10) / 10;
      const managerScore =
        Math.round((selfScore + (Math.random() - 0.5) * 1.5) * 10) / 10;
      const score360 =
        Math.round((selfScore + (Math.random() - 0.5) * 1) * 10) / 10;
      const isCalibrated = Math.random() > 0.4;

      // Derive potential from score360 + okr + random factor
      const potentialScore =
        score360 * 0.4 + selfScore * 0.3 + Math.random() * 2.5;
      const potential: CalibrationParticipant["potential"] =
        potentialScore >= 4
          ? "alto"
          : potentialScore >= 2.8
            ? "médio"
            : "baixo";

      // Spread response dates across the evaluation period
      const responseDay = Math.min(28, 5 + Math.floor(Math.random() * 24));
      const responseMonth = Math.random() > 0.5 ? 2 : 3; // Feb or Mar 2026
      const respondedAt = `2026-${String(responseMonth).padStart(2, "0")}-${String(responseDay).padStart(2, "0")}`;

      return {
        id: `p-${index + 1}`,
        name: person.name,
        initials: person.initials,
        department: HR_DEPARTMENTS[index % HR_DEPARTMENTS.length] ?? "Geral",
        selfScore: Math.max(1, Math.min(5, selfScore)),
        managerScore: Math.max(1, Math.min(5, managerScore)),
        score360: Math.max(1, Math.min(5, score360)),
        finalScore: isCalibrated
          ? Math.round(
              (managerScore * 0.5 + score360 * 0.3 + selfScore * 0.2) * 10,
            ) / 10
          : null,
        potential,
        status: isCalibrated ? "calibrado" : "pendente",
        respondedAt,
        biasAlert: BIAS_ALERTS[index % BIAS_ALERTS.length],
        okrCompletion: Math.round(50 + Math.random() * 50),
        feedbackCount: Math.floor(Math.random() * 12) + 1,
        pulseMean: Math.round((3 + Math.random() * 2) * 10) / 10,
      };
    },
  );

  const calibratedCount = participants.filter(
    (p) => p.status === "calibrado",
  ).length;

  return {
    sessionStatus: "em_andamento",
    totalParticipants: participants.length,
    calibratedCount,
    participants,
  };
}

/* ——————————————————————————————————————————————————————————————————————————
   HR Review data generator
   Provides heatmap, attention points, trends, and action items
   for the "Visao Geral" (Overview) tab
   —————————————————————————————————————————————————————————————————————————— */

function generateHrReview(sections: ResultSection[]): HrReviewData {
  // Get likert/rating/nps questions for heatmap analysis
  const numericQuestions = sections
    .flatMap((section) => section.questions)
    .filter((question) =>
      ["likert", "rating", "nps"].includes(question.questionType),
    );

  // Create short labels (P1, P2, ...) for heatmap
  const shortLabels = numericQuestions.map((_, index) => `P${index + 1}`);
  const questionLabels: Record<string, string> = {};
  shortLabels.forEach((label, index) => {
    const question = numericQuestions[index];
    if (question) {
      questionLabels[label] = question.questionText;
    }
  });

  // Generate heatmap entries: question x department with scores 1-5
  const heatmapEntries: HeatmapEntry[] = [];
  shortLabels.forEach((label) => {
    HR_DEPARTMENTS.forEach((department) => {
      heatmapEntries.push({
        question: label,
        department,
        score: Math.round((2.5 + Math.random() * 2.5) * 10) / 10,
      });
    });
  });

  // Generate attention points for questions with low scores
  const attentionPoints: AttentionPoint[] = numericQuestions
    .map((question, index) => {
      // Calculate average score based on question type
      let avgScore: number;
      if (question.questionType === "nps") {
        const npsData = question.data as NpsData;
        // Normalize NPS (-100 to +100) to 1-5 scale
        avgScore = ((npsData.score + 100) / 200) * 4 + 1;
      } else {
        const likertData = question.data as LikertData;
        avgScore = likertData.average;
      }

      const benchmark = 3.8;
      if (avgScore < benchmark) {
        const worstDepartment =
          HR_DEPARTMENTS[Math.floor(Math.random() * HR_DEPARTMENTS.length)] ??
          "Geral";
        const severity: AttentionPoint["severity"] =
          avgScore < 3 ? "critical" : "warning";

        return {
          id: `ap-${index}`,
          questionText: question.questionText,
          score: Math.round(avgScore * 10) / 10,
          benchmark,
          department: worstDepartment,
          severity,
          insight:
            severity === "critical"
              ? `Score ${avgScore.toFixed(1)} esta significativamente abaixo do benchmark de ${benchmark}. O departamento de ${worstDepartment} apresenta o menor indice.`
              : `Score ${avgScore.toFixed(1)} esta abaixo do benchmark de ${benchmark}. Requer atencao no departamento de ${worstDepartment}.`,
        };
      }
      return null;
    })
    .filter((point): point is AttentionPoint => point !== null);

  // Generate trends: current vs previous period with history
  const trends: TrendPoint[] = numericQuestions.slice(0, 6).map((question) => {
    let current: number;
    if (question.questionType === "nps") {
      const npsData = question.data as NpsData;
      current = ((npsData.score + 100) / 200) * 4 + 1;
    } else {
      const likertData = question.data as LikertData;
      current = likertData.average;
    }

    const previous =
      Math.round((current + (Math.random() - 0.5) * 1.2) * 10) / 10;
    const delta = Math.round((current - previous) * 10) / 10;

    // Generate 4-period history
    const history = [
      Math.round((current - 0.3 + Math.random() * 0.6) * 10) / 10,
      Math.round((current - 0.2 + Math.random() * 0.4) * 10) / 10,
      Math.max(1, Math.min(5, previous)),
      Math.max(1, Math.min(5, current)),
    ].map((value) => Math.max(1, Math.min(5, value)));

    return {
      questionText: question.questionText,
      current: Math.round(current * 10) / 10,
      previous: Math.max(1, Math.min(5, previous)),
      delta,
      history,
    };
  });

  // Generate action items based on survey insights
  const actionItems: ActionItem[] = [
    {
      id: "ai-1",
      title: "Workshop de comunicacao interna",
      description:
        "Organizar sessao com times que pontuaram abaixo de 3.5 em comunicacao",
      priority: "alta",
      department: "Geral",
      status: "pendente",
    },
    {
      id: "ai-2",
      title: "Revisao do programa de desenvolvimento",
      description:
        "Atualizar trilhas de carreira com base no feedback de crescimento profissional",
      priority: "alta",
      department: "People",
      status: "em_andamento",
      assignee: "Gabriela Lima",
    },
    {
      id: "ai-3",
      title: "Pesquisa de follow-up com Engenharia",
      description:
        "Aprofundar investigacao sobre carga de trabalho e bem-estar no time de engenharia",
      priority: "média",
      department: "Engenharia",
      status: "pendente",
    },
    {
      id: "ai-4",
      title: "Sessao de feedback com lideranca",
      description:
        "Apresentar resultados consolidados e alinhar proximos passos",
      priority: "média",
      department: "Geral",
      status: "pendente",
    },
    {
      id: "ai-5",
      title: "Melhoria nas ferramentas de trabalho",
      description:
        "Avaliar NPS de ferramentas internas e priorizar substituicoes",
      priority: "baixa",
      department: "Operacoes",
      status: "concluída",
      assignee: "Otavio Ribeiro",
    },
  ];

  return {
    heatmap: {
      entries: heatmapEntries,
      questions: shortLabels,
      questionLabels,
      departments: HR_DEPARTMENTS,
    },
    attentionPoints,
    trends,
    actionItems,
  };
}

export function buildSurveyResultsFromRecord(
  record: SurveyLocalRecord,
  options?: { submissions?: SurveySubmissionRecord[] },
): SurveyResultData {
  const state =
    record.wizardState ?? createWizardStateFromListItem(record.listItem);
  const submissions = options?.submissions ?? [];
  const hasRealSubmissions = submissions.length > 0;
  const totalResponses = hasRealSubmissions
    ? submissions.length
    : record.listItem.totalResponses;
  const totalRecipients = Math.max(
    record.listItem.totalRecipients,
    totalResponses,
  );
  const completionRate =
    totalRecipients > 0
      ? Math.min(100, Math.round((totalResponses / totalRecipients) * 100))
      : 0;
  const period =
    record.listItem.startDate && record.listItem.endDate
      ? `${record.listItem.startDate} – ${record.listItem.endDate}`
      : "";

  // Build sections from real submissions or generate mock data
  const sections = hasRealSubmissions
    ? buildSectionsResultFromSubmissions(state, submissions)
    : buildSectionsResult(state, totalResponses);

  // Build the base result
  const result: SurveyResultData = {
    surveyId: record.id,
    surveyName: record.listItem.name,
    surveyType: record.listItem.type,
    surveyCategory: record.listItem.category,
    status: record.listItem.status,
    period,
    kpis: {
      views: totalRecipients + Math.round(totalRecipients * 0.1),
      started: Math.max(totalResponses, Math.round(totalRecipients * 0.6)),
      responses: totalResponses,
      completionRate,
      avgCompletionTime: "5min 20s",
    },
    sections,
  };

  // Add calibration data for ciclo surveys (performance reviews, 360 feedback)
  if (record.listItem.category === "ciclo") {
    result.calibration = generateCalibration();
  }

  // Add HR review data for surveys with responses (provides heatmap, attention points, trends)
  if (totalResponses > 0 && sections.length > 0) {
    result.hrReview = generateHrReview(sections);
  }

  return result;
}
