export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type UserRole =
  | 'User'
  | 'Admin'
  | 'Support'
  | 'ContentManager'
  | 'Analyst'
  | 'TravelAgency';

export const ROLE_LABELS: Record<UserRole, string> = {
  User: 'User',
  Admin: 'Admin',
  Support: 'Support',
  ContentManager: 'Content Manager',
  Analyst: 'Analyst',
  TravelAgency: 'Travel Agency',
};

// ================= Dashboard =================

export interface DashboardSummary {
  totalUsers: number;
  activeUsers: number;
  verifiedUsers: number;
  totalTrips: number;
  totalVisaApplications: number;
  totalConversations: number;
  totalAiMessages: number;
  itineraryJobsTotal: number;
  itineraryJobsPending: number;
  itineraryJobsFailed: number;
  recentAiFailuresLast24Hours: number;
  qdrantHealthy: boolean;
  degradedServiceCount: number;
}

export interface RecentActivityItem {
  type: string;
  title: string;
  detail: string | null;
  actorUserId: string | null;
  actorName: string | null;
  timestamp: string;
}

// ================= Users =================

export interface AdminUserSummary {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  isEmailVerified: boolean;
  provider: string | null;
  createdAt: string;
  lastActiveAt: string | null;
}

export interface AdminUserDetail extends AdminUserSummary {
  phoneNumber: string | null;
  country: string | null;
  city: string | null;
  bio: string | null;
  profilePhotoUrl: string | null;
  updatedAt: string | null;
  tripsCount: number;
  visaApplicationsCount: number;
  conversationsCount: number;
  messagesCount: number;
}

export interface ForcePasswordResetResult {
  emailSent: boolean;
  message: string;
}

// ================= Knowledge =================

export interface KnowledgeItem {
  id: number;
  title: string;
  category: string;
  content: string;
  country: string | null;
  city: string | null;
  sourceUrl: string | null;
  metadataJson: string;
  isArchived: boolean;
  createdAt: string;
  lastUpdated: string;
  updatedByUserId: string | null;
}

export interface KnowledgeInput {
  title: string;
  category: string;
  content: string;
  country?: string | null;
  city?: string | null;
  sourceUrl?: string | null;
  metadataJson?: string | null;
}

export interface ReindexResult {
  success: boolean;
  message: string;
  chunkCount: number;
  qdrantHealthy: boolean;
}

// ================= AI Activity =================

export interface AiActivityItem {
  id: string;
  userId: string | null;
  conversationId: string | null;
  itineraryJobId: string | null;
  provider: string;
  model: string;
  operation: string;
  status: string;
  latencyMs: number;
  promptTokens: number | null;
  completionTokens: number | null;
  errorCode: string | null;
  createdAt: string;
}

export interface AdminConversationMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
}

export interface AdminConversation {
  id: string;
  title: string;
  userId: string;
  userEmail: string;
  userFullName: string;
  createdAt: string;
  isPinned: boolean;
  messages: AdminConversationMessage[];
}

// ================= Integrations =================

export interface IntegrationHealthCheck {
  name: string;
  displayName: string;
  affectedFeature: string;
  status: 'Healthy' | 'Unhealthy' | 'NotConfigured' | string;
  detail: string;
  isConfigured: boolean;
  checkedAt: string;
  latencyMs: number;
}

export interface IntegrationConfigEntry {
  integration: string;
  key: string;
  isConfigured: boolean;
  maskedValue: string | null;
}

// ================= Analytics =================

export interface AnalyticsDailyPoint {
  date: string;
  users: number;
  trips: number;
  visaApplications: number;
  messages: number;
  itineraryJobs: number;
}

export interface AnalyticsOverview {
  points: AnalyticsDailyPoint[];
}

export interface AnalyticsFunnel {
  registeredUsers: number;
  verifiedUsers: number;
  usersWithTrips: number;
  usersWithGeneratedItineraries: number;
  usersWithVisaApplications: number;
}

export interface TopDestination {
  name: string;
  tripCount: number;
}

// ================= Roles =================

export interface RoleInfo {
  name: string;
  description: string;
  capabilities: string[];
}

// ================= Audit Logs =================

export type AuditSeverity = 'Info' | 'Warning' | 'Error';

export interface AuditLogEntry {
  id: string;
  actorUserId: string | null;
  action: string;
  targetType: string;
  targetId: string | null;
  metadataJson: string;
  ipAddress: string | null;
  userAgent: string | null;
  severity: AuditSeverity;
  createdAt: string;
}
