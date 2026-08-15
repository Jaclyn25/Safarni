import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import type { ApiResponse } from '../models/api-response.model';

export interface AuthorProfileDto {
  userId: string;
  displayName: string;
  profilePhotoUrl: string;
  isDeleted: boolean;
}

export interface UserRatingDto {
  score: number;
  comment?: string;
  createdAt: string;
}

export interface SearchPublicTripsRequest {
  destination?: string;
  minDuration?: number;
  maxDuration?: number;
  maxBudget?: number;
  sortOrder?: string;
}

export interface AiTripPlanResponse {
  generatedItinerary: string | null;
  relatedPublicTrips: PublicTripSearchResponse[];
}


export interface PublicTripSearchResponse {
  versionId: string;
  tripId: string;
  tripName: string;
  destination: string;
  startDate: string;
  endDate: string;
  durationDays: number;
  costAmount: number | null;
  authorName: string;
  authorPhotoUrl: string;
  viewsCount: number;
  likesCount: number;
  averageRating: number;
  publishedAt: string;
  isNewTrip: boolean;
}

export interface PublicTripDetailsResponse {
  snapshotVersionId: string;
  tripId: string;
  tripName: string;
  itineraryJson: string;
  costShown: boolean;
  costAmount: number | null;
  includedPhotoIdsJson: string;
  viewsCount: number;
  likesCount: number;
  savesCount: number;
  copiesCount: number;
  ratingsCount: number;
  averageRating: number;
  author: AuthorProfileDto;
  isLikedByCurrentUser: boolean;
  isSavedByCurrentUser: boolean;
  userRating?: UserRatingDto | null;
}

export interface ToggleTripInteractionResult {
  versionId: string;
  newCount: number;
  isActive: boolean;
}

export interface CopyPublicTripResult {
  newTripId: string;
}

export interface RatePublicTripResult {
  versionId: string;
  newAverageRating: number;
  newRatingsCount: number;
}

export interface ReportPublicTripResult {
  versionId: string;
  success: boolean;
}

export interface RateTripRequest {
  score: number;
  comment?: string;
}

export interface ReportTripRequest {
  reason?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PublicTripService {
  private readonly apiUrl = '/api/public-trips';

  constructor(private readonly http: HttpClient) {}

  getPublicTripDetails(versionId: string): Observable<ApiResponse<PublicTripDetailsResponse>> {
    return this.http.get<ApiResponse<PublicTripDetailsResponse>>(`${this.apiUrl}/${versionId}`, {
      headers: this.getHeaders()
    });
  }

  searchPublicTrips(request: SearchPublicTripsRequest): Observable<ApiResponse<PublicTripSearchResponse[]>> {
    let params = new HttpParams();
    if (request.destination) params = params.set('destination', request.destination);
    if (request.minDuration != null) params = params.set('minDuration', request.minDuration.toString());
    if (request.maxDuration != null) params = params.set('maxDuration', request.maxDuration.toString());
    if (request.maxBudget != null) params = params.set('maxBudget', request.maxBudget.toString());
    if (request.sortOrder) params = params.set('sortOrder', request.sortOrder);

    return this.http.get<ApiResponse<PublicTripSearchResponse[]>>(`${this.apiUrl}/search`, {
      headers: this.getHeaders(),
      params
    });
  }

  aiPlanTrip(prompt: string): Observable<ApiResponse<AiTripPlanResponse>> {
    return this.http.post<ApiResponse<AiTripPlanResponse>>('/api/ai/plan-trip', { prompt }, {
      headers: this.getHeaders()
    });
  }

  toggleLike(versionId: string): Observable<ApiResponse<ToggleTripInteractionResult>> {
    return this.http.post<ApiResponse<ToggleTripInteractionResult>>(`${this.apiUrl}/${versionId}/like`, {}, {
      headers: this.getHeaders()
    });
  }

  toggleSave(versionId: string): Observable<ApiResponse<ToggleTripInteractionResult>> {
    return this.http.post<ApiResponse<ToggleTripInteractionResult>>(`${this.apiUrl}/${versionId}/save`, {}, {
      headers: this.getHeaders()
    });
  }

  copyTrip(versionId: string): Observable<ApiResponse<CopyPublicTripResult>> {
    return this.http.post<ApiResponse<CopyPublicTripResult>>(`${this.apiUrl}/${versionId}/copy`, {}, {
      headers: this.getHeaders()
    });
  }

  rateTrip(versionId: string, request: RateTripRequest): Observable<ApiResponse<RatePublicTripResult>> {
    return this.http.post<ApiResponse<RatePublicTripResult>>(`${this.apiUrl}/${versionId}/rate`, request, {
      headers: this.getHeaders()
    });
  }

  reportTrip(versionId: string, request: ReportTripRequest): Observable<ApiResponse<ReportPublicTripResult>> {
    return this.http.post<ApiResponse<ReportPublicTripResult>>(`${this.apiUrl}/${versionId}/report`, request, {
      headers: this.getHeaders()
    });
  }

  private getHeaders(): HttpHeaders {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    const token = localStorage.getItem('access_token');
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }

    return headers;
  }
}