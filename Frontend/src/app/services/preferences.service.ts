import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, map } from 'rxjs';

/**
 * Mirrors Safarni.Application.Features.Preferences.TravelProfileResponse.
 */
export interface TravelProfile {
  userId: string;
  preferredDestinationCountry: string;
  travelPurpose: string;
  travelStyle: string;
  minBudget: number | null;
  maxBudget: number | null;
  passportNumber: string;
  nationality: string;
  passportExpiryDate: string;
  updatedAt: string;
}

/**
 * Request body for PUT /api/preferences/travel
 * (mirrors UpsertTravelProfileCommand).
 */
export interface UpsertTravelProfileCommand {
  preferredDestinationCountry: string;
  travelPurpose: string;
  travelStyle: string;
  minBudget: number | null;
  maxBudget: number | null;
  passportNumber: string;
  nationality: string;
  passportExpiryDate: string;
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
export function getPreferencesErrorMessage(error: unknown, fallback: string): string {
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
export class PreferencesService {
  private readonly baseUrl = '/api/preferences';

  constructor(private http: HttpClient) {}

  getTravelProfile(): Observable<TravelProfile> {
    return this.http
      .get<ApiResponse<TravelProfile>>(`${this.baseUrl}/travel`)
      .pipe(map((response) => response.data!));
  }

  updateTravelProfile(command: UpsertTravelProfileCommand): Observable<TravelProfile> {
    return this.http
      .put<ApiResponse<TravelProfile>>(`${this.baseUrl}/travel`, command)
      .pipe(map((response) => response.data!));
  }
}
