"use client";

import { createContext, useContext } from "react";

interface AssistantContextValue {
  open: boolean;
  toggle: () => void;
}

export const AssistantContext = createContext<AssistantContextValue>({
  open: false,
  toggle: () => {},
});

export function useAssistant() {
  return useContext(AssistantContext);
}
