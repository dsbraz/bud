import { useField } from "formik";
import { InputHTMLAttributes } from "react";

interface TextFieldProperties extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  name: string;
}

export function TextField({ label, ...props }: TextFieldProperties) {
  const [field, meta] = useField(props);

  return (
    <div className="flex flex-col mb-5">
      <label
        className="text-[14px] font-semibold mb-1"
        htmlFor={props.id || props.name}
      >
        {label}
      </label>

      <input
        className={`border-1 border-solid border-[#EAE3CD] min-h-[40px] rounded-sm p-[10px] ${meta.touched && meta.error ? "error-border" : ""}`}
        {...field}
        {...props}
      />

      {meta.touched && meta.error ? (
        <div className="error-message">{meta.error}</div>
      ) : null}
    </div>
  );
}
