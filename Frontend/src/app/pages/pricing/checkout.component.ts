import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService, getApiErrorMessage } from '../../services/auth.service';
import { PricingService } from '../../services/pricing.service';
import { Subscription } from '../../models/billing.models';

@Component({ selector: 'app-pricing-checkout', standalone: true, imports: [CommonModule, RouterLink], templateUrl: './checkout.component.html', styleUrl: './checkout.component.css' })
export class CheckoutComponent implements OnInit {
  readonly sessionId = signal('');
  readonly state = signal<'pending' | 'success' | 'failure' | 'cancelled'>('pending');
  readonly subscription = signal<Subscription | null>(null);
  readonly error = signal('');
  readonly processing = signal(false);
  readonly stripeCheckout = signal(false);

  constructor(private readonly route: ActivatedRoute, private readonly pricing: PricingService, private readonly auth: AuthService) {}

  ngOnInit(): void {
    const status = this.route.snapshot.queryParamMap.get('status');
    const sessionId = this.route.snapshot.queryParamMap.get('session_id')
      ?? this.route.snapshot.queryParamMap.get('sessionId');
    this.sessionId.set(sessionId ?? '');
    if (status === 'cancelled') this.state.set('cancelled');
    if (status === 'success') {
      this.stripeCheckout.set(true);
      this.confirmStripeSubscription();
    } else if (!sessionId) {
      this.state.set('cancelled');
    }
    if (!this.auth.isAuthenticated()) this.error.set('Sign in to continue with this checkout.');
  }

  complete(success: boolean): void {
    if (!this.sessionId() || !this.auth.isAuthenticated()) return;
    this.processing.set(true); this.error.set('');
    this.pricing.completeTestPayment(this.sessionId(), success).subscribe({
      next: (subscription) => { this.subscription.set(subscription); this.state.set(success ? 'success' : 'failure'); this.processing.set(false); },
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
}
