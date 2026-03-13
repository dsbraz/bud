"use client";

import { ComboboxComponent } from "@/components/ComboboxComponent";
import { useTranslations } from "next-intl";
import { changeLanguage } from "./utils/changeLanguage";
import { useRouter } from "next/navigation";

export function LanguageSelection() {
  const t = useTranslations("TranslationInput");
  const router = useRouter();

  const values = [
    {
      value: "pt",
      label: t("portuguese"),
    },
    {
      value: "en",
      label: t("english"),
    },
  ];
  return (
    <header className="h-16 flex items-center justify-end md:justify-between py-3">
      <ComboboxComponent
        values={values}
        placeholder={t("LanguageSelection")}
        defaultValue="pt"
        required={true}
        onChange={(selected) => {
          changeLanguage(selected);
          router.refresh();
        }}
      />
    </header>
  );
}
