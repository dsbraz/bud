import {
  Badge,
  Card,
  CardBody,
  Button,
  Chart,
  ChartTooltipContent,
  Modal,
  ModalHeader,
  ModalBody,
  Table,
  TableContent,
  TableHead,
  TableBody,
  TableRow,
  TableHeaderCell,
  TableCell,
  AvatarLabelGroup,
  Input,
} from "@mdonangelo/bud-ds";
import { useState, useMemo, type ChangeEvent } from "react";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from "recharts";
import { ListMagnifyingGlass, MagnifyingGlass } from "@phosphor-icons/react";
import type { QuestionResult, IndividualResponse, LikertData, NpsData, ChoiceData, TextData, YesNoData, RankingData } from "../types";
import { QUESTION_TYPE_LABELS } from "@/utils/questionTypeLabels";
import styles from "./QuestionResultCard.module.css";

/* ——— Recharts axis tick style ——— */

const AXIS_TICK = {
  fontFamily: "var(--font-label)",
  fontSize: 12,
  fill: "var(--color-neutral-500)",
};

/* ——— Colors ——— */

const CHART_COLORS = [
  "var(--color-chart-1)",
  "var(--color-chart-2)",
  "var(--color-chart-3)",
  "var(--color-chart-4)",
  "var(--color-chart-5)",
];

function getNpsBarColor(value: number): string {
  if (value <= 6) return "var(--color-error-400)";
  if (value <= 8) return "var(--color-warning-400)";
  return "var(--color-success-500)";
}

/* ——— Sub-renderers ——— */

function LikertChart({ data }: { data: LikertData }) {
  const chartData = data.distribution.map((d, i) => ({
    name: String(i + 1),
    value: d.percent,
    count: d.count,
    label: d.label,
  }));

  return (
    <div className={styles.chartArea}>
      <span className={styles.avgBadge}>Média: <strong>{data.average}</strong></span>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={chartData} margin={{ top: 8, right: 8, bottom: 0, left: -16 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="var(--color-caramel-200)" vertical={false} />
          <XAxis dataKey="name" tick={AXIS_TICK} axisLine={false} tickLine={false} />
          <YAxis tick={AXIS_TICK} axisLine={false} tickLine={false} domain={[0, 100]} tickFormatter={(v: number | string) => `${v}%`} />
          <Tooltip content={<ChartTooltipContent valueFormatter={(v: number | string) => `${v}%`} />} animationDuration={150} animationEasing="ease-out" />
          <Bar dataKey="value" name="Respostas" radius={[4, 4, 0, 0]} barSize={32}>
            {chartData.map((_, i) => (
              <Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

function NpsChart({ data }: { data: NpsData }) {
  const total = data.distribution.reduce((s, x) => s + x.count, 0) || 1;
  const chartData = data.distribution.map((d) => ({
    name: String(d.value),
    count: d.count,
    percent: Math.round((d.count / total) * 100),
  }));

  return (
    <div className={styles.chartArea}>
      <div className={styles.npsHeader}>
        <div className={styles.npsScoreBlock}>
          <span className={styles.npsNumber}>{data.score}</span>
          <span className={styles.npsLabel}>NPS</span>
        </div>
        <div className={styles.npsLegend}>
          <span className={styles.legendItem}>
            <span className={styles.legendDot} style={{ backgroundColor: "var(--color-success-500)" }} />
            Promotores {data.promoters}%
          </span>
          <span className={styles.legendItem}>
            <span className={styles.legendDot} style={{ backgroundColor: "var(--color-warning-400)" }} />
            Neutros {data.passives}%
          </span>
          <span className={styles.legendItem}>
            <span className={styles.legendDot} style={{ backgroundColor: "var(--color-error-400)" }} />
            Detratores {data.detractors}%
          </span>
        </div>
      </div>
      <ResponsiveContainer width="100%" height={180}>
        <BarChart data={chartData} margin={{ top: 8, right: 8, bottom: 0, left: -16 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="var(--color-caramel-200)" vertical={false} />
          <XAxis dataKey="name" tick={AXIS_TICK} axisLine={false} tickLine={false} />
          <YAxis tick={AXIS_TICK} axisLine={false} tickLine={false} />
          <Tooltip content={<ChartTooltipContent valueFormatter={(v: number | string) => `${v} respostas`} />} animationDuration={150} animationEasing="ease-out" />
          <Bar dataKey="count" name="Respostas" radius={[4, 4, 0, 0]} barSize={28}>
            {chartData.map((d, i) => (
              <Cell key={i} fill={getNpsBarColor(Number(d.name))} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

function ChoiceChart({ data }: { data: ChoiceData }) {
  const chartData = data.options.map((opt) => ({
    name: opt.label,
    value: opt.percent,
    count: opt.count,
  }));

  return (
    <ResponsiveContainer width="100%" height={chartData.length * 44 + 20}>
      <BarChart data={chartData} layout="vertical" margin={{ top: 4, right: 40, bottom: 0, left: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="var(--color-caramel-200)" horizontal={false} />
        <XAxis type="number" domain={[0, 100]} tick={AXIS_TICK} axisLine={false} tickLine={false} tickFormatter={(v: number | string) => `${v}%`} />
        <YAxis type="category" dataKey="name" tick={AXIS_TICK} axisLine={false} tickLine={false} width={140} />
        <Tooltip content={<ChartTooltipContent valueFormatter={(v: number | string) => `${v}%`} />} animationDuration={150} animationEasing="ease-out" />
        <Bar dataKey="value" name="Respostas" radius={[0, 4, 4, 0]} barSize={18}>
          {chartData.map((_, i) => (
            <Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}

function YesNoChart({ data }: { data: YesNoData }) {
  return (
    <div className={styles.yesNoArea}>
      <div className={styles.yesNoItem}>
        <Chart value={data.yesPercent} size={72} />
        <div className={styles.yesNoDetail}>
          <span className={styles.yesNoValue}>Sim</span>
          <span className={styles.yesNoSub}>{data.yes} respostas ({data.yesPercent}%)</span>
        </div>
      </div>
      <div className={styles.yesNoItem}>
        <Chart value={data.noPercent} size={72} />
        <div className={styles.yesNoDetail}>
          <span className={styles.yesNoValue}>Não</span>
          <span className={styles.yesNoSub}>{data.no} respostas ({data.noPercent}%)</span>
        </div>
      </div>
    </div>
  );
}

function TextResponses({ data }: { data: TextData }) {
  const [showAll, setShowAll] = useState(false);
  const visible = showAll ? data.responses : data.responses.slice(0, 5);

  return (
    <div className={styles.textResponses}>
      {visible.map((r, i) => (
        <div key={i} className={styles.textItem}>
          <span className={styles.textQuote}>&ldquo;</span>
          <p>{r}</p>
        </div>
      ))}
      {!showAll && data.responses.length > 5 && (
        <Button variant="tertiary" size="sm" onClick={() => setShowAll(true)}>
          Ver todas ({data.totalCount} respostas)
        </Button>
      )}
    </div>
  );
}

function RankingChart({ data }: { data: RankingData }) {
  const maxPos = Math.max(...data.items.map((d) => d.avgPosition), 1);
  const chartData = data.items.map((item, i) => ({
    name: `${i + 1}º ${item.label}`,
    score: Math.round((1 - (item.avgPosition - 1) / maxPos) * 100),
    avgPos: item.avgPosition,
  }));

  return (
    <ResponsiveContainer width="100%" height={chartData.length * 44 + 20}>
      <BarChart data={chartData} layout="vertical" margin={{ top: 4, right: 40, bottom: 0, left: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="var(--color-caramel-200)" horizontal={false} />
        <XAxis type="number" domain={[0, 100]} tick={AXIS_TICK} axisLine={false} tickLine={false} hide />
        <YAxis type="category" dataKey="name" tick={AXIS_TICK} axisLine={false} tickLine={false} width={160} />
        <Tooltip
          content={<ChartTooltipContent valueFormatter={(v: number | string) => {
            const numericValue = typeof v === "number" ? v : Number(v);
            return `posição média ~${((1 - numericValue / 100) * maxPos + 1).toFixed(1)}`;
          }} />}
          animationDuration={150}
          animationEasing="ease-out"
        />
        <Bar dataKey="score" name="Ranking" radius={[0, 4, 4, 0]} barSize={18} fill="var(--color-chart-1)" />
      </BarChart>
    </ResponsiveContainer>
  );
}

/* ——— Type labels (from shared utils) ——— */

/* ——— Answer column label ——— */

function getAnswerColumnLabel(type: string): string {
  switch (type) {
    case "likert":
    case "rating":
      return "Nota";
    case "nps":
      return "Nota (0-10)";
    case "yes_no":
      return "Resposta";
    case "multiple_choice":
    case "dropdown":
      return "Opção";
    case "checkbox":
      return "Opções";
    case "ranking":
      return "Ordenação";
    case "text_short":
    case "text_long":
      return "Resposta";
    default:
      return "Resposta";
  }
}

function getScoreColor(type: string, value?: number): string | undefined {
  if (value == null) return undefined;
  if (type === "nps") {
    if (value >= 9) return "var(--color-success-600)";
    if (value >= 7) return "var(--color-warning-600)";
    return "var(--color-error-600)";
  }
  if (type === "likert" || type === "rating") {
    if (value >= 4) return "var(--color-success-600)";
    if (value >= 3) return "var(--color-warning-600)";
    return "var(--color-error-600)";
  }
  return undefined;
}

/* ——— Detail modal ——— */

function ResponseDetailModal({
  open,
  onClose,
  questionText,
  questionIndex,
  responses,
  questionType,
}: {
  open: boolean;
  onClose: () => void;
  questionText: string;
  questionIndex: number;
  responses: IndividualResponse[];
  questionType: string;
}) {
  const answerLabel = getAnswerColumnLabel(questionType);
  const hasNumeric = ["likert", "rating", "nps"].includes(questionType);
  const [search, setSearch] = useState("");

  const filtered = useMemo(() => {
    if (!search) return responses;
    const q = search.toLowerCase();
    return responses.filter(
      (r) => r.name.toLowerCase().includes(q) || r.department.toLowerCase().includes(q),
    );
  }, [responses, search]);

  return (
    <Modal open={open} onClose={onClose} size="lg">
      <ModalHeader
        title={`P${questionIndex + 1}. ${questionText}`}
        onClose={onClose}
      />
      <ModalBody>
        <div className={styles.modalToolbar}>
          <Input
            size="sm"
            leftIcon={MagnifyingGlass}
            placeholder="Buscar respondente..."
            value={search}
            onChange={(e: ChangeEvent<HTMLInputElement>) => setSearch(e.target.value)}
          />
          <Badge color="neutral" size="sm">{filtered.length} respostas</Badge>
        </div>
        <Table variant="divider" elevated={false} bordered={false}>
          <TableContent>
            <TableHead>
              <TableRow>
                <TableHeaderCell>Respondente</TableHeaderCell>
                <TableHeaderCell>Departamento</TableHeaderCell>
                <TableHeaderCell>{answerLabel}</TableHeaderCell>
                <TableHeaderCell>Data</TableHeaderCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filtered.map((r) => (
                <TableRow key={r.id}>
                  <TableCell>
                    <AvatarLabelGroup name={r.name} initials={r.initials} size="sm" />
                  </TableCell>
                  <TableCell>{r.department}</TableCell>
                  <TableCell>
                    {hasNumeric && r.numericValue != null ? (
                      <span
                        className={styles.scoreValue}
                        style={{ color: getScoreColor(questionType, r.numericValue) }}
                      >
                        {r.numericValue}
                      </span>
                    ) : (
                      <span className={styles.textAnswer}>{r.textValue}</span>
                    )}
                  </TableCell>
                  <TableCell>{r.answeredAt}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </TableContent>
        </Table>
      </ModalBody>
    </Modal>
  );
}

/* ——— Main component ——— */

interface QuestionResultCardProps {
  result: QuestionResult;
  index: number;
}

export function QuestionResultCard({ result, index }: QuestionResultCardProps) {
  const typeLabel = QUESTION_TYPE_LABELS[result.questionType as keyof typeof QUESTION_TYPE_LABELS] ?? result.questionType;
  const [detailOpen, setDetailOpen] = useState(false);

  function renderChart() {
    switch (result.questionType) {
      case "likert":
      case "rating":
        return <LikertChart data={result.data as LikertData} />;
      case "nps":
        return <NpsChart data={result.data as NpsData} />;
      case "multiple_choice":
      case "dropdown":
      case "checkbox":
        return <ChoiceChart data={result.data as ChoiceData} />;
      case "yes_no":
        return <YesNoChart data={result.data as YesNoData} />;
      case "text_short":
      case "text_long":
        return <TextResponses data={result.data as TextData} />;
      case "ranking":
        return <RankingChart data={result.data as RankingData} />;
      default:
        return <p>Tipo de pergunta não suportado</p>;
    }
  }

  return (
    <Card padding="sm">
      <CardBody>
        <div className={styles.cardHeader}>
          <h4 className={styles.questionTitle}>
            {index + 1}. {result.questionText}
          </h4>
          <div className={styles.headerRight}>
            <Badge color="caramel" size="sm">{typeLabel}</Badge>
            <Badge color="neutral" size="sm">{result.responseCount} respostas</Badge>
            <Button
              variant="secondary"
              size="sm"
              leftIcon={ListMagnifyingGlass}
              onClick={() => setDetailOpen(true)}
            >
              Detalhes
            </Button>
          </div>
        </div>
        <div className={styles.chartBody}>
          {renderChart()}
        </div>
      </CardBody>

      <ResponseDetailModal
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
        questionText={result.questionText}
        questionIndex={index}
        responses={result.individualResponses}
        questionType={result.questionType}
      />
    </Card>
  );
}
