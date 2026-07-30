import {
  HttpClient,
  HttpHeaders,
  HttpParams
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from './itinerary.service';

export interface TravelCountry {
  name: string;
  iso2: string;
}

export interface TripCitySelection {
  name: string;
  days: number;
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
  startDate: string;
  endDate: string;
  cities: TripCitySelection[];
}

export interface TripResponse {
  id: string;
  userId: string;
  name: string;
  destination: string;
  originCountry: string;
  cities: TripCitySelection[];
  startDate: string;
  endDate: string;
  createdAt: string;
  updatedAt: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class TripService {
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
      .set('country', country);

    return this.http.get<ApiResponse<string[]>>(
      '/api/travel-locations/cities',
      {
        params
      }
    );
  }

  getAirports(
    country: string
  ): Observable<
    ApiResponse<FlightAirportSuggestion[]>
  > {
    const params = new HttpParams()
      .set('country', country);

    return this.http.get<
      ApiResponse<FlightAirportSuggestion[]>
    >(
      '/api/travel-locations/airports',
      {
        params
      }
    );
  }

  createTrip(
    request: CreateTripRequest
  ): Observable<ApiResponse<TripResponse>> {
    return this.http.post<ApiResponse<TripResponse>>(
      '/api/trips',
      request,
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
