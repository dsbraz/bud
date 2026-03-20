"use client";

// TODO: migrate @/types/survey, @/lib/surveys-store, @/pages/surveys/... from bud-2-saas

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type Dispatch,
  type ReactNode,
  type SetStateAction,
} from "react";
import type {
  SurveyType,
  SurveyWizardState,
  WizardQuestion,
  WizardSection,
} from "@/types/survey/survey";
import type { SurveyRendererData } from "@/presentation/surveys/components/SurveyRenderer";
import {
  loadSurveysSnapshot,
  resetSurveysSnapshot,
  saveSurveysSnapshot,
  type SurveyListItemData,
  type SurveyLocalRecord,
  type SurveySubmissionRecord,
  type SurveyTemplateRecord,
  type SurveysStoreSnapshot,
} from "@/lib/tempStorage/surveys-store";
import { useConfigData } from "@/contexts/ConfigDataContext";
import { useActivityData } from "@/contexts/ActivityDataContext";
import {
  createWizardStateFromListItem,
  wizardStateToRendererData,
  wizardStateToSurveyListItem,
} from "@/presentation/surveys/utils/localSurveyAdapters";

type UpsertMode = "draft" | "launch";

interface UpsertTemplateInput {
  templateId?: string;
  type: SurveyType;
  name: string;
  subtitle: string;
  category: "pesquisa" | "ciclo";
  isAnonymous: boolean;
  sections: WizardSection[];
  questions: WizardQuestion[];
}

interface SubmitSurveyResponseInput {
  surveyId: string;
  answers: Record<string, unknown>;
  respondentKey?: string;
  userId?: string;
}

interface SurveysDataContextValue {
  surveys: SurveyListItemData[];
  setSurveys: Dispatch<SetStateAction<SurveyListItemData[]>>;
  templates: SurveyTemplateRecord[];
  getSurveyTemplateById: (templateId: string) => SurveyTemplateRecord | null;
  getSurveyTemplateByType: (type: SurveyType) => SurveyTemplateRecord | null;
  upsertSurveyTemplate: (input: UpsertTemplateInput) => string;
  duplicateSurveyTemplate: (templateId: string) => SurveyTemplateRecord | null;
  deleteSurveyTemplate: (templateId: string) => boolean;
  getSurveyRecordById: (surveyId: string) => SurveyLocalRecord | null;
  getSurveySubmissionsBySurveyId: (
    surveyId: string,
  ) => SurveySubmissionRecord[];
  getWizardStateBySurveyId: (surveyId: string) => SurveyWizardState | null;
  getRendererDataBySurveyId: (surveyId: string) => SurveyRendererData | null;
  upsertSurveyFromWizard: (
    state: SurveyWizardState,
    options: { surveyId?: string; mode: UpsertMode; templateId?: string },
  ) => string;
  duplicateSurvey: (surveyId: string) => SurveyListItemData | null;
  submitSurveyResponse: (input: SubmitSurveyResponseInput) => string | null;
  recordSurveyResponse: (surveyId: string) => void;
  resetToSeed: () => void;
  updatedAt: string;
}

const SurveysDataContext = createContext<SurveysDataContextValue | null>(null);

const RESPONDENT_DEVICE_KEY_STORAGE = "bud.saas.respondent-device-key";

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") return structuredClone(value);
  return JSON.parse(JSON.stringify(value)) as T;
}

function getOrCreateDeviceRespondentKey(): string {
  if (typeof window === "undefined") return "device-server";

  const existing = window.localStorage.getItem(RESPONDENT_DEVICE_KEY_STORAGE);
  if (existing && existing.trim().length > 0) return existing;

  const generated = `device-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
  window.localStorage.setItem(RESPONDENT_DEVICE_KEY_STORAGE, generated);
  return generated;
}

function resolveTemplateForSurvey(
  state: SurveyWizardState,
  templates: SurveyTemplateRecord[],
  explicitTemplateId?: string,
  fallbackTemplateId?: string | null,
): SurveyTemplateRecord | null {
  if (explicitTemplateId) {
    const byId = templates.find(
      (t) => t.id === explicitTemplateId && !t.isArchived,
    );
    if (byId) return byId;
  }

  if (fallbackTemplateId) {
    const byId = templates.find(
      (t) => t.id === fallbackTemplateId && !t.isArchived,
    );
    if (byId) return byId;
  }

  if (state.type) {
    const byType = templates.find(
      (t) => t.type === state.type && !t.isArchived,
    );
    if (byType) return byType;
  }

  return null;
}

export function SurveysDataProvider({ children }: { children: ReactNode }) {
  const { activeOrgId } = useConfigData();
  const { logActivity } = useActivityData();
  const [snapshot, setSnapshot] = useState<SurveysStoreSnapshot>(() =>
    loadSurveysSnapshot(activeOrgId),
  );

  useEffect(() => {
    setSnapshot(loadSurveysSnapshot(activeOrgId));
  }, [activeOrgId]);

  const surveys = useMemo(
    () => snapshot.records.map((record) => record.listItem),
    [snapshot.records],
  );

  const templates = useMemo(() => snapshot.templates, [snapshot.templates]);

  const setSurveys = useCallback<
    Dispatch<SetStateAction<SurveyListItemData[]>>
  >(
    (updater) => {
      setSnapshot((prev) => {
        const previousList = prev.records.map((record) => record.listItem);
        const nextList =
          typeof updater === "function"
            ? (
                updater as (
                  previousState: SurveyListItemData[],
                ) => SurveyListItemData[]
              )(previousList)
            : updater;

        const previousById = new Map(
          prev.records.map((record) => [record.id, record]),
        );
        const now = new Date().toISOString();

        const nextRecords = nextList.map((item) => {
          const existing = previousById.get(item.id);
          if (!existing) {
            return {
              id: item.id,
              listItem: item,
              wizardState: null,
              createdAt: now,
              updatedAt: now,
            };
          }
          return { ...existing, listItem: item, updatedAt: now };
        });

        return saveSurveysSnapshot(
          {
            records: nextRecords,
            templates: prev.templates,
            submissions: prev.submissions,
          },
          activeOrgId,
        );
      });
    },
    [activeOrgId],
  );

  const getSurveyTemplateById = useCallback(
    (templateId: string) => {
      const template = snapshot.templates.find(
        (item) => item.id === templateId,
      );
      return template ? cloneDeep(template) : null;
    },
    [snapshot.templates],
  );

  const getSurveyTemplateByType = useCallback(
    (type: SurveyType) => {
      const template =
        snapshot.templates.find(
          (item) => item.type === type && !item.isArchived,
        ) ??
        snapshot.templates.find((item) => item.type === type) ??
        null;
      return template ? cloneDeep(template) : null;
    },
    [snapshot.templates],
  );

  const upsertSurveyTemplate = useCallback(
    (input: UpsertTemplateInput) => {
      const nowIso = new Date().toISOString();
      const nextTemplateId =
        input.templateId ?? `tpl-${input.type}-${Date.now()}`;

      setSnapshot((prev) => {
        const existing =
          prev.templates.find((t) => t.id === nextTemplateId) ?? null;
        const byType =
          prev.templates.find((t) => t.type === input.type && t.isSystem) ??
          null;
        const target = existing ?? byType;

        const nextTemplate: SurveyTemplateRecord = {
          id: target?.id ?? nextTemplateId,
          type: input.type,
          name: input.name.trim() || "Template sem nome",
          subtitle: input.subtitle.trim(),
          category: input.category,
          isSystem: target?.isSystem ?? false,
          isArchived: false,
          sections: cloneDeep(input.sections),
          questions: cloneDeep(input.questions),
          defaultConfig: {
            isAnonymous: input.isAnonymous,
            recurrence: target?.defaultConfig.recurrence ?? null,
            aiPrefillOkrs: target?.defaultConfig.aiPrefillOkrs ?? false,
            aiPrefillFeedback: target?.defaultConfig.aiPrefillFeedback ?? false,
            aiBiasDetection: target?.defaultConfig.aiBiasDetection ?? false,
          },
          createdAt: target?.createdAt ?? nowIso,
          updatedAt: nowIso,
        };

        const nextTemplates = target
          ? prev.templates.map((t) => (t.id === target.id ? nextTemplate : t))
          : [...prev.templates, nextTemplate];

        return saveSurveysSnapshot(
          {
            records: prev.records,
            templates: nextTemplates,
            submissions: prev.submissions,
          },
          activeOrgId,
        );
      });

      return nextTemplateId;
    },
    [activeOrgId],
  );

  const duplicateSurveyTemplate = useCallback(
    (templateId: string) => {
      const nowIso = new Date().toISOString();
      let duplicated: SurveyTemplateRecord | null = null;

      setSnapshot((prev) => {
        const source = prev.templates.find((t) => t.id === templateId);
        if (!source) return prev;

        const nextTemplate: SurveyTemplateRecord = {
          ...cloneDeep(source),
          id: `tpl-custom-${Date.now()}`,
          type: "custom",
          name: `${source.name} (copia)`,
          isSystem: false,
          isArchived: false,
          createdAt: nowIso,
          updatedAt: nowIso,
        };

        duplicated = nextTemplate;

        return saveSurveysSnapshot(
          {
            records: prev.records,
            templates: [...prev.templates, nextTemplate],
            submissions: prev.submissions,
          },
          activeOrgId,
        );
      });

      return duplicated;
    },
    [activeOrgId],
  );

  const deleteSurveyTemplate = useCallback(
    (templateId: string) => {
      let deleted = false;

      setSnapshot((prev) => {
        const existing = prev.templates.find((t) => t.id === templateId);
        if (!existing || existing.isSystem) return prev;

        deleted = true;
        return saveSurveysSnapshot(
          {
            records: prev.records,
            templates: prev.templates.filter((t) => t.id !== templateId),
            submissions: prev.submissions,
          },
          activeOrgId,
        );
      });

      return deleted;
    },
    [activeOrgId],
  );

  const getSurveyRecordById = useCallback(
    (surveyId: string) => {
      const record = snapshot.records.find((item) => item.id === surveyId);
      return record ? cloneDeep(record) : null;
    },
    [snapshot.records],
  );

  const getSurveySubmissionsBySurveyId = useCallback(
    (surveyId: string) =>
      snapshot.submissions
        .filter((s) => s.surveyId === surveyId)
        .map((s) => cloneDeep(s)),
    [snapshot.submissions],
  );

  const getWizardStateBySurveyId = useCallback(
    (surveyId: string) => {
      const record = snapshot.records.find((item) => item.id === surveyId);
      if (!record) return null;

      if (record.wizardState) return cloneDeep(record.wizardState);

      const template = record.listItem.templateId
        ? snapshot.templates.find(
            (item) => item.id === record.listItem.templateId,
          )
        : snapshot.templates.find((item) => item.type === record.listItem.type);

      return createWizardStateFromListItem(record.listItem, template ?? null);
    },
    [snapshot.records, snapshot.templates],
  );

  const getRendererDataBySurveyId = useCallback(
    (surveyId: string) => {
      const state = getWizardStateBySurveyId(surveyId);
      if (!state) return null;
      return wizardStateToRendererData(state);
    },
    [getWizardStateBySurveyId],
  );

  const upsertSurveyFromWizard = useCallback(
    (
      state: SurveyWizardState,
      options: { surveyId?: string; mode: UpsertMode; templateId?: string },
    ) => {
      const now = new Date();
      const nowIso = now.toISOString();
      const nowDate = nowIso.split("T")[0]!;
      const fallbackId = options.surveyId ?? `survey-${Date.now()}`;
      let persistedId = fallbackId;

      setSnapshot((prev) => {
        const existing =
          prev.records.find((record) => record.id === fallbackId) ?? null;
        persistedId = existing?.id ?? fallbackId;

        const selectedTemplate = resolveTemplateForSurvey(
          state,
          prev.templates,
          options.templateId,
          existing?.listItem.templateId,
        );

        const status =
          options.mode === "draft"
            ? "draft"
            : state.launchOption === "scheduled"
              ? "scheduled"
              : "active";

        const listItem = wizardStateToSurveyListItem(state, {
          surveyId: persistedId,
          templateId:
            selectedTemplate?.id ?? existing?.listItem.templateId ?? null,
          status,
          createdAt: existing?.listItem.createdAt ?? nowDate,
          totalResponses: existing?.listItem.totalResponses ?? 0,
          fallbackType: existing?.listItem.type,
          fallbackCategory: existing?.listItem.category,
        });

        const nextRecord: SurveyLocalRecord = {
          id: persistedId,
          listItem,
          wizardState: cloneDeep(state),
          createdAt: existing?.createdAt ?? nowIso,
          updatedAt: nowIso,
        };

        const nextRecords = existing
          ? prev.records.map((record) =>
              record.id === persistedId ? nextRecord : record,
            )
          : [...prev.records, nextRecord];

        return saveSurveysSnapshot(
          {
            records: nextRecords,
            templates: prev.templates,
            submissions: prev.submissions,
          },
          activeOrgId,
        );
      });

      return persistedId;
    },
    [activeOrgId],
  );

  const duplicateSurvey = useCallback(
    (surveyId: string) => {
      const now = new Date();
      const nowIso = now.toISOString();
      const nowDate = nowIso.split("T")[0]!;
      const duplicateId = `survey-${Date.now()}`;
      let duplicated: SurveyListItemData | null = null;

      setSnapshot((prev) => {
        const source = prev.records.find((record) => record.id === surveyId);
        if (!source) return prev;

        const listItem: SurveyListItemData = {
          ...source.listItem,
          id: duplicateId,
          name: `${source.listItem.name} (copia)`,
          status: "draft",
          totalResponses: 0,
          completionRate: 0,
          createdAt: nowDate,
        };

        const duplicateRecord: SurveyLocalRecord = {
          id: duplicateId,
          listItem,
          wizardState: source.wizardState
            ? { ...cloneDeep(source.wizardState), name: listItem.name }
            : null,
          createdAt: nowIso,
          updatedAt: nowIso,
        };

        duplicated = listItem;

        return saveSurveysSnapshot(
          {
            records: [...prev.records, duplicateRecord],
            templates: prev.templates,
            submissions: prev.submissions,
          },
          activeOrgId,
        );
      });

      return duplicated;
    },
    [activeOrgId],
  );

  const submitSurveyResponse = useCallback(
    (input: SubmitSurveyResponseInput) => {
      let submissionId: string | null = null;

      setSnapshot((prev) => {
        const targetSurvey = prev.records.find(
          (record) => record.id === input.surveyId,
        );
        if (!targetSurvey) return prev;

        const nowIso = new Date().toISOString();
        const deviceKey =
          input.respondentKey ?? getOrCreateDeviceRespondentKey();
        const respondentKey = `survey:${input.surveyId}:respondent:${deviceKey}`;
        const existingSubmission = prev.submissions.find(
          (s) =>
            s.surveyId === input.surveyId && s.respondentKey === respondentKey,
        );

        let nextSubmissions: SurveySubmissionRecord[];
        if (existingSubmission) {
          submissionId = existingSubmission.id;
          nextSubmissions = prev.submissions.map((s) =>
            s.id === existingSubmission.id
              ? {
                  ...s,
                  answers: cloneDeep(input.answers),
                  updatedAt: nowIso,
                  submittedAt: nowIso,
                }
              : s,
          );
        } else {
          const nextSubmission: SurveySubmissionRecord = {
            id: `submission-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
            surveyId: input.surveyId,
            respondentKey,
            answers: cloneDeep(input.answers),
            createdAt: nowIso,
            updatedAt: nowIso,
            submittedAt: nowIso,
          };
          submissionId = nextSubmission.id;
          nextSubmissions = [...prev.submissions, nextSubmission];
        }

        const surveySubmissions = nextSubmissions.filter(
          (s) => s.surveyId === input.surveyId,
        );
        const totalResponses = surveySubmissions.length;

        const nextRecords = prev.records.map((record) => {
          if (record.id !== input.surveyId) return record;

          const recipients = Math.max(1, record.listItem.totalRecipients);
          const completionRate = Math.min(
            100,
            Math.round((totalResponses / recipients) * 100),
          );

          return {
            ...record,
            listItem: {
              ...record.listItem,
              totalResponses,
              completionRate,
              status:
                record.listItem.status === "draft" ||
                record.listItem.status === "scheduled"
                  ? "active"
                  : record.listItem.status,
            },
            updatedAt: nowIso,
          };
        });

        return saveSurveysSnapshot(
          {
            records: nextRecords,
            templates: prev.templates,
            submissions: nextSubmissions,
          },
          activeOrgId,
        );
      });

      if (submissionId && input.userId) {
        logActivity({
          userId: input.userId,
          type: "survey_complete",
          entityId: input.surveyId,
          entityType: "survey",
          metadata: { surveyId: input.surveyId, submissionId },
        });
      }

      return submissionId;
    },
    [activeOrgId, logActivity],
  );

  const recordSurveyResponse = useCallback(
    (surveyId: string) => {
      void submitSurveyResponse({ surveyId, answers: {} });
    },
    [submitSurveyResponse],
  );

  const resetToSeed = useCallback(() => {
    setSnapshot(resetSurveysSnapshot(activeOrgId));
  }, [activeOrgId]);

  const value = useMemo<SurveysDataContextValue>(
    () => ({
      surveys,
      setSurveys,
      templates,
      getSurveyTemplateById,
      getSurveyTemplateByType,
      upsertSurveyTemplate,
      duplicateSurveyTemplate,
      deleteSurveyTemplate,
      getSurveyRecordById,
      getSurveySubmissionsBySurveyId,
      getWizardStateBySurveyId,
      getRendererDataBySurveyId,
      upsertSurveyFromWizard,
      duplicateSurvey,
      submitSurveyResponse,
      recordSurveyResponse,
      resetToSeed,
      updatedAt: snapshot.updatedAt,
    }),
    [
      surveys,
      setSurveys,
      templates,
      getSurveyTemplateById,
      getSurveyTemplateByType,
      upsertSurveyTemplate,
      duplicateSurveyTemplate,
      deleteSurveyTemplate,
      getSurveyRecordById,
      getSurveySubmissionsBySurveyId,
      getWizardStateBySurveyId,
      getRendererDataBySurveyId,
      upsertSurveyFromWizard,
      duplicateSurvey,
      submitSurveyResponse,
      recordSurveyResponse,
      resetToSeed,
      snapshot.updatedAt,
    ],
  );

  return (
    <SurveysDataContext.Provider value={value}>
      {children}
    </SurveysDataContext.Provider>
  );
}

export function useSurveysData() {
  const ctx = useContext(SurveysDataContext);
  if (!ctx) {
    throw new Error("useSurveysData must be used within SurveysDataProvider");
  }
  return ctx;
}
