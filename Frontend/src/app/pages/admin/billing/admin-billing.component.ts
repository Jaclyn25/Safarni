import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminBillingOverview, AdminBillingPlan } from '../../../models/admin.models';
import { AdminService } from '../../../services/admin.service';

@Component({
  selector: 'app-admin-billing',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-billing.component.html',
})
export class AdminBillingComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly error = signal<string | null>(null);
  readonly notice = signal<{ kind: 'success' | 'error'; text: string } | null>(null);
  readonly savingPlanId = signal<string | null>(null);

  overview: AdminBillingOverview | null = null;
  drafts: Record<string, { price: number; currencyCode: string; aiItineraryGenerationsPerPeriod: number; maxPublishedPackages: number; bookingCommissionRate: number; isActive: boolean }> = {};

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.adminService.getBillingOverview().subscribe({
      next: (overview) => {
        this.overview = overview;
        this.drafts = Object.fromEntries(
          overview.plans.map((plan) => [plan.id, {
            price: plan.price,
            currencyCode: plan.currencyCode,
            aiItineraryGenerationsPerPeriod: plan.aiItineraryGenerationsPerPeriod,
            maxPublishedPackages: plan.maxPublishedPackages,
            bookingCommissionRate: plan.bookingCommissionRate,
            isActive: plan.isActive,
          }])
        );
        this.isLoading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(this.extractError(err, 'Could not load billing overview.'));
        this.isLoading.set(false);
      },
    });
  }

  draft(plan: AdminBillingPlan): { price: number; currencyCode: string; aiItineraryGenerationsPerPeriod: number; maxPublishedPackages: number; bookingCommissionRate: number; isActive: boolean } {
    return this.drafts[plan.id] ?? { price: plan.price, currencyCode: plan.currencyCode, aiItineraryGenerationsPerPeriod: plan.aiItineraryGenerationsPerPeriod, maxPublishedPackages: plan.maxPublishedPackages, bookingCommissionRate: plan.bookingCommissionRate, isActive: plan.isActive };
  }

  save(plan: AdminBillingPlan): void {
    const draft = this.draft(plan);
    if (!Number.isFinite(Number(draft.price)) || Number(draft.price) < 0) {
      this.notice.set({ kind: 'error', text: 'Price must be zero or greater.' });
      return;
    }

    this.savingPlanId.set(plan.id);
    this.notice.set(null);
    this.adminService.updatePricingPlan(plan.id, {
      price: Number(draft.price),
      currencyCode: draft.currencyCode,
      aiItineraryGenerationsPerPeriod: Number(draft.aiItineraryGenerationsPerPeriod),
      maxPublishedPackages: Number(draft.maxPublishedPackages),
      bookingCommissionRate: Number(draft.bookingCommissionRate),
      isActive: draft.isActive,
    }).subscribe({
      next: () => {
        this.notice.set({ kind: 'success', text: `${plan.displayName} was updated successfully.` });
        this.savingPlanId.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.savingPlanId.set(null);
        this.notice.set({ kind: 'error', text: this.extractError(err, 'Pricing plan update failed.') });
      },
    });
  }

  formatStatus(status: string): string {
    return status === 'Succeeded' ? 'Paid' : status;
  }

  private extractError(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null) {
      const body = (error as { error?: { errors?: Array<{ message?: string }>; message?: string } }).error;
      return body?.errors?.[0]?.message ?? body?.message ?? fallback;
    }
    return fallback;
  }
}
