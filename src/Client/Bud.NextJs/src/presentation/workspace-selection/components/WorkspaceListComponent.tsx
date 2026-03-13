"use client";

import { WorkspaceSummary } from "@/types/workspace/WorkspaceSummaryType";
import { WorkspaceAvatar } from "./WorkspaceAvatarComponent";
import { useWorkspace } from "@/providers/workspace-provider";
import { redirect } from "next/navigation";

interface WorkspaceListProperties {
  values: WorkspaceSummary[];
}

export function WorkspaceListComponent({ values }: WorkspaceListProperties) {
  const { currentWorkspace, setWorkspace } = useWorkspace();

  if (currentWorkspace) {
    redirect("/");
  }

  const setWorkspaceAndRedirect = (workspace: string) => {
    setWorkspace(workspace);
  };
  return (
    <div>
      {values.map((workspace) => {
        return (
          <WorkspaceAvatar
            key={workspace.id}
            workspace={workspace}
            onClick={setWorkspaceAndRedirect}
          />
        );
      })}
    </div>
  );
}
