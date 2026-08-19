export type PricingCustomerType = 'Traveler' | 'Agency';
export type BillingPeriod = 'Monthly' | 'Yearly';
export type SubscriptionStatus = 'Pending' | 'Active' | 'PaymentFailed' | 'PastDue' | 'Cancelled' | 'Expired';

export interface PricingPlan {
  id: string;
  code: string;
  displayName: string;
  description: string;
  customerType: PricingCustomerType;
  billingPeriod: BillingPeriod;
  price: number;
  currencyCode: string;
  bookingCommissionRate: number;
  aiItineraryGenerationsPerPeriod: number;
  maxPublishedPackages: number;
  benefits: string[];
  isActive: boolean;
}

export interface Subscription {
  id: string;
  plan: PricingPlan;
  status: SubscriptionStatus;
  startAt: string | null;
  currentPeriodEnd: string | null;
  cancelAtPeriodEnd: boolean;
  paymentSessionId: string | null;
}

export interface CheckoutSession {
  subscriptionId: string;
  provider: string;
  paymentSessionId: string;
  checkoutUrl: string;
  subscription: Subscription;
}
