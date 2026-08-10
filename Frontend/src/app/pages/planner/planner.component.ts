import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnDestroy,
  OnInit,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ActivatedRoute,
  Router
} from '@angular/router';
import {
  Observable,
  Subscription,
  combineLatest,
  distinctUntilChanged,
  firstValueFrom,
  map
} from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import * as L from 'leaflet';

const leafletAssetsUrl = new URL('assets/leaflet/', document.baseURI).href;

L.Icon.Default.imagePath = '';
L.Icon.Default.mergeOptions({
  iconRetinaUrl: `${leafletAssetsUrl}marker-icon-2x.png`,
  iconUrl: `${leafletAssetsUrl}marker-icon.png`,
  shadowUrl: `${leafletAssetsUrl}marker-shadow.png`
});

import {
  GenerateItineraryPreferences,
  AddItineraryItemRequest,
  GenerateItineraryRequest,
  ItineraryDay,
  ItineraryGenerationJob,
  ItineraryItem,
  ItineraryResponse,
  UpdateItineraryItemRequest,
  ItineraryService,
  VenueSuggestion
} from '../../services/itinerary.service';
import type { ApiResponse } from '../../models/api-response.model';
import {
  ItineraryGenerationService
} from '../../services/itinerary-generation.service';
import {
  TripResponse,
  TripRouteResponse,
  TripRouteActivityDto,
  NearbyAttractionDto,
  TripService
} from '../../services/trip.service';
import { WeatherService, ItineraryDayWeatherResponse } from '../../services/weather.service';

interface ItemDraft {
  targetDayId: string;
  title: string;
  description: string;
  startTime: string;
  endTime: string;
  location: string;
  category: string;
  estimatedCost: number | null;
  orderIndex: number;
}

interface PreferenceOption {
  value: string;
  label: string;
  icon: string;
  description?: string;
}

interface AddCategoryOption {
  value: string;
  label: string;
  icon: string;
  description: string;
  usesRecommendations: boolean;
}

@Component({
  selector: 'app-planner',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './planner.component.html',
  styleUrl: './planner.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlannerComponent
  implements OnInit, OnDestroy {
  tripId = '';

  private readonly tripState = signal<TripResponse | null>(null);
  get trip(): TripResponse | null {
    return this.tripState();
  }
  private set trip(value: TripResponse | null) {
    this.tripState.set(value);
  }

  isEditingPreferences = false;

  readonly tripsWeather = signal<{ loading: boolean, error: string | null, data: ItineraryDayWeatherResponse[] | null }>({ loading: false, error: null, data: null });

  // Map state
  tripRoute = signal<TripRouteActivityDto[]>([]);
  nearbyAttractions = signal<NearbyAttractionDto[]>([]);
  map: L.Map | null = null;
  mapMarkers: L.Marker[] = [];
  routePolyline: L.Polyline | null = null;

  searchRadius = 2000;
  searchCategory = '';
  private readonly isSearchingNearbyState = signal(false);
  get isSearchingNearby(): boolean {
    return this.isSearchingNearbyState();
  }
  private set isSearchingNearby(value: boolean) {
    this.isSearchingNearbyState.set(value);
  }
  nearbyCategories = [
    { value: '', label: 'All' },
    { value: 'catering.restaurant', label: 'Restaurants' },
    { value: 'catering.cafe', label: 'Cafes' },
    { value: 'accommodation.hotel', label: 'Hotels' },
    { value: 'entertainment.museum', label: 'Museums' },
    { value: 'leisure.park', label: 'Parks' },
    { value: 'commercial', label: 'Shopping' },
    { value: 'tourism', label: 'Tourism' }
  ];

  private readonly generationJobState =
    signal<ItineraryGenerationJob | null>(null);
  get generationJob(): ItineraryGenerationJob | null {
    return this.generationJobState();
  }
  private set generationJob(
    value: ItineraryGenerationJob | null
  ) {
    this.generationJobState.set(value);
  }

  private readonly generationProgressState = signal(0);
  get generationProgress(): number {
    return this.generationProgressState();
  }
  private set generationProgress(value: number) {
    this.generationProgressState.set(
      Math.max(0, Math.min(100, Math.round(value)))
    );
  }

  generationCityIndex = 0;

  private readonly generationMessageState = signal('Queued');
  get generationMessage(): string {
    return this.generationMessageState();
  }
  private set generationMessage(value: string) {
    this.generationMessageState.set(value);
  }

  selectedInterests = [
    'culture',
    'food'
  ];

  selectedPace = 'balanced';

  selectedTransportation = [
    'walking',
    'public_transport'
  ];

  accommodationStyle = 'mid_range';
  preferredLanguage: 'en' | 'ar' = 'en';
  additionalNotes = '';

  currencyCode = 'USD';
  travelerCount = 1;

  private readonly itineraryState =
    signal<ItineraryResponse | null>(null);
  get itinerary(): ItineraryResponse | null {
    return this.itineraryState();
  }
  private set itinerary(value: ItineraryResponse | null) {
    this.itineraryState.set(value);
  }

  private readonly errorMessageState = signal('');
  get errorMessage(): string {
    return this.errorMessageState();
  }
  private set errorMessage(value: string) {
    this.errorMessageState.set(value);
  }

  failedPhotoUrls = new Set<string>();
  venueSuggestions:
    Record<string, VenueSuggestion[]> = {};
  activeVenueItemId = '';

  addItemStep: 'category' | 'recommendations' | 'manual' =
    'category';
  newItemSuggestions: VenueSuggestion[] = [];

  private readonly loadingNewItemSuggestionsState = signal(false);
  get loadingNewItemSuggestions(): boolean {
    return this.loadingNewItemSuggestionsState();
  }
  private set loadingNewItemSuggestions(value: boolean) {
    this.loadingNewItemSuggestionsState.set(value);
  }

  private readonly loadingVenueItemIdState = signal('');
  get loadingVenueItemId(): string {
    return this.loadingVenueItemIdState();
  }
  private set loadingVenueItemId(value: string) {
    this.loadingVenueItemIdState.set(value);
  }

  private readonly isGeneratingState = signal(false);
  get isGenerating(): boolean {
    return this.isGeneratingState();
  }
  private set isGenerating(value: boolean) {
    this.isGeneratingState.set(value);
  }

  private readonly isSavingState = signal(false);
  get isSaving(): boolean {
    return this.isSavingState();
  }
  private set isSaving(value: boolean) {
    this.isSavingState.set(value);
  }

  private readonly isCancellingGenerationState = signal(false);
  get isCancellingGeneration(): boolean {
    return this.isCancellingGenerationState();
  }
  private set isCancellingGeneration(value: boolean) {
    this.isCancellingGenerationState.set(value);
  }

  editingItemId = '';
  addingDayId = '';

  itemDraft: ItemDraft = this.emptyDraft();

  private generationSubscription: Subscription | null = null;
  private handledTerminalJobId = '';

  readonly categories = [
    'activity',
    'meal',
    'transport',
    'accommodation',
    'other'
  ];

  readonly addCategoryOptions: AddCategoryOption[] = [
    {
      value: 'activity',
      label: 'Activity',
      icon: 'local_activity',
      description: 'Attractions, tours and experiences nearby',
      usesRecommendations: true
    },
    {
      value: 'meal',
      label: 'Restaurant',
      icon: 'restaurant',
      description: 'Restaurants and cafes near your day plan',
      usesRecommendations: true
    },
    {
      value: 'accommodation',
      label: 'Hotel',
      icon: 'hotel',
      description: 'Available stays for this city and date',
      usesRecommendations: true
    },
    {
      value: 'transport',
      label: 'Transport',
      icon: 'directions_transit',
      description: 'Stations, transit hubs and car rentals',
      usesRecommendations: true
    },
    {
      value: 'other',
      label: 'Something else',
      icon: 'edit_note',
      description: 'Enter a custom item yourself',
      usesRecommendations: false
    }
  ];

  readonly currencies = [
    'USD',
    'EUR',
    'GBP',
    'EGP',
    'TRY',
    'AED',
    'SAR'
  ];

  readonly interestOptions: PreferenceOption[] = [
    {
      value: 'adventure',
      label: 'Adventure',
      icon: 'hiking'
    },
    {
      value: 'nature',
      label: 'Nature',
      icon: 'landscape'
    },
    {
      value: 'culture',
      label: 'Culture & history',
      icon: 'museum'
    },
    {
      value: 'food',
      label: 'Local food',
      icon: 'restaurant'
    },
    {
      value: 'shopping',
      label: 'Shopping',
      icon: 'shopping_bag'
    },
    {
      value: 'nightlife',
      label: 'Nightlife',
      icon: 'nightlife'
    },
    {
      value: 'relaxation',
      label: 'Relaxation',
      icon: 'spa'
    },
    {
      value: 'family',
      label: 'Family activities',
      icon: 'family_restroom'
    }
  ];

  readonly paceOptions: PreferenceOption[] = [
    {
      value: 'relaxed',
      label: 'Relaxed',
      icon: 'self_improvement',
      description: 'Fewer daily stops and more rest'
    },
    {
      value: 'balanced',
      label: 'Balanced',
      icon: 'balance',
      description: 'Activities with comfortable breaks'
    },
    {
      value: 'active',
      label: 'Active',
      icon: 'bolt',
      description: 'Full days with more experiences'
    }
  ];

  readonly transportationOptions:
    PreferenceOption[] = [
      {
        value: 'walking',
        label: 'Walking',
        icon: 'directions_walk'
      },
      {
        value: 'public_transport',
        label: 'Public transport',
        icon: 'train'
      },
      {
        value: 'taxi_ride_share',
        label: 'Taxi / ride share',
        icon: 'local_taxi'
      },
      {
        value: 'rental_car',
        label: 'Rental car',
        icon: 'directions_car'
      },
      {
        value: 'bicycle',
        label: 'Bicycle',
        icon: 'directions_bike'
      },
      {
        value: 'mixed',
        label: 'Best available mix',
        icon: 'route'
      }
    ];

  readonly accommodationOptions:
    PreferenceOption[] = [
      {
        value: 'already_booked',
        label: 'Already booked',
        icon: 'check_circle'
      },
      {
        value: 'budget',
        label: 'Budget',
        icon: 'savings'
      },
      {
        value: 'mid_range',
        label: 'Mid-range',
        icon: 'hotel'
      },
      {
        value: 'luxury',
        label: 'Luxury',
        icon: 'diamond'
      },
      {
        value: 'boutique',
        label: 'Boutique',
        icon: 'apartment'
      },
      {
        value: 'resort',
        label: 'Resort',
        icon: 'beach_access'
      }
    ];


  constructor(
    private readonly route: ActivatedRoute,
    private readonly itineraryService: ItineraryService,
    private readonly generationService: ItineraryGenerationService,
    private readonly tripService: TripService,
    private readonly weatherService: WeatherService,
    private readonly destroyRef: DestroyRef,
    private readonly router: Router
  ) { }

  ngOnInit(): void {
    combineLatest([
      this.route.paramMap,
      this.route.queryParamMap
    ])
      .pipe(
        map(([params, queryParams]) =>
          params.get('tripId') ??
          queryParams.get('tripId') ??
          ''
        ),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((tripId) => {
        void this.initializePlanner(tripId);
        if (tripId) {
          this.loadTripWeather(tripId);
        }
      });
  }

  private loadTripWeather(tripId: string): void {
    this.tripsWeather.set({ loading: true, error: null, data: null });

    this.weatherService.getTripWeather(tripId).subscribe({
      next: (response) => {
        this.tripsWeather.set({ loading: false, error: null, data: response.data ?? [] });
      },
      error: (error: unknown) => {
        this.tripsWeather.set({
          loading: false,
          error: this.tripService.getErrorMessage(error, 'Could not load weather forecast.'),
          data: null
        });
      }
    });
  }

  ngOnDestroy(): void {
    this.generationSubscription?.unsubscribe();
    this.setGenerationScrollLocked(false);
  }

  get visibleDays(): ItineraryDay[] {
    return this.itinerary?.days ?? [];
  }

  get tripCities(): string[] {
    if (this.trip?.cities?.length) {
      return this.trip.cities.map(
        (city) => city.name
      );
    }

    return [];
  }

  get tripDateRange(): string {
    if (
      !this.trip?.startDate ||
      !this.trip?.endDate
    ) {
      return `${this.visibleDays.length} ${this.visibleDays.length === 1
          ? 'day'
          : 'days'
        }`;
    }

    const start = this.parseDateOnly(this.trip.startDate);
    const end = this.parseDateOnly(this.trip.endDate);
    const formatter = new Intl.DateTimeFormat(
      'en-US',
      {
        month: 'short',
        day: 'numeric',
        year:
          start.getFullYear() !==
            end.getFullYear()
            ? 'numeric'
            : undefined
      }
    );

    return `${formatter.format(start)} – ${formatter.format(end)}, ${end.getFullYear()}`;
  }

  get itineraryHeroImage(): string {
    return this.destinationImage(
      this.trip?.destination ||
      this.tripCities[0] ||
      this.visibleDays[0]?.title ||
      ''
    );
  }

  get generationCities(): string[] {
    return this.tripCities.length
      ? this.tripCities
      : ['Your day-by-day plan'];
  }

  generationCityStatus(
    index: number
  ): 'complete' | 'active' | 'waiting' {
    if (this.generationProgress >= 78) {
      return 'complete';
    }

    if (index < this.generationCityIndex) {
      return 'complete';
    }

    if (index === this.generationCityIndex) {
      return 'active';
    }

    return 'waiting';
  }

  editPreferences(): void {
    this.isEditingPreferences = true;
    window.setTimeout(
      () =>
        document
          .getElementById('preferences')
          ?.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
          }),
      0
    );
  }

  cancelPreferenceEditing(): void {
    this.isEditingPreferences = false;
  }

  dayImage(
    day: ItineraryDay
  ): string {
    return this.destinationImage(
      `${day.title} ${day.notes ?? ''}`
    );
  }

  isFlight(item: ItineraryItem): boolean {
    return Boolean(
      item.flightDepartureAirportCode &&
      item.flightArrivalAirportCode
    );
  }

  flightDepartureCode(
    item: ItineraryItem
  ): string {
    return item.flightDepartureAirportCode || 'DEP';
  }

  flightArrivalCode(
    item: ItineraryItem
  ): string {
    return item.flightArrivalAirportCode || 'ARR';
  }

  flightDetail(
    item: ItineraryItem,
    index: number
  ): string {
    return index === 0
      ? item.flightAirlines || ''
      : item.flightNumbers || '';
  }

  flightDuration(
    item: ItineraryItem
  ): string {
    const totalMinutes = item.flightDurationMinutes;

    if (totalMinutes == null) {
      return this.planText('Flight', 'رحلة طيران');
    }

    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;

    return [
      hours > 0 ? `${hours}h` : '',
      minutes > 0 ? `${minutes}m` : ''
    ].filter(Boolean).join(' ');
  }

  flightStops(
    item: ItineraryItem
  ): string {
    const stops = item.flightStopCount;

    if (stops == null) {
      return this.planText('Route details', 'تفاصيل المسار');
    }

    if (stops === 0) {
      return this.planText('Direct', 'مباشرة');
    }

    return this.planText(
      `${stops} ${stops === 1 ? 'stop' : 'stops'}`,
      `${stops} توقف`
    );
  }

  get displayCurrency(): string {
    return (
      this.itinerary?.currencyCode ||
      this.currencyCode ||
      'USD'
    );
  }

  get contentDirection(): 'rtl' | 'ltr' {
    const language =
      this.itinerary
        ?.preferences
        .preferredLanguage ??
      this.preferredLanguage;

    return language === 'ar'
      ? 'rtl'
      : 'ltr';
  }

  get isArabicPlan(): boolean {
    return this.contentDirection === 'rtl';
  }

  get usesQualitySafeguards(): boolean {
    return (
      this.itinerary
        ?.preferences
        .qualityVersion ?? 0
    ) >= 3;
  }

  planText(
    english: string,
    arabic: string
  ): string {
    return this.isArabicPlan
      ? arabic
      : english;
  }

  formatPlanDate(date: string): string {
    const parsedDate = new Date(
      `${date}T12:00:00`
    );

    return new Intl.DateTimeFormat(
      this.isArabicPlan ? 'ar-EG' : 'en-US',
      {
        weekday: 'long',
        month: 'long',
        day: 'numeric'
      }
    ).format(parsedDate);
  }

  categoryLabel(category: string): string {
    const labels: Record<
      string,
      [string, string]
    > = {
      activity: ['Activity', 'نشاط'],
      meal: ['Meal', 'وجبة'],
      transport: ['Transport', 'تنقّل'],
      accommodation: [
        'Accommodation',
        'إقامة'
      ],
      other: ['Other', 'أخرى']
    };

    const label =
      labels[category] ?? labels['other'];

    return this.isArabicPlan
      ? label[1]
      : label[0];
  }

  categoryIcon(category: string): string {
    const icons: Record<string, string> = {
      activity: 'local_activity',
      meal: 'restaurant',
      transport: 'directions_transit',
      accommodation: 'hotel',
      other: 'event_note'
    };

    return icons[category] ?? icons['other'];
  }

  routeModeLabel(mode: string | null): string {
    const labels: Record<
      string,
      [string, string]
    > = {
      walking: ['Walk', 'مشي'],
      bicycle: ['Bicycle', 'دراجة'],
      public_transport: [
        'Public transport',
        'مواصلات عامة'
      ],
      driving: ['Car / taxi', 'سيارة أو تاكسي']
    };

    const label =
      labels[mode ?? ''] ??
      ['Route', 'مسار'];

    return this.isArabicPlan
      ? label[1]
      : label[0];
  }

  routeModeIcon(mode: string | null): string {
    const icons: Record<string, string> = {
      walking: 'directions_walk',
      bicycle: 'directions_bike',
      public_transport: 'directions_transit',
      driving: 'directions_car'
    };

    return icons[mode ?? ''] ?? 'route';
  }

  formatRouteDistance(
    distanceMeters: number | null
  ): string {
    if (distanceMeters === null) {
      return '';
    }

    if (distanceMeters < 1000) {
      return `${distanceMeters} m`;
    }

    return `${(distanceMeters / 1000).toFixed(
      distanceMeters < 10000 ? 1 : 0
    )} km`;
  }

  hotelRangeLow(
    nightlyRate: number
  ): number {
    return Math.max(
      0,
      Math.round(nightlyRate * 0.9)
    );
  }

  hotelRangeHigh(
    nightlyRate: number
  ): number {
    return Math.round(nightlyRate * 1.1);
  }

  toggleInterest(value: string): void {
    this.selectedInterests =
      this.toggleOption(
        this.selectedInterests,
        value,
        5,
        'interests'
      );
  }

  toggleTransportation(value: string): void {
    this.selectedTransportation =
      this.toggleOption(
        this.selectedTransportation,
        value,
        3,
        'transportation options'
      );
  }



  isSelected(
    values: string[],
    value: string
  ): boolean {
    return values.includes(value);
  }

  shouldShowPhoto(
    photoUrl: string | null
  ): boolean {
    return (
      !!photoUrl &&
      !this.failedPhotoUrls.has(photoUrl)
    );
  }

  handlePhotoError(
    photoUrl: string | null
  ): void {
    if (photoUrl) {
      this.failedPhotoUrls.add(photoUrl);
    }
  }

  async toggleVenueSuggestions(
    item: ItineraryItem
  ): Promise<void> {
    if (this.activeVenueItemId === item.id) {
      this.activeVenueItemId = '';
      return;
    }

    if (this.venueSuggestions[item.id]?.length) {
      this.activeVenueItemId = item.id;
      return;
    }

    this.activeVenueItemId = '';
    this.errorMessage = '';
    this.loadingVenueItemId = item.id;

    try {
      const response = await firstValueFrom(
        this.itineraryService
          .getVenueSuggestions(
            this.tripId,
            item.id
          )
      );
      const suggestions = response.data ?? [];
      this.venueSuggestions[item.id] = suggestions;

      if (suggestions.length > 0) {
        this.activeVenueItemId = item.id;
      } else {
        delete this.venueSuggestions[item.id];
        this.errorMessage =
          `No additional ${this.categoryLabel(item.category).toLowerCase()} recommendations were found nearby.`;
      }
    } catch (error: unknown) {
      this.errorMessage = this.getErrorMessage(
        error,
        'Nearby venue choices could not be loaded.'
      );
    } finally {
      this.loadingVenueItemId = '';
    }
  }

  canRecommendItem(item: ItineraryItem): boolean {
    return !this.isFlight(item);
  }

  changeRecommendationLabel(item: ItineraryItem): string {
    const labels: Record<string, string> = {
      activity: 'Change activity',
      meal: 'Change restaurant',
      accommodation: 'Change hotel',
      transport: 'Change transport',
      other: 'Find another place'
    };

    return labels[item.category] ?? 'Find alternatives';
  }

  async chooseVenue(
    item: ItineraryItem,
    venue: VenueSuggestion
  ): Promise<void> {
    this.isSaving = true;

    try {
      const response = await firstValueFrom(
        this.itineraryService.selectVenue(
          this.tripId,
          item.id,
          venue.placeId
        )
      );

      if (!response.data) {
        throw new Error(
          'The server did not return the updated itinerary.'
        );
      }

      this.applyItinerary(response.data);
      this.activeVenueItemId = '';
      delete this.venueSuggestions[item.id];
    } catch (error: unknown) {
      this.errorMessage = this.getErrorMessage(
        error,
        'The selected venue could not be added.'
      );
    } finally {
      this.isSaving = false;
    }
  }

  async generateItinerary(): Promise<void> {
    this.errorMessage = '';

    if (this.isGenerating || !this.validateGenerationForm()) {
      return;
    }

    const request: GenerateItineraryRequest = {
      preferences: this.buildPreferences(),
      budgetAmount: null,
      currencyCode: this.currencyCode.toUpperCase(),
      travelerCount: this.travelerCount
    };

    this.isGenerating = true;
    this.setGenerationScrollLocked(true);
    this.generationProgress = 0;
    this.generationMessage = 'Queued';
    this.generationCityIndex = 0;
    this.cancelItemForm();

    const idempotencyKey =
      this.generationService.createOrReuseIdempotencyKey(
        this.tripId
      );

    try {
      const response = await firstValueFrom(
        this.itineraryService.startGeneration(
          this.tripId,
          request,
          idempotencyKey
        )
      );

      if (!response.data) {
        throw new Error(
          'The server did not return a generation job.'
        );
      }

      this.generationService.rememberJob(
        this.tripId,
        response.data.id
      );
      this.trackGeneration(response.data, false);
    } catch (error: unknown) {
      this.isGenerating = false;
      this.setGenerationScrollLocked(false);
      if (this.isAiGenerationAccessBlocked(error)) {
        const limitReached = this.getBillingErrorCode(error) === 'Billing.TravelerAiLimitReached';
        this.errorMessage = limitReached
          ? 'You have reached your AI itinerary generation limit for today. Opening Pricing...'
          : 'Premium AI planning requires an active Traveler Premium subscription. Opening Pricing...';
        window.setTimeout(() => void this.router.navigate(['/pricing'], {
          queryParams: {
            reason: limitReached ? 'ai-limit-reached' : 'premium-required',
            returnUrl: `/planner/${this.tripId}`
          }
        }), 700);
        return;
      }
      this.errorMessage = this.getErrorMessage(
        error,
        'The itinerary could not be queued. Please try again.'
      );
    }
  }

  async cancelGeneration(): Promise<void> {
    const jobId = this.generationJob?.id;

    if (!jobId || this.isCancellingGeneration) {
      return;
    }

    this.isCancellingGeneration = true;

    try {
      const response = await firstValueFrom(
        this.itineraryService.cancelGeneration(
          this.tripId,
          jobId
        )
      );

      if (response.data) {
        this.updateGenerationJob(response.data);

        if (
          this.generationService.isTerminal(
            response.data.status
          )
        ) {
          await this.handleTerminalGeneration(
            response.data
          );
        }
      }
    } catch (error: unknown) {
      this.errorMessage = this.getErrorMessage(
        error,
        'The generation request could not be cancelled.'
      );
    } finally {
      this.isCancellingGeneration = false;
    }
  }

  startAdd(day: ItineraryDay): void {
    this.editingItemId = '';
    this.addingDayId = day.id;

    this.itemDraft = this.emptyDraft(
      day.id,
      day.items.length
    );
    this.addItemStep = 'category';
    this.newItemSuggestions = [];
    this.loadingNewItemSuggestions = false;
  }

  async selectAddCategory(
    option: AddCategoryOption
  ): Promise<void> {
    this.itemDraft.category = option.value;
    this.errorMessage = '';

    if (!option.usesRecommendations) {
      this.addItemStep = 'manual';
      return;
    }

    this.addItemStep = 'recommendations';
    this.newItemSuggestions = [];
    this.loadingNewItemSuggestions = true;

    try {
      const response = await firstValueFrom(
        this.itineraryService.getDayVenueSuggestions(
          this.tripId,
          this.addingDayId,
          option.value
        )
      );
      this.newItemSuggestions = response.data ?? [];
    } catch (error: unknown) {
      this.errorMessage = this.getErrorMessage(
        error,
        'Recommendations could not be loaded for this day.'
      );
    } finally {
      this.loadingNewItemSuggestions = false;
    }
  }

  useManualItemForm(): void {
    this.addItemStep = 'manual';
  }

  chooseAnotherAddCategory(): void {
    this.addItemStep = 'category';
    this.newItemSuggestions = [];
  }

  async addRecommendedItem(
    venue: VenueSuggestion
  ): Promise<void> {
    if (!this.addingDayId) {
      return;
    }

    await this.runItemRequest(
      this.itineraryService.addRecommendedItem(
        this.tripId,
        {
          dayId: this.addingDayId,
          category: this.itemDraft.category,
          placeId: venue.placeId,
          orderIndex: this.itemDraft.orderIndex
        }
      )
    );
  }

  startEdit(
    day: ItineraryDay,
    item: ItineraryItem
  ): void {
    this.addingDayId = '';
    this.editingItemId = item.id;

    this.itemDraft = {
      targetDayId: day.id,
      title: item.title,
      description: item.description ?? '',
      startTime: item.startTime ?? '',
      endTime: item.endTime ?? '',
      location: item.location ?? '',
      category: item.category,
      estimatedCost: item.estimatedCost,
      orderIndex: item.orderIndex
    };
  }

  cancelItemForm(): void {
    this.editingItemId = '';
    this.addingDayId = '';
    this.itemDraft = this.emptyDraft();
    this.addItemStep = 'category';
    this.newItemSuggestions = [];
    this.loadingNewItemSuggestions = false;
  }

  async saveNewItem(): Promise<void> {
    if (
      !this.addingDayId ||
      !this.validateDraft()
    ) {
      return;
    }

    await this.runItemRequest(
      this.itineraryService.addItem(
        this.tripId,
        this.toAddRequest(this.addingDayId)
      )
    );
  }

  async saveEditedItem(): Promise<void> {
    if (
      !this.editingItemId ||
      !this.validateDraft()
    ) {
      return;
    }

    await this.runItemRequest(
      this.itineraryService.updateItem(
        this.tripId,
        this.editingItemId,
        this.toUpdateRequest()
      )
    );
  }

  async deleteItem(
    item: ItineraryItem
  ): Promise<void> {
    const confirmed = window.confirm(
      `Delete "${item.title}"?`
    );

    if (!confirmed) {
      return;
    }

    await this.runItemRequest(
      this.itineraryService.deleteItem(
        this.tripId,
        item.id
      )
    );
  }

  async moveItem(
    item: ItineraryItem,
    targetDay: ItineraryDay,
    targetOrderIndex: number
  ): Promise<void> {
    await this.runItemRequest(
      this.itineraryService.moveItem(
        this.tripId,
        item.id,
        {
          targetDayId: targetDay.id,
          targetOrderIndex: Math.max(
            0,
            targetOrderIndex
          )
        }
      )
    );
  }

  previousDay(
    day: ItineraryDay
  ): ItineraryDay | null {
    const index = this.visibleDays.findIndex(
      (candidate) =>
        candidate.id === day.id
    );

    return index > 0
      ? this.visibleDays[index - 1]
      : null;
  }

  nextDay(
    day: ItineraryDay
  ): ItineraryDay | null {
    const index = this.visibleDays.findIndex(
      (candidate) =>
        candidate.id === day.id
    );

    return (
      index >= 0 &&
      index < this.visibleDays.length - 1
    )
      ? this.visibleDays[index + 1]
      : null;
  }

  private async initializePlanner(
    tripId: string
  ): Promise<void> {
    this.generationSubscription?.unsubscribe();
    this.generationSubscription = null;
    this.handledTerminalJobId = '';
    this.tripId = tripId;
    this.trip = null;
    this.itinerary = null;
    this.errorMessage = '';
    this.isGenerating = false;
    this.setGenerationScrollLocked(false);
    this.generationJob = null;
    this.generationProgress = 0;
    this.generationMessage = 'Queued';

    if (!tripId) {
      this.errorMessage =
        'No trip was selected. Open the planner from one of your trips.';
      return;
    }

    this.trip = this.tripService.getRememberedTrip(tripId);

    await Promise.all([
      this.loadTrip(),
      this.loadExistingItinerary()
    ]);

    await this.loadTripRoute();
    await this.resumeGenerationIfNeeded();
  }

  private async loadTrip(): Promise<void> {
    try {
      const response = await firstValueFrom(
        this.tripService.getTrip(this.tripId)
      );

      if (response.data) {
        this.trip = response.data;
        this.tripService.rememberTrip(response.data);
      }
    } catch (error: unknown) {
      if (!this.trip) {
        this.errorMessage = this.getErrorMessage(
          error,
          'The trip details could not be loaded.'
        );
      }
    }
  }

  private async loadExistingItinerary(): Promise<void> {
    try {
      const response = await firstValueFrom(
        this.itineraryService
          .getActiveItinerary(this.tripId)
      );

      if (response.data) {
        this.applyItinerary(response.data);
      }
    } catch (error: unknown) {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 404
      ) {
        return;
      }

      if (
        error instanceof HttpErrorResponse &&
        error.status === 401
      ) {
        this.errorMessage =
          'Your login has expired. Please log in again.';
        return;
      }

      this.errorMessage =
        'The saved itinerary could not be loaded.';
    }
  }

  private async runItemRequest(
    request: Observable<
      ApiResponse<ItineraryResponse>
    >
  ): Promise<void> {
    this.errorMessage = '';
    this.isSaving = true;

    try {
      const response =
        await firstValueFrom(request);

      if (response.data) {
        this.applyItinerary(response.data);
      }

      this.cancelItemForm();
    } catch (error: unknown) {
      this.errorMessage =
        this.getErrorMessage(
          error,
          'The itinerary could not be updated.'
        );
    } finally {
      this.isSaving = false;
    }
  }

  private validateGenerationForm(): boolean {
    if (!this.tripId) {
      this.errorMessage =
        'No trip was selected. Open the planner from one of your trips.';
      return false;
    }

    if (this.selectedInterests.length === 0) {
      this.errorMessage =
        'Select at least one travel interest.';
      return false;
    }

    if (
      this.selectedTransportation.length === 0
    ) {
      this.errorMessage =
        'Select at least one transportation option.';
      return false;
    }

    if (
      !Number.isInteger(this.travelerCount) ||
      this.travelerCount < 1 ||
      this.travelerCount > 20
    ) {
      this.errorMessage =
        'Traveler count must be between 1 and 20.';
      return false;
    }

    if (
      !this.currencyCode ||
      this.currencyCode.length !== 3
    ) {
      this.errorMessage =
        'Please select a valid currency.';
      return false;
    }

    if (this.additionalNotes.length > 500) {
      this.errorMessage =
        'Additional notes cannot exceed 500 characters.';
      return false;
    }

    return true;
  }

  private buildPreferences():
    GenerateItineraryPreferences {
    return {
      interests: [...this.selectedInterests],
      pace: this.selectedPace,
      transportation: [
        ...this.selectedTransportation
      ],
      accommodationStyle:
        this.accommodationStyle,
      preferredLanguage:
        this.preferredLanguage,
      additionalNotes:
        this.additionalNotes.trim() || null
    };
  }

  private applyItinerary(
    itinerary: ItineraryResponse
  ): void {
    this.itinerary = itinerary;
    this.failedPhotoUrls.clear();
    this.applyPreferences(
      itinerary.preferences
    );

    this.currencyCode =
      itinerary.currencyCode;
    this.travelerCount =
      itinerary.travelerCount;
    this.isEditingPreferences = false;
  }

  private async resumeGenerationIfNeeded(): Promise<void> {
    const jobId =
      this.generationService.getRememberedJobId(this.tripId);

    if (!jobId) {
      return;
    }

    this.isGenerating = true;
    this.setGenerationScrollLocked(true);
    this.generationProgress = 0;
    this.generationMessage = 'Restoring generation progress';

    this.generationSubscription?.unsubscribe();
    this.generationSubscription =
      this.generationService
        .trackJob(this.tripId, jobId, true)
        .subscribe({
          next: (job) => {
            this.updateGenerationJob(job);

            if (
              this.generationService.isTerminal(job.status)
            ) {
              void this.handleTerminalGeneration(job);
            }
          },
          error: (error: unknown) => {
            this.isGenerating = false;
            this.setGenerationScrollLocked(false);

            if (
              error instanceof HttpErrorResponse &&
              error.status === 404
            ) {
              this.generationService
                .clearRememberedGeneration(this.tripId);
            }

            this.errorMessage = this.getErrorMessage(
              error,
              'Generation progress could not be restored. Refresh the page to try again.'
            );
          }
        });
  }

  private trackGeneration(
    initialJob: ItineraryGenerationJob,
    startImmediately: boolean
  ): void {
    this.updateGenerationJob(initialJob);
    this.generationService.rememberJob(
      this.tripId,
      initialJob.id
    );

    if (this.generationService.isTerminal(initialJob.status)) {
      void this.handleTerminalGeneration(initialJob);
      return;
    }

    this.generationSubscription?.unsubscribe();
    this.generationSubscription =
      this.generationService
        .trackJob(
          this.tripId,
          initialJob.id,
          startImmediately
        )
        .subscribe({
          next: (job) => {
            this.updateGenerationJob(job);

            if (
              this.generationService.isTerminal(job.status)
            ) {
              void this.handleTerminalGeneration(job);
            }
          },
          error: (error: unknown) => {
            this.isGenerating = false;
            this.setGenerationScrollLocked(false);
            this.errorMessage = this.getErrorMessage(
              error,
              'The itinerary generation status could not be loaded. Refresh the page to resume checking the server job.'
            );
          }
        });
  }

  private updateGenerationJob(
    job: ItineraryGenerationJob
  ): void {
    this.generationJob = job;
    this.generationProgress = job.progressPercent;
    this.generationMessage =
      job.stage || 'Building your itinerary';

    const planningPrefix = 'planning ';
    const normalizedStage = job.stage
      .trim()
      .toLocaleLowerCase();

    if (normalizedStage.startsWith(planningPrefix)) {
      const activeCity = normalizedStage
        .slice(planningPrefix.length)
        .trim();
      const cityIndex = this.tripCities.findIndex(
        (city) =>
          city.toLocaleLowerCase() === activeCity
      );

      if (cityIndex >= 0) {
        this.generationCityIndex = cityIndex;
      }
    } else if (job.progressPercent >= 78) {
      this.generationCityIndex = this.generationCities.length;
    } else if (job.progressPercent < 30) {
      this.generationCityIndex = 0;
    }
  }

  private async handleTerminalGeneration(
    job: ItineraryGenerationJob
  ): Promise<void> {
    if (this.handledTerminalJobId === job.id) {
      return;
    }

    this.handledTerminalJobId = job.id;
    this.generationSubscription?.unsubscribe();
    this.generationSubscription = null;

    try {
      if (job.status === 'completed') {
        this.generationProgress = 100;
        this.generationMessage = 'Completed';
        this.generationCityIndex =
          this.generationCities.length;
        await this.loadExistingItinerary();
      } else if (job.status === 'failed') {
        this.errorMessage =
          job.errorMessage ||
          'The itinerary could not be generated.';
      } else if (job.status === 'cancelled') {
        this.errorMessage =
          'Itinerary generation was cancelled.';
      }
    } finally {
      this.isGenerating = false;
      this.setGenerationScrollLocked(false);
      this.isCancellingGeneration = false;
      this.generationService
        .clearRememberedGeneration(this.tripId);
    }
  }

  private setGenerationScrollLocked(locked: boolean): void {
    document.body.style.overflow = locked ? 'hidden' : '';
  }

  private parseDateOnly(value: string): Date {
    return new Date(`${value.slice(0, 10)}T12:00:00`);
  }

  trackById(
    _index: number,
    item: { id: string }
  ): string {
    return item.id;
  }

  trackByValue(
    _index: number,
    option: PreferenceOption
  ): string {
    return option.value;
  }

  trackByString(
    _index: number,
    value: string
  ): string {
    return value;
  }

  trackByCity(
    _index: number,
    city: { name: string }
  ): string {
    return city.name;
  }

  trackByVenue(
    _index: number,
    venue: VenueSuggestion
  ): string {
    return venue.placeId;
  }

  private destinationImage(
    source: string
  ): string {
    const normalized = source
      .toLocaleLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');

    const images: Array<
      [string[], string]
    > = [
        [
          [
            'turkey',
            'turkiye',
            'istanbul',
            'izmir',
            'antalya',
            'cappadocia'
          ],
          'assets/countries/turkey.png'
        ],
        [
          ['thailand', 'bangkok', 'phuket'],
          'assets/countries/thailand.png'
        ],
        [
          ['maldives', 'male'],
          'assets/countries/maldives.png'
        ],
        [
          ['dubai', 'united arab emirates', 'uae'],
          'assets/countries/dubai.png'
        ],
        [
          ['japan', 'tokyo', 'kyoto', 'osaka'],
          'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=1800&q=88&auto=format'
        ],
        [
          ['france', 'paris', 'lyon', 'nice'],
          'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=1800&q=88&auto=format'
        ],
        [
          ['italy', 'rome', 'florence', 'venice', 'amalfi'],
          'https://images.unsplash.com/photo-1533105079780-92b9be482077?w=1800&q=88&auto=format'
        ],
        [
          ['egypt', 'cairo', 'luxor', 'aswan', 'hurghada', 'dahab', 'sharm'],
          'https://images.unsplash.com/photo-1503177119275-0aa32b3a9368?w=1800&q=88&auto=format'
        ],
        [
          ['spain', 'barcelona', 'madrid', 'seville'],
          'https://images.unsplash.com/photo-1509840841025-9088ba78a826?w=1800&q=88&auto=format'
        ],
        [
          ['morocco', 'marrakech', 'casablanca', 'fes'],
          'https://images.unsplash.com/photo-1489493585363-d69421e0edd3?w=1800&q=88&auto=format'
        ],
        [
          ['united kingdom', 'england', 'london'],
          'https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=1800&q=88&auto=format'
        ],
        [
          ['poland', 'krakow', 'warsaw'],
          'https://images.unsplash.com/photo-1519197924294-4ba991a11128?w=1800&q=88&auto=format'
        ]
      ];

    return (
      images.find(([keys]) =>
        keys.some((key) =>
          normalized.includes(key)
        )
      )?.[1] ??
      'https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=1800&q=88&auto=format'
    );
  }

  private applyPreferences(
    preferences: GenerateItineraryPreferences
  ): void {
    this.selectedInterests = [
      ...(preferences.interests || [])
    ];

    this.selectedPace =
      preferences.pace || 'balanced';

    this.selectedTransportation = [
      ...(preferences.transportation || [])
    ];

    this.accommodationStyle =
      preferences.accommodationStyle ||
      'mid_range';


    this.preferredLanguage =
      preferences.preferredLanguage === 'ar'
        ? 'ar'
        : 'en';

    this.additionalNotes =
      preferences.additionalNotes || '';
  }

  private toggleOption(
    values: string[],
    value: string,
    maximumCount: number,
    fieldName: string
  ): string[] {
    if (values.includes(value)) {
      this.errorMessage = '';
      return values.filter(
        (candidate) => candidate !== value
      );
    }

    if (values.length >= maximumCount) {
      this.errorMessage =
        `Select no more than ${maximumCount} ${fieldName}.`;
      return values;
    }

    this.errorMessage = '';
    return [...values, value];
  }

  private validateDraft(): boolean {
    this.errorMessage = '';

    if (!this.itemDraft.title.trim()) {
      this.errorMessage =
        'Activity title is required.';
      return false;
    }

    if (!this.itemDraft.targetDayId) {
      this.errorMessage =
        'Select an itinerary day.';
      return false;
    }

    if (
      this.itemDraft.estimatedCost !== null &&
      this.itemDraft.estimatedCost < 0
    ) {
      this.errorMessage =
        'Estimated cost cannot be negative.';
      return false;
    }

    if (
      this.itemDraft.startTime &&
      this.itemDraft.endTime &&
      this.itemDraft.endTime <= this.itemDraft.startTime
    ) {
      this.errorMessage =
        'End time must be later than start time.';
      return false;
    }

    return true;
  }

  private toAddRequest(
    dayId: string
  ): AddItineraryItemRequest {
    return {
      dayId,
      ...this.itemContentRequest(),
      orderIndex: this.itemDraft.orderIndex
    };
  }

  private toUpdateRequest(): UpdateItineraryItemRequest {
    return this.itemContentRequest();
  }

  private itemContentRequest(): UpdateItineraryItemRequest {
    return {
      title: this.itemDraft.title.trim(),
      description:
        this.itemDraft.description.trim() || null,
      startTime: this.itemDraft.startTime || null,
      endTime: this.itemDraft.endTime || null,
      location:
        this.itemDraft.location.trim() || null,
      category: this.itemDraft.category,
      estimatedCost: this.itemDraft.estimatedCost
    };
  }

  private emptyDraft(
    targetDayId = '',
    orderIndex = 0
  ): ItemDraft {
    return {
      targetDayId,
      title: '',
      description: '',
      startTime: '',
      endTime: '',
      location: '',
      category: 'activity',
      estimatedCost: null,
      orderIndex
    };
  }

  private getErrorMessage(
    error: unknown,
    fallback: string
  ): string {
    if (error instanceof HttpErrorResponse) {
      return (
        error.error?.errors?.[0]?.message ||
        error.error?.message ||
        fallback
      );
    }

    return error instanceof Error
      ? error.message
      : fallback;
  }

  private isAiGenerationAccessBlocked(error: unknown): boolean {
    const code = this.getBillingErrorCode(error);
    return code === 'Billing.TravelerAiLimitReached' ||
      code === 'Billing.PremiumAiSubscriptionRequired';
  }

  private getBillingErrorCode(error: unknown): string | undefined {
    return error instanceof HttpErrorResponse
      ? error.error?.errors?.[0]?.code
      : undefined;
  }

  // --- Map and Nearby Logic ---

  getCategoryIcon(category?: string | null): string {
    if (!category) return 'place';
    const lower = category.toLowerCase();
    if (lower.includes('restaurant') || lower.includes('catering')) return 'restaurant';
    if (lower.includes('cafe') || lower.includes('coffee')) return 'local_cafe';
    if (lower.includes('hotel') || lower.includes('accommodation')) return 'hotel';
    if (lower.includes('museum') || lower.includes('heritage')) return 'museum';
    if (lower.includes('park') || lower.includes('nature') || lower.includes('forest')) return 'park';
    if (lower.includes('shopping') || lower.includes('commercial')) return 'shopping_bag';
    if (lower.includes('tourism') || lower.includes('entertainment')) return 'attractions';
    return 'place';
  }

  private initMap(): void {
    if (this.map) return; // already initialized

    const mapEl = document.getElementById('trip-map');
    if (!mapEl) return;

    this.map = L.map(mapEl).setView([0, 0], 2);

    L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
      attribution: '&copy; OpenStreetMap &copy; CARTO',
      subdomains: 'abcd',
      maxZoom: 20
    }).addTo(this.map);
  }

  private async loadTripRoute(): Promise<void> {
    if (!this.tripId) return;

    try {
      const response = await firstValueFrom(this.tripService.getTripRoute(this.tripId));
      if (response.data) {
        this.tripRoute.set(response.data.activities);
        setTimeout(() => {
          this.initMap();
          this.drawRouteOnMap();
        }, 100);
      }
    } catch (err) {
      console.error('Failed to load trip route', err);
    }
  }

  private drawRouteOnMap(): void {
    if (!this.map) return;

    // Clear existing markers and polyline
    this.mapMarkers.forEach(m => m.remove());
    this.mapMarkers = [];
    if (this.routePolyline) {
      this.routePolyline.remove();
      this.routePolyline = null;
    }

    const route = this.tripRoute();
    if (!route.length) return;

    const latlngs: L.LatLngExpression[] = [];

    route.forEach((activity, index) => {
      if (activity.latitude != null && activity.longitude != null) {
        const latlng: L.LatLngExpression = [activity.latitude, activity.longitude];
        latlngs.push(latlng);

        const marker = L.marker(latlng)
          .bindPopup(`<b>${index + 1}. ${activity.name}</b><br>${activity.address || ''}`)
          .addTo(this.map!);
        this.mapMarkers.push(marker);
      }
    });

    if (latlngs.length > 0) {
      this.routePolyline = L.polyline(latlngs, { color: '#0ea5e9', weight: 4, opacity: 0.7 }).addTo(this.map);
      this.map.fitBounds(L.latLngBounds(latlngs), { padding: [50, 50] });
    }
  }

  async searchNearby(): Promise<void> {
    if (!this.tripId || this.isSearchingNearby) return;

    this.isSearchingNearby = true;
    try {
      const response = await firstValueFrom(
        this.tripService.getNearbyAttractions(this.tripId, this.searchRadius, this.searchCategory)
      );
      if (response.data) {
        this.nearbyAttractions.set(response.data);
        this.drawNearbyOnMap();
      }
    } catch (err: any) {
      console.error('Failed to search nearby attractions', err);
      const code = err?.error?.errors?.[0]?.code;
      if (code === 'Trip.NoLocation') {
        this.errorMessage = 'Please add at least one activity with a location to your itinerary before searching for nearby attractions.';
      } else {
        this.errorMessage = 'Failed to search for nearby attractions. Please try again.';
      }
      setTimeout(() => this.errorMessage = '', 5000);
    } finally {
      this.isSearchingNearby = false;
    }
  }

  private drawNearbyOnMap(): void {
    if (!this.map) return;

    // Remove old nearby markers (keep route markers)
    this.mapMarkers = this.mapMarkers.filter(m => {
      if ((m as any).isNearby) {
        m.remove();
        return false;
      }
      return true;
    });

    const attractions = this.nearbyAttractions();

    // Custom icon for nearby attractions
    const nearbyIcon = L.divIcon({
      className: 'custom-div-icon',
      html: `<div style="background-color: #f43f5e; width: 12px; height: 12px; border-radius: 50%; border: 2px solid white; box-shadow: 0 1px 3px rgba(0,0,0,0.4);"></div>`,
      iconSize: [12, 12],
      iconAnchor: [6, 6]
    });

    attractions.forEach(attr => {
      if (attr.latitude != null && attr.longitude != null) {
        const latlng: L.LatLngExpression = [attr.latitude, attr.longitude];
        const marker = L.marker(latlng, { icon: nearbyIcon })
          .bindPopup(`<b>${attr.name}</b><br><span style="text-transform: capitalize; font-size: 0.8em; color: #666;">${attr.category || ''}</span><br>${attr.address || ''}`)
          .addTo(this.map!);

        (marker as any).isNearby = true;
        this.mapMarkers.push(marker);
      }
    });
  }

  focusAttraction(attraction: NearbyAttractionDto): void {
    if (!this.map || attraction.latitude == null || attraction.longitude == null) return;

    const latlng: L.LatLngExpression = [attraction.latitude, attraction.longitude];
    this.map.flyTo(latlng, 16, { duration: 1 });

    // Find and open popup
    const marker = this.mapMarkers.find(m => {
      const pos = m.getLatLng();
      return pos.lat === attraction.latitude && pos.lng === attraction.longitude;
    });

    if (marker) {
      setTimeout(() => marker.openPopup(), 1000);
    }
  }
}
