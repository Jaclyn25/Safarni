import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AgencyPayoutProfile, AgencyPayoutMethod, AgencyProfile, UpdateAgencyProfileRequest } from '../../../models/agency.models';
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
  payout: AgencyPayoutProfile | null = null;
  payoutForm = { method: 'BankTransfer' as AgencyPayoutMethod, accountHolderName: '', country: '', bankName: '', iban: '', accountNumber: '', swiftCode: '', walletProvider: '', walletNumber: '' };
  readonly isPayoutSaving = signal(false);

  constructor(private readonly agencies: AgencyService) {}
  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true); this.error.set('');
    this.agencies.getProfile().subscribe({
      next: (profile) => { this.setProfile(profile); this.isLoading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load your company profile.')); this.isLoading.set(false); }
    });
    this.agencies.getPayoutProfile().subscribe({
      next: (payout) => this.setPayout(payout),
      error: () => undefined
    });
  }

  savePayout(): void {
    this.isPayoutSaving.set(true); this.error.set(''); this.success.set('');
    this.agencies.savePayoutProfile({
      ...this.payoutForm,
      bankName: this.trimOrNull(this.payoutForm.bankName), iban: this.trimOrNull(this.payoutForm.iban),
      accountNumber: this.trimOrNull(this.payoutForm.accountNumber), swiftCode: this.trimOrNull(this.payoutForm.swiftCode),
      walletProvider: this.trimOrNull(this.payoutForm.walletProvider), walletNumber: this.trimOrNull(this.payoutForm.walletNumber)
    }).pipe(finalize(() => this.isPayoutSaving.set(false))).subscribe({
      next: (payout) => { this.setPayout(payout); this.success.set('Payout details saved and submitted for review.'); },
      error: (error: unknown) => this.error.set(getApiErrorMessage(error, 'Payout details could not be saved.'))
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
  private setPayout(payout: AgencyPayoutProfile): void {
    this.payout = payout;
    this.payoutForm = {
      method: payout.method, accountHolderName: payout.accountHolderName, country: payout.country,
      bankName: payout.bankName || '', iban: payout.iban || '', accountNumber: payout.accountNumber || '',
      swiftCode: payout.swiftCode || '', walletProvider: payout.walletProvider || '', walletNumber: payout.walletNumber || ''
    };
  }
  private emptyForm(): UpdateAgencyProfileRequest { return { companyName: '', description: '', address: '', city: '', country: '', companyPhone: '', logoUrl: null, websiteUrl: null }; }
  private trimOrNull(value: string | null): string | null { return value?.trim() || null; }
  private validOptionalUrl(value: string | null): boolean { if (!value?.trim()) return true; try { const url = new URL(value.trim()); return url.protocol === 'http:' || url.protocol === 'https:'; } catch { return false; } }
}
