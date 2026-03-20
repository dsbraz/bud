import { Input, Textarea, Radio, Checkbox, Select, DatePicker } from "@mdonangelo/bud-ds";
import { Star } from "@phosphor-icons/react";
import type { CalendarDate } from "@mdonangelo/bud-ds";
import type { WizardQuestion } from "@/types/survey";
import styles from "./PreviewQuestionRenderer.module.css";

interface Props {
  question: WizardQuestion;
  index: number;
  total: number;
  value: unknown;
  onChange: (value: unknown) => void;
}

export function PreviewQuestionRenderer({ question, index, total, value, onChange }: Props) {
  return (
    <div className={styles.question}>
      <div className={styles.questionHeader}>
        <span className={styles.questionNumber}>Pergunta {index + 1} de {total}</span>
        {question.isRequired && <span className={styles.required}>Obrigatória</span>}
      </div>
      <p className={styles.questionText}>{question.text}</p>
      {question.description && (
        <p className={styles.questionDescription}>{question.description}</p>
      )}
      <div className={styles.questionField}>
        {renderField(question, value, onChange)}
      </div>
    </div>
  );
}

function renderField(q: WizardQuestion, value: unknown, onChange: (v: unknown) => void) {
  switch (q.type) {
    case "text_short":
      return (
        <Input
          placeholder="Digite sua resposta..."
          value={(value as string) ?? ""}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => onChange(e.target.value)}
        />
      );

    case "text_long":
      return (
        <Textarea
          placeholder="Digite sua resposta..."
          value={(value as string) ?? ""}
          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => onChange(e.target.value)}
          rows={4}
        />
      );

    case "multiple_choice":
      return (
        <div className={styles.optionsList}>
          {(q.options ?? []).map((opt) => (
            <Radio
              key={opt.id}
              name={`preview-${q.id}`}
              label={opt.label}
              checked={value === opt.id}
              onChange={() => onChange(opt.id)}
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
                  const next = selected.includes(opt.id)
                    ? selected.filter((id) => id !== opt.id)
                    : [...selected, opt.id];
                  onChange(next);
                }}
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
          onChange={(val: string) => onChange(val)}
        />
      );

    case "yes_no":
      return (
        <div className={styles.optionsList}>
          <Radio
            name={`preview-${q.id}`}
            label="Sim"
            checked={value === "yes"}
            onChange={() => onChange("yes")}
          />
          <Radio
            name={`preview-${q.id}`}
            label="Não"
            checked={value === "no"}
            onChange={() => onChange("no")}
          />
        </div>
      );

    case "likert":
      return <LikertScale question={q} value={value as number | null} onChange={onChange} />;

    case "nps":
      return <NpsScale value={value as number | null} onChange={onChange} />;

    case "rating":
      return <StarRating max={q.ratingMax ?? 5} value={value as number | null} onChange={onChange} />;

    case "ranking":
      return (
        <div className={styles.optionsList}>
          {(q.options ?? []).map((opt, i) => (
            <div key={opt.id} className={styles.rankingItem}>
              <span className={styles.rankingNumber}>{i + 1}</span>
              <span className={styles.rankingLabel}>{opt.label}</span>
            </div>
          ))}
        </div>
      );

    case "date":
      return (
        <DatePicker
          mode="single"
          value={value as CalendarDate | null}
          onChange={(cd: CalendarDate | null) => onChange(cd ?? null)}
        />
      );

    default:
      return <Input placeholder="Resposta..." disabled />;
  }
}

/* ——— Custom scale components ——— */

function LikertScale({ question, value, onChange }: { question: WizardQuestion; value: number | null; onChange: (v: unknown) => void }) {
  const min = question.scaleMin ?? 1;
  const max = question.scaleMax ?? 5;
  const points = Array.from({ length: max - min + 1 }, (_, i) => min + i);

  return (
    <div className={styles.scaleWrapper}>
      <div className={styles.scaleRow}>
        {points.map((n) => (
          <button
            key={n}
            type="button"
            className={`${styles.scaleButton} ${value === n ? styles.scaleButtonActive : ""}`}
            onClick={() => onChange(n)}
          >
            {n}
          </button>
        ))}
      </div>
      {question.scaleLabels && (
        <div className={styles.scaleLabels}>
          <span>{question.scaleLabels.min}</span>
          <span>{question.scaleLabels.max}</span>
        </div>
      )}
    </div>
  );
}

function NpsScale({ value, onChange }: { value: number | null; onChange: (v: unknown) => void }) {
  const points = Array.from({ length: 11 }, (_, i) => i);

  return (
    <div className={styles.scaleWrapper}>
      <div className={styles.npsRow}>
        {points.map((n) => (
          <button
            key={n}
            type="button"
            className={`${styles.npsButton} ${value === n ? styles.npsButtonActive : ""} ${n <= 6 ? styles.npsDetractor : n <= 8 ? styles.npsPassive : styles.npsPromoter}`}
            onClick={() => onChange(n)}
          >
            {n}
          </button>
        ))}
      </div>
      <div className={styles.scaleLabels}>
        <span>Nada provável</span>
        <span>Extremamente provável</span>
      </div>
    </div>
  );
}

function StarRating({ max, value, onChange }: { max: number; value: number | null; onChange: (v: unknown) => void }) {
  return (
    <div className={styles.starRow}>
      {Array.from({ length: max }, (_, i) => i + 1).map((n) => (
        <button
          key={n}
          type="button"
          className={`${styles.starButton} ${value !== null && n <= value ? styles.starFilled : ""}`}
          onClick={() => onChange(n)}
          aria-label={`${n} de ${max}`}
        >
          <Star size={28} color={value !== null && n <= value ? "var(--color-orange-500)" : "var(--color-neutral-300)"} />
        </button>
      ))}
    </div>
  );
}
