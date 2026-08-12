import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, getApiErrorMessage } from '../../services/auth.service';
import { PricingService } from '../../services/pricing.service';
import { CurrencyService } from '../../services/currency.service';
import { PricingCustomerType, PricingPlan, Subscription } from '../../models/billing.models';
import { CurrencyPipe } from '../../shared/pipes/currency.pipe';

@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [CommonModule, CurrencyPipe],
  templateUrl: './pricing.component.html',
  styleUrl: './pricing.component.css'
})
export class PricingComponent implements OnInit {
  readonly plans = signal<PricingPlan[]>([]);
  readonly subscription = signal<Subscription | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly actionError = signal('');
  readonly cancelling = signal(false);
  readonly customerType: PricingCustomerType;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly auth: AuthService,
    private readonly pricing: PricingService,
    private readonly currency: CurrencyService
  ) {
    this.customerType = (route.snapshot.data['customerType'] as PricingCustomerType | undefined)
      ?? (auth.getUserRole() === 'TravelAgency' ? 'Agency' : 'Traveler');
  }

  ngOnInit(): void {
    this.pricing.listPlans(this.customerType).subscribe({
      next: (plans) => { this.plans.set(plans); this.loading.set(false); this.loadSubscription(); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load pricing plans.')); this.loading.set(false); }
    });
  }

  get isAgency(): boolean { return this.customerType === 'Agency'; }
  get title(): string { return this.isAgency ? 'Grow your agency on Safarni.' : 'Plan more, with a little more room to explore.'; }
  get eyebrow(): string { return this.isAgency ? 'Agency marketplace' : 'Traveler Premium'; }
  get freePlanName(): string { return this.isAgency ? 'Agency Basic' : 'Traveler Basic'; }
  get freePlanDescription(): string {
    return this.isAgency
      ? 'Create your verified agency presence and publish up to three marketplace packages every day.'
      : 'Explore Safarni and generate up to three AI-powered itineraries every day.';
  }
  get freePlanBenefits(): readonly string[] {
    return this.isAgency
      ? ['Create 3 packages per day', 'Publish 3 packages per day', 'Build your verified agency presence']
      : ['3 AI itinerary generations per day', 'Explore destinations and marketplace packages', 'Create and manage your trips'];
  }
  get freePlanButtonLabel(): string {
    if (!this.auth.isAuthenticated()) return 'Start for free';
    return this.subscription()?.status === 'Active' ? 'Included with your account' : 'Your current plan';
  }

  price(plan: PricingPlan): string { return this.currency.format(plan.price, plan.currencyCode); }
  period(plan: PricingPlan): string { return plan.billingPeriod === 'Monthly' ? '/ month' : '/ year'; }
  statusLabel(status: string): string { return status.replace(/([A-Z])/g, ' $1').trim(); }
  date(value: string | null): string { return value ? new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : '—'; }
  isCurrent(plan: PricingPlan): boolean { return this.subscription()?.plan.id === plan.id && this.subscription()?.status === 'Active'; }

  startFree(): void {
    if (!this.auth.isAuthenticated()) void this.router.navigate(['/register']);
  }

  choose(plan: PricingPlan): void {
    this.actionError.set('');
    if (!this.auth.isAuthenticated()) { void this.router.navigate(['/login'], { queryParams: { returnUrl: this.isAgency ? '/agency/pricing' : '/pricing' } }); return; }
    this.pricing.checkout(plan.id).subscribe({
      next: (checkout) => {
        if (checkout.provider !== 'Test') {
          window.location.assign(checkout.checkoutUrl);
          return;
        }
        void this.router.navigate(['/pricing/checkout'], { queryParams: { sessionId: checkout.paymentSessionId } });
      },
      error: (error: unknown) => this.actionError.set(getApiErrorMessage(error, 'We could not start checkout.'))
    });
  }

  cancel(): void {
    this.cancelling.set(true); this.actionError.set('');
    this.pricing.cancelSubscription().subscribe({
      next: (subscription) => { this.subscription.set(subscription); this.cancelling.set(false); },
      error: (error: unknown) => { this.actionError.set(getApiErrorMessage(error, 'We could not schedule cancellation.')); this.cancelling.set(false); }
    });
  }

  private loadSubscription(): void {
    if (!this.auth.isAuthenticated()) return;
    this.pricing.getSubscription().subscribe({ next: (subscription) => this.subscription.set(subscription), error: () => undefined });
  }
}
