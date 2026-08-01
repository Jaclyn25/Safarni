import {
  HttpClient,
  HttpHeaders
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import type { ApiResponse } from '../models/api-response.model';

export type { ApiResponse } from '../models/api-response.model';

export interface GenerateItineraryPreferences {
  qualityVersion?: number;
  interests: string[];
  pace: string;
  transportation: string[];
  accommodationStyle: string;
  preferredLanguage: 'en' | 'ar';
  additionalNotes: string | null;
}

export interface GenerateItineraryRequest {
  preferences: GenerateItineraryPreferences;
  budgetAmount: number | null;
  currencyCode: string;
  travelerCount: number;
}

export type ItineraryGenerationStatus =
  | 'pending'
  | 'running'
  | 'completed'
  | 'failed'
  | 'cancelled';

export interface ItineraryGenerationJob {
  id: string;
  tripId: string;
  status: ItineraryGenerationStatus;
  progressPercent: number;
  stage: string;
  errorCode: string | null;
  errorMessage: string | null;
  itineraryId: string | null;
  createdAt: string;
  updatedAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export interface ItineraryItem {
  id: string;
  title: string;
  description: string | null;
  startTime: string | null;
  endTime: string | null;

  location: string | null;
  googlePlaceId: string | null;
  address: string | null;
  googleMapsUrl: string | null;
  rating: number | null;
  photoUrl: string | null;
  priceLevel: string | null;
  ratePerNight: number | null;
  stayNights: number | null;
  priceSource: string | null;
  externalUrl: string | null;
  costSource: string | null;

  flightDepartureAirportCode: string | null;
  flightDepartureAirportName: string | null;
  flightArrivalAirportCode: string | null;
  flightArrivalAirportName: string | null;
  flightAirlines: string | null;
  flightNumbers: string | null;
  flightDurationMinutes: number | null;
  flightStopCount: number | null;

  routeDistanceMeters: number | null;
  routeDurationMinutes: number | null;
  routeTravelMode: string | null;
  routeTransitDetails: string | null;

  category: string;
  estimatedCost: number | null;
  orderIndex: number;
}

export interface ItineraryDay {
  id: string;
  dayNumber: number;
  date: string;
  title: string;
  notes: string | null;
  estimatedTotalCost: number;
  unknownCostItemCount: number;
  isCompleteCostEstimate: boolean;
  items: ItineraryItem[];
}

export interface ItineraryResponse {
  id: string;
  tripId: string;
  preferences: GenerateItineraryPreferences;

  budgetAmount: number | null;
  currencyCode: string;
  travelerCount: number;

  status: string;
  generatedBy: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  estimatedTotalCost: number;
  unknownCostItemCount: number;
  isCompleteCostEstimate: boolean;
  days: ItineraryDay[];
}

export interface VenueSuggestion {
  placeId: string;
  name: string;
  description: string | null;
  address: string | null;
  googleMapsUrl: string | null;
  rating: number | null;
  photoUrl: string | null;
  priceLevel: string | null;
  ratePerNight: number | null;
  totalRate: number | null;
  priceSource: string | null;
  externalUrl: string | null;
  latitude: number | null;
  longitude: number | null;
}

export interface AddItineraryItemRequest {
  dayId: string;
  title: string;
  description: string | null;
  startTime: string | null;
  endTime: string | null;
  location: string | null;
  category: string;
  estimatedCost: number | null;
  orderIndex: number | null;
}

export interface AddRecommendedItineraryItemRequest {
  dayId: string;
  category: string;
  placeId: string;
  orderIndex: number | null;
}

export interface UpdateItineraryItemRequest {
  title: string;
  description: string | null;
  startTime: string | null;
  endTime: string | null;
  location: string | null;
  category: string;
  estimatedCost: number | null;
}

export interface MoveItineraryItemRequest {
  targetDayId: string;
  targetOrderIndex: number;
}

@Injectable({
  providedIn: 'root'
})
export class ItineraryService {
  private readonly apiUrl = '/api/trips';

  constructor(
    private readonly http: HttpClient
  ) {}

  getActiveItinerary(
    tripId: string
  ): Observable<ApiResponse<ItineraryResponse>> {
    return this.http.get<
      ApiResponse<ItineraryResponse>
    >(
      `${this.apiUrl}/${tripId}/itinerary`,
      {
        headers: this.getHeaders()
      }
    );
  }

  startGeneration(
    tripId: string,
    request: GenerateItineraryRequest,
    idempotencyKey: string
  ): Observable<
    ApiResponse<ItineraryGenerationJob>
  > {
    return this.http.post<
      ApiResponse<ItineraryGenerationJob>
    >(
      `${this.apiUrl}/${tripId}/itinerary/generate`,
      request,
      {
        headers: this.getHeaders().set(
          'Idempotency-Key',
          idempotencyKey
        )
      }
    );
  }

  getGenerationJob(
    tripId: string,
    jobId: string
  ): Observable<
    ApiResponse<ItineraryGenerationJob>
  > {
    return this.http.get<
      ApiResponse<ItineraryGenerationJob>
    >(
      `${this.apiUrl}/${tripId}/itinerary/generation/${jobId}`,
      {
        headers: this.getHeaders()
      }
    );
  }

  cancelGeneration(
    tripId: string,
    jobId: string
  ): Observable<
    ApiResponse<ItineraryGenerationJob>
  > {
    return this.http.delete<
      ApiResponse<ItineraryGenerationJob>
    >(
      `${this.apiUrl}/${tripId}/itinerary/generation/${jobId}`,
      {
        headers: this.getHeaders()
      }
    );
  }

  addItem(
    tripId: string,
    request: AddItineraryItemRequest
  ): Observable<ApiResponse<ItineraryResponse>> {
    return this.http.post<
      ApiResponse<ItineraryResponse>
    >(
      `${this.apiUrl}/${tripId}/itinerary/items`,
      request,
      {
        headers: this.getHeaders()
      }
    );
  }

  addRecommendedItem(
    tripId: string,
    request: AddRecommendedItineraryItemRequest
  ): Observable<ApiResponse<ItineraryResponse>> {
    return this.http.post<ApiResponse<ItineraryResponse>>(
      `${this.apiUrl}/${tripId}/itinerary/items/from-venue`,
      request,
      {
        headers: this.getHeaders()
      }
    );
  }

  updateItem(
    tripId: string,
    itemId: string,
    request: UpdateItineraryItemRequest
  ): Observable<ApiResponse<ItineraryResponse>> {
    return this.http.put<
      ApiResponse<ItineraryResponse>
    >(
      `${this.apiUrl}/${tripId}/itinerary/items/${itemId}`,
      request,
      {
        headers: this.getHeaders()
      }
    );
  }

  moveItem(
    tripId: string,
    itemId: string,
    request: MoveItineraryItemRequest
  ): Observable<ApiResponse<ItineraryResponse>> {
    return this.http.put<
      ApiResponse<ItineraryResponse>
    >(
      `${this.apiUrl}/${tripId}/itinerary/items/${itemId}/position`,
      request,
      {
        headers: this.getHeaders()
      }
    );
  }

  deleteItem(
    tripId: string,
    itemId: string
  ): Observable<ApiResponse<ItineraryResponse>> {
    return this.http.delete<
      ApiResponse<ItineraryResponse>
    >(
      `${this.apiUrl}/${tripId}/itinerary/items/${itemId}`,
      {
        headers: this.getHeaders()
      }
    );
  }

  getVenueSuggestions(
    tripId: string,
    itemId: string
  ): Observable<ApiResponse<VenueSuggestion[]>> {
    return this.http.get<
      ApiResponse<VenueSuggestion[]>
    >(
      `${this.apiUrl}/${tripId}/itinerary/items/${itemId}/venues`,
      {
        headers: this.getHeaders()
      }
    );
  }

  getDayVenueSuggestions(
    tripId: string,
    dayId: string,
    category: string
  ): Observable<ApiResponse<VenueSuggestion[]>> {
    return this.http.get<ApiResponse<VenueSuggestion[]>>(
      `${this.apiUrl}/${tripId}/itinerary/days/${dayId}/venues`,
      {
        headers: this.getHeaders(),
        params: { category }
      }
    );
  }

  selectVenue(
    tripId: string,
    itemId: string,
    placeId: string
  ): Observable<ApiResponse<ItineraryResponse>> {
    return this.http.put<
      ApiResponse<ItineraryResponse>
    >(
      `${this.apiUrl}/${tripId}/itinerary/items/${itemId}/venue`,
      { placeId },
      {
        headers: this.getHeaders()
      }
    );
  }

  private getHeaders(): HttpHeaders {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    const token =
      localStorage.getItem('access_token');

    if (token) {
      headers = headers.set(
        'Authorization',
        `Bearer ${token}`
      );
    }

    return headers;
  }
}
