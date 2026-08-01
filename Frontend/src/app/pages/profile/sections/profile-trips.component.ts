import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TripResponse, TripService } from '../../../services/trip.service';

@Component({
  selector: 'app-profile-trips',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './profile-trips.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileTripsComponent implements OnInit {
  readonly trips = signal<TripResponse[]>([]);
  readonly isTripsLoading = signal(true);
  readonly tripsError = signal<string | null>(null);

  constructor(private readonly tripService: TripService) {}

  ngOnInit(): void {
    this.loadTrips();
  }

  loadTrips(): void {
    this.isTripsLoading.set(true);
    this.tripsError.set(null);

    this.tripService.getTrips().subscribe({
      next: (response) => {
        this.trips.set(response.data ?? []);
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
}
