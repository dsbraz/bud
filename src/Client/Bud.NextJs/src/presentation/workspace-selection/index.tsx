"use server";

import Link from "next/link";
import { getTranslations } from "next-intl/server";

import { WorkspaceSummary } from "@/types/workspace/WorkspaceSummaryType";
import { WorkspaceListComponent } from "./components/WorkspaceListComponent";
import { WorkspaceUserSection } from "./components/WorkspaceUserSection";

export async function WorkspaceSelection() {
  const t = await getTranslations("WorkspaceSelection");

  const UserMockWorkspaces: WorkspaceSummary[] = [
    { id: "1", name: "bud tech", members: 3 },
    { id: "2", name: "brq px", members: 90 },
  ];

  return (
    <div className="mt-6">
      <div className="w-full bg-[#ffffff] p-[2rem] md:px-[4rem] flex flex-col rounded-lg">
        <h1 className="font-semibold text-2xl text-center tracking-[-0.005em] leading-[1.1] text-[#0A0A0A]">
          {t("mainTitle")}
        </h1>
        <WorkspaceListComponent values={UserMockWorkspaces} />
        <div className="w-full solid border-t-1 border-[#EAE3CD] mb-8" />
        <Link
          href="/workspace/creation"
          className="bg-[#F9F7F0] border-[#EAE3CD] text-sm border-1 border-solid text-center text-[#0A0A0A] py-[0.563rem] rounded-md"
        >
          {t("redirectCreation")}
        </Link>
      </div>
      <WorkspaceUserSection />
    </div>
  );
}
