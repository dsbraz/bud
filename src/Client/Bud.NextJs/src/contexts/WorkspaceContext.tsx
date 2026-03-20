"use client";

import Cookies from "js-cookie";
import { createContext, useContext, useState } from "react";

interface WorkspaceContextType {
  currentWorkspace: string | undefined;
  setWorkspace: (name: string | undefined) => void;
}

export const WorkspaceContext = createContext<WorkspaceContextType | undefined>(
  undefined,
);

export function WorkspaceProvider({
  children,
  initialWorkspace,
}: {
  children: React.ReactNode;
  initialWorkspace?: string;
}) {
  const [currentWorkspace, setCurrentWorkspace] = useState<string | undefined>(
    initialWorkspace,
  );

  const setCookieWorkspace = (name: string | undefined) => {
    if (name) {
      Cookies.set("selectedWorkspace", name, {
        secure: true,
        sameSite: "lax",
        path: "/",
        expires: 7,
      });
      setCurrentWorkspace(name);
      return;
    }
    Cookies.remove("selectedWorkspace", {
      path: "/",
    });
    setCurrentWorkspace(name);
  };

  return (
    <WorkspaceContext.Provider
      value={{ currentWorkspace, setWorkspace: setCookieWorkspace }}
    >
      {children}
    </WorkspaceContext.Provider>
  );
}

export function useWorkspace() {
  const context = useContext(WorkspaceContext);

  if (context === undefined) {
    throw new Error("useWorkspace must be used into WorkspaceProvider");
  }

  return context;
}
