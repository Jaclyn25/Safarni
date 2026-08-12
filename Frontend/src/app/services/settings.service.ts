import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiResponse, getApiErrorMessage } from '../models/api-response.model';

export { getApiErrorMessage as getSettingsErrorMessage } from '../models/api-response.model';

/**
 * Mirrors Safarni.Application.Features.Settings.GetMySettings.GetMySettingsResponse.
 */
export interface MySettings {
  userId: string;
  emailNotificationsEnabled: boolean;
  pushNotificationsEnabled: boolean;
  profileIsPrivate: boolean;
  showLastSeen: boolean;
  showEmail: boolean;
  showPhoneNumber: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/**
 * Request body for PUT /api/settings/me (mirrors UpdateMySettingsRequest).
 */
export interface UpdateMySettingsCommand {
  emailNotificationsEnabled: boolean;
  pushNotificationsEnabled: boolean;
  profileIsPrivate: boolean;
  showLastSeen: boolean;
  showEmail: boolean;
  showPhoneNumber: boolean;
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly baseUrl = '/api/settings';

  constructor(private http: HttpClient) {}

  getMySettings(): Observable<MySettings> {
    return this.http
      .get<ApiResponse<MySettings>>(`${this.baseUrl}/me`)
      .pipe(map((response) => response.data!));
  }

  updateMySettings(command: UpdateMySettingsCommand): Observable<MySettings> {
    return this.http
      .put<ApiResponse<MySettings>>(`${this.baseUrl}/me`, command)
      .pipe(map((response) => response.data!));
  }
}
