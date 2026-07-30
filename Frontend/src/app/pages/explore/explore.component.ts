import {
  CommonModule
} from '@angular/common';
import {
  Component,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  Router,
  RouterLink,
  RouterLinkActive
} from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  CreateTripRequest,
  TravelCountry,
  TripCitySelection,
  TripService
} from '../../services/trip.service';

@Component({
  selector: 'app-explore',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './explore.component.html'
})
export class ExploreComponent implements OnInit {
  countries: TravelCountry[] = [];
  availableCities: string[] = [];
  selectedCities: TripCitySelection[] = [];

  tripName = '';
  originCountry = '';
  destinationCountry = '';
  startDate = this.formatDate(new Date());
  endDate = this.formatDate(
    new Date(Date.now() + 2 * 86400000)
  );
  citySearch = '';
  cityDropdownOpen = false;
  cityLimitMessage = '';
  errorMessage = '';
  isLoadingCountries = false;
  isLoadingCities = false;
  isCreating = false;

  constructor(
    private readonly tripService: TripService,
    private readonly router: Router
  ) {}

  async ngOnInit(): Promise<void> {
    this.isLoadingCountries = true;

    try {
      const response = await firstValueFrom(
        this.tripService.getCountries()
      );
      this.countries = response.data ?? [];
    } catch {
      this.errorMessage =
        'Countries could not be loaded. Please refresh and try again.';
    } finally {
      this.isLoadingCountries = false;
    }
  }

  get tripDayCount(): number {
    const start = new Date(
      `${this.startDate}T00:00:00`
    );
    const end = new Date(
      `${this.endDate}T00:00:00`
    );

    if (
      Number.isNaN(start.getTime()) ||
      Number.isNaN(end.getTime()) ||
      end < start
    ) {
      return 0;
    }

    return Math.floor(
      (end.getTime() - start.getTime()) /
      86400000
    ) + 1;
  }

  get filteredCities(): string[] {
    const search = this.normalizeSearchText(
      this.citySearch
    );

    return this.availableCities
      .filter(city =>
        !this.selectedCities.some(selected =>
          this.normalizeSearchText(
            selected.name
          ) === this.normalizeSearchText(city)
        )
      )
      .filter(city =>
        search.length === 0 ||
        this.normalizeSearchText(city)
          .includes(search)
      )
      .slice(0, 100);
  }

  get allocatedDays(): number {
    return this.selectedCities.reduce(
      (total, city) => total + city.days,
      0
    );
  }

  get remainingDays(): number {
    return this.tripDayCount -
      this.allocatedDays;
  }

  get canCreateTrip(): boolean {
    return (
      !this.isCreating &&
      this.tripName.trim().length > 0 &&
      this.originCountry.length > 0 &&
      this.destinationCountry.length > 0 &&
      this.tripDayCount > 0 &&
      this.selectedCities.length > 0 &&
      this.selectedCities.length <=
        this.tripDayCount &&
      this.remainingDays === 0
    );
  }

  async onDestinationCountryChange():
    Promise<void> {
    this.selectedCities = [];
    this.availableCities = [];
    this.citySearch = '';
    this.cityLimitMessage = '';

    if (!this.destinationCountry) {
      return;
    }

    this.isLoadingCities = true;

    try {
      const response = await firstValueFrom(
        this.tripService.getCities(
          this.destinationCountry
        )
      );
      this.availableCities = response.data ?? [];
      this.cityDropdownOpen = false;
    } catch {
      this.errorMessage =
        'Cities could not be loaded for this country.';
    } finally {
      this.isLoadingCities = false;
    }
  }

  onDatesChanged(): void {
    this.cityLimitMessage = '';

    if (
      this.tripDayCount > 0 &&
      this.selectedCities.length >
        this.tripDayCount
    ) {
      this.selectedCities =
        this.selectedCities.slice(
          0,
          this.tripDayCount
        );
      this.cityLimitMessage =
        'Some cities were removed because every selected city needs at least one trip day.';
    }

    this.rebalanceCityDays();
  }

  selectCity(city: string): void {
    if (
      this.tripDayCount <= 0 ||
      this.selectedCities.length >=
        this.tripDayCount
    ) {
      this.cityLimitMessage =
        'Choose more days in your trip to be able to choose more cities.';
      return;
    }

    this.selectedCities = [
      ...this.selectedCities,
      {
        name: city,
        days: 1
      }
    ];
    this.citySearch = '';
    this.cityLimitMessage = '';
    this.cityDropdownOpen = false;
    this.rebalanceCityDays();
  }

  removeCity(cityName: string): void {
    this.selectedCities =
      this.selectedCities.filter(city =>
        city.name !== cityName
      );
    this.cityLimitMessage = '';
    this.rebalanceCityDays();
  }

  setCityDays(
    city: TripCitySelection,
    value: number | string
  ): void {
    const parsed = Number(value);
    city.days = Number.isFinite(parsed)
      ? Math.max(
          1,
          Math.min(this.tripDayCount, parsed)
        )
      : 1;
  }

  async createTrip(): Promise<void> {
    if (!this.canCreateTrip) {
      this.errorMessage =
        'Complete the trip details and allocate every trip day before continuing.';
      return;
    }

    this.isCreating = true;
    this.errorMessage = '';

    const request: CreateTripRequest = {
      name: this.tripName.trim(),
      destination:
        this.destinationCountry,
      originCountry: this.originCountry,
      startDate: this.startDate,
      endDate: this.endDate,
      cities: this.selectedCities.map(city => ({
        name: city.name,
        days: city.days
      }))
    };

    try {
      const response = await firstValueFrom(
        this.tripService.createTrip(request)
      );

      if (!response.success || !response.data) {
        this.errorMessage =
          response.errors?.[0]?.message ??
          'The trip could not be created.';
        return;
      }

      await this.router.navigate([
        '/planner',
        response.data.id
      ]);
    } catch {
      this.errorMessage =
        localStorage.getItem('access_token')
          ? 'The trip could not be created. Please check the details and try again.'
          : 'Sign in before creating a trip.';
    } finally {
      this.isCreating = false;
    }
  }

  private rebalanceCityDays(): void {
    if (
      this.selectedCities.length === 0 ||
      this.tripDayCount <= 0
    ) {
      return;
    }

    const baseDays = Math.floor(
      this.tripDayCount /
      this.selectedCities.length
    );
    let extraDays =
      this.tripDayCount %
      this.selectedCities.length;

    this.selectedCities.forEach(city => {
      city.days = baseDays +
        (extraDays > 0 ? 1 : 0);
      extraDays--;
    });
  }

  private formatDate(value: Date): string {
    const year = value.getFullYear();
    const month = String(
      value.getMonth() + 1
    ).padStart(2, '0');
    const day = String(
      value.getDate()
    ).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private normalizeSearchText(
    value: string
  ): string {
    return value
      .trim()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLocaleLowerCase();
  }
}
