import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../services/auth.service';
import { MarketplaceService } from '../../services/marketplace.service';
import { BookingDocument, BookingTraveler, PackageBookingMessage, PackageBookingRequest } from '../../models/marketplace.models';
import { BookingChatPanelComponent } from '../../shared/booking-chat/booking-chat-panel.component';

@Component({
  selector: 'app-agency-trips',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, BookingChatPanelComponent],
  templateUrl: './agency-trips.component.html',
  styleUrl: './agency-trips.component.css'
})
export class AgencyTripsComponent implements OnInit {
  readonly requests = signal<PackageBookingRequest[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly actionId = signal<string | null>(null);
  readonly notice = signal('');
  readonly chatRequestId = signal<string | null>(null);
  readonly messages = signal<PackageBookingMessage[]>([]);
  readonly messageDraft = signal('');
  readonly activeChatRequest = computed(() => this.requests().find((request) => request.id === this.chatRequestId()) ?? null);
  readonly activeRequestCount = computed(() => this.requests().filter((request) => ['Pending', 'OfferSent', 'AwaitingPayment'].includes(request.status)).length);
  readonly confirmedRequestCount = computed(() => this.requests().filter((request) => ['Confirmed', 'InProgress', 'Completed'].includes(request.status)).length);
  readonly dataRequestId = signal<string | null>(null);
  readonly travelers = signal<BookingTraveler[]>([]);
  readonly documents = signal<BookingDocument[]>([]);
  selectedTravelerId: string | null = null;
  travelerFullName = '';
  travelerDateOfBirth = '';
  travelerNationality = '';
  travelerPassportNumber = '';
  travelerPassportExpiry = '';
  travelerEmail = '';
  travelerPhone = '';
  documentType = 'Passport';
  documentFile: File | null = null;

  constructor(private readonly marketplace: MarketplaceService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true); this.error.set('');
    this.marketplace.listTravelerBookings().subscribe({
      next: (requests) => { this.requests.set(requests); this.loading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load your agency trip requests.')); this.loading.set(false); }
    });
  }

  statusCopy(status: PackageBookingRequest['status']): string {
    if (status === 'Pending') return 'Waiting for agency review';
    if (status === 'OfferSent') return 'The agency sent an official offer for your review';
    if (status === 'AwaitingPayment') return 'Offer accepted, payment is the next step';
    if (status === 'Confirmed') return 'Your booking is confirmed';
    if (status === 'Accepted') return 'Your place is confirmed';
    if (status === 'Rejected') return 'The agency could not accept this request';
    if (status === 'Expired') return 'This offer expired';
    if (status === 'InProgress') return 'Your trip is in progress';
    if (status === 'Completed') return 'Trip completed';
    if (status === 'Refunded') return 'Payment refunded';
    if (status === 'Disputed') return 'Safarni is reviewing this booking';
    return 'Request cancelled';
  }

  statusLabel(status: PackageBookingRequest['status']): string {
    const labels: Partial<Record<PackageBookingRequest['status'], string>> = {
      Pending: 'Needs review',
      OfferSent: 'Offer ready',
      AwaitingPayment: 'Payment due',
      Confirmed: 'Confirmed',
      InProgress: 'In progress',
      Completed: 'Completed',
      Accepted: 'Confirmed',
      Rejected: 'Declined',
      Cancelled: 'Cancelled',
      CancelledByTraveler: 'Cancelled',
      CancelledByAgency: 'Cancelled',
      Expired: 'Expired',
      Refunded: 'Refunded',
      Disputed: 'Under review'
    };
    return labels[status] ?? status;
  }

  isPaymentDue(dueAt: string | null): boolean {
    return !dueAt || new Date(dueAt).getTime() <= Date.now();
  }

  acceptOffer(request: PackageBookingRequest): void {
    const offer = request.currentOffer;
    if (!offer) return;
    this.actionId.set(request.id); this.error.set(''); this.notice.set('');
    this.marketplace.acceptBookingOffer(request.id, offer.id).subscribe({
      next: (updated) => {
        this.actionId.set(null);
        this.requests.update((items) => items.map((item) => item.id === updated.id ? updated : item));
        this.notice.set('Offer accepted. Payment will be enabled in the next step.');
      },
      error: (error: unknown) => {
        this.actionId.set(null);
        this.error.set(getApiErrorMessage(error, 'We could not accept this offer.'));
      }
    });
  }

  payForBooking(request: PackageBookingRequest): void {
    this.actionId.set(request.id); this.error.set(''); this.notice.set('');
    this.marketplace.createBookingPayment(request.id).subscribe({
      next: (payment) => {
        this.actionId.set(null);
        if (payment.checkoutUrl) window.location.assign(payment.checkoutUrl);
        else this.error.set('The payment checkout link was not returned.');
      },
      error: (error: unknown) => {
        this.actionId.set(null);
        this.error.set(getApiErrorMessage(error, 'We could not start the booking payment.'));
      }
    });
  }

  refundBooking(request: PackageBookingRequest): void {
    const reason = window.prompt('Why would you like to request a refund?', 'My travel plans changed.')?.trim();
    if (!reason) return;
    this.actionId.set(request.id); this.error.set(''); this.notice.set('');
    this.marketplace.refundBooking(request.id, reason).subscribe({
      next: () => {
        this.actionId.set(null);
        this.requests.update((items) => items.map((item) => item.id === request.id ? { ...item, status: 'Refunded' } : item));
        this.notice.set('Your payment was refunded successfully.');
      },
      error: (error: unknown) => {
        this.actionId.set(null);
        this.error.set(getApiErrorMessage(error, 'We could not refund this booking.'));
      }
    });
  }

  openDispute(request: PackageBookingRequest): void {
    const reason = window.prompt('Describe the booking issue for Safarni support:')?.trim();
    if (!reason) return;
    const requested = window.prompt('Requested refund amount (optional):', request.currentOffer?.amount?.toString() ?? '');
    const amount = requested?.trim() ? Number(requested) : null;
    this.actionId.set(request.id); this.error.set(''); this.notice.set('');
    this.marketplace.openBookingDispute(request.id, reason, Number.isFinite(amount) ? amount : null).subscribe({
      next: () => { this.actionId.set(null); this.requests.update((items) => items.map((item) => item.id === request.id ? { ...item, status: 'Disputed' } : item)); this.notice.set('Your dispute was opened. Safarni support will review it before releasing funds.'); },
      error: (error: unknown) => { this.actionId.set(null); this.error.set(getApiErrorMessage(error, 'We could not open the dispute.')); }
    });
  }

  openChat(request: PackageBookingRequest): void {
    this.dataRequestId.set(null);
    this.chatRequestId.set(request.id); this.messageDraft.set('');
    this.marketplace.listBookingMessages(request.id).subscribe({
      next: (messages) => this.messages.set(messages),
      error: (error: unknown) => this.error.set(getApiErrorMessage(error, 'We could not load the booking chat.'))
    });
  }

  closeChat(): void { this.chatRequestId.set(null); this.messages.set([]); this.messageDraft.set(''); }

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

  openTravelerData(request: PackageBookingRequest): void {
    this.dataRequestId.set(request.id);
    this.marketplace.listBookingTravelers(request.id).subscribe({
      next: (travelers) => { this.travelers.set(travelers); if (travelers[0]) this.editTraveler(travelers[0]); },
      error: (error: unknown) => this.error.set(getApiErrorMessage(error, 'We could not load traveler data.'))
    });
    this.marketplace.listBookingDocuments(request.id).subscribe({
      next: (documents) => this.documents.set(documents),
      error: () => undefined
    });
  }

  editTraveler(traveler: BookingTraveler): void {
    this.selectedTravelerId = traveler.id;
    this.travelerFullName = traveler.fullName;
    this.travelerDateOfBirth = traveler.dateOfBirth?.slice(0, 10) ?? '';
    this.travelerNationality = traveler.nationality;
    this.travelerPassportNumber = traveler.passportNumber;
    this.travelerPassportExpiry = traveler.passportExpiry?.slice(0, 10) ?? '';
    this.travelerEmail = traveler.email;
    this.travelerPhone = traveler.phone;
  }

  newTraveler(): void {
    this.selectedTravelerId = null; this.travelerFullName = ''; this.travelerDateOfBirth = '';
    this.travelerNationality = ''; this.travelerPassportNumber = ''; this.travelerPassportExpiry = '';
    this.travelerEmail = ''; this.travelerPhone = '';
  }

  saveTraveler(request: PackageBookingRequest): void {
    this.actionId.set(request.id); this.error.set('');
    this.marketplace.saveBookingTraveler(request.id, {
      travelerId: this.selectedTravelerId,
      fullName: this.travelerFullName, dateOfBirth: this.travelerDateOfBirth || null,
      nationality: this.travelerNationality, passportNumber: this.travelerPassportNumber,
      passportExpiry: this.travelerPassportExpiry || null, email: this.travelerEmail, phone: this.travelerPhone
    }).subscribe({
      next: (traveler) => { this.travelers.update((items) => items.some(item => item.id === traveler.id) ? items.map(item => item.id === traveler.id ? traveler : item) : [...items, traveler]); this.editTraveler(traveler); this.actionId.set(null); },
      error: (error: unknown) => { this.actionId.set(null); this.error.set(getApiErrorMessage(error, 'We could not save traveler data.')); }
    });
  }

  selectDocument(event: Event): void { this.documentFile = (event.target as HTMLInputElement).files?.[0] ?? null; }

  uploadDocument(request: PackageBookingRequest): void {
    if (!this.documentFile) return;
    this.actionId.set(request.id); this.error.set('');
    this.marketplace.uploadBookingDocument(request.id, this.documentFile, this.documentType, this.selectedTravelerId).subscribe({
      next: (document) => { this.documents.update((items) => [document, ...items]); this.documentFile = null; this.actionId.set(null); },
      error: (error: unknown) => { this.actionId.set(null); this.error.set(getApiErrorMessage(error, 'We could not upload this document.')); }
    });
  }
}
