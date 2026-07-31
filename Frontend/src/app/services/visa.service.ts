import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from './itinerary.service';

export interface VisaApplicationResponse {
  id: string;
  userId: string;
  destinationCountry: string;
  travelPurpose: string;
  plannedTravelDate: string;
  visaType: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateVisaApplicationRequest {
  destinationCountry: string;
  travelPurpose: string;
  plannedTravelDate: string;
}

export const COMMON_VISA_TYPES = [
  'Tourist',
  'Business',
  'Student',
  'Work',
  'Transit',
  'Medical'
] as const;

@Injectable({
  providedIn: 'root'
})
export class VisaService {
  private readonly apiUrl = '/api/visa-applications';

  constructor(private readonly http: HttpClient) {}

  list(): Observable<ApiResponse<VisaApplicationResponse[]>> {
    return this.http.get<ApiResponse<VisaApplicationResponse[]>>(this.apiUrl);
  }

  create(command: CreateVisaApplicationRequest): Observable<ApiResponse<VisaApplicationResponse>> {
    return this.http.post<ApiResponse<VisaApplicationResponse>>(this.apiUrl, command);
  }

  selectVisaType(id: string, visaType: string): Observable<ApiResponse<VisaApplicationResponse>> {
    return this.http.put<ApiResponse<VisaApplicationResponse>>(
      `${this.apiUrl}/${id}/visa-type`,
      { visaType }
    );
  }
}
