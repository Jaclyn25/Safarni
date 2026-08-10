import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiResponse } from './auth.service';
import { CheckoutSession, PricingCustomerType, PricingPlan, Subscription } from '../models/billing.models';

@Injectable({ providedIn: 'root' })
export class PricingService {
  constructor(private readonly http: HttpClient) {}

  listPlans(customerType?: PricingCustomerType): Observable<PricingPlan[]> {
    const suffix = customerType ? `?customerType=${customerType}` : '';
    return this.http.get<ApiResponse<PricingPlan[]>>(`/api/pricing/plans${suffix}`).pipe(map((response) => this.requireData(response)));
  }

  getSubscription(): Observable<Subscription> {
    return this.http.get<ApiResponse<Subscription>>('/api/pricing/subscription').pipe(map((response) => this.requireData(response)));
  }

  checkout(planId: string): Observable<CheckoutSession> {
    return this.http.post<ApiResponse<CheckoutSession>>('/api/pricing/checkout', { planId }).pipe(map((response) => this.requireData(response)));
  }

  cancelSubscription(): Observable<Subscription> {
    return this.http.post<ApiResponse<Subscription>>('/api/pricing/subscription/cancel', {}).pipe(map((response) => this.requireData(response)));
  }

  completeTestPayment(sessionId: string, success: boolean): Observable<Subscription> {
    return this.http.post<ApiResponse<Subscription>>(`/api/pricing/test/complete/${encodeURIComponent(sessionId)}?success=${success}`, {}).pipe(map((response) => this.requireData(response)));
  }

  private requireData<T>(response: ApiResponse<T>): T {
    if (!response.success || response.data === null) throw new Error(response.errors?.[0]?.message || response.message || 'The request failed.');
    return response.data;
  }
}
