import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiResponse } from './auth.service';
import {
  MarketplaceFilters,
  MarketplacePage,
  BookingPayment,
  BookingDocument,
  BookingTraveler,
  PackageBookingMessage,
  SupplierEvidence,
  PackageBookingRequest,
  PackagePlaceSearchResult,
  PublicAgencyProfile,
  PublicAgencyReview,
  TravelPackage,
  TravelPackageCityRequest,
  TravelPackageItemRequest,
  TravelPackageItemType,
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

  getAgencyPackage(packageId: string): Observable<TravelPackage> {
    return this.http.get<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}`)
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

  addCity(packageId: string, request: TravelPackageCityRequest): Observable<TravelPackage> {
    return this.http.post<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/cities`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  updateCity(packageId: string, cityId: string, request: TravelPackageCityRequest): Observable<TravelPackage> {
    return this.http.put<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/cities/${cityId}`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  deleteCity(packageId: string, cityId: string): Observable<TravelPackage> {
    return this.http.delete<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/cities/${cityId}`)
      .pipe(map((response) => this.requireData(response)));
  }

  addItem(packageId: string, dayId: string, request: TravelPackageItemRequest): Observable<TravelPackage> {
    return this.http.post<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/days/${dayId}/items`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  updateItem(packageId: string, dayId: string, itemId: string, request: TravelPackageItemRequest): Observable<TravelPackage> {
    return this.http.put<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/days/${dayId}/items/${itemId}`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  deleteItem(packageId: string, dayId: string, itemId: string): Observable<TravelPackage> {
    return this.http.delete<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/days/${dayId}/items/${itemId}`)
      .pipe(map((response) => this.requireData(response)));
  }

  searchPlaces(packageId: string, dayId: string, input: {
    query?: string; type: TravelPackageItemType;
  }): Observable<PackagePlaceSearchResult[]> {
    const params = new HttpParams()
      .set('query', input.query ?? '').set('type', input.type);
    return this.http.get<ApiResponse<PackagePlaceSearchResult[]>>(
      `/api/agency/packages/${packageId}/days/${dayId}/place-search`, { params })
      .pipe(map((response) => this.requireData(response)));
  }

  uploadGalleryImage(packageId: string, file: File, options: {
    dayId?: string | null; itemId?: string | null; caption?: string; altText?: string; isCover?: boolean;
  } = {}): Observable<TravelPackage> {
    const form = new FormData();
    form.append('file', file, file.name);
    if (options.dayId) form.append('dayId', options.dayId);
    if (options.itemId) form.append('itemId', options.itemId);
    if (options.caption) form.append('caption', options.caption);
    if (options.altText) form.append('altText', options.altText);
    form.append('isCover', String(options.isCover ?? false));
    return this.http.post<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/images`, form)
      .pipe(map((response) => this.requireData(response)));
  }

  deleteGalleryImage(packageId: string, imageId: string): Observable<TravelPackage> {
    return this.http.delete<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/images/${imageId}`)
      .pipe(map((response) => this.requireData(response)));
  }

  uploadPackageImage(packageId: string, file: File): Observable<TravelPackage> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ApiResponse<TravelPackage>>(`/api/agency/packages/${packageId}/image`, form)
      .pipe(map((response) => this.requireData(response)));
  }

  createBooking(packageId: string, request: {
    adultCount: number; childrenAges: number[]; contactPhone: string; alternatePhone: string | null;
    country: string; city: string; message: string | null;
  }): Observable<PackageBookingRequest> {
    const travelerCount = request.adultCount + request.childrenAges.length;
    return this.http.post<ApiResponse<PackageBookingRequest>>(
      `/api/marketplace/packages/${packageId}/booking-requests`, { ...request, travelerCount })
      .pipe(map((response) => this.requireData(response)));
  }

  getPublicAgency(agencyId: string): Observable<PublicAgencyProfile> {
    return this.http.get<ApiResponse<PublicAgencyProfile>>(`/api/marketplace/agencies/${agencyId}`)
      .pipe(map((response) => this.requireData(response)));
  }

  createAgencyReview(agencyId: string, bookingRequestId: string, rating: number, comment: string): Observable<PublicAgencyReview> {
    return this.http.post<ApiResponse<PublicAgencyReview>>(`/api/marketplace/agencies/${agencyId}/reviews`,
      { bookingRequestId, rating, comment }).pipe(map((response) => this.requireData(response)));
  }

  listTravelerBookings(): Observable<PackageBookingRequest[]> {
    return this.http.get<ApiResponse<PackageBookingRequest[]>>('/api/booking-requests')
      .pipe(map((response) => this.requireData(response)));
  }

  listAgencyBookings(): Observable<PackageBookingRequest[]> {
    return this.http.get<ApiResponse<PackageBookingRequest[]>>('/api/agency/booking-requests')
      .pipe(map((response) => this.requireData(response)));
  }

  sendBookingOffer(requestId: string, request: {
    amount: number; supplierCostAmount?: number | null; agencyServiceFeeAmount?: number | null; safarniProtectionFeeAmount?: number | null; currencyCode: string; expiresAt: string;
    included?: string | null; excluded?: string | null;
    cancellationPolicy?: string | null; agencyNote?: string | null;
    initialPaymentPercentage?: number | null; finalPaymentDueAt?: string | null;
  }): Observable<PackageBookingRequest> {
    return this.http.post<ApiResponse<PackageBookingRequest>>(
      `/api/agency/booking-requests/${requestId}/offers`, request)
      .pipe(map((response) => this.requireData(response)));
  }

  acceptBookingOffer(requestId: string, offerId: string): Observable<PackageBookingRequest> {
    return this.http.post<ApiResponse<PackageBookingRequest>>(
      `/api/booking-requests/${requestId}/offers/${offerId}/accept`, {})
      .pipe(map((response) => this.requireData(response)));
  }

  createBookingPayment(requestId: string): Observable<BookingPayment> {
    return this.http.post<ApiResponse<BookingPayment>>(`/api/booking-requests/${requestId}/payment`, {})
      .pipe(map((response) => this.requireData(response)));
  }

  confirmBookingPayment(requestId: string, sessionId: string): Observable<boolean> {
    return this.http.post<ApiResponse<boolean>>(
      `/api/booking-requests/${requestId}/payment/confirm?sessionId=${encodeURIComponent(sessionId)}`, {})
      .pipe(map((response) => this.requireData(response)));
  }

  completeBookingTestPayment(sessionId: string, success: boolean): Observable<BookingPayment> {
    return this.http.post<ApiResponse<BookingPayment>>(
      `/api/booking-payments/test/complete/${encodeURIComponent(sessionId)}?success=${success}`, {})
      .pipe(map((response) => this.requireData(response)));
  }

  refundBooking(requestId: string, reason: string): Observable<BookingPayment> {
    return this.http.post<ApiResponse<BookingPayment>>(`/api/booking-requests/${requestId}/refund`, { reason })
      .pipe(map((response) => this.requireData(response)));
  }

  openBookingDispute(requestId: string, reason: string, requestedRefundAmount?: number | null): Observable<import('../models/marketplace.models').BookingDispute> {
    return this.http.post<ApiResponse<import('../models/marketplace.models').BookingDispute>>(
      `/api/booking-requests/${requestId}/disputes`, { reason, requestedRefundAmount: requestedRefundAmount ?? null })
      .pipe(map((response) => this.requireData(response)));
  }

  listBookingMessages(requestId: string): Observable<PackageBookingMessage[]> {
    return this.http.get<ApiResponse<PackageBookingMessage[]>>(`/api/booking-requests/${requestId}/messages`)
      .pipe(map((response) => this.requireData(response)));
  }

  sendBookingMessage(requestId: string, body: string): Observable<PackageBookingMessage> {
    return this.http.post<ApiResponse<PackageBookingMessage>>(`/api/booking-requests/${requestId}/messages`, { body })
      .pipe(map((response) => this.requireData(response)));
  }

  listBookingTravelers(requestId: string): Observable<BookingTraveler[]> {
    return this.http.get<ApiResponse<BookingTraveler[]>>(`/api/booking-requests/${requestId}/travelers`)
      .pipe(map((response) => this.requireData(response)));
  }

  saveBookingTraveler(requestId: string, traveler: Omit<BookingTraveler, 'id' | 'bookingRequestId' | 'createdAt' | 'updatedAt'> & { travelerId?: string | null }): Observable<BookingTraveler> {
    return this.http.post<ApiResponse<BookingTraveler>>(`/api/booking-requests/${requestId}/travelers`, traveler)
      .pipe(map((response) => this.requireData(response)));
  }

  listBookingDocuments(requestId: string): Observable<BookingDocument[]> {
    return this.http.get<ApiResponse<BookingDocument[]>>(`/api/booking-requests/${requestId}/documents`)
      .pipe(map((response) => this.requireData(response)));
  }

  uploadBookingDocument(requestId: string, file: File, documentType: string, travelerId?: string | null): Observable<BookingDocument> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('documentType', documentType);
    if (travelerId) form.append('travelerId', travelerId);
    return this.http.post<ApiResponse<BookingDocument>>(`/api/booking-requests/${requestId}/documents`, form)
      .pipe(map((response) => this.requireData(response)));
  }

  submitSupplierEvidence(requestId: string, request: {
    supplierType: string; supplierName: string; confirmationCode: string;
    invoiceAmount: number; currencyCode: string; file: File;
  }): Observable<SupplierEvidence> {
    const form = new FormData();
    form.append('supplierType', request.supplierType);
    form.append('supplierName', request.supplierName);
    form.append('confirmationCode', request.confirmationCode);
    form.append('invoiceAmount', String(request.invoiceAmount));
    form.append('currencyCode', request.currencyCode);
    form.append('file', request.file, request.file.name);
    return this.http.post<ApiResponse<SupplierEvidence>>(`/api/agency/booking-requests/${requestId}/supplier-evidence`, form)
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
