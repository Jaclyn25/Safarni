import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TripResponse, TripService } from '../../../services/trip.service';
import { destinationImageUrl } from '../../../shared/constants/destination-images.constants';

@Component({
  selector: 'app-profile-trips',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './profile-trips.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileTripsComponent implements OnInit {
  readonly trackByIndex = (index: number): number => index;
  readonly trips = signal<TripResponse[]>([]);
  readonly isTripsLoading = signal(true);
  readonly tripsError = signal<string | null>(null);
  readonly pendingDeleteTripId = signal<string | null>(null);
  readonly deletingTripId = signal<string | null>(null);

  constructor(
    private readonly tripService: TripService
  ) {}

  ngOnInit(): void {
    this.loadTrips();
  }

  loadTrips(): void {
    this.isTripsLoading.set(true);
    this.tripsError.set(null);

    this.tripService.getTrips().subscribe({
      next: (response) => {
        const trips = response.data ?? [];
        this.trips.set(trips);
        this.isTripsLoading.set(false);
      },
      error: (error: unknown) => {
        this.isTripsLoading.set(false);
        this.tripsError.set(
          this.tripService.getErrorMessage(
            error,
            'Could not load your trips. Please try again.'
          )
        );
      },
    });
  }

  tripDateRange(trip: TripResponse): string {
    const start = new Date(trip.startDate);
    const end = new Date(trip.endDate);

    const format = (date: Date): string =>
      date.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      });

    return `${format(start)} – ${format(end)}`;
  }

  tripCityNames(trip: TripResponse): string {
    return (trip.cities ?? [])
      .map((city) => city.name)
      .join(', ');
  }

  tripImage(trip: TripResponse): string {
    return destinationImageUrl(
      [trip.destination, ...(trip.cities ?? []).map((city) => city.name)].join(' ')
    );
  }

  tripBadge(trip: TripResponse): { label: string; classes: string } {
    switch (trip.itineraryStatus) {
      case 'completed':
        return {
          label: 'Itinerary ready',
          classes: 'badge--success'
        };
      case 'generating':
        return {
          label: 'Generating…',
          classes: 'badge--progress'
        };
      case 'failed':
        return {
          label: 'Generation failed',
          classes: 'badge--danger'
        };
      default:
        return {
          label: 'No itinerary yet',
          classes: 'badge--muted'
        };
    }
  }

  tripCost(trip: TripResponse): string | null {
    if (trip.estimatedTotalCost == null) {
      return null;
    }

    const currency = trip.itineraryCurrency ?? 'USD';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency
    }).format(trip.estimatedTotalCost);
  }

  requestDelete(tripId: string): void {
    this.pendingDeleteTripId.set(tripId);
    this.tripsError.set(null);
  }

  cancelDelete(): void {
    this.pendingDeleteTripId.set(null);
  }

  confirmDelete(tripId: string): void {
    this.deletingTripId.set(tripId);
    this.tripsError.set(null);

    this.tripService.deleteTrip(tripId).subscribe({
      next: () => {
        this.trips.update((trips) => trips.filter((trip) => trip.id !== tripId));
        this.tripService.forgetTrip(tripId);
        this.pendingDeleteTripId.set(null);
        this.deletingTripId.set(null);
      },
      error: (error: unknown) => {
        this.deletingTripId.set(null);
        this.tripsError.set(
          this.tripService.getErrorMessage(error, 'Could not delete this trip. Please try again.')
        );
      },
    });
  }
}
