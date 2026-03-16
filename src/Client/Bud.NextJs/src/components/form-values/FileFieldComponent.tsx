import { useField, useFormikContext } from "formik";
import { ChangeEvent, useRef } from "react";
import { Button } from "../ui/button";
import { useTranslations } from "next-intl";

interface FileFieldProperties {
  label: string;
  name: string;
  accept?: string;
}

export function FileField({ label, name, accept }: FileFieldProperties) {
  const t = useTranslations("FileInput");

  const { setFieldValue } = useFormikContext();
  const [, meta] = useField(name);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.currentTarget.files?.[0];
    if (file) {
      setFieldValue(name, file);
    }
  };

  return (
    <div className="flex flex-col mb-5">
      <label className="text-[14px] font-semibold mb-1">{label}</label>

      <div className="flex flex-col border-1 border-dotted border-[#737373] rounded-sm p-[24px] w-full">
        <p className="text-center mb-2 text-[#737373] text-[14px]">
          {t("fileDescription")}
        </p>
        <input
          className="hidden"
          type="file"
          name={name}
          accept={accept}
          onChange={handleChange}
          ref={fileInputRef}
        />
        <Button
          variant="outline"
          className="bg-[#F9F7F0] border-1 border-solid border-[#EAE3CD]"
        >
          {t("inputText")}
        </Button>
      </div>

      {meta.touched && meta.error ? (
        <div className="error-message">{meta.error}</div>
      ) : null}
    </div>
  );
}
