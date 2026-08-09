import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { TravelPackage, TravelPackageRequest } from '../../../models/marketplace.models';
import { AgencyService } from '../../../services/agency.service';
import { getApiErrorMessage } from '../../../services/auth.service';
import { MarketplaceService } from '../../../services/marketplace.service';
import { AgencyEmptyStateComponent } from '../shared/agency-empty-state.component';
import { AgencyKpiCardComponent } from '../shared/agency-kpi-card.component';
import { AgencyLoadingSkeletonComponent } from '../shared/agency-loading-skeleton.component';
import { AgencyPageHeaderComponent } from '../shared/agency-page-header.component';
import { AgencyStatusBadgeComponent } from '../shared/agency-status-badge.component';

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

  readonly searchQuery = signal('');
  readonly selectedStatusFilter = signal<'all' | 'published' | 'draft' | 'inactive'>('all');

  readonly form: FormGroup;
  readonly dayForm: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly marketplace: MarketplaceService,
    private readonly agencies: AgencyService
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
      offerLabel: ['']
    });
    this.dayForm = this.fb.nonNullable.group({
      dayNumber: [1, [Validators.required, Validators.min(1)]],
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.required, Validators.maxLength(2000)]],
      activities: ['']
    });
  }

  ngOnInit(): void {
    this.load();
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
      offerLabel: ''
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
      offerLabel: item.offerLabel ?? ''
    });
    this.resetDayForm();
    this.formError.set('');
    this.dayError.set('');
    this.notice.set('');
    this.isEditorOpen.set(true);
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
      description: day.description,
      activities: day.activities ?? ''
    });
    this.dayError.set('');
  }

  resetDayForm(): void {
    this.editingDayId.set(null);
    this.dayForm.reset({ dayNumber: 1, title: '', description: '', activities: '' });
  }

  saveDay(): void {
    const packageItem = this.editingPackage();
    if (!packageItem || this.dayForm.invalid) {
      this.dayForm.markAllAsTouched();
      this.dayError.set('Complete the day title and description before saving.');
      return;
    }
    this.daySaving.set(true);
    this.dayError.set('');
    const value = this.dayForm.getRawValue();
    const request = {
      dayNumber: Number(value.dayNumber),
      title: value.title,
      description: value.description,
      activities: value.activities.trim() || null
    };
    const operation = this.editingDayId()
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
        this.dayError.set(getApiErrorMessage(error, 'We could not save this itinerary day.'));
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

  uploadImage(event: Event): void {
    const packageItem = this.editingPackage();
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!packageItem || !file) return;
    this.imageSaving.set(true);
    this.error.set('');
    this.marketplace.uploadPackageImage(packageItem.id, file).subscribe({
      next: (updated) => {
        this.imageSaving.set(false);
        this.applyPackage(updated);
        this.notice.set('Package image updated.');
        input.value = '';
      },
      error: (error: unknown) => {
        this.imageSaving.set(false);
        this.error.set(getApiErrorMessage(error, 'We could not upload this image.'));
        input.value = '';
      }
    });
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
      offerLabel: value.offerLabel.trim() || null
    };
    this.saving.set(true);
    const operation = this.editingId()
      ? this.marketplace.updateAgencyPackage(this.editingId()!, request)
      : this.marketplace.createAgencyPackage(request);

    operation.subscribe({
      next: (item) => {
        const wasEditing = Boolean(this.editingId());
        this.saving.set(false);
        this.packages.update((items) =>
          wasEditing ? items.map((current) => (current.id === item.id ? item : current)) : [item, ...items]
        );
        this.notice.set(wasEditing ? 'Package updated.' : 'Draft package created.');
        this.closeEditor();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.formError.set(getApiErrorMessage(error, 'We could not save this package.'));
      }
    });
  }

  publish(item: TravelPackage): void {
    this.mutate(item, 'publish');
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
      }
    });
  }

  private applyPackage(updated: TravelPackage): void {
    this.packages.update((items) =>
      items.map((current) => (current.id === updated.id ? updated : current))
    );
  }

  getPackageStatus(item: TravelPackage): string {
    if (!item.isActive) return 'Inactive';
    return item.isPublished ? 'Published' : 'Draft';
  }
}
