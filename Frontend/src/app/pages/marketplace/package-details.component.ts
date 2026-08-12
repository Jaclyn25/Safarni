import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { getApiErrorMessage, AuthService } from '../../services/auth.service';
import { MarketplaceService } from '../../services/marketplace.service';
import { CurrencyService } from '../../services/currency.service';
import { TravelPackage } from '../../models/marketplace.models';
import { TravelCountry, TripService } from '../../services/trip.service';
import { orderedPackageImages, packageCoverUrl } from '../../shared/constants/marketplace.constants';

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
  adultCount = 1;
  childCount = 0;
  childrenAges: number[] = [];
  contactPhone = '';
  alternatePhone = '';
  country = '';
  city = '';
  countries: TravelCountry[] = [];
  cities: string[] = [];
  message = '';
  galleryIndex = 0;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly marketplace: MarketplaceService,
    private readonly auth: AuthService,
    private readonly trips: TripService,
    private readonly currency: CurrencyService
  ) {}

  ngOnInit(): void {
    this.load();
    this.trips.getCountries().subscribe({ next: (response) => this.countries = response.data ?? [], error: () => undefined });
  }

  load(): void {
    const id = this.route.snapshot.paramMap.get('packageId');
    if (!id) { this.error.set('This package link is incomplete.'); this.loading.set(false); return; }
    this.marketplace.get(id).subscribe({
      next: (result) => { this.package.set(result); this.loading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load this package.')); this.loading.set(false); }
    });
  }

  isTraveler(): boolean { return this.auth.getUserRole() === 'User'; }
  price(): string { const item = this.package(); return item ? this.currency.format(item.price, item.currencyCode) : ''; }
  date(value: string): string { return new Date(value).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' }); }
  totalTravelers(): number { return this.adultCount + this.childrenAges.length; }
  coverUrl(packageItem: TravelPackage): string { return packageCoverUrl(packageItem); }
  galleryImages(packageItem: TravelPackage) { return orderedPackageImages(packageItem); }
  activeImageUrl(packageItem: TravelPackage): string {
    return this.galleryImages(packageItem)[this.galleryIndex]?.url ?? this.coverUrl(packageItem);
  }

  updateChildren(): void {
    const count = Math.max(0, Math.min(20, Number(this.childCount) || 0));
    this.childCount = count;
    this.childrenAges = Array.from({ length: count }, (_, index) => this.childrenAges[index] ?? 6);
  }

  loadHomeCities(): void {
    this.city = '';
    this.cities = [];
    if (!this.country) return;
    this.trips.getCities(this.country).subscribe({ next: (response) => this.cities = response.data ?? [], error: () => undefined });
  }

  book(): void {
    const item = this.package();
    if (!item) return;
    this.bookingError.set(''); this.bookingSuccess.set('');
    if (!this.isTraveler()) { this.bookingError.set('Sign in as a traveler to request this package.'); return; }
    const total = this.totalTravelers();
    if (!Number.isInteger(this.adultCount) || this.adultCount < 1 || total > item.remainingSeats) { this.bookingError.set(`Choose at least one adult and no more than ${item.remainingSeats} travelers.`); return; }
    if (!this.contactPhone.trim() || !this.country.trim() || !this.city.trim()) { this.bookingError.set('Add your phone number, country, and city so the agency can contact you.'); return; }
    if (this.childrenAges.some(age => !Number.isInteger(Number(age)) || Number(age) < 0 || Number(age) > 17)) { this.bookingError.set('Each child age must be between 0 and 17.'); return; }
    this.saving.set(true);
    this.marketplace.createBooking(item.id, {
      adultCount: this.adultCount,
      childrenAges: this.childrenAges.map(Number),
      contactPhone: this.contactPhone.trim(), alternatePhone: this.alternatePhone.trim() || null,
      country: this.country.trim(), city: this.city.trim(), message: this.message.trim() || null
    }).subscribe({
      next: () => { this.saving.set(false); this.bookingSuccess.set('Your request is with the agency. You can track its status from Bookings.'); this.message = ''; },
      error: (error: unknown) => { this.saving.set(false); this.bookingError.set(getApiErrorMessage(error, 'We could not send your booking request.')); }
    });
  }
}
