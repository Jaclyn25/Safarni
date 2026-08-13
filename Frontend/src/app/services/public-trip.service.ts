import { HttpClient, HttpHeaders } from '@angular/common/http';
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