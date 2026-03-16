"use client";

import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { useUser } from "@auth0/nextjs-auth0";

export function WorkspaceUserSection() {
  const t = useTranslations("WorkspaceSelection");

  const { user } = useUser();
  return (
    <p className="flex flex-col items-center mt-5 font-semibold">
      {t("userConnected", { email: user?.email || "" })}
      <Button
        className="text-base font-semibold text-[#FA4405]"
        variant="ghost"
      >
        {t("logout")}
      </Button>
    </p>
  );
}
