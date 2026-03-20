"use client";

// TODO: migrate @/types (Team, TeamMember, UserStatus) and @/lib/people-store from bud-2-saas

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
import type { Team, TeamMember, UserStatus } from "@/types";
import { useConfigData } from "@/contexts/ConfigDataContext";
import {
  createTeamIdFromName,
  loadPeopleSnapshot,
  resetPeopleSnapshot,
  savePeopleSnapshot,
  type PeopleStoreSnapshot,
  type PeopleUserRecord,
} from "@/lib/tempStorage/people-store";

export interface PeopleUserView extends PeopleUserRecord {
  teams: string[];
}

export interface OrgPersonView {
  id: string;
  firstName: string;
  lastName: string;
  jobTitle: string | null;
  initials: string | null;
  managerId: string | null;
  status: UserStatus;
  teams: string[];
}

export interface OwnerOption {
  id: string;
  label: string;
  initials: string;
}

interface PeopleDataContextValue {
  users: PeopleUserView[];
  teams: Team[];
  orgPeople: OrgPersonView[];
  teamOptions: Array<{ id: string; label: string }>;
  teamNameOptions: string[];
  ownerOptions: OwnerOption[];
  mentionPeople: OwnerOption[];
  currentUser: OwnerOption | null;
  currentUserId: string | null;
  setCurrentUserId: (userId: string) => void;
  setUsers: Dispatch<SetStateAction<PeopleUserView[]>>;
  setTeams: Dispatch<SetStateAction<Team[]>>;
  setOrgPeople: Dispatch<SetStateAction<OrgPersonView[]>>;
  resolveUserId: (legacyOrCanonicalId: string) => string;
  resolveTeamId: (legacyOrCanonicalId: string) => string;
  getTeamNameById: (teamId: string) => string | null;
  getTeamIdByName: (teamName: string) => string | null;
  getUserDisplayName: (userId: string) => string;
  resetToSeed: () => void;
  updatedAt: string;
}

const PeopleDataContext = createContext<PeopleDataContextValue | null>(null);

function cloneDeep<T>(value: T): T {
  if (typeof structuredClone === "function") return structuredClone(value);
  return JSON.parse(JSON.stringify(value)) as T;
}

function normalizeKey(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}

function toTeamNameById(snapshot: PeopleStoreSnapshot): Map<string, string> {
  return new Map(
    Object.values(snapshot.teamsById).map((team) => [team.id, team.name]),
  );
}

function membersByTeamId(
  snapshot: PeopleStoreSnapshot,
): Map<string, TeamMember[]> {
  const map = new Map<string, TeamMember[]>();
  snapshot.teamMembers.forEach((member) => {
    const bucket = map.get(member.teamId) ?? [];
    bucket.push(member);
    map.set(member.teamId, bucket);
  });
  return map;
}

function teamNamesByUserId(
  snapshot: PeopleStoreSnapshot,
): Map<string, string[]> {
  const teamNameById = toTeamNameById(snapshot);
  const result = new Map<string, string[]>();

  snapshot.teamMembers.forEach((member) => {
    const teamName = teamNameById.get(member.teamId);
    if (!teamName) return;
    const current = result.get(member.userId) ?? [];
    if (!current.includes(teamName)) {
      result.set(member.userId, [...current, teamName]);
    }
  });

  return result;
}

function ensureCurrentUser(snapshot: PeopleStoreSnapshot): string {
  if (snapshot.usersById[snapshot.currentUserId]) return snapshot.currentUserId;
  const activeUser = Object.values(snapshot.usersById).find(
    (user) => user.status === "active",
  );
  return activeUser?.id ?? Object.keys(snapshot.usersById)[0] ?? "";
}

function buildUsersView(snapshot: PeopleStoreSnapshot): PeopleUserView[] {
  const teamsByUser = teamNamesByUserId(snapshot);

  return Object.values(snapshot.usersById)
    .map((user) => ({
      ...cloneDeep(user),
      teams: cloneDeep(teamsByUser.get(user.id) ?? []),
    }))
    .sort((a, b) =>
      `${a.firstName} ${a.lastName}`.localeCompare(
        `${b.firstName} ${b.lastName}`,
        "pt-BR",
      ),
    );
}

function buildTeamsView(snapshot: PeopleStoreSnapshot): Team[] {
  const memberMap = membersByTeamId(snapshot);

  return Object.values(snapshot.teamsById)
    .map((team) => {
      const members = (memberMap.get(team.id) ?? []).reduce<TeamMember[]>(
        (acc, member) => {
          const user = snapshot.usersById[member.userId];
          if (!user) return acc;

          acc.push({
            ...cloneDeep(member),
            user: {
              id: user.id,
              firstName: user.firstName,
              lastName: user.lastName,
              initials: user.initials,
              jobTitle: user.jobTitle,
              avatarUrl: user.avatarUrl,
            },
          });

          return acc;
        },
        [],
      );

      return { ...cloneDeep(team), members };
    })
    .sort((a, b) => a.name.localeCompare(b.name, "pt-BR"));
}

function buildTeamMembersFromUsers(
  users: PeopleUserView[],
  teamsById: Record<string, Team>,
  previousTeamMembers: TeamMember[],
): { teamsById: Record<string, Team>; teamMembers: TeamMember[] } {
  const nextTeamsById = cloneDeep(teamsById);
  const existingMembershipByKey = new Map(
    previousTeamMembers.map((member) => [
      `${member.teamId}:${member.userId}`,
      member,
    ]),
  );

  const teamIdByName = new Map(
    Object.values(nextTeamsById).map((team) => [
      normalizeKey(team.name),
      team.id,
    ]),
  );

  const nextMembers: TeamMember[] = [];

  users.forEach((user) => {
    user.teams.forEach((teamName) => {
      const normalizedName = normalizeKey(teamName);
      if (!normalizedName) return;

      let teamId = teamIdByName.get(normalizedName) ?? null;
      if (!teamId) {
        teamId = createTeamIdFromName(teamName);
        const now = new Date().toISOString();
        nextTeamsById[teamId] = {
          id: teamId,
          orgId: user.orgId,
          name: teamName,
          description: null,
          color: "neutral",
          leaderId: null,
          parentTeamId: null,
          status: "active",
          createdAt: now,
          updatedAt: now,
          deletedAt: null,
        };
        teamIdByName.set(normalizedName, teamId);
      }

      const previous = existingMembershipByKey.get(`${teamId}:${user.id}`);
      nextMembers.push({
        teamId,
        userId: user.id,
        roleInTeam: previous?.roleInTeam ?? "member",
        joinedAt: previous?.joinedAt ?? new Date().toISOString(),
      });
    });
  });

  return { teamsById: nextTeamsById, teamMembers: nextMembers };
}

function createOwnerOption(user: PeopleUserView): OwnerOption {
  return {
    id: user.id,
    label: `${user.firstName} ${user.lastName}`,
    initials:
      user.initials ??
      `${user.firstName[0] ?? ""}${user.lastName[0] ?? ""}`.toUpperCase(),
  };
}

export function PeopleDataProvider({ children }: { children: ReactNode }) {
  const { activeOrgId } = useConfigData();
  const [snapshot, setSnapshot] = useState<PeopleStoreSnapshot>(() =>
    loadPeopleSnapshot(activeOrgId),
  );

  useEffect(() => {
    setSnapshot(loadPeopleSnapshot(activeOrgId));
  }, [activeOrgId]);

  const users = useMemo(() => buildUsersView(snapshot), [snapshot]);
  const teams = useMemo(() => buildTeamsView(snapshot), [snapshot]);

  const setUsers = useCallback<Dispatch<SetStateAction<PeopleUserView[]>>>(
    (updater) => {
      setSnapshot((prev) => {
        const prevUsers = buildUsersView(prev);
        const nextUsers =
          typeof updater === "function"
            ? (updater as (value: PeopleUserView[]) => PeopleUserView[])(
                prevUsers,
              )
            : updater;

        const nextUsersById: Record<string, PeopleUserRecord> =
          Object.fromEntries(
            nextUsers.map((user) => {
              const previous = prev.usersById[user.id];
              const userRecord: PeopleUserRecord = {
                ...cloneDeep(previous ?? user),
                ...cloneDeep(user),
                roleType: user.roleType,
                teams: undefined,
              } as unknown as PeopleUserRecord;

              const { teams: _, ...withoutTeams } =
                userRecord as PeopleUserView;
              return [user.id, withoutTeams as PeopleUserRecord];
            }),
          );

        const memberships = buildTeamMembersFromUsers(
          nextUsers,
          prev.teamsById,
          prev.teamMembers,
        );

        const nextCurrentUserId = nextUsersById[prev.currentUserId]
          ? prev.currentUserId
          : (nextUsers[0]?.id ?? "");

        return savePeopleSnapshot(
          {
            currentUserId: nextCurrentUserId,
            usersById: nextUsersById,
            teamsById: memberships.teamsById,
            teamMembers: memberships.teamMembers,
            legacyUserIdAliases: prev.legacyUserIdAliases,
            legacyTeamIdAliases: prev.legacyTeamIdAliases,
          },
          activeOrgId,
        );
      });
    },
    [activeOrgId],
  );

  const setTeams = useCallback<Dispatch<SetStateAction<Team[]>>>(
    (updater) => {
      setSnapshot((prev) => {
        const prevTeams = buildTeamsView(prev);
        const nextTeams =
          typeof updater === "function"
            ? (updater as (value: Team[]) => Team[])(prevTeams)
            : updater;

        const nextTeamsById: Record<string, Team> = Object.fromEntries(
          nextTeams.map((team) => {
            const { members, ...teamWithoutMembers } = cloneDeep(team);
            return [team.id, teamWithoutMembers];
          }),
        );

        const previousMembershipByKey = new Map(
          prev.teamMembers.map((member) => [
            `${member.teamId}:${member.userId}`,
            member,
          ]),
        );

        const nextTeamMembers: TeamMember[] = [];

        nextTeams.forEach((team) => {
          (team.members ?? []).forEach((member) => {
            if (!prev.usersById[member.userId]) return;
            const previous = previousMembershipByKey.get(
              `${team.id}:${member.userId}`,
            );
            nextTeamMembers.push({
              teamId: team.id,
              userId: member.userId,
              roleInTeam: member.roleInTeam,
              joinedAt:
                previous?.joinedAt ??
                member.joinedAt ??
                new Date().toISOString(),
            });
          });
        });

        return savePeopleSnapshot(
          {
            currentUserId: ensureCurrentUser(prev),
            usersById: prev.usersById,
            teamsById: nextTeamsById,
            teamMembers: nextTeamMembers,
            legacyUserIdAliases: prev.legacyUserIdAliases,
            legacyTeamIdAliases: prev.legacyTeamIdAliases,
          },
          activeOrgId,
        );
      });
    },
    [activeOrgId],
  );

  const orgPeople = useMemo<OrgPersonView[]>(
    () =>
      users.map((user) => ({
        id: user.id,
        firstName: user.firstName,
        lastName: user.lastName,
        jobTitle: user.jobTitle,
        initials: user.initials,
        managerId: user.managerId,
        status: user.status,
        teams: user.teams,
      })),
    [users],
  );

  const setOrgPeople = useCallback<Dispatch<SetStateAction<OrgPersonView[]>>>(
    (updater) => {
      setSnapshot((prev) => {
        const prevPeople = buildUsersView(prev).map((user) => ({
          id: user.id,
          firstName: user.firstName,
          lastName: user.lastName,
          jobTitle: user.jobTitle,
          initials: user.initials,
          managerId: user.managerId,
          status: user.status,
          teams: user.teams,
        }));

        const nextPeople =
          typeof updater === "function"
            ? (updater as (value: OrgPersonView[]) => OrgPersonView[])(
                prevPeople,
              )
            : updater;

        const prevUsersView = buildUsersView(prev);
        const prevUserById = new Map(
          prevUsersView.map((user) => [user.id, user]),
        );

        const nextUsersView: PeopleUserView[] = nextPeople.map((person) => {
          const previous = prevUserById.get(person.id);
          return {
            ...(previous ?? (prev.usersById[person.id] as PeopleUserView)),
            id: person.id,
            firstName: person.firstName,
            lastName: person.lastName,
            jobTitle: person.jobTitle,
            initials: person.initials,
            managerId: person.managerId,
            status: person.status,
            teams: cloneDeep(person.teams),
            roleType:
              previous?.roleType ??
              prev.usersById[person.id]?.roleType ??
              "colaborador",
            orgId:
              previous?.orgId ??
              prev.usersById[person.id]?.orgId ??
              activeOrgId,
            email:
              previous?.email ??
              prev.usersById[person.id]?.email ??
              `${normalizeKey(person.firstName)}.${normalizeKey(person.lastName)}@acme.com`,
            nickname:
              previous?.nickname ?? prev.usersById[person.id]?.nickname ?? null,
            avatarUrl:
              previous?.avatarUrl ??
              prev.usersById[person.id]?.avatarUrl ??
              null,
            birthDate:
              previous?.birthDate ??
              prev.usersById[person.id]?.birthDate ??
              null,
            gender:
              previous?.gender ?? prev.usersById[person.id]?.gender ?? null,
            language:
              previous?.language ??
              prev.usersById[person.id]?.language ??
              "pt-br",
            phone: previous?.phone ?? prev.usersById[person.id]?.phone ?? null,
            invitedAt:
              previous?.invitedAt ??
              prev.usersById[person.id]?.invitedAt ??
              null,
            activatedAt:
              previous?.activatedAt ??
              prev.usersById[person.id]?.activatedAt ??
              null,
            lastLoginAt:
              previous?.lastLoginAt ??
              prev.usersById[person.id]?.lastLoginAt ??
              null,
            authProvider:
              previous?.authProvider ??
              prev.usersById[person.id]?.authProvider ??
              "email",
            authProviderId:
              previous?.authProviderId ??
              prev.usersById[person.id]?.authProviderId ??
              null,
            createdAt:
              previous?.createdAt ??
              prev.usersById[person.id]?.createdAt ??
              new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            deletedAt:
              previous?.deletedAt ??
              prev.usersById[person.id]?.deletedAt ??
              null,
          };
        });

        const nextUsersById: Record<string, PeopleUserRecord> =
          Object.fromEntries(
            nextUsersView.map((user) => {
              const { teams: _, ...userRecord } = user;
              return [user.id, userRecord as PeopleUserRecord];
            }),
          );

        const memberships = buildTeamMembersFromUsers(
          nextUsersView,
          prev.teamsById,
          prev.teamMembers,
        );

        return savePeopleSnapshot(
          {
            currentUserId: nextUsersById[prev.currentUserId]
              ? prev.currentUserId
              : (nextUsersView[0]?.id ?? ""),
            usersById: nextUsersById,
            teamsById: memberships.teamsById,
            teamMembers: memberships.teamMembers,
            legacyUserIdAliases: prev.legacyUserIdAliases,
            legacyTeamIdAliases: prev.legacyTeamIdAliases,
          },
          activeOrgId,
        );
      });
    },
    [activeOrgId],
  );

  const teamOptions = useMemo(
    () =>
      Object.values(snapshot.teamsById)
        .filter((team) => team.status === "active")
        .sort((a, b) => a.name.localeCompare(b.name, "pt-BR"))
        .map((team) => ({ id: team.id, label: team.name })),
    [snapshot.teamsById],
  );

  const teamNameOptions = useMemo(
    () => teamOptions.map((option) => option.label),
    [teamOptions],
  );

  const ownerOptions = useMemo(
    () =>
      users
        .filter((user) => user.status === "active" || user.status === "invited")
        .map(createOwnerOption),
    [users],
  );

  const mentionPeople = useMemo(() => ownerOptions, [ownerOptions]);

  const resolveUserId = useCallback(
    (legacyOrCanonicalId: string) => {
      if (snapshot.usersById[legacyOrCanonicalId]) return legacyOrCanonicalId;
      return (
        snapshot.legacyUserIdAliases[legacyOrCanonicalId] ?? legacyOrCanonicalId
      );
    },
    [snapshot.usersById, snapshot.legacyUserIdAliases],
  );

  const resolveTeamId = useCallback(
    (legacyOrCanonicalId: string) => {
      if (snapshot.teamsById[legacyOrCanonicalId]) return legacyOrCanonicalId;
      return (
        snapshot.legacyTeamIdAliases[legacyOrCanonicalId] ?? legacyOrCanonicalId
      );
    },
    [snapshot.teamsById, snapshot.legacyTeamIdAliases],
  );

  const getTeamNameById = useCallback(
    (teamId: string) => {
      const resolved = resolveTeamId(teamId);
      return snapshot.teamsById[resolved]?.name ?? null;
    },
    [resolveTeamId, snapshot.teamsById],
  );

  const getTeamIdByName = useCallback(
    (teamName: string) => {
      const normalized = normalizeKey(teamName);
      const match = Object.values(snapshot.teamsById).find(
        (team) => normalizeKey(team.name) === normalized,
      );
      return match?.id ?? null;
    },
    [snapshot.teamsById],
  );

  const getUserDisplayName = useCallback(
    (userId: string) => {
      const resolved = resolveUserId(userId);
      const user = snapshot.usersById[resolved];
      if (!user) return userId;
      return `${user.firstName} ${user.lastName}`;
    },
    [resolveUserId, snapshot.usersById],
  );

  const currentUserId = useMemo(() => {
    const ensured = ensureCurrentUser(snapshot);
    return ensured || null;
  }, [snapshot]);

  const currentUser = useMemo(() => {
    if (!currentUserId) return null;
    const user = users.find((item) => item.id === currentUserId);
    return user ? createOwnerOption(user) : null;
  }, [currentUserId, users]);

  const setCurrentUserId = useCallback(
    (userId: string) => {
      setSnapshot((prev) => {
        const resolvedId = prev.usersById[userId]
          ? userId
          : (prev.legacyUserIdAliases[userId] ?? userId);

        if (!prev.usersById[resolvedId]) return prev;

        return savePeopleSnapshot(
          {
            currentUserId: resolvedId,
            usersById: prev.usersById,
            teamsById: prev.teamsById,
            teamMembers: prev.teamMembers,
            legacyUserIdAliases: prev.legacyUserIdAliases,
            legacyTeamIdAliases: prev.legacyTeamIdAliases,
          },
          activeOrgId,
        );
      });
    },
    [activeOrgId],
  );

  const resetToSeed = useCallback(() => {
    setSnapshot(resetPeopleSnapshot(activeOrgId));
  }, [activeOrgId]);

  const value = useMemo<PeopleDataContextValue>(
    () => ({
      users,
      teams,
      orgPeople,
      teamOptions,
      teamNameOptions,
      ownerOptions,
      mentionPeople,
      currentUser,
      currentUserId,
      setCurrentUserId,
      setUsers,
      setTeams,
      setOrgPeople,
      resolveUserId,
      resolveTeamId,
      getTeamNameById,
      getTeamIdByName,
      getUserDisplayName,
      resetToSeed,
      updatedAt: snapshot.updatedAt,
    }),
    [
      users,
      teams,
      orgPeople,
      teamOptions,
      teamNameOptions,
      ownerOptions,
      mentionPeople,
      currentUser,
      currentUserId,
      setCurrentUserId,
      setUsers,
      setTeams,
      setOrgPeople,
      resolveUserId,
      resolveTeamId,
      getTeamNameById,
      getTeamIdByName,
      getUserDisplayName,
      resetToSeed,
      snapshot.updatedAt,
    ],
  );

  return (
    <PeopleDataContext.Provider value={value}>
      {children}
    </PeopleDataContext.Provider>
  );
}

export function usePeopleData() {
  const context = useContext(PeopleDataContext);
  if (!context) {
    throw new Error("usePeopleData must be used within PeopleDataProvider");
  }
  return context;
}
