import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { getApiErrorMessage, AuthService } from '../../services/auth.service';
import { MarketplaceService } from '../../services/marketplace.service';
import { TravelPackage } from '../../models/marketplace.models';

@Component({
  selector: 'app-package-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './package-details.component.html',
  styleUrl: './package-details.component.css'
})
export class PackageDetailsComponent implements OnInit {
  readonly package = signal<TravelPackage | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly bookingError = signal('');
  readonly bookingSuccess = signal('');
  readonly saving = signal(false);
  travelerCount = 1;
  message = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly marketplace: MarketplaceService,
    private readonly auth: AuthService
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    const id = this.route.snapshot.paramMap.get('packageId');
    if (!id) { this.error.set('This package link is incomplete.'); this.loading.set(false); return; }
    this.marketplace.get(id).subscribe({
      next: (result) => { this.package.set(result); this.loading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load this package.')); this.loading.set(false); }
    });
  }

  isTraveler(): boolean { return this.auth.getUserRole() === 'User'; }
  price(): string { const item = this.package(); return item ? new Intl.NumberFormat('en-US', { style: 'currency', currency: item.currencyCode }).format(item.price) : ''; }
  date(value: string): string { return new Date(value).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' }); }

  book(): void {
    const item = this.package();
    if (!item) return;
    this.bookingError.set(''); this.bookingSuccess.set('');
    if (!this.isTraveler()) { this.bookingError.set('Sign in as a traveler to request this package.'); return; }
    if (!Number.isInteger(this.travelerCount) || this.travelerCount < 1 || this.travelerCount > item.availableSeats) { this.bookingError.set(`Choose between 1 and ${item.availableSeats} travelers.`); return; }
    this.saving.set(true);
    this.marketplace.createBooking(item.id, this.travelerCount, this.message.trim() || null).subscribe({
      next: () => { this.saving.set(false); this.bookingSuccess.set('Your request is with the agency. You can track its status from Bookings.'); this.message = ''; },
      error: (error: unknown) => { this.saving.set(false); this.bookingError.set(getApiErrorMessage(error, 'We could not send your booking request.')); }
    });
  }
}
