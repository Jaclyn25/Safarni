import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiResponse, getApiErrorMessage } from '../models/api-response.model';

export { getApiErrorMessage as getPreferencesErrorMessage } from '../models/api-response.model';

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
