import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, from, of } from 'rxjs';
import { concatMap } from 'rxjs/operators';
import {
  PackagePlaceSearchResult,
  TravelPackage,
  TravelPackageCity,
  TravelPackageCityRequest,
  TravelPackageItem,
  TravelPackageItemRequest,
  TravelPackageItemType,
  TravelPackageRequest
} from '../../../models/marketplace.models';
import { AgencyService } from '../../../services/agency.service';
import { getApiErrorMessage } from '../../../services/auth.service';
import { MarketplaceService } from '../../../services/marketplace.service';
import { TravelCountry, TripService } from '../../../services/trip.service';
import { AgencyEmptyStateComponent } from '../shared/agency-empty-state.component';
import { AgencyKpiCardComponent } from '../shared/agency-kpi-card.component';
import { AgencyLoadingSkeletonComponent } from '../shared/agency-loading-skeleton.component';
import { AgencyPageHeaderComponent } from '../shared/agency-page-header.component';
import { AgencyStatusBadgeComponent } from '../shared/agency-status-badge.component';
import { DEFAULT_PACKAGE_COVER_URL, packageCoverUrl } from '../../../shared/constants/marketplace.constants';

@Component({
  selector: 'app-agency-packages',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AgencyPageHeaderComponent,
    AgencyStatusBadgeComponent,
    AgencyEmptyStateComponent,
    AgencyLoadingSkeletonComponent
  ],
  templateUrl: './agency-packages.component.html'
})
export class AgencyPackagesComponent implements OnInit {
  readonly defaultPackageCover = DEFAULT_PACKAGE_COVER_URL;
  readonly packages = signal<TravelPackage[]>([]);
  readonly isLoading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly formError = signal('');
  readonly notice = signal('');
  readonly editingId = signal<string | null>(null);
  readonly actionId = signal<string | null>(null);
  readonly canPublish = signal(false);
  readonly daySaving = signal(false);
  readonly imageSaving = signal(false);
  readonly dayError = signal('');
  readonly editingDayId = signal<string | null>(null);
  readonly isEditorOpen = signal(false);
  readonly selectedCoverImage = signal<File | null>(null);
  readonly selectedCoverImagePreview = signal<string | null>(null);
  readonly countries = signal<TravelCountry[]>([]);
  readonly citySuggestions = signal<string[]>([]);
  readonly citySaving = signal(false);
  readonly editingCityId = signal<string | null>(null);
  readonly itemSaving = signal(false);
  readonly placeSearching = signal(false);
  readonly placeResults = signal<PackagePlaceSearchResult[]>([]);
  readonly itemPanelOpen = signal(false);
  readonly selectedItemDayId = signal<string | null>(null);
  readonly editingItemId = signal<string | null>(null);
  readonly selectedItemType = signal<TravelPackageItemType>('Place');
  readonly includeLocation = signal(false);
  readonly itemError = signal('');
  readonly uploadingTarget = signal<string | null>(null);

  readonly itemTypes: Array<{ type: TravelPackageItemType; icon: string; label: string; hint: string }> = [
    { type: 'Place', icon: 'location_on', label: 'Place', hint: 'Landmark or point of interest' },
    { type: 'Activity', icon: 'local_activity', label: 'Activity', hint: 'Tour, ticket or experience' },
    { type: 'Restaurant', icon: 'restaurant', label: 'Restaurant', hint: 'Meal or food experience' },
    { type: 'Accommodation', icon: 'hotel', label: 'Stay', hint: 'Hotel, resort or apartment' },
    { type: 'Transport', icon: 'directions_bus', label: 'Transport', hint: 'Transfer or journey' },
    { type: 'WalkingTour', icon: 'directions_walk', label: 'Walking plan', hint: 'A flexible walking program' },
    { type: 'Other', icon: 'add_notes', label: 'Other', hint: 'Anything unique to your program' }
  ];

  readonly searchQuery = signal('');
  readonly selectedStatusFilter = signal<'all' | 'published' | 'draft' | 'inactive'>('all');

  readonly form: FormGroup;
  readonly dayForm: FormGroup;
  readonly cityForm: FormGroup;
  readonly itemForm: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly marketplace: MarketplaceService,
    private readonly agencies: AgencyService,
    private readonly trips: TripService,
    private readonly router: Router
  ) {
    this.form = this.fb.nonNullable.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.required, Validators.maxLength(4000)]],
      destination: ['', [Validators.required, Validators.maxLength(200)]],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      durationDays: [3, [Validators.required, Validators.min(1)]],
      price: [0, [Validators.required, Validators.min(0)]],
      currencyCode: ['USD', [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)]],
      availableSeats: [10, [Validators.required, Validators.min(1)]],
      coverImageUrl: [''],
      discountPercentage: [null as number | null, [Validators.min(0), Validators.max(100)]],
      offerLabel: [''],
      inclusions: [''],
      exclusions: [''],
      importantNotes: [''],
      cancellationPolicy: ['']
    });
    this.dayForm = this.fb.nonNullable.group({
      dayNumber: [1, [Validators.required, Validators.min(1)]],
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.required, Validators.maxLength(2000)]]
    });
    this.cityForm = this.fb.nonNullable.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      nights: [1, [Validators.required, Validators.min(1), Validators.max(365)]],
      description: ['']
    });
    this.itemForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]], description: [''],
      locationName: [''], address: [''], latitude: [null as number | null], longitude: [null as number | null],
      mapUrl: [''], provider: [''], providerPlaceId: [''], providerPhotoUrl: [''], searchQuery: [''],
      websiteUrl: [''],
      cuisine: [''], mealType: [''], reservationRequired: [false], accommodationType: [''],
      starRating: [null as number | null], roomType: [''], boardBasis: [''], transportMode: [''],
      fromLocation: [''], toLocation: [''], meetingPoint: [''], difficultyLevel: [''], additionalDetails: ['']
    });
  }

  ngOnInit(): void {
    this.load();
    this.trips.getCountries().subscribe({ next: (response) => this.countries.set(response.data ?? []), error: () => undefined });
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set('');
    forkJoin({
      packages: this.marketplace.listAgencyPackages(),
      verification: this.agencies.getVerification()
    }).subscribe({
      next: ({ packages, verification }) => {
        this.packages.set(packages);
        this.canPublish.set(verification.canPublishPackages);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(getApiErrorMessage(error, 'We could not load your packages.'));
        this.isLoading.set(false);
      }
    });
  }

  readonly filteredPackages = computed(() => {
    let list = this.packages();
    const query = this.searchQuery().toLowerCase().trim();
    const filter = this.selectedStatusFilter();

    if (query) {
      list = list.filter(
        (p) => p.title.toLowerCase().includes(query) || p.destination.toLowerCase().includes(query)
      );
    }

    if (filter === 'published') {
      list = list.filter((p) => p.isPublished && p.isActive);
    } else if (filter === 'draft') {
      list = list.filter((p) => !p.isPublished && p.isActive);
    } else if (filter === 'inactive') {
      list = list.filter((p) => !p.isActive);
    }

    return list;
  });

  readonly countAll = computed(() => this.packages().length);
  readonly countPublished = computed(() => this.packages().filter((p) => p.isPublished && p.isActive).length);
  readonly countDrafts = computed(() => this.packages().filter((p) => !p.isPublished && p.isActive).length);
  readonly countInactive = computed(() => this.packages().filter((p) => !p.isActive).length);

  startCreate(): void {
    this.editingId.set(null);
    this.editingDayId.set(null);
    this.clearSelectedCoverImage();
    this.form.reset({
      title: '',
      description: '',
      destination: '',
      startDate: '',
      endDate: '',
      durationDays: 3,
      price: 0,
      currencyCode: 'USD',
      availableSeats: 10,
      coverImageUrl: '',
      discountPercentage: null,
      offerLabel: '',
      inclusions: '', exclusions: '', importantNotes: '', cancellationPolicy: ''
    });
    this.resetDayForm();
    this.formError.set('');
    this.dayError.set('');
    this.notice.set('');
    this.isEditorOpen.set(true);
  }

  edit(item: TravelPackage): void {
    this.editingId.set(item.id);
    this.form.patchValue({
      ...item,
      startDate: item.startDate.slice(0, 10),
      endDate: item.endDate.slice(0, 10),
      coverImageUrl: item.coverImageUrl ?? '',
      offerLabel: item.offerLabel ?? '',
      inclusions: item.inclusions ?? '', exclusions: item.exclusions ?? '',
      importantNotes: item.importantNotes ?? '', cancellationPolicy: item.cancellationPolicy ?? ''
    });
    this.resetDayForm();
    this.clearSelectedCoverImage();
    this.formError.set('');
    this.dayError.set('');
    this.notice.set('');
    this.isEditorOpen.set(true);
    this.loadCitySuggestions(item.destination);
  }

  closeEditor(): void {
    this.isEditorOpen.set(false);
  }

  editingPackage(): TravelPackage | null {
    const id = this.editingId();
    return id ? this.packages().find((item) => item.id === id) ?? null : null;
  }

  editDay(day: TravelPackage['days'][number]): void {
    this.editingDayId.set(day.id);
    this.dayForm.patchValue({
      dayNumber: day.dayNumber,
      title: day.title,
      description: day.description
    });
    this.dayError.set('');
  }

  resetDayForm(): void {
    this.editingDayId.set(null);
    const nextDayNumber = this.nextAvailableDayNumber();
    this.dayForm.reset({ dayNumber: nextDayNumber, title: '', description: '' });
  }

  cityForDay(dayNumber: number): TravelPackageCity | null {
    const cities = [...(this.editingPackage()?.cities ?? [])].sort((a, b) => a.orderIndex - b.orderIndex);
    if (!cities.length) return null;
    let allocatedDays = 0;
    for (let index = 0; index < cities.length - 1; index++) {
      allocatedDays += cities[index].nights;
      if (dayNumber <= allocatedDays) return cities[index];
    }
    return cities[cities.length - 1];
  }

  activeItemCity(): string {
    return this.editingPackage()?.days.find(day => day.id === this.selectedItemDayId())?.cityName ?? '';
  }

  programComplete(): boolean {
    const packageItem = this.editingPackage();
    return Boolean(packageItem && packageItem.days.length >= packageItem.durationDays);
  }

  routeNights(): number {
    return this.editingPackage()?.cities.reduce((total, city) => total + city.nights, 0) ?? 0;
  }

  routeNightsRemaining(): number {
    const duration = this.editingPackage()?.durationDays ?? 0;
    return Math.max(0, Math.max(1, duration - 1) - this.routeNights());
  }

  private nextAvailableDayNumber(): number {
    const packageItem = this.editingPackage();
    const used = new Set(packageItem?.days.map((d) => d.dayNumber) ?? []);
    if (used.size === 0) return 1;
    const maxDay = Math.max(...used);
    for (let i = 1; i <= maxDay + 1; i++) {
      if (!used.has(i)) return i;
    }
    return maxDay + 1;
  }

  saveDay(): void {
    if (this.daySaving()) return;
    const packageItem = this.editingPackage();
    if (this.dayForm.invalid) {
      this.dayForm.markAllAsTouched();
      this.dayError.set('Complete the day title and description before saving.');
      return;
    }
    const value = this.dayForm.getRawValue();
    const dayNumber = Number(value.dayNumber);
    const isEditing = Boolean(this.editingDayId());
    const duplicate = packageItem?.days.find(
      (d) => d.dayNumber === dayNumber && d.id !== this.editingDayId()
    );
    if (duplicate) {
      const suggested = this.nextAvailableDayNumber();
      this.dayError.set(`Day ${dayNumber} already exists. Next available day is ${suggested}.`);
      this.dayForm.patchValue({ dayNumber: suggested });
      this.daySaving.set(false);
      return;
    }
    if (!packageItem || !packageItem.cities.length) {
      this.dayError.set('Save the package and add its route cities before creating itinerary days.');
      return;
    }
    this.daySaving.set(true);
    this.dayError.set('');
    const request = {
      dayNumber,
      title: value.title,
      description: value.description
    };
    const operation = isEditing
      ? this.marketplace.updateDay(packageItem.id, this.editingDayId()!, request)
      : this.marketplace.addDay(packageItem.id, request);

    operation.subscribe({
      next: (updated) => {
        this.daySaving.set(false);
        this.applyPackage(updated);
        this.resetDayForm();
        this.notice.set('Itinerary day saved.');
      },
      error: (error: unknown) => {
        this.daySaving.set(false);
        const isDuplicateDayConflict =
          error instanceof HttpErrorResponse &&
          error.status === 409 &&
          error.error?.errors?.some(
            (item: { code?: string }) => item.code === 'TravelPackage.DuplicateDayNumber'
          );
        if (isDuplicateDayConflict) {
          this.marketplace.getAgencyPackage(packageItem.id).subscribe({
            next: (freshPackage) => {
              this.applyPackage(freshPackage);
              const suggested = this.nextAvailableDayNumber();
              this.dayError.set(`Day ${dayNumber} already exists. Next available day is ${suggested}.`);
              this.dayForm.patchValue({ dayNumber: suggested });
            },
            error: () => {
              this.dayError.set(`Day ${dayNumber} already exists. Refresh the package and try again.`);
            }
          });
        } else {
          this.dayError.set(getApiErrorMessage(error, 'We could not save this itinerary day.'));
        }
      }
    });
  }

  removeDay(dayId: string): void {
    const packageItem = this.editingPackage();
    if (!packageItem) return;
    this.daySaving.set(true);
    this.dayError.set('');
    this.marketplace.deleteDay(packageItem.id, dayId).subscribe({
      next: (updated) => {
        this.daySaving.set(false);
        this.applyPackage(updated);
        this.resetDayForm();
        this.notice.set('Itinerary day removed.');
      },
      error: (error: unknown) => {
        this.daySaving.set(false);
        this.dayError.set(getApiErrorMessage(error, 'We could not remove this itinerary day.'));
      }
    });
  }

  selectCoverImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const allowedTypes = new Set(['image/jpeg', 'image/png', 'image/webp']);
    if (!allowedTypes.has(file.type.toLowerCase())) {
      this.formError.set('Choose a JPG, PNG, or WEBP image.');
      input.value = '';
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.formError.set('Choose an image smaller than 5 MB.');
      input.value = '';
      return;
    }
    this.formError.set('');
    this.clearSelectedCoverImage();
    this.selectedCoverImage.set(file);
    this.selectedCoverImagePreview.set(URL.createObjectURL(file));
  }

  removeSelectedCoverImage(): void {
    this.clearSelectedCoverImage();
    this.formError.set('');
  }

  destinationChanged(country: string): void {
    this.form.patchValue({ destination: country });
    this.loadCitySuggestions(country);
  }

  private loadCitySuggestions(country: string): void {
    if (!country.trim()) { this.citySuggestions.set([]); return; }
    this.trips.getCities(country).subscribe({
      next: (response) => this.citySuggestions.set(response.data ?? []),
      error: () => this.citySuggestions.set([])
    });
  }

  resetCityForm(): void {
    this.editingCityId.set(null);
    this.cityForm.reset({
      name: '', nights: 1, description: ''
    });
  }

  editCity(city: TravelPackageCity): void {
    this.editingCityId.set(city.id);
    this.cityForm.patchValue({ ...city, description: city.description ?? '' });
  }

  saveCity(): void {
    const item = this.editingPackage();
    if (!item || this.cityForm.invalid || this.citySaving()) {
      this.cityForm.markAllAsTouched();
      return;
    }
    const value = this.cityForm.getRawValue();
    const request: TravelPackageCityRequest = {
      name: value.name.trim(), nights: Number(value.nights), description: value.description.trim() || null
    };
    this.citySaving.set(true);
    const cityId = this.editingCityId();
    const operation = cityId
      ? this.marketplace.updateCity(item.id, cityId, request)
      : this.marketplace.addCity(item.id, request);
    operation.subscribe({
      next: (updated) => { this.citySaving.set(false); this.applyPackage(updated); this.resetCityForm(); this.notice.set('Route city saved.'); },
      error: (error: unknown) => { this.citySaving.set(false); this.formError.set(getApiErrorMessage(error, 'We could not save this city.')); }
    });
  }

  removeCity(cityId: string): void {
    const item = this.editingPackage();
    if (!item || this.citySaving()) return;
    this.citySaving.set(true);
    this.marketplace.deleteCity(item.id, cityId).subscribe({
      next: (updated) => { this.citySaving.set(false); this.applyPackage(updated); this.resetCityForm(); },
      error: (error: unknown) => { this.citySaving.set(false); this.formError.set(getApiErrorMessage(error, 'We could not remove this city.')); }
    });
  }

  startItem(dayId: string, type: TravelPackageItemType): void {
    this.selectedItemDayId.set(dayId);
    this.editingItemId.set(null);
    this.selectedItemType.set(type);
    this.includeLocation.set(['Place', 'Activity', 'Restaurant', 'Accommodation'].includes(type));
    this.placeResults.set([]);
    this.itemError.set('');
    this.itemForm.reset({ reservationRequired: false });
    this.itemPanelOpen.set(true);
    if (this.isSearchableType(type)) this.searchPlaces();
  }

  editItem(dayId: string, item: TravelPackageItem): void {
    this.selectedItemDayId.set(dayId);
    this.editingItemId.set(item.id);
    this.selectedItemType.set(item.type);
    this.includeLocation.set(Boolean(item.locationName || item.address || item.latitude != null));
    this.placeResults.set([]);
    this.itemError.set('');
    this.itemForm.reset({ reservationRequired: false });
    this.itemForm.patchValue({ ...item, searchQuery: '' });
    this.itemPanelOpen.set(true);
  }

  chooseItemType(type: TravelPackageItemType): void {
    this.selectedItemType.set(type);
    if (type === 'Transport' || type === 'WalkingTour' || type === 'Other') this.includeLocation.set(false);
    this.placeResults.set([]);
    this.itemForm.patchValue({ provider: '', providerPlaceId: '', providerPhotoUrl: '', websiteUrl: '' });
    if (this.isSearchableType(type)) this.searchPlaces();
  }

  isSearchableType(type: TravelPackageItemType = this.selectedItemType()): boolean {
    return ['Place', 'Activity', 'Restaurant', 'Accommodation'].includes(type);
  }

  searchPlaces(): void {
    const packageItem = this.editingPackage();
    const day = packageItem?.days.find(value => value.id === this.selectedItemDayId());
    const query = String(this.itemForm.value.searchQuery ?? '').trim();
    if (!packageItem || !day?.cityName || !this.isSearchableType()) {
      this.itemError.set('This day needs a route city before place suggestions can be loaded.');
      return;
    }
    this.placeSearching.set(true);
    this.itemError.set('');
    this.marketplace.searchPlaces(packageItem.id, day.id, { query, type: this.selectedItemType() }).subscribe({
      next: (results) => { this.placeSearching.set(false); this.placeResults.set(results); },
      error: (error: unknown) => { this.placeSearching.set(false); this.itemError.set(getApiErrorMessage(error, 'Search is unavailable. You can still enter this item manually.')); }
    });
  }

  usePlace(result: PackagePlaceSearchResult): void {
    this.includeLocation.set(true);
    this.itemForm.patchValue({
      title: result.name, description: result.description ?? '', locationName: result.name,
      address: result.address, latitude: result.latitude, longitude: result.longitude,
      mapUrl: result.mapUrl ?? '', provider: result.provider, providerPlaceId: result.providerPlaceId,
      providerPhotoUrl: result.photoUrl ?? '', websiteUrl: result.websiteUrl ?? ''
    });
    this.placeResults.set([]);
  }

  saveItem(): void {
    const packageItem = this.editingPackage();
    const dayId = this.selectedItemDayId();
    if (!packageItem || !dayId || this.itemForm.invalid || this.itemSaving()) {
      this.itemForm.markAllAsTouched();
      this.itemError.set('Add a clear title before saving this program item.');
      return;
    }
    const value = this.itemForm.getRawValue();
    const hasProvider = Boolean(value.providerPlaceId);
    const location = this.includeLocation();
    const clean = (input: unknown): string | null => typeof input === 'string' && input.trim() ? input.trim() : null;
    const request: TravelPackageItemRequest = {
      type: this.selectedItemType(), source: hasProvider ? 'Provider' : 'Manual',
      orderIndex: packageItem.days.find(day => day.id === dayId)?.items.find(item => item.id === this.editingItemId())?.orderIndex
        ?? packageItem.days.find(day => day.id === dayId)?.items.length ?? 0,
      title: String(value.title).trim(), description: clean(value.description),
      locationName: location ? clean(value.locationName) : null,
      address: location ? clean(value.address) : null,
      latitude: location && value.latitude != null ? Number(value.latitude) : null,
      longitude: location && value.longitude != null ? Number(value.longitude) : null,
      mapUrl: location ? clean(value.mapUrl) : null,
      provider: hasProvider ? clean(value.provider) : null, providerPlaceId: hasProvider ? clean(value.providerPlaceId) : null,
      providerPhotoUrl: hasProvider ? clean(value.providerPhotoUrl) : null,
      websiteUrl: hasProvider ? clean(value.websiteUrl) : null,
      cuisine: clean(value.cuisine), mealType: clean(value.mealType), reservationRequired: Boolean(value.reservationRequired),
      accommodationType: clean(value.accommodationType), starRating: value.starRating == null ? null : Number(value.starRating),
      roomType: clean(value.roomType), boardBasis: clean(value.boardBasis), transportMode: clean(value.transportMode),
      fromLocation: clean(value.fromLocation), toLocation: clean(value.toLocation), meetingPoint: clean(value.meetingPoint),
      difficultyLevel: clean(value.difficultyLevel), additionalDetails: clean(value.additionalDetails)
    };
    this.itemSaving.set(true);
    const itemId = this.editingItemId();
    const operation = itemId
      ? this.marketplace.updateItem(packageItem.id, dayId, itemId, request)
      : this.marketplace.addItem(packageItem.id, dayId, request);
    operation.subscribe({
      next: (updated) => { this.itemSaving.set(false); this.applyPackage(updated); this.itemPanelOpen.set(false); this.notice.set('Program item saved.'); },
      error: (error: unknown) => { this.itemSaving.set(false); this.itemError.set(getApiErrorMessage(error, 'We could not save this program item.')); }
    });
  }

  removeItem(dayId: string, itemId: string): void {
    const packageItem = this.editingPackage();
    if (!packageItem) return;
    this.marketplace.deleteItem(packageItem.id, dayId, itemId).subscribe({
      next: (updated) => this.applyPackage(updated),
      error: (error: unknown) => this.formError.set(getApiErrorMessage(error, 'We could not remove this item.'))
    });
  }

  uploadImages(event: Event, target: { dayId?: string; itemId?: string; cover?: boolean } = {}): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    const packageItem = this.editingPackage();
    if (!packageItem || !files.length) return;
    const allowedTypes = new Set(['image/jpeg', 'image/png', 'image/webp']);
    if (files.some(file => !allowedTypes.has(file.type.toLowerCase()) || file.size > 5 * 1024 * 1024)) {
      this.formError.set('Every image must be JPG, PNG, or WEBP and smaller than 5 MB.');
      input.value = '';
      return;
    }
    this.formError.set('');
    const targetKey = target.itemId ?? target.dayId ?? 'package';
    this.uploadingTarget.set(targetKey);
    let index = 0;
    from(files).pipe(concatMap(file => this.marketplace.uploadGalleryImage(packageItem.id, file, {
      dayId: target.dayId, itemId: target.itemId, isCover: Boolean(target.cover && index++ === 0),
      altText: `${packageItem.title} travel program photo`
    }))).subscribe({
      next: (updated) => this.applyPackage(updated),
      complete: () => {
        this.uploadingTarget.set(null);
        input.value = '';
        this.notice.set(target.cover
          ? 'Package cover replaced successfully.'
          : `${files.length} image${files.length === 1 ? '' : 's'} added.`);
      },
      error: (error: unknown) => {
        this.uploadingTarget.set(null);
        input.value = '';
        this.formError.set(getApiErrorMessage(error, target.cover
          ? 'The package cover could not be replaced. Please choose the image again and retry.'
          : 'One or more images could not be uploaded.'));
      }
    });
  }

  deleteImage(imageId: string): void {
    const packageItem = this.editingPackage();
    if (!packageItem) return;
    this.marketplace.deleteGalleryImage(packageItem.id, imageId).subscribe({
      next: (updated) => this.applyPackage(updated),
      error: (error: unknown) => this.formError.set(getApiErrorMessage(error, 'We could not remove this image.'))
    });
  }

  itemIcon(type: TravelPackageItemType): string {
    return this.itemTypes.find(item => item.type === type)?.icon ?? 'place';
  }

  packageCover(packageItem: TravelPackage): string { return packageCoverUrl(packageItem); }

  private clearSelectedCoverImage(): void {
    const preview = this.selectedCoverImagePreview();
    if (preview) URL.revokeObjectURL(preview);
    this.selectedCoverImage.set(null);
    this.selectedCoverImagePreview.set(null);
  }

  save(): void {
    this.formError.set('');
    this.notice.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Complete the required package details before saving.');
      return;
    }
    const value = this.form.getRawValue();
    const request: TravelPackageRequest = {
      ...value,
      currencyCode: value.currencyCode.toUpperCase(),
      coverImageUrl: value.coverImageUrl.trim() || null,
      offerLabel: value.offerLabel.trim() || null,
      inclusions: value.inclusions.trim() || null,
      exclusions: value.exclusions.trim() || null,
      importantNotes: value.importantNotes.trim() || null,
      cancellationPolicy: value.cancellationPolicy.trim() || null
    };
    this.saving.set(true);
    const operation = this.editingId()
      ? this.marketplace.updateAgencyPackage(this.editingId()!, request)
      : this.marketplace.createAgencyPackage(request);

    operation.subscribe({
      next: (item) => {
        const wasEditing = Boolean(this.editingId());
        // Retain the new draft identity before uploading its cover so an upload retry can
        // never create a duplicate package.
        if (!wasEditing) {
          this.editingId.set(item.id);
          this.packages.update((items) => items.some((current) => current.id === item.id)
            ? items.map((current) => current.id === item.id ? item : current)
            : [item, ...items]);
          this.resetCityForm();
        }
        const coverImage = this.selectedCoverImage();
        const completion = coverImage
          ? this.marketplace.uploadGalleryImage(item.id, coverImage, {
              isCover: true,
              altText: `${item.title} package cover`
            })
          : of(item);
        completion.subscribe({
          next: (savedItem) => {
            this.saving.set(false);
            this.packages.update((items) => items.some((current) => current.id === savedItem.id)
              ? items.map((current) => current.id === savedItem.id ? savedItem : current)
              : [savedItem, ...items]);
            this.clearSelectedCoverImage();
            this.notice.set(wasEditing ? 'Package updated.' : 'Draft saved. Now build the route and day-by-day program.');
            if (wasEditing) {
              this.closeEditor();
            }
          },
          error: (error: unknown) => {
            this.saving.set(false);
            this.formError.set(getApiErrorMessage(error,
              'The draft was created, but its cover image was not saved. You can safely retry without creating another package.'));
            this.marketplace.getAgencyPackage(item.id).subscribe({
              next: (recovered) => {
                this.packages.update((items) => items.some((current) => current.id === recovered.id)
                  ? items.map((current) => current.id === recovered.id ? recovered : current)
                  : [recovered, ...items]);
              },
              error: () => undefined
            });
          }
        });
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.formError.set(getApiErrorMessage(error, 'We could not save this package.'));
        if (error instanceof HttpErrorResponse &&
          error.error?.errors?.some((item: { code?: string }) => item.code === 'Billing.AgencyPackageCreationLimitReached')) {
          window.setTimeout(() => void this.router.navigate(['/agency/pricing'], {
            queryParams: { reason: 'agency-package-creation-limit' }
          }), 700);
        }
      }
    });
  }

  publish(item: TravelPackage): void {
    const issues = this.publishIssues(item);
    if (issues.length) {
      this.error.set(`Complete this package before publishing: ${issues.join(' ')}`);
      return;
    }
    this.mutate(item, 'publish');
  }

  publishIssues(item: TravelPackage): string[] {
    const issues: string[] = [];
    const totalNights = item.cities.reduce((total, city) => total + city.nights, 0);
    const minimumNights = Math.max(1, item.durationDays - 1);
    if (!item.cities.length) {
      issues.push('Add at least one route stop.');
    } else if (totalNights < minimumNights) {
      const missingNights = minimumNights - totalNights;
      issues.push(`Add ${missingNights} more route night${missingNights === 1 ? '' : 's'}.`);
    } else if (totalNights > item.durationDays) {
      issues.push('Reduce route nights to match the package duration.');
    }

    const existingDays = new Set(item.days.map(day => day.dayNumber));
    const missingDays = Array.from({ length: item.durationDays }, (_, index) => index + 1)
      .filter(dayNumber => !existingDays.has(dayNumber));
    if (missingDays.length) {
      issues.push(`Add program day${missingDays.length === 1 ? '' : 's'} ${missingDays.join(', ')}.`);
    }
    if (item.days.some(day => !day.cityId || !day.cityName)) {
      issues.push('Resolve every day to its route city.');
    }
    return issues;
  }

  publishReadinessMessage(item: TravelPackage): string {
    return this.publishIssues(item).join(' ');
  }
  unpublish(item: TravelPackage): void {
    this.mutate(item, 'unpublish');
  }
  deactivate(item: TravelPackage): void {
    this.mutate(item, 'deactivate');
  }

  private mutate(item: TravelPackage, action: 'publish' | 'unpublish' | 'deactivate'): void {
    this.actionId.set(item.id);
    this.error.set('');
    this.notice.set('');
    const operation =
      action === 'publish'
        ? this.marketplace.publish(item.id)
        : action === 'unpublish'
        ? this.marketplace.unpublish(item.id)
        : this.marketplace.deactivate(item.id);

    operation.subscribe({
      next: (updated) => {
        this.actionId.set(null);
        this.packages.update((items) =>
          items.map((current) => (current.id === updated.id ? updated : current))
        );
        this.notice.set(`Package ${action}d successfully.`);
      },
      error: (error: unknown) => {
        this.actionId.set(null);
        this.error.set(getApiErrorMessage(error, `We could not ${action} this package.`));
        if (action === 'publish' && error instanceof HttpErrorResponse &&
          error.error?.errors?.some((item: { code?: string }) =>
            item.code === 'Billing.AgencyMarketplaceSubscriptionRequired' ||
            item.code === 'Billing.AgencyPackageLimitReached')) {
          window.setTimeout(() => void this.router.navigate(['/agency/pricing'], {
            queryParams: { reason: 'agency-package-publishing-limit' }
          }), 700);
        }
      }
    });
  }

  private applyPackage(updated: TravelPackage): void {
    this.packages.update((items) =>
      items.map((current) => (current.id === updated.id ? updated : current))
    );
    if (this.editingId() === updated.id) {
      this.form.patchValue({ coverImageUrl: updated.coverImageUrl ?? '' }, { emitEvent: false });
    }
  }

  getPackageStatus(item: TravelPackage): string {
    if (!item.isActive) return 'Inactive';
    return item.isPublished ? 'Published' : 'Draft';
  }
}
