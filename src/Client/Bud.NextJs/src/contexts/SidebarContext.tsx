"use client";

import { createContext, useContext } from "react";

interface SidebarContextValue {
  isMobile: boolean;
  openSidebar: () => void;
}

export const SidebarContext = createContext<SidebarContextValue>({
  isMobile: false,
  openSidebar: () => {},
});

export function useSidebar() {
  return useContext(SidebarContext);
}
