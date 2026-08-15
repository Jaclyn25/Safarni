import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import { PublicTripService, PublicTripSearchResponse, SearchPublicTripsRequest } from '../../services/public-trip.service';
import { CurrencyService } from '../../services/currency.service';
import { marked } from 'marked';


@Component({
  selector: 'app-public-trips',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './public-trips.component.html',
  styleUrl: './public-trips.component.css'
})
export class PublicTripsComponent implements OnInit, OnDestroy {
  private publicTripService = inject(PublicTripService);
  private currencyService = inject(CurrencyService);
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<void>();

  // State
  isLoading = signal(false);
  trips = signal<PublicTripSearchResponse[]>([]);
  mainTrips = signal<PublicTripSearchResponse[]>([]);
  newTrips = signal<PublicTripSearchResponse[]>([]);
  error = signal('');
  generatedItineraryHtml = signal<string | null>(null);


  // Filters
  destination = '';
  minDuration: number | null = null;
  maxDuration: number | null = null;
  maxBudget: number | null = null;
  sortOrder = 'relevance';

  ngOnInit(): void {
    // Debounce search
    this.searchSubject
      .pipe(
        debounceTime(400),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.performSearch();
      });

    // Initial search
    this.search();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onFilterChange(): void {
    this.search();
  }

  search(): void {
    this.searchSubject.next();
  }

  private performSearch(): void {
    this.isLoading.set(true);
    this.error.set('');
    this.generatedItineraryHtml.set(null);

    const destination = this.destination?.trim() || undefined;

    const request: SearchPublicTripsRequest = {
      destination,
      minDuration: this.minDuration ?? undefined,
      maxDuration: this.maxDuration ?? undefined,
      maxBudget: this.maxBudget ?? undefined,
      sortOrder: this.sortOrder
    };

    const hasOnlyDestination = destination && 
      this.minDuration == null && 
      this.maxDuration == null && 
      this.maxBudget == null && 
      this.sortOrder === 'relevance';

    if (hasOnlyDestination) {
      this.publicTripService.aiPlanTrip(destination)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: async (response) => {
            const data = response.data;
            if (data?.generatedItinerary) {
              const html = await marked.parse(data.generatedItinerary);
              this.generatedItineraryHtml.set(html);
            }
            
            const allTrips = data?.relatedPublicTrips || [];
            this.trips.set(allTrips);
            this.mainTrips.set(allTrips.filter(t => !t.isNewTrip));
            this.newTrips.set(allTrips.filter(t => t.isNewTrip));
            this.isLoading.set(false);
          },
          error: (err) => {
            console.error('Failed to search public trips via AI', err);
            this.error.set('Failed to load trips. Please try again later.');
            this.isLoading.set(false);
          }
        });
    } else {
      this.publicTripService.searchPublicTrips(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            const allTrips = response.data || [];
            this.trips.set(allTrips);
            
            if (this.sortOrder === 'relevance') {
              this.mainTrips.set(allTrips.filter(t => !t.isNewTrip));
              this.newTrips.set(allTrips.filter(t => t.isNewTrip));
            } else {
              this.mainTrips.set(allTrips);
              this.newTrips.set([]);
            }
            
            this.isLoading.set(false);
          },
          error: (err) => {
            console.error('Failed to search public trips', err);
            this.error.set('Failed to load trips. Please try again later.');
            this.isLoading.set(false);
          }
        });
    }
  }

  formatCost(costAmount: number | null): string {
    if (costAmount == null) return 'N/A';
    return this.currencyService.format(costAmount, 'USD'); // Assuming USD for now
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }
}
