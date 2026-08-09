import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  AgencyActivityItem,
  AgencyDashboardSummary,
  AgencyDashboardTrendPoint,
  AgencyProfile,
  AgencyVerificationOverview,
  UpdateAgencyProfileRequest
} from '../../../models/agency.models';
import { AgencyService } from '../../../services/agency.service';
import { getApiErrorMessage } from '../../../services/auth.service';
import { AgencyActivityListComponent } from '../shared/agency-activity-list.component';
import { AgencyEmptyStateComponent } from '../shared/agency-empty-state.component';
import { AgencyKpiCardComponent } from '../shared/agency-kpi-card.component';
import { AgencyLoadingSkeletonComponent } from '../shared/agency-loading-skeleton.component';
import { AgencyPageHeaderComponent } from '../shared/agency-page-header.component';
import { AgencyStatusBadgeComponent } from '../shared/agency-status-badge.component';

@Component({
  selector: 'app-agency-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    AgencyPageHeaderComponent,
    AgencyKpiCardComponent,
    AgencyStatusBadgeComponent,
    AgencyEmptyStateComponent,
    AgencyLoadingSkeletonComponent,
    AgencyActivityListComponent
  ],
  templateUrl: './agency-dashboard.component.html'
})
export class AgencyDashboardComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly isSavingDetails = signal(false);
  readonly error = signal('');
  readonly isDetailsModalOpen = signal(false);
  readonly selectedDays = signal<number>(30);

  summary: AgencyDashboardSummary | null = null;
  trends: AgencyDashboardTrendPoint[] = [];
  activities: AgencyActivityItem[] = [];
  verification: AgencyVerificationOverview | null = null;
  profile: AgencyProfile | null = null;

  editForm!: FormGroup;

  constructor(
    private readonly agencyService: AgencyService,
    private readonly fb: FormBuilder
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadData();
  }

  private initForm(): void {
    this.editForm = this.fb.group({
      companyName: ['', [Validators.required, Validators.maxLength(150)]],
      description: ['', [Validators.required, Validators.maxLength(1000)]],
      address: ['', [Validators.required]],
      city: ['', [Validators.required]],
      country: ['', [Validators.required]],
      companyPhone: ['', [Validators.required]],
      websiteUrl: ['', [Validators.pattern('https?://.+')]]
    });
  }

  loadData(): void {
    this.isLoading.set(true);
    this.error.set('');

    forkJoin({
      summary: this.agencyService.getDashboardSummary(),
      trends: this.agencyService.getDashboardTrends(this.selectedDays()),
      activities: this.agencyService.getDashboardRecentActivity(10),
      verification: this.agencyService.getVerification(),
      profile: this.agencyService.getProfile()
    }).subscribe({
      next: ({ summary, trends, activities, verification, profile }) => {
        this.summary = summary;
        this.trends = trends;
        this.activities = activities;
        this.verification = verification;
        this.profile = profile;
        this.populateForm(profile);
        this.isLoading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(getApiErrorMessage(err, 'Could not load agency dashboard data.'));
        this.isLoading.set(false);
      }
    });
  }

  changeTrendDays(days: number): void {
    this.selectedDays.set(days);
    this.agencyService.getDashboardTrends(days).subscribe({
      next: (trends) => (this.trends = trends),
      error: () => {}
    });
  }

  openDetailsModal(): void {
    if (this.profile) {
      this.populateForm(this.profile);
    }
    this.isDetailsModalOpen.set(true);
  }

  closeDetailsModal(): void {
    this.isDetailsModalOpen.set(false);
  }

  saveCompanyDetails(): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.isSavingDetails.set(true);
    const req: UpdateAgencyProfileRequest = {
      ...this.editForm.value,
      logoUrl: this.profile?.logoUrl || null
    };

    this.agencyService.updateProfile(req).subscribe({
      next: (updatedProfile) => {
        this.profile = updatedProfile;
        this.isSavingDetails.set(false);
        this.closeDetailsModal();
        this.loadData();
      },
      error: (err: unknown) => {
        this.isSavingDetails.set(false);
        alert(getApiErrorMessage(err, 'Failed to update company details.'));
      }
    });
  }

  private populateForm(p: AgencyProfile): void {
    this.editForm.patchValue({
      companyName: p.companyName || '',
      description: p.description || '',
      address: p.address || '',
      city: p.city || '',
      country: p.country || '',
      companyPhone: p.companyPhone || '',
      websiteUrl: p.websiteUrl || ''
    });
  }

  get maxTrendCount(): number {
    if (!this.trends.length) return 1;
    return Math.max(...this.trends.map((t) => t.requestsCount), 1);
  }

  get formattedLastUpdated(): string {
    if (!this.summary?.lastUpdated) return 'Just now';
    return new Date(this.summary.lastUpdated).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
}
