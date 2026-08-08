import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { UpdateAgencyProfileRequest } from '../../../models/agency.models';
import { AgencyService } from '../../../services/agency.service';
import { AuthService, getApiErrorMessage } from '../../../services/auth.service';

@Component({
  selector: 'app-agency-onboarding',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './agency-onboarding.component.html',
  styleUrl: './agency-onboarding.component.css'
})
export class AgencyOnboardingComponent implements OnInit {
  readonly isChecking = signal(true);
  readonly isSaving = signal(false);
  readonly error = signal('');

  companyName = '';
  description = '';
  address = '';
  city = '';
  country = '';
  companyPhone = '';
  websiteUrl = '';
  logoUrl = '';

  constructor(
    private readonly agencies: AgencyService,
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.agencies.getOnboardingStatus().subscribe({
      next: (status) => {
        if (status.isCompanyProfileComplete) {
          void this.router.navigateByUrl('/agency');
          return;
        }
        if (!status.isEmailVerified) {
          this.error.set('Verify your email address before completing your company profile.');
        }
        this.isChecking.set(false);
      },
      error: (error: unknown) => {
        this.error.set(getApiErrorMessage(error, 'Could not check your agency setup status.'));
        this.isChecking.set(false);
      }
    });
  }

  submit(): void {
    this.error.set('');
    const request: UpdateAgencyProfileRequest = {
      companyName: this.companyName.trim(),
      description: this.description.trim(),
      address: this.address.trim(),
      city: this.city.trim(),
      country: this.country.trim(),
      companyPhone: this.companyPhone.trim(),
      websiteUrl: this.websiteUrl.trim() || null,
      logoUrl: this.logoUrl.trim() || null
    };

    if (!request.companyName || !request.description || !request.address ||
        !request.city || !request.country || !request.companyPhone) {
      this.error.set('Complete every required company field before continuing.');
      return;
    }
    if ((request.websiteUrl && !this.isHttpUrl(request.websiteUrl)) ||
        (request.logoUrl && !this.isHttpUrl(request.logoUrl))) {
      this.error.set('Website and logo links must begin with http:// or https://.');
      return;
    }

    this.isSaving.set(true);
    this.agencies.completeOnboarding(request)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl('/agency'),
        error: (error: unknown) =>
          this.error.set(getApiErrorMessage(error, 'Could not create your company profile.'))
      });
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }

  private isHttpUrl(value: string): boolean {
    try {
      const url = new URL(value);
      return url.protocol === 'http:' || url.protocol === 'https:';
    } catch {
      return false;
    }
  }
}
