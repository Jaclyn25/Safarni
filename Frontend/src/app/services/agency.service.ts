import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import {
  AdminAgencyDetail,
  AdminAgencySummary,
  AgencyActivityItem,
  AgencyDashboardSummary,
  AgencyDashboardTrendPoint,
  AgencyDocumentDecision,
  AgencyOnboardingStatus,
  AgencyProfile,
  AgencyVerificationDecision,
  AgencyVerificationDocumentType,
  AgencyVerificationOverview,
  AgencyVerificationStatus,
  PagedResult,
  UpdateAgencyProfileRequest
} from '../models/agency.models';
import { ApiResponse } from './auth.service';

@Injectable({ providedIn: 'root' })
export class AgencyService {
  private readonly agencyApi = '/api/agency';
  private readonly adminApi = '/api/admin/agencies';

  constructor(private readonly http: HttpClient) {}

  getOnboardingStatus(): Observable<AgencyOnboardingStatus> {
    return this.http
      .get<ApiResponse<AgencyOnboardingStatus>>(`${this.agencyApi}/onboarding`)
      .pipe(map((response) => this.requireData(response)));
  }

  completeOnboarding(request: UpdateAgencyProfileRequest): Observable<AgencyProfile> {
    return this.http
      .post<ApiResponse<AgencyProfile>>(`${this.agencyApi}/onboarding`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  getVerification(): Observable<AgencyVerificationOverview> {
    return this.http
      .get<ApiResponse<AgencyVerificationOverview>>(`${this.agencyApi}/verification`)
      .pipe(map((response) => this.requireData(response)));
  }

  uploadDocument(
    type: AgencyVerificationDocumentType,
    documentNumber: string,
    expiryDate: string | null,
    file: File
  ): Observable<AgencyVerificationOverview> {
    const form = new FormData();
    form.append('documentNumber', documentNumber.trim());
    if (expiryDate) {
      form.append('expiryDate', expiryDate);
    }
    form.append('file', file, file.name);

    return this.http
      .post<ApiResponse<AgencyVerificationOverview>>(
        `${this.agencyApi}/verification-documents/${type}`,
        form
      )
      .pipe(map((response) => this.requireData(response)));
  }

  deleteDocument(type: AgencyVerificationDocumentType): Observable<AgencyVerificationOverview> {
    return this.http
      .delete<ApiResponse<AgencyVerificationOverview>>(
        `${this.agencyApi}/verification-documents/${type}`
      )
      .pipe(map((response) => this.requireData(response)));
  }

  getOwnDocument(type: AgencyVerificationDocumentType): Observable<Blob> {
    return this.http.get(`${this.agencyApi}/verification-documents/${type}/content`, {
      responseType: 'blob'
    });
  }

  getProfile(): Observable<AgencyProfile> {
    return this.http
      .get<ApiResponse<AgencyProfile>>(`${this.agencyApi}/profile`)
      .pipe(map((response) => this.requireData(response)));
  }

  updateProfile(request: UpdateAgencyProfileRequest): Observable<AgencyProfile> {
    return this.http
      .put<ApiResponse<AgencyProfile>>(`${this.agencyApi}/profile`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  getDashboardSummary(): Observable<AgencyDashboardSummary> {
    return this.http
      .get<ApiResponse<AgencyDashboardSummary>>(`${this.agencyApi}/dashboard/summary`)
      .pipe(map((response) => this.requireData(response)));
  }

  getDashboardTrends(days: number = 30): Observable<AgencyDashboardTrendPoint[]> {
    const params = new HttpParams().set('days', days);
    return this.http
      .get<ApiResponse<AgencyDashboardTrendPoint[]>>(`${this.agencyApi}/dashboard/trends`, { params })
      .pipe(map((response) => this.requireData(response)));
  }

  getDashboardRecentActivity(count: number = 10): Observable<AgencyActivityItem[]> {
    const params = new HttpParams().set('count', count);
    return this.http
      .get<ApiResponse<AgencyActivityItem[]>>(`${this.agencyApi}/dashboard/recent-activity`, { params })
      .pipe(map((response) => this.requireData(response)));
  }

  listAdminAgencies(filters: {
    status?: AgencyVerificationStatus | '';
    search?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<AdminAgencySummary>> {
    let params = new HttpParams()
      .set('page', filters.page ?? 1)
      .set('pageSize', filters.pageSize ?? 20);
    if (filters.status) {
      params = params.set('status', filters.status);
    }
    if (filters.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }

    return this.http
      .get<ApiResponse<PagedResult<AdminAgencySummary>>>(this.adminApi, { params })
      .pipe(map((response) => this.requireData(response)));
  }

  getAdminAgency(agencyId: string): Observable<AdminAgencyDetail> {
    return this.http
      .get<ApiResponse<AdminAgencyDetail>>(`${this.adminApi}/${agencyId}`)
      .pipe(map((response) => this.requireData(response)));
  }

  decideAgency(
    agencyId: string,
    decision: AgencyVerificationDecision,
    reason: string | null
  ): Observable<AdminAgencyDetail> {
    return this.http
      .post<ApiResponse<AdminAgencyDetail>>(`${this.adminApi}/${agencyId}/decision`, {
        decision,
        reason
      })
      .pipe(map((response) => this.requireData(response)));
  }

  decideDocument(
    agencyId: string,
    type: AgencyVerificationDocumentType,
    decision: AgencyDocumentDecision,
    reason: string | null
  ): Observable<AdminAgencyDetail> {
    return this.http
      .post<ApiResponse<AdminAgencyDetail>>(
        `${this.adminApi}/${agencyId}/verification-documents/${type}/decision`,
        { decision, reason }
      )
      .pipe(map((response) => this.requireData(response)));
  }

  getAdminDocument(
    agencyId: string,
    type: AgencyVerificationDocumentType
  ): Observable<Blob> {
    return this.http.get(
      `${this.adminApi}/${agencyId}/verification-documents/${type}/content`,
      { responseType: 'blob' }
    );
  }

  private requireData<T>(response: ApiResponse<T>): T {
    if (!response.success || response.data === null) {
      throw new Error(response.errors?.[0]?.message || response.message || 'The request failed.');
    }
    return response.data;
  }
}
