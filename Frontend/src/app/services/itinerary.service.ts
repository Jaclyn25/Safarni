import {
  HttpClient,
  HttpHeaders
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ApiError {
  code: string;
  message: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: ApiError[];
  statusCode: number;
  timestamp: string;
}

export interface GenerateItineraryPreferences {
  qualityVersion?: number;
  interests: string[];
  pace: string;
  transportation: string[];
  accommodationStyle: string;
  dietaryNeeds: string[];
  accessibilityNeeds: string[];
  preferredLanguage: 'en' | 'ar';
  additionalNotes: string | null;
}

export interface GenerateItineraryRequest {
  preferences: GenerateItineraryPreferences;
  budgetAmount: number;
  currencyCode: string;
  travelerCount: number;
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
  items: ItineraryItem[];
}

export interface ItineraryResponse {
  id: string;
  tripId: string;
  preferences: GenerateItineraryPreferences;

  budgetAmount: number;
  currencyCode: string;
  travelerCount: number;

  status: string;
  generatedBy: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  estimatedTotalCost: number;
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

export interface ItineraryItemRequest {
  dayId?: string;
  targetDayId?: string;

  title: string;
  description: string | null;
  startTime: string | null;
  endTime: string | null;
  location: string | null;
  category: string;
  estimatedCost: number | null;
  orderIndex: number | null;
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

  generateItinerary(
    tripId: string,
    request: GenerateItineraryRequest
  ): Observable<ApiResponse<ItineraryResponse>> {
    return this.http.post<
      ApiResponse<ItineraryResponse>
    >(
      `${this.apiUrl}/${tripId}/itinerary/generate`,
      request,
      {
        headers: this.getHeaders()
      }
    );
  }

  addItem(
    tripId: string,
    request: ItineraryItemRequest
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

  updateItem(
    tripId: string,
    itemId: string,
    request: ItineraryItemRequest
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

  selectVenue(
    tripId: string,
    itemId: string,
    venue: VenueSuggestion
  ): Observable<ApiResponse<ItineraryResponse>> {
    return this.http.put<
      ApiResponse<ItineraryResponse>
    >(
      `${this.apiUrl}/${tripId}/itinerary/items/${itemId}/venue`,
      venue,
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
