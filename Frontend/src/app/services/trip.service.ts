import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
  HttpParams
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import type {
  ApiResponse
} from '../models/api-response.model';

export interface TravelCountry {
  name: string;
  iso2: string;
}

export interface TripCitySelection {
  name: string;
  days: number;
  airportCode?: string | null;
  airportName?: string | null;
}

export interface FlightAirportSuggestion {
  code: string;
  name: string;
  city: string;
}

export interface CreateTripRequest {
  name: string;
  destination: string;
  originCountry: string;
  originCountryCode?: string | null;
  originCity?: string | null;
  originAirportCode?: string | null;
  originAirportName?: string | null;
  startDate: string;
  endDate: string;
  cities: TripCitySelection[];
}

export interface UpdateTripRequest
  extends CreateTripRequest {}

export interface TripResponse {
  id: string;
  userId: string;
  name: string;
  destination: string;
  originCountry: string;
  originCountryCode: string | null;
  originCity: string | null;
  originAirportCode: string | null;
  originAirportName: string | null;
  cities: TripCitySelection[];
  startDate: string;
  endDate: string;
  createdAt: string;
  updatedAt: string | null;

  activeItineraryId: string | null;
  itineraryStatus: string | null;
  itineraryCurrency: string | null;
  estimatedTotalCost: number | null;
  itineraryUpdatedAt: string | null;
}

export interface TripRouteActivityDto {
  activityId: string;
  name: string;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  order: number;
  description: string | null;
}

export interface TripRouteResponse {
  activities: TripRouteActivityDto[];
}

export interface NearbyAttractionDto {
  name: string;
  category: string | null;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  distance: number | null;
  website: string | null;
  phone: string | null;
  openingHours: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class TripService {
  private readonly apiUrl = '/api/trips';

  constructor(
    private readonly http: HttpClient
  ) {}

  getCountries():
    Observable<ApiResponse<TravelCountry[]>> {
    return this.http.get<
      ApiResponse<TravelCountry[]>
    >('/api/travel-locations/countries');
  }

  getCities(
    country: string
  ): Observable<ApiResponse<string[]>> {
    const params = new HttpParams()
      .set('country', country.trim());

    return this.http.get<ApiResponse<string[]>>(
      '/api/travel-locations/cities',
      { params }
    );
  }

  /**
   * The backend accepts a free-text airport query even though the
   * query-string key is still named "country" for backwards compatibility.
   *
   * Pass a country for origin airports or "City, Country" for a
   * destination airport search.
   */
  getAirports(
    query: string
  ): Observable<
    ApiResponse<FlightAirportSuggestion[]>
  > {
    const params = new HttpParams()
      .set('country', query.trim());

    return this.http.get<
      ApiResponse<FlightAirportSuggestion[]>
    >(
      '/api/travel-locations/airports',
      { params }
    );
  }

  createTrip(
    request: CreateTripRequest
  ): Observable<ApiResponse<TripResponse>> {
    return this.http.post<
      ApiResponse<TripResponse>
    >(
      this.apiUrl,
      request,
      {
        headers: this.getHeaders()
      }
    );
  }

  getTrips():
    Observable<ApiResponse<TripResponse[]>> {
    return this.http.get<
      ApiResponse<TripResponse[]>
    >(
      this.apiUrl,
      {
        headers: this.getHeaders()
      }
    );
  }

  getTrip(
    tripId: string
  ): Observable<ApiResponse<TripResponse>> {
    return this.http.get<
      ApiResponse<TripResponse>
    >(
      `${this.apiUrl}/${tripId}`,
      {
        headers: this.getHeaders()
      }
    );
  }

  updateTrip(
    tripId: string,
    request: UpdateTripRequest
  ): Observable<ApiResponse<TripResponse>> {
    return this.http.put<
      ApiResponse<TripResponse>
    >(
      `${this.apiUrl}/${tripId}`,
      request,
      {
        headers: this.getHeaders()
      }
    );
  }

  deleteTrip(tripId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${tripId}`,
      {
        headers: this.getHeaders()
      }
    );
  }

  getTripRoute(
    tripId: string
  ): Observable<ApiResponse<TripRouteResponse>> {
    return this.http.get<ApiResponse<TripRouteResponse>>(
      `${this.apiUrl}/${tripId}/route`,
      {
        headers: this.getHeaders()
      }
    );
  }

  getNearbyAttractions(
    tripId: string,
    radius: number,
    category?: string
  ): Observable<ApiResponse<NearbyAttractionDto[]>> {
    let params = new HttpParams().set('radius', radius.toString());
    if (category) {
      params = params.set('category', category);
    }

    return this.http.get<ApiResponse<NearbyAttractionDto[]>>(
      `${this.apiUrl}/${tripId}/nearby`,
      {
        headers: this.getHeaders(),
        params
      }
    );
  }

  rememberTrip(
    trip: TripResponse
  ): void {
    sessionStorage.setItem(
      this.tripStorageKey(trip.id),
      JSON.stringify(trip)
    );
  }

  getRememberedTrip(
    tripId: string
  ): TripResponse | null {
    const stored = sessionStorage.getItem(
      this.tripStorageKey(tripId)
    );

    if (!stored) {
      return null;
    }

    try {
      return JSON.parse(
        stored
      ) as TripResponse;
    } catch {
      sessionStorage.removeItem(
        this.tripStorageKey(tripId)
      );

      return null;
    }
  }

  forgetTrip(
    tripId: string
  ): void {
    sessionStorage.removeItem(
      this.tripStorageKey(tripId)
    );
  }

  getErrorMessage(
    error: unknown,
    fallback: string
  ): string {
    if (error instanceof HttpErrorResponse) {
      return (
        error.error?.errors?.[0]?.message ||
        error.error?.message ||
        fallback
      );
    }

    return error instanceof Error
      ? error.message
      : fallback;
  }

  private tripStorageKey(
    tripId: string
  ): string {
    return `safarni_trip_${tripId}`;
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
