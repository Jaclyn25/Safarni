import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TripService, TravelCountry } from '../../services/trip.service';
import {
  VisaService,
  VisaApplicationResponse,
  COMMON_VISA_TYPES
} from '../../services/visa.service';
import { getApiErrorMessage } from '../../services/auth.service';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './bookings.component.html'
})
export class BookingsComponent implements OnInit {
  readonly applications = signal<VisaApplicationResponse[]>([]);
  readonly countries = signal<TravelCountry[]>([]);
  readonly loading = signal(true);
  readonly loadingCountries = signal(false);
  readonly error = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly selectingId = signal<string | null>(null);
  readonly selectError = signal<string | null>(null);

  readonly commonVisaTypes = COMMON_VISA_TYPES;

  readonly today = new Date().toISOString().split('T')[0];

  form: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly visaService: VisaService,
    private readonly tripService: TripService
  ) {
    this.form = this.fb.group({
      destinationCountry: ['', [Validators.required]],
      travelPurpose: ['Tourism', [Validators.required]],
      plannedTravelDate: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.loadApplications();
    this.loadCountries();
  }

  formatDate(value: string): string {
    const date = new Date(`${value}T00:00:00`);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  statusBadge(status: string): { label: string; classes: string } {    switch (status) {
      case 'VisaTypeSelected':
        return {
          label: 'Type selected',
          classes: 'bg-green-500'
        };
      case 'Draft':
      default:
        return {
          label: 'In progress',
          classes: 'bg-amber-500'
        };
    }
  }

  visaTypeLabel(application: VisaApplicationResponse): string {
    return application.visaType ?? 'Choose visa type';
  }

  loadApplications(): void {
    this.loading.set(true);
    this.error.set(null);

    this.visaService.list().subscribe({
      next: (response) => {
        this.applications.set(response.data ?? []);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(
          getApiErrorMessage(
            err,
            'We could not load your bookings. Please try again.'
          )
        );
        this.loading.set(false);
      }
    });
  }

  loadCountries(): void {
    this.loadingCountries.set(true);

    this.tripService.getCountries().subscribe({
      next: (response) => {
        this.countries.set(
          (response.data ?? []).sort((a, b) =>
            a.name.localeCompare(b.name)
          )
        );
        this.loadingCountries.set(false);
      },
      error: () => {
        this.loadingCountries.set(false);
      }
    });
  }

  openForm(): void {
    this.formError.set(null);
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.formError.set(null);
  }

  submitApplication(): void {
    if (this.form.invalid) {
      this.formError.set('Please fill in the destination, purpose, and travel date.');
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    const value = this.form.value;

    this.visaService.create(value).subscribe({
      next: (response) => {
        this.saving.set(false);

        if (response.success && response.data) {
          this.applications.update((current) => [
            response.data!,
            ...current
          ]);
          this.closeForm();
          this.form.reset({
            destinationCountry: '',
            travelPurpose: 'Tourism',
            plannedTravelDate: ''
          });
        } else {
          this.formError.set(
            response.errors?.[0]?.message ??
              'We could not create the visa application. Please try again.'
          );
        }
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(
          getApiErrorMessage(
            err,
            'We could not create the visa application. Please try again.'
          )
        );
      }
    });
  }

  selectVisaType(application: VisaApplicationResponse, visaType: string): void {
    this.selectingId.set(application.id);
    this.selectError.set(null);

    this.visaService.selectVisaType(application.id, visaType).subscribe({
      next: (response) => {
        this.selectingId.set(null);

        if (response.success && response.data) {
          this.applications.update((current) =>
            current.map((app) =>
              app.id === response.data!.id ? response.data! : app
            )
          );
        } else {
          this.selectError.set(
            response.errors?.[0]?.message ??
              'We could not update the visa type. Please try again.'
          );
        }
      },
      error: (err) => {
        this.selectingId.set(null);
        this.selectError.set(
          getApiErrorMessage(
            err,
            'We could not update the visa type. Please try again.'
          )
        );
      }
    });
  }
}
