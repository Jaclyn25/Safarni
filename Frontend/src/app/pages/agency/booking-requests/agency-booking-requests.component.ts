import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PackageBookingRequest } from '../../../models/marketplace.models';
import { getApiErrorMessage } from '../../../services/auth.service';
import { MarketplaceService } from '../../../services/marketplace.service';
import { AgencyEmptyStateComponent } from '../shared/agency-empty-state.component';
import { AgencyKpiCardComponent } from '../shared/agency-kpi-card.component';
import { AgencyLoadingSkeletonComponent } from '../shared/agency-loading-skeleton.component';
import { AgencyPageHeaderComponent } from '../shared/agency-page-header.component';
import { AgencyStatusBadgeComponent } from '../shared/agency-status-badge.component';

type RequestFilterTab = 'all' | 'pending' | 'accepted' | 'rejected';

@Component({
  selector: 'app-agency-booking-requests',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    AgencyPageHeaderComponent,
    AgencyKpiCardComponent,
    AgencyStatusBadgeComponent,
    AgencyEmptyStateComponent,
    AgencyLoadingSkeletonComponent
  ],
  templateUrl: './agency-booking-requests.component.html'
})
export class AgencyBookingRequestsComponent implements OnInit {
  readonly requests = signal<PackageBookingRequest[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal('');
  readonly actionId = signal<string | null>(null);
  readonly activeTab = signal<RequestFilterTab>('all');
  readonly selectedRequest = signal<PackageBookingRequest | null>(null);
  readonly responseMessage = signal('');

  constructor(private readonly marketplace: MarketplaceService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.marketplace.listAgencyBookings().subscribe({
      next: (requests) => {
        this.requests.set(requests);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(getApiErrorMessage(error, 'We could not load booking requests.'));
        this.loading.set(false);
      }
    });
  }

  readonly filteredRequests = computed(() => {
    const list = this.requests();
    const tab = this.activeTab();
    if (tab === 'pending') return list.filter((r) => r.status === 'Pending');
    if (tab === 'accepted') return list.filter((r) => r.status === 'Accepted');
    if (tab === 'rejected') return list.filter((r) => r.status === 'Rejected');
    return list;
  });

  readonly countTotal = computed(() => this.requests().length);
  readonly countPending = computed(() => this.requests().filter((r) => r.status === 'Pending').length);
  readonly countAccepted = computed(() => this.requests().filter((r) => r.status === 'Accepted').length);
  readonly countRejected = computed(() => this.requests().filter((r) => r.status === 'Rejected').length);

  openDetailPanel(request: PackageBookingRequest): void {
    this.selectedRequest.set(request);
    this.responseMessage.set(request.agencyResponse || '');
  }

  closeDetailPanel(): void {
    this.selectedRequest.set(null);
    this.responseMessage.set('');
  }

  process(request: PackageBookingRequest, action: 'accept' | 'reject'): void {
    const response = this.responseMessage().trim();
    if (action === 'reject' && !response) {
      this.error.set('Please provide a short reason before declining a booking request.');
      return;
    }

    this.actionId.set(request.id);
    this.error.set('');
    this.notice.set('');

    const operation =
      action === 'accept'
        ? this.marketplace.acceptBooking(request.id, response || null)
        : this.marketplace.rejectBooking(request.id, response);

    operation.subscribe({
      next: (updated) => {
        this.actionId.set(null);
        this.requests.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
        this.notice.set(action === 'accept' ? 'Booking request accepted!' : 'Booking request declined.');
        this.closeDetailPanel();
      },
      error: (error: unknown) => {
        this.actionId.set(null);
        this.error.set(getApiErrorMessage(error, 'We could not update this booking request.'));
      }
    });
  }

  statusLabel(status: PackageBookingRequest['status']): string {
    return status === 'Pending' ? 'Needs Review' : status;
  }
}
