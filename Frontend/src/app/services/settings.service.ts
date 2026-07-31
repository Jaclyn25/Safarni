import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, map } from 'rxjs';

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

interface ApiError {
  code: string;
  message: string;
}

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: ApiError[];
  statusCode: number;
  timestamp: string;
}

/** Extracts the first backend error message, with a fallback. */
export function getSettingsErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const apiResponse = error.error as Partial<ApiResponse<unknown>> | null;
    const firstError = apiResponse?.errors?.[0]?.message;

    if (firstError) {
      return firstError;
    }

    if (apiResponse?.message) {
      return apiResponse.message;
    }

    if (error.status === 0) {
      return 'The API is unavailable. Make sure the backend is running on http://localhost:5110.';
    }
  }

  return fallback;
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
