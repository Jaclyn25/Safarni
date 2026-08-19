import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  OnDestroy,
  OnInit,
  PLATFORM_ID,
  ViewChild,
  inject
} from '@angular/core';
import {
  CommonModule,
  isPlatformBrowser
} from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ActivatedRoute,
  Router,
  RouterLink,
  RouterLinkActive
} from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  CreateTripRequest,
  FlightAirportSuggestion,
  TravelCountry,
  TripCitySelection,
  TripResponse,
  TripService
} from '../../services/trip.service';
import { AuthService } from '../../services/auth.service';
import {
  PreferencesService,
  TravelProfile
} from '../../services/preferences.service';
import { MarketplaceService } from '../../services/marketplace.service';
import { CurrencyService } from '../../services/currency.service';
import { TravelPackage } from '../../models/marketplace.models';
import { packageCoverUrl } from '../../shared/constants/marketplace.constants';

export interface FeaturedDestination {
  id: string;
  slug: string;
  country: string;
  title: string;
  description: string;
  bestFor: string;
  suggestedLength: string;
  image: string;
  imageSrcset: string;
  tag?: string;
  tagType?: 'hot' | 'luxury' | 'popular' | 'budget';
  /** Primary spotlight card (50% width on large screens, spans both tablet columns). */
  featured?: boolean;
  /** Large-screen column span tier for supporting cards (3 = first row, 4 = second row). */
  lgSpan?: 3 | 4;
}

export interface AiRecommendation {
  id: string;
  destination: string;
  title: string;
  description: string;
  durationDays: number;
  styleTags: string[];
  estimatedBudget: number;
  image: string;
  highlights: string[];
}

@Component({
  selector: 'app-explore',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './explore.component.html',
  styleUrl: './explore.component.css'
})
export class ExploreComponent
  implements OnInit, AfterViewInit, OnDestroy {
  private static readonly TRIP_DRAFT_KEY =
    'safarni_trip_draft';

  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private tripService = inject(TripService);
  private authService = inject(AuthService);
  private preferencesService = inject(PreferencesService);
  private marketplaceService = inject(MarketplaceService);
  private currencyService = inject(CurrencyService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);
  private platformId = inject(PLATFORM_ID);

  @ViewChild('explorePage', { static: true })
  private explorePage!: ElementRef<HTMLElement>;

  @ViewChild('bgVideo', { static: true })
  private bgVideo!: ElementRef<HTMLVideoElement>;

  private revealObserver: IntersectionObserver | null = null;
  private reducedMotionMediaQuery: MediaQueryList | null = null;
  private closeAnimationTimer: ReturnType<typeof setTimeout> | null = null;
  private motionInitTimer: ReturnType<typeof setTimeout> | null = null;
  private revealRefreshTimer: ReturnType<typeof setTimeout> | null = null;

  isCreateTripOpen = false;
  isCreateTripClosing = false;
  isLoadingCountries = false;
  isLoadingCities = false;
  isLoadingOriginAirports = false;
  isLoadingDestinationAirports = false;
  isCreatingTrip = false;
  createTripError = '';
  countryLoadError = '';
  cityLoadError = '';
  originAirportLoadError = '';
  destinationAirportLoadError = '';

  countries: TravelCountry[] = [];
  availableCities: string[] = [];
  originAirports: FlightAirportSuggestion[] = [];
  destinationAirports: FlightAirportSuggestion[] = [];
  destinationAirportCode: string | null = null;
  destinationAirportName: string | null = null;
  destinationAirportCity: string | null = null;
  citySearch = '';
  selectedCities: TripCitySelection[] = [];

  currentTrip: TripResponse | null = null;
  recentTrips: TripResponse[] = [];
  isTripsLoading = false;
  tripsLoadError = '';
  travelProfile: TravelProfile | null = null;
  isPreferencesLoading = false;
  marketplacePackages: TravelPackage[] = [];
  isMarketplaceLoading = true;
  marketplaceLoadError = '';

  tripDraft: CreateTripRequest = {
    name: '',
    destination: '',
    originCountry: '',
    originCountryCode: null,
    originCity: null,
    originAirportCode: null,
    originAirportName: null,
    startDate: '',
    endDate: '',
    cities: []
  };

  searchQuery: string = '';
  videoLoaded: boolean = false;
  motionReady = false;
  prefersReducedMotion = false;

  loadedImages = new Set<string>();
  carouselIndex = 0;
  isCarouselScrolling = false;
  private carouselScrollTimer: ReturnType<typeof setTimeout> | null = null;

  quickPrompts: string[] = [
    'Romantic weekend in Paris',
    'Beach holiday in Maldives',
    'Cultural and food trip in Kyoto'
  ];

  featuredDestinations: FeaturedDestination[] = [
    {
      id: 'maldives',
      slug: 'maldives',
      country: 'Maldives',
      title: 'Tropical Paradise',
      description: 'Crystal clear waters, overwater villas & unforgettable sunsets',
      bestFor: 'Beach escapes',
      suggestedLength: '5–7 days',
      image: 'assets/countries/maldives.webp',
      imageSrcset: 'assets/countries/maldives-480.webp 480w, assets/countries/maldives-800.webp 800w, assets/countries/maldives.webp 1024w',
      tag: 'Hot',
      tagType: 'hot',
      featured: true
    },
    {
      id: 'thailand',
      slug: 'thailand',
      country: 'Thailand',
      title: 'Land of Smiles',
      description: 'Ancient temples, vibrant street food & pristine islands',
      bestFor: 'Culture & food',
      suggestedLength: '7–10 days',
      image: 'assets/countries/thailand.webp',
      imageSrcset: 'assets/countries/thailand-480.webp 480w, assets/countries/thailand-800.webp 800w, assets/countries/thailand.webp 1024w',
      lgSpan: 3
    },
    {
      id: 'turkey',
      slug: 'turkey',
      country: 'Turkey',
      title: 'Where East Meets West',
      description: 'Majestic mosques, hot air balloons & the Bosphorus',
      bestFor: 'History & cities',
      suggestedLength: '6–9 days',
      image: 'assets/countries/turkey.webp',
      imageSrcset: 'assets/countries/turkey-480.webp 480w, assets/countries/turkey-800.webp 800w, assets/countries/turkey.webp 1024w',
      lgSpan: 3
    },
    {
      id: 'dubai',
      slug: 'dubai',
      country: 'Dubai, UAE',
      title: 'City of Gold',
      description: 'Futuristic skyline, desert safaris & world-class luxury',
      bestFor: 'Luxury & adventure',
      suggestedLength: '4–6 days',
      image: 'assets/countries/dubai.webp',
      imageSrcset: 'assets/countries/dubai-480.webp 480w, assets/countries/dubai-800.webp 800w, assets/countries/dubai.webp 1024w',
      tag: 'Luxury',
      tagType: 'luxury',
      lgSpan: 4
    },
    {
      id: 'japan',
      slug: 'japan',
      country: 'Japan',
      title: 'Ancient & Modern',
      description: 'Cherry blossoms, neon lights & centuries of tradition',
      bestFor: 'Culture & cuisine',
      suggestedLength: '8–12 days',
      image: 'assets/countries/japan.webp',
      imageSrcset: 'assets/countries/japan-480.webp 480w, assets/countries/japan-800.webp 800w, assets/countries/japan-1200.webp 1200w, assets/countries/japan.webp 1600w',
      lgSpan: 4
    },
    {
      id: 'france',
      slug: 'france',
      country: 'France',
      title: 'City of Love',
      description: 'Eiffel Tower, Louvre Museum & French countryside charm',
      bestFor: 'Art & romance',
      suggestedLength: '5–8 days',
      image: 'assets/countries/france.webp',
      imageSrcset: 'assets/countries/france-480.webp 480w, assets/countries/france-800.webp 800w, assets/countries/france-1200.webp 1200w, assets/countries/france.webp 1600w',
      tag: 'Popular',
      tagType: 'popular',
      lgSpan: 4
    }
  ];

  aiRecommendations: AiRecommendation[] = [
    {
      id: 'kyoto-modernism',
      destination: 'Japan',
      title: 'Kyoto Modernism',
      description: "Discover the intersection of ancient tradition and contemporary architecture in Japan's cultural heart.",
      durationDays: 6,
      styleTags: ['Culture', 'Architecture', 'Foodie'],
      estimatedBudget: 1850,
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAwzJeouqB-QGW8xaF3oCU6ggXGNk6FTBcbPHTm7x8kw9EWzM2IMkrq7gELWCzQwqdRWv0szO_l7Kcys-9vgtx3DpSIR2h3C-Hqrmo47dqz59m0nD0WApCUw5P86bsb2CR_VumsRgR-7IM3Bz3gRSKc0sHqlkm1aa3g2pwVdReAeRMCE18DuxzL-YeinmCKxBHd7igjPK7ndTUu7Um_nN81MNEVoeI77gIL0Y-eMG65I19gAv1_c_SNoA',
      highlights: ['Curated Museum Pass', 'Secret Temple Access']
    },
    {
      id: 'lisbon-textures',
      destination: 'Portugal',
      title: 'Lisbon Textures',
      description: 'Perfect for a 4-day deep dive into tile art and coastal dining.',
      durationDays: 4,
      styleTags: ['Art & Tile', 'Coastal Dining'],
      estimatedBudget: 1240,
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBqU7pwfRPO5mu8-6wDyuH7u4ijRV2qyHzDcGh7spwDUVlITEk4AyEEDj5UvkC09H1RHke9zLV6s8gWdnhmm5r5Y_X45-Vbc_bWE4sQh5CPFVjmxvvAP4DPVkb2g_wO-Az29GggTTe-kCHLi0jZF7I8fyuJqAFQqSCFd4pF8QB_HS9HRAkN77ls3oX1-bbryDljr4G-ftK3k2KxJcQ6orG6uCv-1V0fi-BLn56jy_vF6BwWRfop33OJtw',
      highlights: ['Historic Tram Route', 'Seafood Tasting']
    },
    {
      id: 'santorini-escape',
      destination: 'Greece',
      title: 'Santorini Escape',
      description: 'A curated selection of private villas with caldera views and sunset cruises.',
      durationDays: 5,
      styleTags: ['Relaxation', 'Luxury', 'Views'],
      estimatedBudget: 2800,
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAMMyb6UfeIlaXccBIYhVWU2pdy2QXcTFucYI-nJSmIamPXVMBDotTZ2csOa446mC8smElnzlmkJQyQ1cMw9XMdpeL3XB-hEeWBhBQf7_O1_MsKs6egD_rXLwSxXOWmUhTFOclZIc7LI4p-jJC3PXkhJVdEK8DnbqSPTIVI8ESaDWvAGv9GikJLQUC6NMISWN5lg2OsB-F2_vuSa4szJMl5fni7Sjwr57s8gZlDtpwmLwn51YHJuY1i0A',
      highlights: ['Private Villa Pool', 'Catamaran Sunset Cruise']
    }
  ];

  get isSignedIn(): boolean {
    return this.authService.isAuthenticated();
  }

  get travelerFirstName(): string {
    const fullName = this.authService.getCurrentUser()?.fullName?.trim();
    return fullName?.split(/\s+/)[0] || 'Traveler';
  }

  get hasPersonalizedIdeas(): boolean {
    return Boolean(
      this.travelProfile &&
      (
        this.travelProfile.preferredDestinationCountry?.trim() ||
        this.travelProfile.travelStyle?.trim() ||
        this.travelProfile.travelPurpose?.trim()
      )
    );
  }

  get tripIdeas(): AiRecommendation[] {
    if (!this.hasPersonalizedIdeas || !this.travelProfile) {
      return this.aiRecommendations;
    }

    const destination = this.travelProfile.preferredDestinationCountry
      .trim()
      .toLocaleLowerCase();
    const preferenceWords = `${this.travelProfile.travelStyle} ${this.travelProfile.travelPurpose}`
      .toLocaleLowerCase()
      .split(/[,\s]+/)
      .filter(Boolean);

    return [...this.aiRecommendations].sort((first, second) =>
      this.ideaMatchScore(second, destination, preferenceWords) -
      this.ideaMatchScore(first, destination, preferenceWords)
    );
  }

  get tripIdeasHeading(): string {
    return this.hasPersonalizedIdeas
      ? 'Picked around your preferences'
      : 'Trip ideas to get you started';
  }

  get tripIdeasSubtitle(): string {
    if (!this.hasPersonalizedIdeas || !this.travelProfile) {
      return 'Use a complete idea as the starting point for your own trip.';
    }

    const details = [
      this.travelProfile.preferredDestinationCountry?.trim(),
      this.travelProfile.travelStyle?.trim()
    ].filter(Boolean);

    return `Starting points shaped by ${details.join(' and ')}.`;
  }

  tripIdeaLabel(index: number): string {
    return this.hasPersonalizedIdeas && index === 0
      ? 'Best preference match'
      : 'Trip template';
  }

  tripDateRange(trip: TripResponse): string {
    const format = (value: string): string =>
      new Date(value).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      });

    return `${format(trip.startDate)} – ${format(trip.endDate)}`;
  }

  tripCitySummary(trip: TripResponse): string {
    const cities = (trip.cities ?? []).map((city) => city.name);
    return cities.length > 0 ? cities.join(' · ') : trip.destination;
  }

  currentTripStatusLabel(trip: TripResponse): string {
    switch (trip.itineraryStatus) {
      case 'completed':
        return 'Itinerary ready';
      case 'generating':
        return 'Building your itinerary';
      case 'failed':
        return 'Itinerary needs attention';
      default:
        return 'Ready for the next step';
    }
  }

  currentTripActionLabel(trip: TripResponse): string {
    switch (trip.itineraryStatus) {
      case 'completed':
        return 'Open itinerary';
      case 'generating':
        return 'View progress';
      case 'failed':
        return 'Review trip';
      default:
        return 'Continue planning';
    }
  }

  currentTripCost(trip: TripResponse): string | null {
    if (trip.estimatedTotalCost == null) {
      return null;
    }

    return this.currencyService.format(
      trip.estimatedTotalCost,
      trip.itineraryCurrency ?? 'USD'
    );
  }

  openCurrentTrip(): void {
    if (!this.currentTrip) {
      return;
    }

    void this.router.navigate(['/planner', this.currentTrip.id]);
  }

  tripImage(trip: TripResponse): string {
    const cities = (trip.cities ?? [])
      .map((city) => city.name)
      .join(' ');

    return this.destinationImage(
      `${trip.destination} ${cities}`
    );
  }

  retryCurrentTrip(): void {
    this.loadCurrentTrip();
  }

  marketplaceCover(packageItem: TravelPackage): string {
    return packageCoverUrl(packageItem);
  }

  marketplacePrice(packageItem: TravelPackage): string {
    return this.currencyService.format(
      packageItem.price,
      packageItem.currencyCode
    );
  }

  marketplaceDate(packageItem: TravelPackage): string {
    return new Date(packageItem.startDate).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  retryMarketplace(): void {
    this.loadMarketplacePackages();
  }

  private ideaMatchScore(
    idea: AiRecommendation,
    destination: string,
    preferenceWords: string[]
  ): number {
    const searchable = `${idea.destination} ${idea.title} ${idea.styleTags.join(' ')}`
      .toLocaleLowerCase();
    const destinationScore = destination && searchable.includes(destination)
      ? 5
      : 0;
    const styleScore = preferenceWords.reduce(
      (score, word) => score + (searchable.includes(word) ? 1 : 0),
      0
    );
    return destinationScore + styleScore;
  }

  get today(): string {
    const now = new Date();
    const timezoneOffset =
      now.getTimezoneOffset() * 60_000;
    return new Date(now.getTime() - timezoneOffset)
      .toISOString()
      .slice(0, 10);
  }

  get tripDayCount(): number {
    if (
      !this.tripDraft.startDate ||
      !this.tripDraft.endDate
    ) {
      return 0;
    }

    const start = new Date(
      `${this.tripDraft.startDate}T12:00:00`
    );
    const end = new Date(
      `${this.tripDraft.endDate}T12:00:00`
    );
    const difference =
      Math.floor(
        (end.getTime() - start.getTime()) /
          86_400_000
      ) + 1;

    return Math.max(0, difference);
  }

  get allocatedDays(): number {
    return this.selectedCities.reduce(
      (sum, city) => sum + Number(city.days || 0),
      0
    );
  }

  get remainingDays(): number {
    return this.tripDayCount - this.allocatedDays;
  }

  get filteredCities(): string[] {
    const query = this.citySearch
      .trim()
      .toLocaleLowerCase();
    const selected = new Set(
      this.selectedCities.map((city) =>
        city.name.toLocaleLowerCase()
      )
    );

    return this.availableCities
      .filter(
        (city) =>
          !selected.has(city.toLocaleLowerCase()) &&
          (!query ||
            city.toLocaleLowerCase().includes(query))
      );
  }

  get selectedOriginAirportLabel(): string {
    if (!this.tripDraft.originAirportCode) {
      return 'Optional — select an airport to include flights.';
    }

    return [
      this.tripDraft.originAirportCode,
      this.tripDraft.originAirportName,
      this.tripDraft.originCity
    ].filter(Boolean).join(' · ');
  }

  get selectedDestinationAirportLabel(): string {
    if (!this.destinationAirportCode) {
      return 'Optional — select an airport to include flights.';
    }

    return [
      this.destinationAirportCode,
      this.destinationAirportName,
      this.destinationAirportCity
    ].filter(Boolean).join(' · ');
  }

  get canCreateTrip(): boolean {
    return Boolean(
      this.tripDraft.name.trim() &&
      this.tripDraft.originCountry &&
      this.tripDraft.destination &&
      this.tripDraft.startDate &&
      this.tripDraft.endDate &&
      this.tripDayCount > 0 &&
      this.selectedCities.length > 0 &&
      this.remainingDays === 0 &&
      !this.isCreatingTrip
    );
  }

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((queryParams) => {
        if (
          queryParams.get('createTrip') ===
            '1' &&
          !this.isCreateTripOpen
        ) {
          void this.openCreateTrip(
            queryParams.get('trip') ?? '',
            queryParams.get('destination') ?? ''
          );
        }
      });

    this.loadExploreData();
  }

  ngAfterViewInit(): void {
    this.motionInitTimer = setTimeout(() => {
      this.motionInitTimer = null;
      this.initializeMotion();
    }, 0);
  }

  ngOnDestroy(): void {
    this.stopRevealObserver();

    if (this.reducedMotionMediaQuery) {
      this.reducedMotionMediaQuery.removeEventListener(
        'change',
        this.handleReducedMotionChange
      );
    }

    if (this.closeAnimationTimer !== null) {
      clearTimeout(this.closeAnimationTimer);
    }

    if (this.carouselScrollTimer !== null) {
      clearTimeout(this.carouselScrollTimer);
    }

    if (this.motionInitTimer !== null) {
      clearTimeout(this.motionInitTimer);
    }

    if (this.revealRefreshTimer !== null) {
      clearTimeout(this.revealRefreshTimer);
    }

    this.setPageScrollLocked(false);
  }

  handleVideoLoaded(video: HTMLVideoElement): void {
    this.videoLoaded = true;

    if (this.prefersReducedMotion) {
      video.pause();
      return;
    }

    void video.play().catch(() => undefined);
  }

  private initializeMotion(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.reducedMotionMediaQuery = window.matchMedia(
      '(prefers-reduced-motion: reduce)'
    );
    this.prefersReducedMotion =
      this.reducedMotionMediaQuery.matches;
    this.reducedMotionMediaQuery.addEventListener(
      'change',
      this.handleReducedMotionChange
    );

    if (this.prefersReducedMotion) {
      this.bgVideo.nativeElement.pause();
      this.showAllRevealElements();
      return;
    }

    this.setupRevealObserver();
  }

  private setupRevealObserver(): void {
    if (
      !isPlatformBrowser(this.platformId) ||
      this.prefersReducedMotion ||
      !('IntersectionObserver' in window)
    ) {
      return;
    }

    const revealElements = Array.from(
      this.explorePage.nativeElement.querySelectorAll<HTMLElement>(
        '[data-reveal]'
      )
    );

    if (revealElements.length === 0) {
      return;
    }

    this.stopRevealObserver();
    this.revealObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) {
            return;
          }

          entry.target.classList.add('is-visible');
          this.revealObserver?.unobserve(entry.target);
        });
      },
      {
        rootMargin: '0px 0px -8% 0px',
        threshold: 0.1
      }
    );

    this.motionReady = true;
    revealElements.forEach((element) => {
      this.revealObserver?.observe(element);
    });
    this.cdr.markForCheck();
  }

  private scheduleRevealRefresh(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    if (this.revealRefreshTimer !== null) {
      clearTimeout(this.revealRefreshTimer);
    }

    this.revealRefreshTimer = setTimeout(() => {
      this.revealRefreshTimer = null;

      if (this.prefersReducedMotion) {
        this.showAllRevealElements();
        return;
      }

      this.setupRevealObserver();
    }, 0);
  }

  private stopRevealObserver(): void {
    this.revealObserver?.disconnect();
    this.revealObserver = null;
  }

  private showAllRevealElements(): void {
    const revealElements =
      this.explorePage?.nativeElement.querySelectorAll<HTMLElement>(
        '[data-reveal]'
      );

    revealElements?.forEach((element) => {
      element.classList.add('is-visible');
    });
  }

  private handleReducedMotionChange = (
    event: MediaQueryListEvent
  ): void => {
    this.prefersReducedMotion = event.matches;
    this.stopRevealObserver();
    this.showAllRevealElements();
    this.motionReady = false;

    if (this.prefersReducedMotion) {
      this.bgVideo.nativeElement.pause();
    } else {
      this.setupRevealObserver();

      if (this.videoLoaded) {
        void this.bgVideo.nativeElement
          .play()
          .catch(() => undefined);
      }
    }

    this.cdr.markForCheck();
  };

  @HostListener('document:keydown.escape')
  handleEscape(): void {
    if (
      this.isCreateTripOpen &&
      !this.isCreatingTrip
    ) {
      this.closeCreateTrip();
    }
  }

  async openCreateTrip(
    suggestedName = '',
    suggestedDestination = ''
  ): Promise<void> {
    if (!this.authService.isAuthenticated()) {
      this.persistTripDraft();
      this.setPageScrollLocked(false);
      void this.router.navigate(['/login'], {
        queryParams: { returnUrl: this.router.url }
      });
      return;
    }

    this.cancelCloseAnimation();
    this.restoreTripDraft();
    this.createTripError = '';
    this.tripDraft.name =
      suggestedName || this.tripDraft.name;
    this.isCreateTripOpen = true;
    this.setPageScrollLocked(true);

    if (this.countries.length === 0) {
      await this.loadCountries();
    }

    await this.loadAirportOptionsForDraft();

    if (
      suggestedDestination &&
      this.countries.some(
        (country) =>
          country.name.toLocaleLowerCase() ===
          suggestedDestination.toLocaleLowerCase()
      )
    ) {
      this.tripDraft.destination =
        suggestedDestination;
      await this.onDestinationChanged();
    }
  }

  closeCreateTrip(): void {
    if (this.isCreatingTrip || this.isCreateTripClosing) {
      return;
    }

    this.isCreateTripClosing = true;
    this.setPageScrollLocked(false);
    this.clearCreateTripQueryParams();

    const closeDuration = this.prefersReducedMotion
      ? 0
      : 180;

    this.closeAnimationTimer = setTimeout(() => {
      this.isCreateTripOpen = false;
      this.isCreateTripClosing = false;
      this.closeAnimationTimer = null;
      this.cdr.markForCheck();
    }, closeDuration);
  }

  async onOriginCountryChanged(): Promise<void> {
    const country = this.countries.find(
      (candidate) => candidate.name === this.tripDraft.originCountry
    );

    this.tripDraft.originCountryCode = country?.iso2 ?? null;
    this.tripDraft.originCity = null;
    this.tripDraft.originAirportCode = null;
    this.tripDraft.originAirportName = null;
    this.originAirports = [];
    this.originAirportLoadError = '';

    if (!this.tripDraft.originCountry) {
      return;
    }

    await this.loadOriginAirports();
  }

  selectOriginAirport(code: string): void {
    const normalizedCode = code?.trim().toUpperCase() || '';
    const airport = this.originAirports.find(
      (candidate) => candidate.code.toUpperCase() === normalizedCode
    );

    this.tripDraft.originAirportCode = airport?.code ?? null;
    this.tripDraft.originAirportName = airport?.name ?? null;
    this.tripDraft.originCity = airport?.city ?? null;
  }

  async onDestinationChanged(): Promise<void> {
    this.availableCities = [];
    this.selectedCities = [];
    this.citySearch = '';
    this.cityLoadError = '';
    this.destinationAirports = [];
    this.destinationAirportCode = null;
    this.destinationAirportName = null;
    this.destinationAirportCity = null;
    this.destinationAirportLoadError = '';

    if (!this.tripDraft.destination) {
      return;
    }

    await Promise.all([
      this.loadDestinationCities(),
      this.loadDestinationAirports()
    ]);
  }

  selectDestinationAirport(code: string): void {
    const normalizedCode = code?.trim().toUpperCase() || '';
    const airport = this.destinationAirports.find(
      (candidate) =>
        candidate.code.toUpperCase() === normalizedCode
    );

    this.destinationAirportCode = airport?.code ?? null;
    this.destinationAirportName = airport?.name ?? null;
    this.destinationAirportCity = airport?.city ?? null;
  }

  onDatesChanged(): void {
    if (
      this.tripDraft.endDate &&
      this.tripDraft.startDate &&
      this.tripDraft.endDate <
        this.tripDraft.startDate
    ) {
      this.tripDraft.endDate =
        this.tripDraft.startDate;
    }

    this.rebalanceCityDays();
  }

  addCity(cityName: string): void {
    if (
      !cityName ||
      this.selectedCities.some(
        (city) =>
          city.name.toLocaleLowerCase() ===
          cityName.toLocaleLowerCase()
      ) ||
      this.selectedCities.length >=
        this.tripDayCount
    ) {
      return;
    }

    if (this.selectedCities.length === 0) {
      this.selectedCities = [
        {
          name: cityName,
          days: Math.max(1, this.tripDayCount)
        }
      ];
    } else {
      const donorIndex =
        this.findCityWithMostDays();

      if (
        donorIndex < 0 ||
        this.selectedCities[donorIndex].days <= 1
      ) {
        return;
      }

      this.selectedCities =
        this.selectedCities.map(
          (city, index) =>
            index === donorIndex
              ? {
                  ...city,
                  days: city.days - 1
                }
              : city
        );
      this.selectedCities = [
        ...this.selectedCities,
        { name: cityName, days: 1 }
      ];
    }

    this.citySearch = '';
  }

  removeCity(cityName: string): void {
    const removed = this.selectedCities.find(
      (city) => city.name === cityName
    );
    this.selectedCities =
      this.selectedCities.filter(
        (city) => city.name !== cityName
      );

    if (
      removed &&
      this.selectedCities.length > 0
    ) {
      this.selectedCities = this.selectedCities.map(
        (city, index) =>
          index === 0
            ? {
                ...city,
                days: city.days + removed.days
              }
            : city
      );
    }
  }

  changeCityDays(
    cityName: string,
    rawValue: number | string
  ): void {
    const nextValue = Math.max(
      1,
      Math.floor(Number(rawValue) || 1)
    );

    this.selectedCities = this.selectedCities.map(
      (city) =>
        city.name === cityName
          ? { ...city, days: nextValue }
          : city
    );
  }

  /**
   * Persists the in-progress trip draft so it survives
   * the round trip to /login when sign-in is required.
   */
  private persistTripDraft(): void {
    sessionStorage.setItem(
      ExploreComponent.TRIP_DRAFT_KEY,
      JSON.stringify({
        draft: this.tripDraft,
        selectedCities: this.selectedCities,
        destinationAirportCode:
          this.destinationAirportCode,
        destinationAirportName:
          this.destinationAirportName,
        destinationAirportCity:
          this.destinationAirportCity
      })
    );
  }

  /**
   * Restores a previously persisted draft (and clears it).
   */
  private restoreTripDraft(): void {
    const raw = sessionStorage.getItem(
      ExploreComponent.TRIP_DRAFT_KEY
    );

    if (!raw) {
      return;
    }

    try {
      const parsed = JSON.parse(raw) as {
        draft?: Partial<CreateTripRequest>;
        selectedCities?: TripCitySelection[];
        destinationAirportCode?: string | null;
        destinationAirportName?: string | null;
        destinationAirportCity?: string | null;
      };

      if (parsed?.draft) {
        this.tripDraft = {
          ...this.tripDraft,
          ...parsed.draft
        };
      }

      if (Array.isArray(parsed?.selectedCities)) {
        this.selectedCities = parsed.selectedCities.map(
          (city) => ({
            name: city.name,
            days: city.days
          })
        );
      }

      this.destinationAirportCode =
        parsed.destinationAirportCode ?? null;
      this.destinationAirportName =
        parsed.destinationAirportName ?? null;
      this.destinationAirportCity =
        parsed.destinationAirportCity ?? null;
    } catch {
      // Ignore corrupt drafts.
    } finally {
      sessionStorage.removeItem(
        ExploreComponent.TRIP_DRAFT_KEY
      );
    }
  }

  async createTrip(): Promise<void> {
    this.createTripError = '';

    if (!this.canCreateTrip) {
      this.createTripError =
        this.remainingDays === 0
          ? 'Please complete all required trip details.'
          : `Allocate all trip days before continuing.`;
      return;
    }

    if (!this.authService.isAuthenticated()) {
      this.persistTripDraft();
      this.setPageScrollLocked(false);
      void this.router.navigate(['/login'], {
        queryParams: { returnUrl: '/?createTrip=1' }
      });
      return;
    }

    this.isCreatingTrip = true;

    const request: CreateTripRequest = {
      ...this.tripDraft,
      name: this.tripDraft.name.trim(),
      originCountryCode:
        this.tripDraft.originCountryCode?.toUpperCase() || null,
      originCity:
        this.tripDraft.originCity?.trim() || null,
      originAirportCode:
        this.tripDraft.originAirportCode?.toUpperCase() || null,
      originAirportName:
        this.tripDraft.originAirportName?.trim() || null,
      cities: this.selectedCities.map((city, index) => {
        const isFlightEndpoint =
          index === 0 ||
          index === this.selectedCities.length - 1;

        return {
          name: city.name.trim(),
          days: Number(city.days),
          airportCode: isFlightEndpoint
            ? this.destinationAirportCode?.toUpperCase() || null
            : null,
          airportName: isFlightEndpoint
            ? this.destinationAirportName?.trim() || null
            : null
        };
      })
    };

    try {
      const response = await firstValueFrom(
        this.tripService.createTrip(request)
      );

      if (!response.data) {
        throw new Error(
          'The trip was created without a response.'
        );
      }

      this.tripService.rememberTrip(response.data);
      this.closeCreateTripAfterSuccess();
      await this.router.navigate([
        '/planner',
        response.data.id
      ]);
    } catch (error: unknown) {
      this.createTripError =
        this.tripService.getErrorMessage(
          error,
          'Your trip could not be created. Please try again.'
        );
    } finally {
      this.isCreatingTrip = false;
      this.cdr.markForCheck();
    }
  }

  onSearch(): void {
    const query = this.searchQuery.trim();
    if (query) {
      this.router.navigate(['/concierge'], { queryParams: { q: query } });
    } else {
      this.router.navigate(['/concierge']);
    }
  }

  onSelectPrompt(promptText: string): void {
    this.searchQuery = promptText;
    this.onSearch();
  }

  onImageLoad(destId: string): void {
    this.loadedImages.add(destId);
  }

  featuredAriaLabel(dest: FeaturedDestination): string {
    return `Explore ${dest.title} in ${dest.country}, suggested for ${dest.bestFor}`;
  }

  /** Horizontal distance (in px) between consecutive carousel card starts. */
  private carouselStep(track: HTMLElement): number {
    const count = this.featuredDestinations.length;
    if (count <= 1) {
      return 0;
    }
    return (
      Math.max(0, track.scrollWidth - track.clientWidth) / (count - 1)
    );
  }

  onCarouselScroll(track: HTMLElement): void {
    const step = this.carouselStep(track);
    this.carouselIndex =
      step > 0 ? Math.min(
        Math.round(track.scrollLeft / step),
        this.featuredDestinations.length - 1
      ) : 0;

    this.isCarouselScrolling = true;
    if (this.carouselScrollTimer !== null) {
      clearTimeout(this.carouselScrollTimer);
    }
    this.carouselScrollTimer = setTimeout(() => {
      this.isCarouselScrolling = false;
      this.carouselScrollTimer = null;
      this.cdr.markForCheck();
    }, 260);
  }

  scrollCarousel(track: HTMLElement, direction: 1 | -1): void {
    const step = this.carouselStep(track);
    const target = Math.min(
      Math.max(0, this.carouselIndex + direction),
      this.featuredDestinations.length - 1
    );
    track.scrollTo({
      left: target * step,
      behavior: this.prefersReducedMotion ? 'auto' : 'smooth'
    });
    this.carouselIndex = target;
  }

  goToCarouselCard(track: HTMLElement, index: number): void {
    const step = this.carouselStep(track);
    track.scrollTo({
      left: index * step,
      behavior: this.prefersReducedMotion ? 'auto' : 'smooth'
    });
    this.carouselIndex = index;
  }

  navigateToDestination(destId: string): void {
    const slug = destId.toLowerCase().split(',')[0].trim().replace(/[^a-z0-9]/g, '');
    this.router.navigate(['/destination', slug]);
  }

  buildRecommendation(idea: AiRecommendation): void {
    void this.openCreateTrip(idea.title, idea.destination);
  }

  private async loadCountries(): Promise<void> {
    this.isLoadingCountries = true;
    this.countryLoadError = '';

    try {
      const response = await firstValueFrom(
        this.tripService.getCountries()
      );
      this.countries = (response.data ?? []).sort(
        (first, second) =>
          first.name.localeCompare(second.name)
      );
    } catch {
      this.countryLoadError =
        'Countries could not be loaded. Please close this window and try again.';
    } finally {
      this.isLoadingCountries = false;
      this.cdr.markForCheck();
    }
  }

  private async loadAirportOptionsForDraft(): Promise<void> {
    const tasks: Promise<void>[] = [];

    if (this.tripDraft.originCountry) {
      const country = this.countries.find(
        (candidate) =>
          candidate.name === this.tripDraft.originCountry
      );
      this.tripDraft.originCountryCode ??=
        country?.iso2 ?? null;
      tasks.push(this.loadOriginAirports(true));
    }

    if (this.tripDraft.destination) {
      tasks.push(this.loadDestinationAirports(true));
    }

    await Promise.all(tasks);
  }

  private async loadOriginAirports(
    preserveSelection = false
  ): Promise<void> {
    if (!this.tripDraft.originCountry) {
      return;
    }

    const preservedCode = preserveSelection
      ? this.tripDraft.originAirportCode
      : null;
    const preservedName = preserveSelection
      ? this.tripDraft.originAirportName
      : null;
    const preservedCity = preserveSelection
      ? this.tripDraft.originCity
      : null;

    this.isLoadingOriginAirports = true;
    this.originAirportLoadError = '';

    try {
      const response = await firstValueFrom(
        this.tripService.getAirports(
          this.tripDraft.originCountry
        )
      );

      this.originAirports = this.uniqueAirports(
        response.data ?? []
      );

      if (
        preservedCode &&
        !this.originAirports.some(
          (airport) => airport.code === preservedCode
        )
      ) {
        this.originAirports = [
          {
            code: preservedCode,
            name: preservedName || preservedCode,
            city: preservedCity || this.tripDraft.originCountry
          },
          ...this.originAirports
        ];
      }
    } catch (error: unknown) {
      this.originAirportLoadError =
        this.tripService.getErrorMessage(
          error,
          'Origin airports could not be loaded. You can continue without flights.'
        );
    } finally {
      this.isLoadingOriginAirports = false;
      this.cdr.markForCheck();
    }
  }

  private async loadDestinationCities(): Promise<void> {
    if (!this.tripDraft.destination) {
      return;
    }

    this.isLoadingCities = true;

    try {
      const response = await firstValueFrom(
        this.tripService.getCities(
          this.tripDraft.destination
        )
      );
      this.availableCities = response.data ?? [];
    } catch {
      this.cityLoadError =
        'Cities could not be loaded. Please try selecting the country again.';
    } finally {
      this.isLoadingCities = false;
      this.cdr.markForCheck();
    }
  }

  private async loadDestinationAirports(
    preserveSelection = false
  ): Promise<void> {
    if (!this.tripDraft.destination) {
      return;
    }

    const preservedCode = preserveSelection
      ? this.destinationAirportCode
      : null;
    const preservedName = preserveSelection
      ? this.destinationAirportName
      : null;
    const preservedCity = preserveSelection
      ? this.destinationAirportCity
      : null;

    this.isLoadingDestinationAirports = true;
    this.destinationAirportLoadError = '';

    try {
      const response = await firstValueFrom(
        this.tripService.getAirports(
          this.tripDraft.destination
        )
      );

      this.destinationAirports = this.uniqueAirports(
        response.data ?? []
      );

      if (
        preservedCode &&
        !this.destinationAirports.some(
          (airport) => airport.code === preservedCode
        )
      ) {
        this.destinationAirports = [
          {
            code: preservedCode,
            name: preservedName || preservedCode,
            city:
              preservedCity ||
              this.tripDraft.destination
          },
          ...this.destinationAirports
        ];
      }
    } catch (error: unknown) {
      this.destinationAirportLoadError =
        this.tripService.getErrorMessage(
          error,
          'Destination airports could not be loaded. You can continue without flights.'
        );
    } finally {
      this.isLoadingDestinationAirports = false;
      this.cdr.markForCheck();
    }
  }

  private uniqueAirports(
    airports: FlightAirportSuggestion[]
  ): FlightAirportSuggestion[] {
    const seen = new Set<string>();

    return airports
      .map((airport) => ({
        ...airport,
        code: airport.code.trim().toUpperCase()
      }))
      .filter((airport) => {
        if (!/^[A-Z]{3}$/.test(airport.code)) {
          return false;
        }

        if (seen.has(airport.code)) {
          return false;
        }

        seen.add(airport.code);
        return true;
      });
  }

  private rebalanceCityDays(): void {
    if (this.selectedCities.length === 0) {
      return;
    }

    if (
      this.tripDayCount <
      this.selectedCities.length
    ) {
      this.selectedCities =
        this.selectedCities.slice(
          0,
          this.tripDayCount
        );
    }

    if (this.selectedCities.length === 0) {
      return;
    }

    const resetCities = this.selectedCities.map(
      (city) => ({ ...city, days: 1 })
    );
    resetCities[0].days +=
      this.tripDayCount - resetCities.length;
    this.selectedCities = resetCities;
  }

  private findCityWithMostDays(): number {
    if (this.selectedCities.length === 0) {
      return -1;
    }

    return this.selectedCities.reduce(
      (bestIndex, city, index, cities) =>
        city.days > cities[bestIndex].days
          ? index
          : bestIndex,
      0
    );
  }

  private setPageScrollLocked(
    locked: boolean
  ): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    document.body.style.overflow = locked
      ? 'hidden'
      : '';
  }

  private clearCreateTripQueryParams(): void {
    if (
      this.route.snapshot.queryParamMap.get(
        'createTrip'
      ) !== '1'
    ) {
      return;
    }

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        createTrip: null,
        destination: null,
        trip: null,
        days: null
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private cancelCloseAnimation(): void {
    if (this.closeAnimationTimer !== null) {
      clearTimeout(this.closeAnimationTimer);
      this.closeAnimationTimer = null;
    }

    this.isCreateTripClosing = false;
  }

  private closeCreateTripAfterSuccess(): void {
    this.cancelCloseAnimation();
    this.isCreateTripOpen = false;
    this.setPageScrollLocked(false);
  }

  private loadExploreData(): void {
    this.loadMarketplacePackages();

    if (!this.isSignedIn) {
      return;
    }

    this.loadCurrentTrip();
    this.loadTravelPreferences();
  }

  private loadCurrentTrip(): void {
    this.isTripsLoading = true;
    this.tripsLoadError = '';

    this.tripService.getTrips()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const sortedTrips = this.sortTripsByRecentActivity(
            response.data ?? []
          );
          const readyTrips = sortedTrips.filter(
  (trip) => trip.itineraryStatus === 'completed'
);

          this.recentTrips = readyTrips.slice(0, 3);
          this.currentTrip = this.recentTrips[0] ?? null;
          this.isTripsLoading = false;
          this.cdr.markForCheck();
          this.scheduleRevealRefresh();
        },
        error: (error: unknown) => {
          this.recentTrips = [];
          this.currentTrip = null;
          this.isTripsLoading = false;
          this.tripsLoadError = this.tripService.getErrorMessage(
            error,
            'We could not load your trips right now.'
          );
          this.cdr.markForCheck();
          this.scheduleRevealRefresh();
        }
      });
  }

  private sortTripsByRecentActivity(
    trips: TripResponse[]
  ): TripResponse[] {
    return [...trips].sort((first, second) => {
      const firstActivity = new Date(
        first.updatedAt ?? first.createdAt
      ).getTime();
      const secondActivity = new Date(
        second.updatedAt ?? second.createdAt
      ).getTime();

      return secondActivity - firstActivity;
    });
  }

  private destinationImage(source: string): string {
    const normalized = source
      .toLocaleLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');

    const images: Array<[string[], string]> = [
      [
        ['turkey', 'turkiye', 'istanbul', 'izmir', 'antalya', 'cappadocia'],
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
        keys.some((key) => normalized.includes(key))
      )?.[1] ??
      'https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=1800&q=88&auto=format'
    );
  }

  private loadTravelPreferences(): void {
    this.isPreferencesLoading = true;

    this.preferencesService.getTravelProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.travelProfile = profile;
          this.isPreferencesLoading = false;
          this.cdr.markForCheck();
          this.scheduleRevealRefresh();
        },
        error: () => {
          this.travelProfile = null;
          this.isPreferencesLoading = false;
          this.cdr.markForCheck();
          this.scheduleRevealRefresh();
        }
      });
  }

  private loadMarketplacePackages(): void {
    this.isMarketplaceLoading = true;
    this.marketplaceLoadError = '';

    this.marketplaceService.list({ page: 1, pageSize: 4 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.marketplacePackages = result.items;
          this.isMarketplaceLoading = false;
          this.cdr.markForCheck();
          this.scheduleRevealRefresh();
        },
        error: () => {
          this.marketplacePackages = [];
          this.isMarketplaceLoading = false;
          this.marketplaceLoadError =
            'Verified agency trips are temporarily unavailable.';
          this.cdr.markForCheck();
          this.scheduleRevealRefresh();
        }
      });
  }
}
