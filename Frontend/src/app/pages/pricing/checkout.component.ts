import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService, getApiErrorMessage } from '../../services/auth.service';
import { PricingService } from '../../services/pricing.service';
import { Subscription } from '../../models/billing.models';
import { BookingPayment } from '../../models/marketplace.models';
import { MarketplaceService } from '../../services/marketplace.service';

@Component({ selector: 'app-pricing-checkout', standalone: true, imports: [CommonModule, RouterLink], templateUrl: './checkout.component.html', styleUrl: './checkout.component.css' })
export class CheckoutComponent implements OnInit {
  readonly sessionId = signal('');
  readonly state = signal<'pending' | 'success' | 'failure' | 'cancelled'>('pending');
  readonly subscription = signal<Subscription | null>(null);
  readonly error = signal('');
  readonly processing = signal(false);
  readonly stripeCheckout = signal(false);
  readonly bookingPayment = signal<BookingPayment | null>(null);
  readonly bookingCheckout = signal(false);
  readonly bookingRequestId = signal('');

  constructor(
    private readonly route: ActivatedRoute,
    private readonly pricing: PricingService,
    private readonly marketplace: MarketplaceService,
    private readonly auth: AuthService) {}

  ngOnInit(): void {
    const status = this.route.snapshot.queryParamMap.get('status');
    this.bookingCheckout.set(this.route.snapshot.queryParamMap.get('type') === 'booking');
    this.bookingRequestId.set(this.route.snapshot.queryParamMap.get('bookingRequestId') ?? '');
    const sessionId = this.route.snapshot.queryParamMap.get('session_id')
      ?? this.route.snapshot.queryParamMap.get('sessionId');
    this.sessionId.set(sessionId ?? '');
    if (status === 'cancelled') this.state.set('cancelled');
    if (status === 'success') {
      this.stripeCheckout.set(true);
      if (this.bookingCheckout()) this.confirmStripeBooking();
      else this.confirmStripeSubscription();
    } else if (!sessionId) {
      this.state.set('cancelled');
    }
    if (!this.auth.isAuthenticated()) this.error.set('Sign in to continue with this checkout.');
  }

  complete(success: boolean): void {
    if (!this.sessionId() || !this.auth.isAuthenticated()) return;
    this.processing.set(true); this.error.set('');
    const operation: Observable<BookingPayment | Subscription> = this.bookingCheckout()
      ? this.marketplace.completeBookingTestPayment(this.sessionId(), success)
      : this.pricing.completeTestPayment(this.sessionId(), success);
    operation.subscribe({
      next: (result) => {
        if (this.bookingCheckout()) this.bookingPayment.set(result as BookingPayment);
        else this.subscription.set(result as Subscription);
        this.state.set(success ? 'success' : 'failure'); this.processing.set(false);
      },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not confirm this payment.')); this.processing.set(false); }
    });
  }

  private confirmStripeSubscription(attempt = 0): void {
    if (!this.auth.isAuthenticated()) return;
    this.pricing.getSubscription().subscribe({
      next: (subscription) => {
        this.subscription.set(subscription);
        if (subscription.status === 'Active') {
          this.state.set('success');
          return;
        }
        if (attempt < 5) {
          window.setTimeout(() => this.confirmStripeSubscription(attempt + 1), 1000);
          return;
        }
        this.error.set('Payment received. We are waiting for Stripe to confirm the subscription. Refresh in a moment.');
      },
      error: (error: unknown) => {
        if (attempt < 5) {
          window.setTimeout(() => this.confirmStripeSubscription(attempt + 1), 1000);
          return;
        }
        this.error.set(getApiErrorMessage(error, 'We could not confirm the Stripe payment yet.'));
      }
    });
  }

  private confirmStripeBooking(attempt = 0): void {
    if (!this.auth.isAuthenticated()) return;
    if (!this.bookingRequestId()) {
      this.error.set('The booking reference was not returned by the payment provider. Open My Agency Trips to check the booking status.');
      return;
    }

    this.marketplace.listTravelerBookings().subscribe({
      next: (requests) => {
        const booking = requests.find((request) => request.id === this.bookingRequestId());
        if (booking && ['Confirmed', 'InProgress', 'Completed'].includes(booking.status)) {
          this.state.set('success');
          return;
        }
        if (attempt < 10) {
          window.setTimeout(() => this.confirmStripeBooking(attempt + 1), 1000);
          return;
        }
        this.error.set('Payment was returned successfully. We are waiting for the booking confirmation webhook. Open My Agency Trips in a moment.');
      },
      error: (error: unknown) => {
        if (attempt < 10) {
          window.setTimeout(() => this.confirmStripeBooking(attempt + 1), 1000);
          return;
        }
        this.error.set(getApiErrorMessage(error, 'We could not confirm the booking payment yet.'));
      }
    });
  }
}
