import {
  Input,
  Textarea,
  Radio,
  Checkbox,
  Select,
  DatePicker,
  ScaleInput,
  SortableList,
} from "@mdonangelo/bud-ds";
import type { CalendarDate, SortableItem } from "@mdonangelo/bud-ds";
import type { WizardQuestion } from "@/types/survey";
import styles from "./QuestionField.module.css";

export interface QuestionFieldProps {
  /** Question data */
  question: WizardQuestion;
  /** 0-based index within the full survey */
  index: number;
  /** Total number of questions in the survey */
  total: number;
  /** Current answer value */
  value: unknown;
  /** Called when the answer changes */
  onChange: (value: unknown) => void;
  /** Whether the field is disabled (preview mode) */
  disabled?: boolean;
  /** Show validation error state */
  error?: boolean;
}

/**
 * Renders a single survey question as an interactive form field.
 * Maps QuestionType to the appropriate DS component.
 */
export function QuestionField({
  question,
  index,
  total,
  value,
  onChange,
  disabled = false,
  error = false,
}: QuestionFieldProps) {
  return (
    <div className={`${styles.question} ${error ? styles.questionError : ""}`}>
      <div className={styles.questionHeader}>
        <span className={styles.questionNumber}>
          Pergunta {index + 1} de {total}
        </span>
        {question.isRequired && <span className={styles.required}>Obrigatória</span>}
      </div>
      <p className={styles.questionText}>{question.text}</p>
      {question.description && (
        <p className={styles.questionDescription}>{question.description}</p>
      )}
      <div className={styles.questionField}>
        {renderField(question, value, onChange, disabled)}
      </div>
      {error && (
        <p className={styles.errorMessage}>Esta pergunta é obrigatória</p>
      )}
    </div>
  );
}

function renderField(
  q: WizardQuestion,
  value: unknown,
  onChange: (v: unknown) => void,
  disabled: boolean,
) {
  switch (q.type) {
    case "text_short":
      return (
        <Input
          placeholder="Digite sua resposta..."
          value={(value as string) ?? ""}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => onChange(e.target.value)}
          disabled={disabled}
        />
      );

    case "text_long":
      return (
        <Textarea
          placeholder="Digite sua resposta..."
          value={(value as string) ?? ""}
          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => onChange(e.target.value)}
          rows={4}
          disabled={disabled}
        />
      );

    case "multiple_choice":
      return (
        <div className={styles.optionsList}>
          {(q.options ?? []).map((opt) => (
            <Radio
              key={opt.id}
              name={`question-${q.id}`}
              label={opt.label}
              checked={value === opt.id}
              onChange={() => onChange(opt.id)}
              disabled={disabled}
            />
          ))}
        </div>
      );

    case "checkbox":
      return (
        <div className={styles.optionsList}>
          {(q.options ?? []).map((opt) => {
            const selected = (value as string[]) ?? [];
            return (
              <Checkbox
                key={opt.id}
                label={opt.label}
                checked={selected.includes(opt.id)}
                onChange={() => {
                  if (disabled) return;
                  const next = selected.includes(opt.id)
                    ? selected.filter((id) => id !== opt.id)
                    : [...selected, opt.id];
                  onChange(next);
                }}
                disabled={disabled}
              />
            );
          })}
        </div>
      );

    case "dropdown":
      return (
        <Select
          placeholder="Selecione..."
          options={(q.options ?? []).map((o) => ({ value: o.id, label: o.label }))}
          value={(value as string) ?? ""}
          onChange={(val: string | undefined) => onChange(val ?? null)}
          disabled={disabled}
        />
      );

    case "yes_no":
      return (
        <div className={styles.optionsList}>
          <Radio
            name={`question-${q.id}`}
            label="Sim"
            checked={value === "yes"}
            onChange={() => onChange("yes")}
            disabled={disabled}
          />
          <Radio
            name={`question-${q.id}`}
            label="Não"
            checked={value === "no"}
            onChange={() => onChange("no")}
            disabled={disabled}
          />
        </div>
      );

    case "likert":
      return (
        <ScaleInput
          min={q.scaleMin ?? 1}
          max={q.scaleMax ?? 5}
          value={value as number | undefined}
          onChange={(v: number | undefined) => onChange(v)}
          minLabel={q.scaleLabels?.min}
          maxLabel={q.scaleLabels?.max}
          disabled={disabled}
        />
      );

    case "nps":
      return (
        <ScaleInput
          min={0}
          max={10}
          value={value as number | undefined}
          onChange={(v: number | undefined) => onChange(v)}
          minLabel={q.scaleLabels?.min ?? "Nada provável"}
          maxLabel={q.scaleLabels?.max ?? "Extremamente provável"}
          disabled={disabled}
        />
      );

    case "rating":
      return (
        <ScaleInput
          min={1}
          max={q.ratingMax ?? 5}
          value={value as number | undefined}
          onChange={(v: number | undefined) => onChange(v)}
          disabled={disabled}
          size="sm"
        />
      );

    case "ranking": {
      const items: SortableItem[] = (value as SortableItem[] | null)
        ?? (q.options ?? []).map((opt) => ({ id: opt.id, label: opt.label }));
      return (
        <SortableList
          items={items}
          onChange={(reordered: SortableItem[]) => onChange(reordered)}
          disabled={disabled}
        />
      );
    }

    case "date":
      return (
        <DatePicker
          mode="single"
          value={value as CalendarDate | null}
          onChange={(cd: CalendarDate | null) => onChange(cd ?? null)}
          disabled={disabled}
        />
      );

    default:
      return <Input placeholder="Resposta..." disabled />;
  }
}
