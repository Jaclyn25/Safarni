import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PackageBookingMessage, PackageBookingRequest } from '../../../models/marketplace.models';
import { getApiErrorMessage } from '../../../services/auth.service';
import { MarketplaceService } from '../../../services/marketplace.service';
import { BookingChatPanelComponent } from '../../../shared/booking-chat/booking-chat-panel.component';
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
    AgencyLoadingSkeletonComponent,
    BookingChatPanelComponent
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
  readonly chatRequestId = signal<string | null>(null);
  readonly messages = signal<PackageBookingMessage[]>([]);
  readonly messageDraft = signal('');
  readonly activeChatRequest = computed(() => this.requests().find((request) => request.id === this.chatRequestId()) ?? null);
  offerAmount = 0;
  offerCurrency = 'USD';
  offerSupplierCost = 0;
  offerServiceFee = 0;
  offerProtectionFee = 0;
  offerExpiresAt = '';
  offerIncluded = '';
  offerExcluded = '';
  offerCancellationPolicy = '';
  offerAgencyNote = '';
  offerInitialPaymentPercentage = 30;
  offerFinalPaymentDueAt = '';
  evidenceSupplierType = 'Hotel';
  evidenceSupplierName = '';
  evidenceConfirmationCode = '';
  evidenceInvoiceAmount = 0;
  evidenceCurrency = 'USD';
  evidenceFile: File | null = null;

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
    this.closeChat();
    this.selectedRequest.set(request);
    this.responseMessage.set(request.agencyResponse || '');
    const offer = request.currentOffer;
    this.offerAmount = offer?.amount ?? 0;
    this.offerSupplierCost = offer?.supplierCostAmount ?? this.offerAmount;
    this.offerServiceFee = offer?.agencyServiceFeeAmount ?? 0;
    this.offerProtectionFee = offer?.safarniProtectionFeeAmount ?? 0;
    this.offerCurrency = offer?.currencyCode ?? 'USD';
    this.offerExpiresAt = offer?.expiresAt ? this.toDateTimeLocal(offer.expiresAt) : this.defaultOfferExpiry();
    this.offerIncluded = offer?.included ?? '';
    this.offerExcluded = offer?.excluded ?? '';
    this.offerCancellationPolicy = offer?.cancellationPolicy ?? '';
    this.offerAgencyNote = offer?.agencyNote ?? '';
    this.offerInitialPaymentPercentage = offer?.initialPaymentPercentage ?? 30;
    this.offerFinalPaymentDueAt = offer?.finalPaymentDueAt ? this.toDateTimeLocal(offer.finalPaymentDueAt) : '';
  }

  closeDetailPanel(): void {
    this.selectedRequest.set(null);
    this.responseMessage.set('');
    this.closeChat();
  }

  closeChat(): void {
    this.chatRequestId.set(null);
    this.messages.set([]);
    this.messageDraft.set('');
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

  sendOffer(request: PackageBookingRequest): void {
    if (this.offerAmount <= 0 || !this.offerCurrency.trim() || !this.offerExpiresAt) {
      this.error.set('Enter a valid amount, currency, and offer expiry.');
      return;
    }
    this.actionId.set(request.id);
    this.error.set('');
    this.notice.set('');
    this.marketplace.sendBookingOffer(request.id, {
      amount: this.offerAmount,
      supplierCostAmount: this.offerSupplierCost,
      agencyServiceFeeAmount: this.offerServiceFee,
      safarniProtectionFeeAmount: this.offerProtectionFee,
      currencyCode: this.offerCurrency.trim().toUpperCase(),
      expiresAt: new Date(this.offerExpiresAt).toISOString(),
      included: this.offerIncluded.trim() || null,
      excluded: this.offerExcluded.trim() || null,
      cancellationPolicy: this.offerCancellationPolicy.trim() || null,
      agencyNote: this.offerAgencyNote.trim() || null,
      initialPaymentPercentage: this.offerInitialPaymentPercentage,
      finalPaymentDueAt: this.offerFinalPaymentDueAt ? new Date(this.offerFinalPaymentDueAt).toISOString() : null
    }).subscribe({
      next: (updated) => {
        this.actionId.set(null);
        this.requests.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
        this.notice.set('Official offer sent to the traveler.');
        this.closeDetailPanel();
      },
      error: (error: unknown) => {
        this.actionId.set(null);
        this.error.set(getApiErrorMessage(error, 'We could not send this offer.'));
      }
    });
  }

  openChat(request: PackageBookingRequest): void {
    this.chatRequestId.set(request.id); this.messageDraft.set('');
    this.marketplace.listBookingMessages(request.id).subscribe({
      next: (messages) => this.messages.set(messages),
      error: (error: unknown) => this.error.set(getApiErrorMessage(error, 'We could not load the booking chat.'))
    });
  }

  sendMessage(request: PackageBookingRequest): void {
    const body = this.messageDraft().trim();
    if (!body) return;
    this.actionId.set(request.id);
    this.marketplace.sendBookingMessage(request.id, body).subscribe({
      next: (message) => {
        this.messages.update((items) => [...items, message]);
        this.messageDraft.set(''); this.actionId.set(null);
      },
      error: (error: unknown) => {
        this.actionId.set(null);
        this.error.set(getApiErrorMessage(error, 'We could not send the message.'));
      }
    });
  }

  selectEvidenceFile(event: Event): void {
    this.evidenceFile = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  submitEvidence(request: PackageBookingRequest): void {
    if (!this.evidenceFile || !this.evidenceSupplierName.trim() || !this.evidenceConfirmationCode.trim() || this.evidenceInvoiceAmount <= 0) {
      this.error.set('Complete the supplier details and attach the official proof file.');
      return;
    }
    this.actionId.set(request.id); this.error.set(''); this.notice.set('');
    this.marketplace.submitSupplierEvidence(request.id, {
      supplierType: this.evidenceSupplierType,
      supplierName: this.evidenceSupplierName.trim(),
      confirmationCode: this.evidenceConfirmationCode.trim(),
      invoiceAmount: this.evidenceInvoiceAmount,
      currencyCode: this.evidenceCurrency.trim().toUpperCase(),
      file: this.evidenceFile
    }).subscribe({
      next: () => { this.actionId.set(null); this.evidenceFile = null; this.notice.set('Supplier proof submitted to Safarni for review.'); },
      error: (error: unknown) => { this.actionId.set(null); this.error.set(getApiErrorMessage(error, 'Supplier proof could not be submitted.')); }
    });
  }

  statusLabel(status: PackageBookingRequest['status']): string {
    return status === 'Pending' ? 'Needs Review' : status.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  private defaultOfferExpiry(): string {
    const expiry = new Date(Date.now() + 48 * 60 * 60 * 1000);
    expiry.setSeconds(0, 0);
    return this.toDateTimeLocal(expiry.toISOString());
  }

  private toDateTimeLocal(value: string): string {
    const date = new Date(value);
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
  }
}
