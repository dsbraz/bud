"use client";

import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { WorkspaceSummary } from "@/types/workspace/WorkspaceSummaryType";
import { generateAcronymAndColor } from "@/lib/generate-acronym-and-color";
import { useTranslations } from "next-intl";

interface WorkspaceAvatarProperties {
  workspace: WorkspaceSummary;
  onClick: (name: string) => void;
}

export function WorkspaceAvatar({
  workspace,
  onClick,
}: WorkspaceAvatarProperties) {
  const t = useTranslations("WorkspaceSelection.WorkspaceAvatar");
  const acronymAndColor = generateAcronymAndColor(workspace.name);
  return (
    <div className="flex my-8 justify-between">
      <div className="flex">
        <Avatar className="h-[40px] w-[40px] rounded-lg">
          <AvatarImage src="" className="rounded-lg" />
          <AvatarFallback
            style={{ backgroundColor: acronymAndColor.hex }}
            className="rounded-lg text-white"
          >
            {acronymAndColor.acronym}
          </AvatarFallback>
        </Avatar>
        <div className="ml-3">
          <h3 className="text-base font-semibold leading-[1.05] tracking-normal">
            {workspace.name}
          </h3>
          <p className="text-base font-normal leading-[1.05] tracking-normal">
            {t("membersCount", { count: workspace.members })}
          </p>
        </div>
      </div>
      <div>
        <Button
          className="py-[0.563rem] px-[0.5rem] bg-[#F9F7F0] border-[#EAE3CD]"
          variant="outline"
          onClick={() => onClick(workspace.name)}
        >
          {t("submit")}
        </Button>
      </div>
    </div>
  );
}
