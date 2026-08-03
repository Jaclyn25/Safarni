import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';

export interface LocationDto {
  name: string;
  region: string;
  country: string;
  lat: number;
  lon: number;
}

export interface ForecastDayDto {
  date: string;
  maxTempC: number;
  minTempC: number;
  avgTempC: number;
  conditionText: string;
  conditionIcon: string;
}

export interface WeatherAlertDto {
  headline: string;
  msgType: string;
  severity: string;
  urgency: string;
  areas: string;
  certainty: string;
  event: string;
  note: string;
  effective: string;
  expires: string;
  desc: string;
  instruction: string;
}

export interface WeatherForecastDto {
  location: LocationDto;
  forecastDays: ForecastDayDto[];
  alerts: WeatherAlertDto[];
}

export interface ItineraryDayWeatherResponse {
  dayId: string;
  date: string;
  weather: WeatherForecastDto;
}

@Injectable({
  providedIn: 'root'
})
export class WeatherService {
  private readonly apiUrl = '/api/trips';

  constructor(
    private readonly http: HttpClient
  ) {}

  getTripWeather(tripId: string): Observable<ApiResponse<ItineraryDayWeatherResponse[]>> {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    const token = localStorage.getItem('access_token');
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }

    return this.http.get<ApiResponse<ItineraryDayWeatherResponse[]>>(`${this.apiUrl}/${tripId}/weather`, { headers });
  }
}

