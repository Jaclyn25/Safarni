import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  HostListener,
  OnDestroy,
  OnInit,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
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
  TripService
} from '../../services/trip.service';
import { AuthService } from '../../services/auth.service';

export interface FeaturedDestination {
  id: string;
  country: string;
  title: string;
  description: string;
  price: number;
  image: string;
  tag?: string;
  tagType?: 'hot' | 'luxury' | 'popular' | 'budget';
  gridSpanClass?: string;
}

export interface TrendingDestination {
  id: string;
  city: string;
  country: string;
  duration: string;
  avgCost: number;
  trendReason: string;
  category: 'Beach' | 'Culture' | 'Budget' | 'Visa-friendly' | 'Weekend';
  image: string;
}

export interface AiRecommendation {
  id: string;
  title: string;
  subtitle: string;
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
export class ExploreComponent implements OnInit, OnDestroy {
  private static readonly TRIP_DRAFT_KEY =
    'safarni_trip_draft';

  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private tripService = inject(TripService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  isCreateTripOpen = false;
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
  selectedFilter: string = 'All';

  quickPrompts: string[] = [
    'Romantic weekend in Paris',
    'Beach holiday in Maldives',
    'Best street food in Bangkok',
    'Cultural exploration in Kyoto'
  ];

  filterCategories: string[] = ['All', 'Beach', 'Culture', 'Budget', 'Visa-friendly', 'Weekend'];

  featuredDestinations: FeaturedDestination[] = [
    {
      id: 'maldives',
      country: 'Maldives',
      title: 'Tropical Paradise',
      description: 'Crystal clear waters, overwater villas & unforgettable sunsets',
      price: 1850,
      image: 'assets/countries/maldives.png',
      tag: 'Hot',
      tagType: 'hot',
      gridSpanClass: 'h-72 md:h-80'
    },
    {
      id: 'thailand',
      country: 'Thailand',
      title: 'Land of Smiles',
      description: 'Ancient temples, vibrant street food & pristine islands',
      price: 980,
      image: 'assets/countries/thailand.png',
      gridSpanClass: 'h-72 md:h-80'
    },
    {
      id: 'turkey',
      country: 'Turkey',
      title: 'Where East Meets West',
      description: 'Majestic mosques, hot air balloons & the Bosphorus',
      price: 1120,
      image: 'assets/countries/turkey.png',
      gridSpanClass: 'h-72 md:h-80'
    },
    {
      id: 'dubai',
      country: 'Dubai, UAE',
      title: 'City of Gold',
      description: 'Futuristic skyline, desert safaris & world-class luxury',
      price: 2200,
      image: 'assets/countries/dubai.png',
      tag: 'Luxury',
      tagType: 'luxury',
      gridSpanClass: 'h-64 md:h-72'
    },
    {
      id: 'japan',
      country: 'Japan',
      title: 'Ancient & Modern',
      description: 'Cherry blossoms, neon lights & centuries of tradition',
      price: 1650,
      image: 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=800&q=80&auto=format',
      gridSpanClass: 'h-64 md:h-72'
    },
    {
      id: 'france',
      country: 'France',
      title: 'City of Love',
      description: 'Eiffel Tower, Louvre Museum & French countryside charm',
      price: 1340,
      image: 'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=800&q=80&auto=format',
      tag: 'Popular',
      tagType: 'popular',
      gridSpanClass: 'h-64 md:h-72'
    },
    {
      id: 'poland',
      country: 'Poland',
      title: "Europe's Hidden Gem",
      description: 'Medieval castles, vibrant culture & incredible value — Kraków, Warsaw & beyond',
      price: 720,
      image: 'https://images.unsplash.com/photo-1519197924294-4ba991a11128?w=1200&q=80&auto=format',
      tag: 'Budget Friendly',
      tagType: 'budget',
      gridSpanClass: 'h-64 md:h-72 md:col-span-3'
    }
  ];

  trendingDestinations: TrendingDestination[] = [
    {
      id: 'amalfi',
      city: 'Amalfi Coast',
      country: 'Italy',
      duration: '5 Days',
      avgCost: 1450,
      trendReason: 'Trending for romantic summer getaways',
      category: 'Weekend',
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAaWkyby5OElt9D0zwD8mQkEIF-vUIcDQjqZyKFAQCIMMDjMKgNLgbuwxareniZ1HSKiFV1Uq02IBw2mdPj8BZb0Zwf0B2qaTyttnl398vIR_L5Mync_YQ1MUaG7M6MyncUDI54Eph5tTmO66vZO7545KHCBnikbC0QNsxE46s8IvEBG9B8god24vC7RxLne2W4eD4TRNLrgIPlETJzSFXVd1HFg8WenuQq8qkNBBTaqUTBWh8vef5svQw'
    },
    {
      id: 'tokyo',
      city: 'Tokyo',
      country: 'Japan',
      duration: '7 Days',
      avgCost: 1650,
      trendReason: 'Top choice for urban culture & culinary tours',
      category: 'Culture',
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuDECYyjvqzEQlav3dRYNAAfx1pGt8BcoWc5zwwIw-Qns-bBtlc08fM577hRJCmfIrLtUZMMz84_QpPvptGbFxoq9cTybqSYk8eAPyJPlFQvfqvRNAf66uRxgv38ISyNMjkVpUTQhHR0IQuZQRCfQb-1gM62iQuZO6KDwpcDEvwCtOrbfdbs6P3rua6GBSd7nZjCMgX7dR8hOeDJ14E6FIaF4jro_hFGHDJUsVzHGGcftpaXdmJRuin_-g'
    },
    {
      id: 'iceland',
      city: 'Blue Lagoon',
      country: 'Iceland',
      duration: '4 Days',
      avgCost: 1200,
      trendReason: 'Popular visa-friendly stopover destination',
      category: 'Visa-friendly',
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuC8h2B3kLKVl5yCEMGPeuwdG-S3HWjeRkxEeVb386op0JODP_DtRJt7hkLCF8HEfVPft0fwBileEWcXQk1f5oMB-2UAvI62Eb2dTHoiP6fYYy4MZNT9vrCGdcAI3z5D5RqMOHM42rhajD9HAvPjKegvgl5M8xBQEIKrMACTg6bSP4NIR33KvbRzFMDydYv1cK7mvua-p0II0eqPKGq5ETwniaQqIDeAiRttbOqTs1j9iF4oLv9jg5qeNg'
    },
    {
      id: 'zermatt',
      city: 'Zermatt',
      country: 'Switzerland',
      duration: '6 Days',
      avgCost: 2100,
      trendReason: 'High demand for luxury mountain retreats',
      category: 'Beach', // Nature/Weekend
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuChIhItiwOVpkGXc4hdCP3Vhk341Z3LTdYmPdzis3Qip8ZNVh-L4QNGyLelEtA1Cwj0nfMqVjCA-sb4McdsuZwoV8EuLHaH0lZnwsgw2u2MPK01B2VyJ3TJpPnH7IBmZl_o_snlMPQ7B1BMvsnA4CYbTZv7XIZ92RV9Ukx3TSt5bqQroPmzWZJtDvW8HDMNatKRdQ57_lGXnKGv4E84rLEs0J_vFOZvGO_8UKJgniuKJiwoA7NqfmGrDQ'
    }
  ];

  aiRecommendations: AiRecommendation[] = [
    {
      id: 'kyoto-modernism',
      title: 'Kyoto Modernism',
      subtitle: 'Top Match for You',
      description: "Discover the intersection of ancient tradition and contemporary architecture in Japan's cultural heart.",
      durationDays: 6,
      styleTags: ['Culture', 'Architecture', 'Foodie'],
      estimatedBudget: 1850,
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAwzJeouqB-QGW8xaF3oCU6ggXGNk6FTBcbPHTm7x8kw9EWzM2IMkrq7gELWCzQwqdRWv0szO_l7Kcys-9vgtx3DpSIR2h3C-Hqrmo47dqz59m0nD0WApCUw5P86bsb2CR_VumsRgR-7IM3Bz3gRSKc0sHqlkm1aa3g2pwVdReAeRMCE18DuxzL-YeinmCKxBHd7igjPK7ndTUu7Um_nN81MNEVoeI77gIL0Y-eMG65I19gAv1_c_SNoA',
      highlights: ['Curated Museum Pass', 'Secret Temple Access']
    },
    {
      id: 'lisbon-textures',
      title: 'Lisbon Textures',
      subtitle: 'Coastal Architecture & Dining',
      description: 'Perfect for a 4-day deep dive into tile art and coastal dining.',
      durationDays: 4,
      styleTags: ['Art & Tile', 'Coastal Dining'],
      estimatedBudget: 1240,
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBqU7pwfRPO5mu8-6wDyuH7u4ijRV2qyHzDcGh7spwDUVlITEk4AyEEDj5UvkC09H1RHke9zLV6s8gWdnhmm5r5Y_X45-Vbc_bWE4sQh5CPFVjmxvvAP4DPVkb2g_wO-Az29GggTTe-kCHLi0jZF7I8fyuJqAFQqSCFd4pF8QB_HS9HRAkN77ls3oX1-bbryDljr4G-ftK3k2KxJcQ6orG6uCv-1V0fi-BLn56jy_vF6BwWRfop33OJtw',
      highlights: ['Historic Tram Route', 'Seafood Tasting']
    },
    {
      id: 'santorini-escape',
      title: 'Santorini Escape',
      subtitle: 'Luxury Caldera Villas',
      description: 'A curated selection of private villas with caldera views and sunset cruises.',
      durationDays: 5,
      styleTags: ['Relaxation', 'Luxury', 'Views'],
      estimatedBudget: 2800,
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAMMyb6UfeIlaXccBIYhVWU2pdy2QXcTFucYI-nJSmIamPXVMBDotTZ2csOa446mC8smElnzlmkJQyQ1cMw9XMdpeL3XB-hEeWBhBQf7_O1_MsKs6egD_rXLwSxXOWmUhTFOclZIc7LI4p-jJC3PXkhJVdEK8DnbqSPTIVI8ESaDWvAGv9GikJLQUC6NMISWN5lg2OsB-F2_vuSa4szJMl5fni7Sjwr57s8gZlDtpwmLwn51YHJuY1i0A',
      highlights: ['Private Villa Pool', 'Catamaran Sunset Cruise']
    }
  ];

  get filteredTrendingDestinations(): TrendingDestination[] {
    if (this.selectedFilter === 'All') {
      return this.trendingDestinations;
    }
    return this.trendingDestinations.filter(d => d.category === this.selectedFilter);
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
          void this.openCreateTrip();
        }
      });
  }

  ngOnDestroy(): void {
    this.setPageScrollLocked(false);
  }

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
        queryParams: { returnUrl: '/?createTrip=1' }
      });
      return;
    }

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
    if (this.isCreatingTrip) {
      return;
    }

    this.isCreateTripOpen = false;
    this.setPageScrollLocked(false);

    if (
      this.route.snapshot.queryParamMap.get(
        'createTrip'
      ) === '1'
    ) {
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {
          createTrip: null
        },
        queryParamsHandling: 'merge',
        replaceUrl: true
      });
    }
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

  setFilter(category: string): void {
    this.selectedFilter = category;
  }

  navigateToDestination(destId: string): void {
    const slug = destId.toLowerCase().split(',')[0].trim().replace(/[^a-z0-9]/g, '');
    this.router.navigate(['/destination', slug]);
  }

  buildRecommendation(title: string): void {
    const destination =
      title.split(' ')[0] || '';
    void this.openCreateTrip(title, destination);
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
    document.body.style.overflow = locked
      ? 'hidden'
      : '';
  }

  private closeCreateTripAfterSuccess(): void {
    this.isCreateTripOpen = false;
    this.setPageScrollLocked(false);
  }
}
