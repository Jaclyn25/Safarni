import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  HostListener,
  OnDestroy,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ActivatedRoute
} from '@angular/router';
import { firstValueFrom } from 'rxjs';

import {
  GenerateItineraryPreferences,
  GenerateItineraryRequest,
  ItineraryDay,
  ItineraryItem,
  ItineraryItemRequest,
  ItineraryResponse,
  ItineraryService,
  VenueSuggestion
} from '../../services/itinerary.service';
import {
  TripResponse,
  TripService
} from '../../services/trip.service';

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

@Component({
  selector: 'app-planner',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './planner.component.html',
  styleUrl: './planner.component.css'
})
export class PlannerComponent
  implements OnInit, OnDestroy {
  scrolled = false;
  tripId = '';
  trip: TripResponse | null = null;
  isEditingPreferences = false;
  generationProgress = 12;
  generationCityIndex = 0;
  generationMessage =
    'Gathering your trip details…';
  private generationTimer:
    ReturnType<typeof setInterval> | null = null;

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
  selectedDietaryNeeds: string[] = [];
  selectedAccessibilityNeeds: string[] = [];
  preferredLanguage: 'en' | 'ar' = 'en';
  additionalNotes = '';

  budgetAmount = 10_000_000;
  currencyCode = 'USD';
  travelerCount = 1;

  itinerary: ItineraryResponse | null = null;
  errorMessage = '';
  failedPhotoUrls = new Set<string>();
  venueSuggestions:
    Record<string, VenueSuggestion[]> = {};
  activeVenueItemId = '';
  loadingVenueItemId = '';

  isGenerating = false;
  isSaving = false;

  editingItemId = '';
  addingDayId = '';

  itemDraft: ItemDraft = this.emptyDraft();

  readonly categories = [
    'activity',
    'meal',
    'transport',
    'accommodation',
    'other'
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

  readonly dietaryOptions: PreferenceOption[] = [
    {
      value: 'vegetarian',
      label: 'Vegetarian',
      icon: 'eco'
    },
    {
      value: 'vegan',
      label: 'Vegan',
      icon: 'nutrition'
    },
    {
      value: 'halal',
      label: 'Halal',
      icon: 'verified'
    },
    {
      value: 'kosher',
      label: 'Kosher',
      icon: 'verified'
    },
    {
      value: 'gluten_free',
      label: 'Gluten-free',
      icon: 'no_food'
    },
    {
      value: 'food_allergies',
      label: 'Food allergies',
      icon: 'allergy'
    }
  ];

  readonly accessibilityOptions:
    PreferenceOption[] = [
      {
        value: 'wheelchair_accessible',
        label: 'Wheelchair accessible',
        icon: 'accessible'
      },
      {
        value: 'limited_walking',
        label: 'Limited walking',
        icon: 'transfer_within_a_station'
      },
      {
        value: 'step_free',
        label: 'Step-free access',
        icon: 'elevator'
      },
      {
        value: 'visual_support',
        label: 'Visual assistance',
        icon: 'visibility'
      },
      {
        value: 'hearing_support',
        label: 'Hearing assistance',
        icon: 'hearing'
      }
    ];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly itineraryService: ItineraryService,
    private readonly tripService: TripService
  ) {}

  ngOnInit(): void {
    this.tripId =
      this.route.snapshot.paramMap.get('tripId') ??
      this.route.snapshot.queryParamMap.get('tripId') ??
      '';

    if (this.tripId) {
      this.trip =
        this.tripService.getRememberedTrip(
          this.tripId
        );
      void this.loadExistingItinerary();
    }
  }

  ngOnDestroy(): void {
    this.stopGenerationProgress();
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.scrolled = window.scrollY > 20;
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
      return `${this.visibleDays.length} ${
        this.visibleDays.length === 1
          ? 'day'
          : 'days'
      }`;
    }

    const start = new Date(this.trip.startDate);
    const end = new Date(this.trip.endDate);
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
    return item.costSource ===
      'live_flight_price';
  }

  flightDepartureCode(
    item: ItineraryItem
  ): string {
    return this.flightCodes(item)[0] || 'DEP';
  }

  flightArrivalCode(
    item: ItineraryItem
  ): string {
    return this.flightCodes(item)[1] || 'ARR';
  }

  flightDetail(
    item: ItineraryItem,
    index: number
  ): string {
    return (
      item.description
        ?.split('·')
        .map((part) => part.trim())
        .filter(Boolean)[index] ?? ''
    );
  }

  flightDuration(
    item: ItineraryItem
  ): string {
    return (
      this.flightDetail(item, 3) ||
      this.flightDetail(item, 2) ||
      'Flight'
    );
  }

  flightStops(
    item: ItineraryItem
  ): string {
    return (
      this.flightDetail(item, 2) ||
      'Route details'
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
    ) >= 2;
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

  toggleDietaryNeed(value: string): void {
    this.selectedDietaryNeeds =
      this.toggleOption(
        this.selectedDietaryNeeds,
        value,
        4,
        'dietary needs'
      );
  }

  toggleAccessibilityNeed(value: string): void {
    this.selectedAccessibilityNeeds =
      this.toggleOption(
        this.selectedAccessibilityNeeds,
        value,
        4,
        'accessibility needs'
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

    this.activeVenueItemId = item.id;

    if (this.venueSuggestions[item.id]) {
      return;
    }

    this.loadingVenueItemId = item.id;

    try {
      const response = await firstValueFrom(
        this.itineraryService
          .getVenueSuggestions(
            this.tripId,
            item.id
          )
      );
      this.venueSuggestions[item.id] =
        response.data ?? [];
    } catch (error: unknown) {
      this.errorMessage = this.getErrorMessage(
        error,
        'Nearby venue choices could not be loaded.'
      );
    } finally {
      this.loadingVenueItemId = '';
    }
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
          venue
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

    if (!this.validateGenerationForm()) {
      return;
    }

      const request: GenerateItineraryRequest = {
        preferences: this.buildPreferences(),
        budgetAmount: this.planningBudgetCeiling(),
      currencyCode:
        this.currencyCode.toUpperCase(),
      travelerCount: this.travelerCount
    };

    this.isGenerating = true;
    this.startGenerationProgress();
    this.cancelItemForm();

    try {
      const response = await firstValueFrom(
        this.itineraryService
          .generateItinerary(
            this.tripId,
            request
          )
      );

      if (!response.data) {
        throw new Error(
          'The server did not return an itinerary.'
        );
      }

      this.applyItinerary(response.data);
    } catch (error: unknown) {
      const message = this.getErrorMessage(
        error,
        'The itinerary could not be generated. Please try again.'
      );

      this.errorMessage = this.itinerary
        ? `${message} Your existing itinerary is still available below.`
        : message;
    } finally {
      this.isGenerating = false;
      this.stopGenerationProgress();
    }
  }

  startAdd(day: ItineraryDay): void {
    this.editingItemId = '';
    this.addingDayId = day.id;

    this.itemDraft = this.emptyDraft(
      day.id,
      day.items.length
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
        {
          ...this.toRequest(),
          dayId: this.addingDayId
        }
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
        this.toRequest()
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
      this.itineraryService.updateItem(
        this.tripId,
        item.id,
        {
          targetDayId: targetDay.id,
          title: item.title,
          description: item.description,
          startTime: item.startTime,
          endTime: item.endTime,
          location: item.location,
          category: item.category,
          estimatedCost: item.estimatedCost,
          orderIndex: Math.max(
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
    request: ReturnType<
      ItineraryService['addItem']
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
      dietaryNeeds: [],
      accessibilityNeeds: [],
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

  private startGenerationProgress(): void {
    this.stopGenerationProgress();
    this.generationProgress = 12;
    this.generationCityIndex = 0;
    this.generationMessage =
      'Gathering your trip details…';

    const messages = [
      'Arranging the best flow for each day…',
      'Balancing activities, meals, and rest…',
      'Checking routes and practical timing…',
      'Almost there — adding the finishing touches…'
    ];
    let messageIndex = 0;

    this.generationTimer = setInterval(() => {
      this.generationProgress = Math.min(
        92,
        this.generationProgress +
          Math.max(
            2,
            Math.round(
              (94 - this.generationProgress) *
                0.09
            )
          )
      );

      messageIndex =
        (messageIndex + 1) % messages.length;
      this.generationMessage =
        messages[messageIndex];

      const cityProgress = Math.floor(
        (this.generationProgress / 94) *
          this.generationCities.length
      );
      this.generationCityIndex = Math.min(
        this.generationCities.length - 1,
        cityProgress
      );
    }, 1800);
  }

  private stopGenerationProgress(): void {
    if (this.generationTimer) {
      clearInterval(this.generationTimer);
      this.generationTimer = null;
    }
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
        ['egypt', 'cairo', 'luxor', 'aswan'],
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

  private flightCodes(
    item: ItineraryItem
  ): [string, string] {
    const placeIdParts =
      item.googlePlaceId?.split(':') ?? [];

    if (
      placeIdParts.length >= 3 &&
      placeIdParts[0] === 'serp-flight'
    ) {
      return [
        placeIdParts[1].toUpperCase(),
        placeIdParts[2].toUpperCase()
      ];
    }

    const addressCodes = [
      ...(item.address?.matchAll(
        /\(([A-Z]{3})\)/g
      ) ?? [])
    ].map((match) => match[1]);

    return [
      addressCodes[0] ?? '',
      addressCodes[1] ?? ''
    ];
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

    this.selectedDietaryNeeds = [];
    this.selectedAccessibilityNeeds = [];

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

  private planningBudgetCeiling(): number {
    // Accommodation style now controls the spending tier. This value is
    // only a generous internal safety ceiling required by the existing API;
    // it is not presented as a traveler-selected budget.
    return 10_000_000;
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

    return true;
  }

  private toRequest(): ItineraryItemRequest {
    return {
      targetDayId:
        this.itemDraft.targetDayId,
      title:
        this.itemDraft.title.trim(),
      description:
        this.itemDraft.description.trim() ||
        null,
      startTime:
        this.itemDraft.startTime || null,
      endTime:
        this.itemDraft.endTime || null,
      location:
        this.itemDraft.location.trim() ||
        null,
      category:
        this.itemDraft.category,
      estimatedCost:
        this.itemDraft.estimatedCost,
      orderIndex:
        this.itemDraft.orderIndex
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
}
