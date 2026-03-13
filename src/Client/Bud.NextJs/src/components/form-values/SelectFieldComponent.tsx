import { useField } from "formik";
import { useTranslations } from "next-intl";
import { SelectHTMLAttributes } from "react";

interface Option {
  value: string;
  label: string;
}

interface SelectFieldProperties
  extends SelectHTMLAttributes<HTMLSelectElement> {
  label: string;
  name: string;
  options: Option[];
}

export function SelectField({
  label,
  options,
  ...props
}: SelectFieldProperties) {
  const t = useTranslations("SelectInput");

  const [field, meta] = useField(props);

  return (
    <div className="flex flex-col mb-5">
      <label
        className="text-[14px] font-semibold mb-1"
        htmlFor={props.id || props.name}
      >
        {label}
      </label>

      <select
        {...field}
        {...props}
        className={`border-1 border-solid border-[#EAE3CD] min-h-[40px] rounded-sm p-[10px] ${meta.touched && meta.error ? "error-border" : ""}`}
      >
        <option value="">{t("defaultSelect")}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>

      {meta.touched && meta.error ? (
        <div className="error-message">{meta.error}</div>
      ) : null}
    </div>
  );
}
