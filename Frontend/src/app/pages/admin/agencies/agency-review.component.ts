import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  AdminAgencyDetail,
  AgencyDocumentDecision,
  AgencyVerificationDecision,
  AgencyVerificationDocumentType
} from '../../../models/agency.models';
import { AgencyService } from '../../../services/agency.service';
import { AuthService, getApiErrorMessage } from '../../../services/auth.service';

@Component({
  selector: 'app-admin-agency-review',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './agency-review.component.html',
  styleUrl: './agency-review.component.css'
})
export class AdminAgencyReviewComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly reviewingType = signal<AgencyVerificationDocumentType | null>(null);
  readonly openingType = signal<AgencyVerificationDocumentType | null>(null);
  readonly error = signal('');
  readonly success = signal('');

  detail: AdminAgencyDetail | null = null;
  reason = '';
  readonly documentReasons: Record<AgencyVerificationDocumentType, string> = {
    CommercialRegistration: '',
    TaxCard: '',
    TourismCompanyLicense: ''
  };
  private agencyId = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly agencies: AgencyService,
    private readonly auth: AuthService
  ) {}

  ngOnInit(): void {
    this.agencyId = this.route.snapshot.paramMap.get('agencyId') ?? '';
    this.load();
  }

  get canDecide(): boolean {
    return this.auth.getUserRole() === 'Admin';
  }

  get allRequiredDocumentsApproved(): boolean {
    const today = new Date(new Date().toDateString());
    return this.detail?.verification.documents.every(
      (document) =>
        document.isUploaded &&
        document.reviewStatus === 'Approved' &&
        (!document.expiryDate || new Date(document.expiryDate) >= today)
    ) ?? false;
  }

  load(): void {
    if (!this.agencyId) {
      this.error.set('The agency identifier is missing.');
      this.isLoading.set(false);
      return;
    }
    this.isLoading.set(true);
    this.error.set('');
    this.agencies.getAdminAgency(this.agencyId).subscribe({
      next: (detail) => {
        this.applyDetail(detail);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(getApiErrorMessage(error, 'Could not load this agency application.'));
        this.isLoading.set(false);
      }
    });
  }

  decide(decision: AgencyVerificationDecision): void {
    if (!this.canDecide) return;
    if (decision !== 'Approve' && !this.reason.trim()) {
      this.error.set('Enter a clear reason before requesting changes, rejecting, or suspending the agency.');
      return;
    }
    if (decision === 'Approve' && !this.allRequiredDocumentsApproved) {
      this.error.set('Approve each valid document before approving the agency.');
      return;
    }

    this.isSaving.set(true);
    this.error.set('');
    this.success.set('');
    this.agencies
      .decideAgency(
        this.agencyId,
        decision,
        decision === 'Approve' ? null : this.reason.trim()
      )
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (detail) => {
          this.applyDetail(detail);
          this.success.set(`Decision saved: ${this.label(detail.verification.verificationStatus)}.`);
        },
        error: (error: unknown) =>
          this.error.set(getApiErrorMessage(error, 'The verification decision could not be saved.'))
      });
  }

  decideDocument(type: AgencyVerificationDocumentType, decision: AgencyDocumentDecision): void {
    if (!this.canDecide) return;
    const reason = this.documentReasons[type].trim();
    if (decision === 'Decline' && !reason) {
      this.error.set('Enter a correction reason before declining this document.');
      return;
    }

    this.reviewingType.set(type);
    this.error.set('');
    this.success.set('');
    this.agencies
      .decideDocument(this.agencyId, type, decision, decision === 'Approve' ? null : reason)
      .pipe(finalize(() => this.reviewingType.set(null)))
      .subscribe({
        next: (detail) => {
          this.applyDetail(detail);
          this.success.set(
            `${detail.verification.documents.find((item) => item.documentType === type)?.displayName} ${decision === 'Approve' ? 'approved' : 'declined'}.`
          );
        },
        error: (error: unknown) =>
          this.error.set(getApiErrorMessage(error, 'The document decision could not be saved.'))
      });
  }

  openDocument(type: AgencyVerificationDocumentType): void {
    this.openingType.set(type);
    this.error.set('');
    this.agencies
      .getAdminDocument(this.agencyId, type)
      .pipe(finalize(() => this.openingType.set(null)))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          window.open(url, '_blank', 'noopener,noreferrer');
          window.setTimeout(() => URL.revokeObjectURL(url), 60000);
        },
        error: (error: unknown) =>
          this.error.set(getApiErrorMessage(error, 'The private document could not be opened.'))
      });
  }

  label(value: string): string {
    return value === 'ChangesRequested' ? 'Changes requested' : value;
  }

  private applyDetail(detail: AdminAgencyDetail): void {
    this.detail = detail;
    this.reason = detail.verification.verificationStatus === 'Verified'
      ? ''
      : detail.verification.decisionReason ?? '';
    for (const document of detail.verification.documents) {
      this.documentReasons[document.documentType] = document.reviewReason ?? '';
    }
  }
}
