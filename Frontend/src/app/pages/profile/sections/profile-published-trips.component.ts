import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  PublishedTripResponse,
  TripService,
  TripVisibility
} from '../../../services/trip.service';

@Component({
  selector: 'app-profile-published-trips',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './profile-published-trips.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfilePublishedTripsComponent implements OnInit {
  readonly publishedTrips = signal<PublishedTripResponse[]>([]);
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);
  readonly unpublishingTripId = signal<string | null>(null);

  constructor(
    private readonly tripService: TripService
  ) {}

  ngOnInit(): void {
    this.loadPublishedTrips();
  }

  loadPublishedTrips(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.tripService.getPublishedTrips().subscribe({
      next: (response) => {
        this.publishedTrips.set(response.data ?? []);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.loadError.set(
          this.tripService.getErrorMessage(
            error,
            'Could not load your published trips. Please try again.'
          )
        );
      },
    });
  }

  async unpublishTrip(trip: PublishedTripResponse): Promise<void> {
    if (this.unpublishingTripId()) {
      return;
    }

    const confirmed = window.confirm(
      `Unpublish "${trip.name}"? It will be removed from public access and discovery.`
    );

    if (!confirmed) {
      return;
    }

    this.unpublishingTripId.set(trip.id);
    this.loadError.set(null);
    this.actionMessage.set(null);

    try {
      await firstValueFrom(this.tripService.unpublishTrip(trip.id));
      this.actionMessage.set('Trip unpublished successfully.');
      this.loadPublishedTrips();
    } catch (error) {
      this.loadError.set(
        this.tripService.getErrorMessage(
          error,
          'Could not unpublish this trip. Please try again.'
        )
      );
    } finally {
      this.unpublishingTripId.set(null);
    }
  }

  tripDateRange(trip: PublishedTripResponse): string {
    const start = new Date(trip.startDate);
    const end = new Date(trip.endDate);

    const format = (date: Date): string =>
      date.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      });

    return `${format(start)} - ${format(end)}`;
  }

  visibilityBadge(trip: PublishedTripResponse): { label: string; classes: string } {
    switch ((trip.visibility ?? '').toLowerCase()) {
      case TripVisibility.Unlisted.toLowerCase():
        return {
          label: 'Unlisted',
          classes: 'badge--muted'
        };
      case TripVisibility.Private.toLowerCase():
        return {
          label: 'Private',
          classes: 'badge--danger'
        };
      default:
        return {
          label: 'Public',
          classes: 'badge--success'
        };
    }
  }

  statusBadge(trip: PublishedTripResponse): { label: string; classes: string } {
    if (trip.isPublished) {
      return {
        label: 'Published',
        classes: 'badge--success'
      };
    }

    return {
      label: 'Unpublished',
      classes: 'badge--muted'
    };
  }

  formatCount(value: number): string {
    return new Intl.NumberFormat('en-US').format(value);
  }

  totalPublishedTrips(): number {
    return this.publishedTrips().length;
  }

  totalViews(): number {
    return this.publishedTrips().reduce((total, trip) => total + trip.viewsCount, 0);
  }

  totalLikes(): number {
    return this.publishedTrips().reduce((total, trip) => total + trip.likesCount, 0);
  }

  totalSaves(): number {
    return this.publishedTrips().reduce((total, trip) => total + trip.savesCount, 0);
  }

  totalCopies(): number {
    return this.publishedTrips().reduce((total, trip) => total + trip.copiesCount, 0);
  }

  trackByTrip(
    _index: number,
    trip: PublishedTripResponse
  ): string {
    return trip.id;
  }
}