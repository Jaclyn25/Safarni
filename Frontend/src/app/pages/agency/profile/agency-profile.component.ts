import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AgencyProfile, UpdateAgencyProfileRequest } from '../../../models/agency.models';
import { AgencyService } from '../../../services/agency.service';
import { getApiErrorMessage } from '../../../services/auth.service';

@Component({
  selector: 'app-agency-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agency-profile.component.html',
  styleUrls: ['../agency-shared.css', './agency-profile.component.css']
})
export class AgencyProfileComponent implements OnInit {
  readonly isLoading = signal(true); readonly isSaving = signal(false);
  readonly error = signal(''); readonly success = signal('');
  profile: AgencyProfile | null = null;
  form: UpdateAgencyProfileRequest = this.emptyForm();

  constructor(private readonly agencies: AgencyService) {}
  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true); this.error.set('');
    this.agencies.getProfile().subscribe({
      next: (profile) => { this.setProfile(profile); this.isLoading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load your company profile.')); this.isLoading.set(false); }
    });
  }

  save(): void {
    const required = [this.form.companyName, this.form.description, this.form.address, this.form.city, this.form.country, this.form.companyPhone];
    if (required.some((value) => !value.trim())) { this.error.set('Please complete all required company fields.'); return; }
    if (!this.validOptionalUrl(this.form.logoUrl) || !this.validOptionalUrl(this.form.websiteUrl)) { this.error.set('Logo and website links must be valid HTTP or HTTPS URLs.'); return; }
    this.isSaving.set(true); this.error.set(''); this.success.set('');
    this.agencies.updateProfile({ ...this.form, logoUrl: this.trimOrNull(this.form.logoUrl), websiteUrl: this.trimOrNull(this.form.websiteUrl) })
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (profile) => { this.setProfile(profile); this.success.set('Company profile updated successfully.'); },
        error: (error: unknown) => this.error.set(getApiErrorMessage(error, 'The company profile could not be updated.'))
      });
  }

  private setProfile(profile: AgencyProfile): void {
    this.profile = profile;
    this.form = { companyName: profile.companyName, description: profile.description, address: profile.address, city: profile.city, country: profile.country, companyPhone: profile.companyPhone, logoUrl: profile.logoUrl, websiteUrl: profile.websiteUrl };
  }
  private emptyForm(): UpdateAgencyProfileRequest { return { companyName: '', description: '', address: '', city: '', country: '', companyPhone: '', logoUrl: null, websiteUrl: null }; }
  private trimOrNull(value: string | null): string | null { return value?.trim() || null; }
  private validOptionalUrl(value: string | null): boolean { if (!value?.trim()) return true; try { const url = new URL(value.trim()); return url.protocol === 'http:' || url.protocol === 'https:'; } catch { return false; } }
}
