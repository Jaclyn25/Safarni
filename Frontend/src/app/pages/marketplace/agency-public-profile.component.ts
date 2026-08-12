import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PackageBookingRequest, PublicAgencyProfile } from '../../models/marketplace.models';
import { AuthService, getApiErrorMessage } from '../../services/auth.service';
import { MarketplaceService } from '../../services/marketplace.service';
import { CurrencyService } from '../../services/currency.service';
import { packageCoverUrl } from '../../shared/constants/marketplace.constants';

@Component({
  selector: 'app-agency-public-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './agency-public-profile.component.html',
  styleUrl: './agency-public-profile.component.css'
})
export class AgencyPublicProfileComponent implements OnInit {
  readonly profile = signal<PublicAgencyProfile | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly reviewError = signal('');
  readonly reviewSuccess = signal('');
  readonly savingReview = signal(false);
  eligibleBookings: PackageBookingRequest[] = [];
  bookingRequestId = '';
  rating = 5;
  comment = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly marketplace: MarketplaceService,
    private readonly auth: AuthService,
    private readonly currency: CurrencyService
  ) {}

  ngOnInit(): void {
    const agencyId = this.route.snapshot.paramMap.get('agencyId');
    if (!agencyId) { this.error.set('This agency link is incomplete.'); this.loading.set(false); return; }
    this.marketplace.getPublicAgency(agencyId).subscribe({
      next: (profile) => { this.profile.set(profile); this.loading.set(false); this.loadEligibleBookings(profile.agencyId); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load this agency.')); this.loading.set(false); }
    });
  }

  isTraveler(): boolean { return this.auth.getUserRole() === 'User'; }
  date(value: string): string { return new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }); }
  price(value: number, currency: string): string { return this.currency.format(value, currency); }
  stars(value: number): number[] { return Array.from({ length: 5 }, (_, index) => index < Math.round(value) ? 1 : 0); }
  packageCover(packageItem: PublicAgencyProfile['packages'][number]): string { return packageCoverUrl(packageItem); }

  submitReview(): void {
    const profile = this.profile();
    if (!profile || !this.bookingRequestId || !this.comment.trim()) {
      this.reviewError.set('Choose a completed booking and write your review.');
      return;
    }
    this.savingReview.set(true); this.reviewError.set(''); this.reviewSuccess.set('');
    this.marketplace.createAgencyReview(profile.agencyId, this.bookingRequestId, this.rating, this.comment.trim()).subscribe({
      next: (review) => {
        const reviewedBookingId = this.bookingRequestId;
        this.savingReview.set(false); this.reviewSuccess.set('Thank you—your verified review is now published.');
        this.comment = ''; this.bookingRequestId = '';
        this.eligibleBookings = this.eligibleBookings.filter(booking => booking.id !== reviewedBookingId);
        this.profile.update(current => {
          if (!current) return current;
          const reviewCount = current.reviewCount + 1;
          return {
            ...current,
            recentReviews: [review, ...current.recentReviews],
            reviewCount,
            averageRating: ((current.averageRating * current.reviewCount) + review.rating) / reviewCount
          };
        });
      },
      error: (error: unknown) => { this.savingReview.set(false); this.reviewError.set(getApiErrorMessage(error, 'This booking is not eligible for a review yet.')); }
    });
  }

  private loadEligibleBookings(agencyId: string): void {
    if (!this.isTraveler()) return;
    this.marketplace.listTravelerBookings().subscribe({
      next: (bookings) => this.eligibleBookings = bookings.filter(booking =>
        booking.travelAgencyId === agencyId && booking.status === 'Accepted' && !booking.hasReview &&
        new Date(booking.packageEndDate).getTime() < new Date().setHours(0, 0, 0, 0)),
      error: () => undefined
    });
  }
}
