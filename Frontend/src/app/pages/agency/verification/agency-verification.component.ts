import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  AgencyVerificationDocumentType,
  AgencyVerificationOverview,
  AgencyVerificationStatus
} from '../../../models/agency.models';
import { AgencyService } from '../../../services/agency.service';
import { getApiErrorMessage } from '../../../services/auth.service';
import { AgencyEmptyStateComponent } from '../shared/agency-empty-state.component';
import { AgencyKpiCardComponent } from '../shared/agency-kpi-card.component';
import { AgencyLoadingSkeletonComponent } from '../shared/agency-loading-skeleton.component';
import { AgencyPageHeaderComponent } from '../shared/agency-page-header.component';
import { AgencyStatusBadgeComponent } from '../shared/agency-status-badge.component';

interface DocumentDraft {
  documentNumber: string;
  expiryDate: string;
  file: File | null;
}

@Component({
  selector: 'app-agency-verification',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    AgencyPageHeaderComponent,
    AgencyStatusBadgeComponent,
    AgencyLoadingSkeletonComponent
  ],
  templateUrl: './agency-verification.component.html'
})
export class AgencyVerificationComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly busyType = signal<AgencyVerificationDocumentType | null>(null);
  readonly error = signal('');
  readonly success = signal('');
  verification: AgencyVerificationOverview | null = null;

  readonly drafts: Record<AgencyVerificationDocumentType, DocumentDraft> = {
    CommercialRegistration: { documentNumber: '', expiryDate: '', file: null },
    TaxCard: { documentNumber: '', expiryDate: '', file: null },
    TourismCompanyLicense: { documentNumber: '', expiryDate: '', file: null }
  };

  constructor(private readonly agencies: AgencyService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set('');
    this.agencies.getVerification().subscribe({
      next: (verification) => {
        this.applyVerification(verification);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(getApiErrorMessage(error, 'We could not load your verification documents.'));
        this.isLoading.set(false);
      }
    });
  }

  selectFile(type: AgencyVerificationDocumentType, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.error.set('');
    this.success.set('');
    if (file && file.size > 10 * 1024 * 1024) {
      this.error.set('The selected document is larger than 10 MB limit.');
      input.value = '';
      return;
    }
    this.drafts[type].file = file;
  }

  upload(type: AgencyVerificationDocumentType): void {
    const draft = this.drafts[type];
    if (!draft.documentNumber.trim() || !draft.file) {
      this.error.set('Please enter document number and select a PDF/JPG/PNG file.');
      return;
    }
    this.busyType.set(type);
    this.error.set('');
    this.success.set('');
    this.agencies
      .uploadDocument(type, draft.documentNumber, draft.expiryDate || null, draft.file)
      .pipe(finalize(() => this.busyType.set(null)))
      .subscribe({
        next: (verification) => {
          this.applyVerification(verification);
          this.drafts[type].file = null;
          this.success.set('Document uploaded securely. Admin review pending.');
        },
        error: (error: unknown) =>
          this.error.set(getApiErrorMessage(error, 'The document could not be uploaded.'))
      });
  }

  remove(type: AgencyVerificationDocumentType): void {
    if (!window.confirm('Remove this verification document? You will need to upload it again for verification.'))
      return;
    this.busyType.set(type);
    this.error.set('');
    this.success.set('');
    this.agencies
      .deleteDocument(type)
      .pipe(finalize(() => this.busyType.set(null)))
      .subscribe({
        next: (verification) => {
          this.applyVerification(verification);
          this.success.set('Document removed.');
        },
        error: (error: unknown) =>
          this.error.set(getApiErrorMessage(error, 'The document could not be removed.'))
      });
  }

  open(type: AgencyVerificationDocumentType): void {
    this.busyType.set(type);
    this.error.set('');
    this.agencies
      .getOwnDocument(type)
      .pipe(finalize(() => this.busyType.set(null)))
      .subscribe({
        next: (blob) => this.openBlob(blob),
        error: (error: unknown) =>
          this.error.set(getApiErrorMessage(error, 'The private document could not be opened.'))
      });
  }

  private openBlob(blob: Blob): void {
    const url = URL.createObjectURL(blob);
    window.open(url, '_blank');
  }

  statusLabel(status: AgencyVerificationStatus): string {
    return status === 'ChangesRequested' ? 'Changes Requested' : status;
  }

  documentStatus(document: { isUploaded: boolean; reviewStatus: string }): string {
    return document.isUploaded ? document.reviewStatus : 'Missing';
  }

  formatSize(bytes: number | null): string {
    if (bytes === null) return '';
    return bytes < 1024 * 1024 ? `${Math.ceil(bytes / 1024)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  private applyVerification(verification: AgencyVerificationOverview): void {
    this.verification = verification;
    for (const document of verification.documents) {
      this.drafts[document.documentType].documentNumber = document.documentNumber ?? '';
      this.drafts[document.documentType].expiryDate = document.expiryDate?.slice(0, 10) ?? '';
    }
  }
}
