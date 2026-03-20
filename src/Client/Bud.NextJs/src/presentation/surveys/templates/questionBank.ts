import type { QuestionType } from "@/types/survey";

export interface BankQuestion {
  id: string;
  category: string;
  type: QuestionType;
  text: string;
  options?: { id: string; label: string }[];
  scaleMin?: number;
  scaleMax?: number;
  scaleLabels?: Record<string, string>;
  ratingMax?: number;
}

export interface QuestionBankCategory {
  id: string;
  label: string;
  questions: BankQuestion[];
}

const LIKERT_DEFAULTS = { scaleMin: 1, scaleMax: 5, scaleLabels: { min: "Discordo totalmente", max: "Concordo totalmente" } };

export const QUESTION_BANK: QuestionBankCategory[] = [
  {
    id: "engagement",
    label: "Engajamento",
    questions: [
      { id: "bank-eng-1", category: "engagement", type: "likert", text: "Estou motivado(a) a dar o meu melhor todos os dias", ...LIKERT_DEFAULTS },
      { id: "bank-eng-2", category: "engagement", type: "likert", text: "Sinto orgulho de trabalhar nesta empresa", ...LIKERT_DEFAULTS },
      { id: "bank-eng-3", category: "engagement", type: "likert", text: "Meu trabalho tem um propósito claro", ...LIKERT_DEFAULTS },
      { id: "bank-eng-4", category: "engagement", type: "likert", text: "Me sinto conectado(a) à missão da empresa", ...LIKERT_DEFAULTS },
      { id: "bank-eng-5", category: "engagement", type: "text_short", text: "O que mais te motiva no seu trabalho?" },
    ],
  },
  {
    id: "leadership",
    label: "Liderança",
    questions: [
      { id: "bank-lid-1", category: "leadership", type: "likert", text: "Meu gestor me dá feedback construtivo regularmente", ...LIKERT_DEFAULTS },
      { id: "bank-lid-2", category: "leadership", type: "likert", text: "Meu gestor se preocupa com meu bem-estar", ...LIKERT_DEFAULTS },
      { id: "bank-lid-3", category: "leadership", type: "likert", text: "Meu gestor reconhece meu bom trabalho", ...LIKERT_DEFAULTS },
      { id: "bank-lid-4", category: "leadership", type: "likert", text: "Confio na capacidade de decisão do meu gestor", ...LIKERT_DEFAULTS },
      { id: "bank-lid-5", category: "leadership", type: "text_short", text: "O que seu gestor poderia fazer melhor?" },
    ],
  },
  {
    id: "wellbeing",
    label: "Bem-estar",
    questions: [
      { id: "bank-bem-1", category: "wellbeing", type: "likert", text: "Consigo desconectar do trabalho fora do horário", ...LIKERT_DEFAULTS },
      { id: "bank-bem-2", category: "wellbeing", type: "likert", text: "Minha carga de trabalho é sustentável", ...LIKERT_DEFAULTS },
      { id: "bank-bem-3", category: "wellbeing", type: "multiple_choice", text: "Como está seu nível de estresse atualmente?", options: [{ id: "green", label: "Saudável" }, { id: "yellow", label: "Atenção" }, { id: "red", label: "Crítico" }] },
      { id: "bank-bem-4", category: "wellbeing", type: "likert", text: "A empresa oferece benefícios adequados", ...LIKERT_DEFAULTS },
      { id: "bank-bem-5", category: "wellbeing", type: "text_short", text: "O que ajudaria a melhorar seu bem-estar no trabalho?" },
    ],
  },
  {
    id: "collaboration",
    label: "Colaboração",
    questions: [
      { id: "bank-col-1", category: "collaboration", type: "likert", text: "As reuniões do time são produtivas", ...LIKERT_DEFAULTS },
      { id: "bank-col-2", category: "collaboration", type: "likert", text: "Existe confiança e respeito mútuo no time", ...LIKERT_DEFAULTS },
      { id: "bank-col-3", category: "collaboration", type: "likert", text: "Consigo colaborar facilmente com outras áreas", ...LIKERT_DEFAULTS },
      { id: "bank-col-4", category: "collaboration", type: "likert", text: "Conflitos são resolvidos de forma saudável", ...LIKERT_DEFAULTS },
      { id: "bank-col-5", category: "collaboration", type: "text_short", text: "Como poderíamos melhorar a colaboração entre times?" },
    ],
  },
  {
    id: "development",
    label: "Desenvolvimento",
    questions: [
      { id: "bank-dev-1", category: "development", type: "likert", text: "Tenho acesso a treinamentos e capacitação", ...LIKERT_DEFAULTS },
      { id: "bank-dev-2", category: "development", type: "likert", text: "Sei quais competências preciso desenvolver", ...LIKERT_DEFAULTS },
      { id: "bank-dev-3", category: "development", type: "likert", text: "A empresa investe no meu crescimento profissional", ...LIKERT_DEFAULTS },
      { id: "bank-dev-4", category: "development", type: "likert", text: "Tenho oportunidade de aprender com meus erros", ...LIKERT_DEFAULTS },
      { id: "bank-dev-5", category: "development", type: "text_long", text: "Quais habilidades você gostaria de desenvolver nos próximos 6 meses?" },
    ],
  },
  {
    id: "competencies",
    label: "Competências (Avaliação)",
    questions: [
      { id: "bank-comp-1", category: "competencies", type: "likert", text: "Demonstra pensamento estratégico e visão de longo prazo", ...LIKERT_DEFAULTS },
      { id: "bank-comp-2", category: "competencies", type: "likert", text: "Toma decisões baseadas em dados e evidências", ...LIKERT_DEFAULTS },
      { id: "bank-comp-3", category: "competencies", type: "likert", text: "Adapta-se rapidamente a mudanças e ambiguidade", ...LIKERT_DEFAULTS },
      { id: "bank-comp-4", category: "competencies", type: "rating", text: "Avaliação geral de desempenho no período", ratingMax: 5 },
      { id: "bank-comp-5", category: "competencies", type: "checkbox", text: "Principais pontos fortes observados", options: [{ id: "comm", label: "Comunicação" }, { id: "lead", label: "Liderança" }, { id: "tech", label: "Competência técnica" }, { id: "team", label: "Trabalho em equipe" }, { id: "innov", label: "Inovação" }] },
    ],
  },
];
