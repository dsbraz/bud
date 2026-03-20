"use client";

// TODO: migrate @/lib/integrations-store from bud-2-saas

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  loadIntegrationsSnapshot,
  saveIntegrationsSnapshot,
  resetIntegrationsSnapshot,
  type IntegrationRecord,
  type IntegrationStatus,
  type IntegrationsStoreSnapshot,
} from "@/lib/tempStorage/integrations-store";
import { useConfigData } from "@/contexts/ConfigDataContext";

interface IntegrationsDataContextValue {
  integrations: IntegrationRecord[];
  connectedIntegrations: IntegrationRecord[];
  disconnectedIntegrations: IntegrationRecord[];
  getIntegrationById: (id: string) => IntegrationRecord | null;
  connectIntegration: (id: string, config?: Record<string, unknown>) => void;
  disconnectIntegration: (id: string) => void;
  updateIntegrationConfig: (
    id: string,
    config: Record<string, unknown>,
  ) => void;
  toggleIntegrationEnabled: (id: string) => void;
  syncIntegration: (id: string) => void;
  resetToSeed: () => void;
  updatedAt: string;
}

const IntegrationsDataContext =
  createContext<IntegrationsDataContextValue | null>(null);

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") return structuredClone(value);
  return JSON.parse(JSON.stringify(value)) as T;
}

export function IntegrationsDataProvider({
  children,
}: {
  children: ReactNode;
}) {
  const { activeOrgId } = useConfigData();
  const [snapshot, setSnapshot] = useState<IntegrationsStoreSnapshot>(() =>
    loadIntegrationsSnapshot(activeOrgId),
  );

  useEffect(() => {
    setSnapshot(loadIntegrationsSnapshot(activeOrgId));
  }, [activeOrgId]);

  useEffect(() => {
    saveIntegrationsSnapshot(snapshot, activeOrgId);
  }, [snapshot, activeOrgId]);

  const integrations = useMemo(() => snapshot.integrations, [snapshot]);

  const connectedIntegrations = useMemo(
    () => integrations.filter((i) => i.status === "connected"),
    [integrations],
  );

  const disconnectedIntegrations = useMemo(
    () => integrations.filter((i) => i.status !== "connected"),
    [integrations],
  );

  const getIntegrationById = useCallback(
    (id: string): IntegrationRecord | null =>
      integrations.find((i) => i.id === id) ?? null,
    [integrations],
  );

  const connectIntegration = useCallback(
    (id: string, config?: Record<string, unknown>) => {
      setSnapshot((prev) => {
        const integrationsCopy = cloneDeep(prev.integrations);
        const index = integrationsCopy.findIndex((i) => i.id === id);
        if (index === -1) return prev;

        integrationsCopy[index] = {
          ...integrationsCopy[index]!,
          status: "connected" as IntegrationStatus,
          enabled: true,
          connectedAt: new Date().toISOString(),
          lastSync: new Date().toISOString(),
          config: config ?? integrationsCopy[index]!.config,
        };

        return {
          ...prev,
          integrations: integrationsCopy,
          updatedAt: new Date().toISOString(),
        };
      });
    },
    [],
  );

  const disconnectIntegration = useCallback((id: string) => {
    setSnapshot((prev) => {
      const integrationsCopy = cloneDeep(prev.integrations);
      const index = integrationsCopy.findIndex((i) => i.id === id);
      if (index === -1) return prev;

      integrationsCopy[index] = {
        ...integrationsCopy[index]!,
        status: "disconnected" as IntegrationStatus,
        enabled: false,
        connectedAt: null,
        lastSync: null,
        config: {},
      };

      return {
        ...prev,
        integrations: integrationsCopy,
        updatedAt: new Date().toISOString(),
      };
    });
  }, []);

  const updateIntegrationConfig = useCallback(
    (id: string, config: Record<string, unknown>) => {
      setSnapshot((prev) => {
        const integrationsCopy = cloneDeep(prev.integrations);
        const index = integrationsCopy.findIndex((i) => i.id === id);
        if (index === -1) return prev;

        integrationsCopy[index] = {
          ...integrationsCopy[index]!,
          config: { ...integrationsCopy[index]!.config, ...config },
        };

        return {
          ...prev,
          integrations: integrationsCopy,
          updatedAt: new Date().toISOString(),
        };
      });
    },
    [],
  );

  const toggleIntegrationEnabled = useCallback((id: string) => {
    setSnapshot((prev) => {
      const integrationsCopy = cloneDeep(prev.integrations);
      const index = integrationsCopy.findIndex((i) => i.id === id);
      if (index === -1) return prev;

      integrationsCopy[index] = {
        ...integrationsCopy[index]!,
        enabled: !integrationsCopy[index]!.enabled,
      };

      return {
        ...prev,
        integrations: integrationsCopy,
        updatedAt: new Date().toISOString(),
      };
    });
  }, []);

  const syncIntegration = useCallback((id: string) => {
    setSnapshot((prev) => {
      const integrationsCopy = cloneDeep(prev.integrations);
      const index = integrationsCopy.findIndex((i) => i.id === id);
      if (index === -1) return prev;

      integrationsCopy[index] = {
        ...integrationsCopy[index]!,
        lastSync: new Date().toISOString(),
      };

      return {
        ...prev,
        integrations: integrationsCopy,
        updatedAt: new Date().toISOString(),
      };
    });
  }, []);

  const resetToSeed = useCallback(() => {
    setSnapshot(resetIntegrationsSnapshot(activeOrgId));
  }, [activeOrgId]);

  const value = useMemo<IntegrationsDataContextValue>(
    () => ({
      integrations,
      connectedIntegrations,
      disconnectedIntegrations,
      getIntegrationById,
      connectIntegration,
      disconnectIntegration,
      updateIntegrationConfig,
      toggleIntegrationEnabled,
      syncIntegration,
      resetToSeed,
      updatedAt: snapshot.updatedAt,
    }),
    [
      integrations,
      connectedIntegrations,
      disconnectedIntegrations,
      getIntegrationById,
      connectIntegration,
      disconnectIntegration,
      updateIntegrationConfig,
      toggleIntegrationEnabled,
      syncIntegration,
      resetToSeed,
      snapshot.updatedAt,
    ],
  );

  return (
    <IntegrationsDataContext.Provider value={value}>
      {children}
    </IntegrationsDataContext.Provider>
  );
}

export function useIntegrationsData(): IntegrationsDataContextValue {
  const context = useContext(IntegrationsDataContext);
  if (!context) {
    throw new Error(
      "useIntegrationsData must be used within IntegrationsDataProvider",
    );
  }
  return context;
}
