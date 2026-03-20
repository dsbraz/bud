import type { ConfidenceLevel } from "@/types/mission/check-in";

export type AlertLevel = "excellent" | "good" | "attention" | "critical";

export type HealthStatus = "healthy" | "attention" | "critical";

export type TrendDirection = "up" | "down" | "stable";

export interface UserHabitMetrics {
  activeDays30d: number;
  lastActiveAt: string | null;
  checkInStreak: number;
  daysSinceLastCheckIn: number;
  checkInsLast30d: number;
  surveyResponseRate: number;
  pendingSurveysCount: number;
  surveysRespondedLast30d: number;
  surveysTotalLast30d: number;
  lastPulseSentiment: number | null;
  lastPulseDate: string | null;
  lastPulseWorkload: "low" | "normal" | "high" | "overload" | null;
  pulseSentimentTrend: "improving" | "stable" | "declining" | null;
}

export interface UserPerformanceMetrics {
  krsCompletedRate: number;
  avgProgress: number;
  avgConfidence: ConfidenceLevel | null;
  activeKRsCount: number;
  completedKRsCount: number;
  avgSurveyResponseTimeHours: number | null;
}

export interface UserEngagementSummary {
  userId: string;
  name: string;
  initials: string;
  avatarUrl: string | null;
  jobTitle: string | null;
  habit: UserHabitMetrics;
  performance: UserPerformanceMetrics;
  engagementScore: number;
  performanceScore: number;
  overallScore: number;
  alertLevel: AlertLevel;
  healthStatus: HealthStatus;
  alerts: string[];
  trend: TrendDirection;
}

export interface TeamEngagementSummary {
  teamId: string;
  memberCount: number;
  avgEngagementScore: number;
  avgPerformanceScore: number;
  engagementTrend: TrendDirection;
  performanceTrend: TrendDirection;
  checkInRateThisWeek: number;
  membersWithPendingSurveys: number;
  membersInAlert: number;
  byAlertLevel: Record<AlertLevel, number>;
  byHealthStatus: Record<HealthStatus, number>;
}
