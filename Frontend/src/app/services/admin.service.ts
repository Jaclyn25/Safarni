import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import {
  AdminConversation,
  AdminUserDetail,
  AdminUserSummary,
  AiActivityItem,
  AnalyticsFunnel,
  AnalyticsOverview,
  AuditLogEntry,
  AuditSeverity,
  DashboardSummary,
  ForcePasswordResetResult,
  IntegrationConfigEntry,
  IntegrationHealthCheck,
  KnowledgeInput,
  KnowledgeItem,
  PagedResult,
  RecentActivityItem,
  ReindexResult,
  RoleInfo,
  TopDestination,
  UserRole,
} from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly baseUrl = '/api/admin';

  constructor(private readonly http: HttpClient) {}

  // ================= Dashboard =================

  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http
      .get<ApiResponse<DashboardSummary>>(`${this.baseUrl}/dashboard/summary`)
      .pipe(map((response) => response.data!));
  }

  getRecentActivity(limit = 15): Observable<RecentActivityItem[]> {
    return this.http
      .get<ApiResponse<RecentActivityItem[]>>(`${this.baseUrl}/dashboard/recent-activity`, {
        params: new HttpParams().set('limit', limit.toString()),
      })
      .pipe(map((response) => response.data ?? []));
  }

  // ================= Users =================

  getUsers(filters: {
    search?: string;
    isActive?: boolean;
    role?: UserRole;
    isEmailVerified?: boolean;
    page?: number;
    pageSize?: number;
  } = {}): Observable<PagedResult<AdminUserSummary>> {
    let params = new HttpParams();
    const entries: Array<[string, string | number | boolean | undefined]> = [
      ['search', filters.search],
      ['isActive', filters.isActive],
      ['role', filters.role],
      ['isEmailVerified', filters.isEmailVerified],
      ['page', filters.page ?? 1],
      ['pageSize', filters.pageSize ?? 20],
    ];
    for (const [key, value] of entries) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http
      .get<ApiResponse<PagedResult<AdminUserSummary>>>(`${this.baseUrl}/users`, { params })
      .pipe(map((response) => response.data!));
  }

  getUser(id: string): Observable<AdminUserDetail> {
    return this.http
      .get<ApiResponse<AdminUserDetail>>(`${this.baseUrl}/users/${id}`)
      .pipe(map((response) => response.data!));
  }

  updateUserStatus(id: string, isActive: boolean): Observable<AdminUserDetail> {
    return this.http
      .patch<ApiResponse<AdminUserDetail>>(`${this.baseUrl}/users/${id}/status`, { isActive })
      .pipe(map((response) => response.data!));
  }

  verifyUserEmail(id: string): Observable<AdminUserDetail> {
    return this.http
      .post<ApiResponse<AdminUserDetail>>(`${this.baseUrl}/users/${id}/verify-email`, {})
      .pipe(map((response) => response.data!));
  }

  forcePasswordReset(id: string): Observable<ForcePasswordResetResult> {
    return this.http
      .post<ApiResponse<ForcePasswordResetResult>>(
        `${this.baseUrl}/users/${id}/force-password-reset`,
        {}
      )
      .pipe(map((response) => response.data!));
  }

  updateUserRoles(id: string, role: UserRole): Observable<AdminUserDetail> {
    return this.http
      .put<ApiResponse<AdminUserDetail>>(`${this.baseUrl}/users/${id}/roles`, { role })
      .pipe(map((response) => response.data!));
  }

  // ================= Knowledge =================

  getKnowledge(filters: {
    search?: string;
    category?: string;
    isArchived?: boolean;
    page?: number;
    pageSize?: number;
  } = {}): Observable<PagedResult<KnowledgeItem>> {
    let params = new HttpParams();
    const entries: Array<[string, string | number | boolean | undefined]> = [
      ['search', filters.search],
      ['category', filters.category],
      ['isArchived', filters.isArchived],
      ['page', filters.page ?? 1],
      ['pageSize', filters.pageSize ?? 20],
    ];
    for (const [key, value] of entries) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http
      .get<ApiResponse<PagedResult<KnowledgeItem>>>(`${this.baseUrl}/knowledge`, { params })
      .pipe(map((response) => response.data!));
  }

  createKnowledge(input: KnowledgeInput): Observable<KnowledgeItem> {
    return this.http
      .post<ApiResponse<KnowledgeItem>>(`${this.baseUrl}/knowledge`, input)
      .pipe(map((response) => response.data!));
  }

  updateKnowledge(id: number, input: KnowledgeInput): Observable<KnowledgeItem> {
    return this.http
      .put<ApiResponse<KnowledgeItem>>(`${this.baseUrl}/knowledge/${id}`, input)
      .pipe(map((response) => response.data!));
  }

  archiveKnowledge(id: number): Observable<KnowledgeItem> {
    return this.http
      .delete<ApiResponse<KnowledgeItem>>(`${this.baseUrl}/knowledge/${id}`)
      .pipe(map((response) => response.data!));
  }

  restoreKnowledge(id: number): Observable<KnowledgeItem> {
    return this.http
      .post<ApiResponse<KnowledgeItem>>(`${this.baseUrl}/knowledge/${id}/restore`, {})
      .pipe(map((response) => response.data!));
  }

  reindexKnowledge(): Observable<ReindexResult> {
    return this.http
      .post<ApiResponse<ReindexResult>>(`${this.baseUrl}/knowledge/reindex`, {})
      .pipe(map((response) => response.data!));
  }

  // ================= AI Activity =================

  getAiActivity(filters: {
    userId?: string;
    provider?: string;
    status?: string;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  } = {}): Observable<PagedResult<AiActivityItem>> {
    let params = new HttpParams();
    const entries: Array<[string, string | number | undefined]> = [
      ['userId', filters.userId],
      ['provider', filters.provider],
      ['status', filters.status],
      ['from', filters.from],
      ['to', filters.to],
      ['page', filters.page ?? 1],
      ['pageSize', filters.pageSize ?? 20],
    ];
    for (const [key, value] of entries) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http
      .get<ApiResponse<PagedResult<AiActivityItem>>>(`${this.baseUrl}/ai/activity`, { params })
      .pipe(map((response) => response.data!));
  }

  getAiConversation(id: string): Observable<AdminConversation> {
    return this.http
      .get<ApiResponse<AdminConversation>>(`${this.baseUrl}/ai/conversations/${id}`)
      .pipe(map((response) => response.data!));
  }

  // ================= Integrations =================

  getIntegrationsHealth(): Observable<IntegrationHealthCheck[]> {
    return this.http
      .get<ApiResponse<IntegrationHealthCheck[]>>(`${this.baseUrl}/integrations/health`)
      .pipe(map((response) => response.data ?? []));
  }

  getIntegrationsConfig(): Observable<IntegrationConfigEntry[]> {
    return this.http
      .get<ApiResponse<IntegrationConfigEntry[]>>(`${this.baseUrl}/integrations/config`)
      .pipe(map((response) => response.data ?? []));
  }

  testIntegration(name: string): Observable<IntegrationHealthCheck> {
    return this.http
      .post<ApiResponse<IntegrationHealthCheck>>(`${this.baseUrl}/integrations/${name}/test`, {})
      .pipe(map((response) => response.data!));
  }

  // ================= Analytics =================

  getAnalyticsOverview(from: string, to: string): Observable<AnalyticsOverview> {
    return this.http
      .get<ApiResponse<AnalyticsOverview>>(`${this.baseUrl}/analytics/overview`, {
        params: new HttpParams().set('from', from).set('to', to),
      })
      .pipe(map((response) => response.data!));
  }

  getAnalyticsFunnels(): Observable<AnalyticsFunnel> {
    return this.http
      .get<ApiResponse<AnalyticsFunnel>>(`${this.baseUrl}/analytics/funnels`)
      .pipe(map((response) => response.data!));
  }

  getTopDestinations(limit = 10): Observable<TopDestination[]> {
    return this.http
      .get<ApiResponse<TopDestination[]>>(`${this.baseUrl}/analytics/top-destinations`, {
        params: new HttpParams().set('limit', limit.toString()),
      })
      .pipe(map((response) => response.data ?? []));
  }

  // ================= Roles =================

  getRoles(): Observable<RoleInfo[]> {
    return this.http
      .get<ApiResponse<RoleInfo[]>>(`${this.baseUrl}/roles`)
      .pipe(map((response) => response.data ?? []));
  }

  // ================= Audit Logs =================

  getAuditLogs(filters: {
    search?: string;
    action?: string;
    targetType?: string;
    actorUserId?: string;
    severity?: AuditSeverity;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  } = {}): Observable<PagedResult<AuditLogEntry>> {
    let params = new HttpParams();
    const entries: Array<[string, string | number | undefined]> = [
      ['search', filters.search],
      ['action', filters.action],
      ['targetType', filters.targetType],
      ['actorUserId', filters.actorUserId],
      ['severity', filters.severity],
      ['from', filters.from],
      ['to', filters.to],
      ['page', filters.page ?? 1],
      ['pageSize', filters.pageSize ?? 25],
    ];
    for (const [key, value] of entries) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http
      .get<ApiResponse<PagedResult<AuditLogEntry>>>(`${this.baseUrl}/audit-logs`, { params })
      .pipe(map((response) => response.data!));
  }
}
