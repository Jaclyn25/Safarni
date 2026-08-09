import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiResponse } from './auth.service';
import {
  MarketplaceFilters,
  MarketplacePage,
  PackageBookingRequest,
  TravelPackage,
  TravelPackageRequest,
  TravelPackageDayRequest
} from '../models/marketplace.models';

@Injectable({ providedIn: 'root' })
export class MarketplaceService {
  constructor(private readonly http: HttpClient) {}

  list(filters: MarketplaceFilters = {}): Observable<MarketplacePage> {
    let params = new HttpParams()
      .set('page', filters.page ?? 1)
      .set('pageSize', filters.pageSize ?? 12);
    if (filters.destination?.trim()) params = params.set('destination', filters.destination.trim());
    if (filters.startDate) params = params.set('startDate', filters.startDate);
    if (filters.durationDays) params = params.set('durationDays', filters.durationDays);
    if (filters.minPrice !== null && filters.minPrice !== undefined) params = params.set('minPrice', filters.minPrice);
    if (filters.maxPrice !== null && filters.maxPrice !== undefined) params = params.set('maxPrice', filters.maxPrice);
    return this.http.get<ApiResponse<MarketplacePage>>('/api/marketplace/packages', { params })
      .pipe(map((response) => this.requireData(response)));
  }

  get(packageId: string): Observable<TravelPackage> {
    return this.http.get<ApiResponse<TravelPackage>>(`/api/marketplace/packages/${packageId}`)
      .pipe(map((response) => this.requireData(response)));
  }

  listAgencyPackages(): Observable<TravelPackage[]> {
    return this.http.get<ApiResponse<TravelPackage[]>>('/api/agency/packages')
      .pipe(map((response) => this.requireData(response)));
  }

  createAgencyPackage(request: TravelPackageRequest): Observable<TravelPackage> {
    return this.http.post<ApiResponse<TravelPackage>>('/api/agency/packages', request)
      .pipe(map((response) => this.requireData(response)));
  }

  updateAgencyPackage(packageId: string, request: TravelPackageRequest): Observable<TravelPackage> {
    return this.http.put<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  publish(packageId: string): Observable<TravelPackage> { return this.mutate(`/api/agency/packages/${packageId}/publish`); }
  unpublish(packageId: string): Observable<TravelPackage> { return this.mutate(`/api/agency/packages/${packageId}/unpublish`); }
  deactivate(packageId: string): Observable<TravelPackage> { return this.mutate(`/api/agency/packages/${packageId}/deactivate`); }

  addDay(packageId: string, request: TravelPackageDayRequest): Observable<TravelPackage> {
    return this.http.post<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/days`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  updateDay(packageId: string, dayId: string, request: TravelPackageDayRequest): Observable<TravelPackage> {
    return this.http.put<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/days/${dayId}`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  deleteDay(packageId: string, dayId: string): Observable<TravelPackage> {
    return this.http.delete<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/days/${dayId}`)
      .pipe(map((response) => this.requireData(response)));
  }

  uploadPackageImage(packageId: string, file: File): Observable<TravelPackage> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/image`, form)
      .pipe(map((response) => this.requireData(response)));
  }

  createBooking(packageId: string, travelerCount: number, message: string | null): Observable<PackageBookingRequest> {
    return this.http.post<ApiResponse<PackageBookingRequest>>(
      `/api/marketplace/packages/${packageId}/booking-requests`, { travelerCount, message })
      .pipe(map((response) => this.requireData(response)));
  }

  listTravelerBookings(): Observable<PackageBookingRequest[]> {
    return this.http.get<ApiResponse<PackageBookingRequest[]>>('/api/booking-requests')
      .pipe(map((response) => this.requireData(response)));
  }

  listAgencyBookings(): Observable<PackageBookingRequest[]> {
    return this.http.get<ApiResponse<PackageBookingRequest[]>>('/api/agency/booking-requests')
      .pipe(map((response) => this.requireData(response)));
  }

  acceptBooking(requestId: string, response: string | null): Observable<PackageBookingRequest> {
    return this.processBooking(`/api/agency/booking-requests/${requestId}/accept`, response);
  }

  rejectBooking(requestId: string, response: string): Observable<PackageBookingRequest> {
    return this.processBooking(`/api/agency/booking-requests/${requestId}/reject`, response);
  }

  private processBooking(url: string, response: string | null): Observable<PackageBookingRequest> {
    return this.http.post<ApiResponse<PackageBookingRequest>>(url, { response })
      .pipe(map((result) => this.requireData(result)));
  }

  private mutate(url: string): Observable<TravelPackage> {
    return this.http.post<ApiResponse<TravelPackage>>(url, {})
      .pipe(map((response) => this.requireData(response)));
  }

  private requireData<T>(response: ApiResponse<T>): T {
    if (!response.success || response.data === null) {
      throw new Error(response.errors?.[0]?.message || response.message || 'The request failed.');
    }
    return response.data;
  }
}
