import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AgencyVerificationDocumentType, AgencyVerificationOverview, AgencyVerificationStatus } from '../../../models/agency.models';
import { AgencyService } from '../../../services/agency.service';
import { getApiErrorMessage } from '../../../services/auth.service';

interface DocumentDraft { documentNumber: string; expiryDate: string; file: File | null; }

@Component({
  selector: 'app-agency-verification',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agency-verification.component.html',
  styleUrls: ['../agency-shared.css', './agency-verification.component.css']
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
  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true); this.error.set('');
    this.agencies.getVerification().subscribe({
      next: (verification) => { this.applyVerification(verification); this.isLoading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load your verification documents.')); this.isLoading.set(false); }
    });
  }

  selectFile(type: AgencyVerificationDocumentType, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.error.set(''); this.success.set('');
    if (file && file.size > 10 * 1024 * 1024) {
      this.error.set('The selected document is larger than 10 MB.'); input.value = ''; return;
    }
    this.drafts[type].file = file;
  }

  upload(type: AgencyVerificationDocumentType): void {
    const draft = this.drafts[type];
    if (!draft.documentNumber.trim() || !draft.file) {
      this.error.set('Enter the document number and choose a PDF, JPG, or PNG file.'); return;
    }
    this.busyType.set(type); this.error.set(''); this.success.set('');
    this.agencies.uploadDocument(type, draft.documentNumber, draft.expiryDate || null, draft.file)
      .pipe(finalize(() => this.busyType.set(null)))
      .subscribe({
        next: (verification) => { this.applyVerification(verification); this.drafts[type].file = null; this.success.set('Document saved securely. Safarni administrators can now review it.'); },
        error: (error: unknown) => this.error.set(getApiErrorMessage(error, 'The document could not be uploaded.'))
      });
  }

  remove(type: AgencyVerificationDocumentType): void {
    if (!window.confirm('Remove this verification document? You will need to upload it again before approval.')) return;
    this.busyType.set(type); this.error.set(''); this.success.set('');
    this.agencies.deleteDocument(type).pipe(finalize(() => this.busyType.set(null))).subscribe({
      next: (verification) => { this.applyVerification(verification); this.success.set('Document removed.'); },
      error: (error: unknown) => this.error.set(getApiErrorMessage(error, 'The document could not be removed.'))
    });
  }

  open(type: AgencyVerificationDocumentType): void {
    this.busyType.set(type); this.error.set('');
    this.agencies.getOwnDocument(type).pipe(finalize(() => this.busyType.set(null))).subscribe({
      next: (blob) => this.openBlob(blob),
      error: (error: unknown) => this.error.set(getApiErrorMessage(error, 'The private document could not be opened.'))
    });
  }

  statusLabel(status: AgencyVerificationStatus): string { return status === 'ChangesRequested' ? 'Changes requested' : status; }
  documentStatus(document: { isUploaded: boolean; reviewStatus: string }): string {
    return document.isUploaded ? document.reviewStatus : 'Missing';
  }
  documentStatusIcon(document: { isUploaded: boolean; reviewStatus: string }): string {
    if (!document.isUploaded) return 'radio_button_unchecked';
    if (document.reviewStatus === 'Approved') return 'check_circle';
    if (document.reviewStatus === 'Declined') return 'cancel';
    return 'schedule';
  }
  formatSize(bytes: number | null): string { return bytes === null ? '' : bytes < 1024 * 1024 ? `${Math.ceil(bytes / 1024)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`; }

  private applyVerification(verification: AgencyVerificationOverview): void {
    this.verification = verification;
    for (const document of verification.documents) {
      this.drafts[document.documentType].documentNumber = document.documentNumber ?? '';
      this.drafts[document.documentType].expiryDate = document.expiryDate?.slice(0, 10) ?? '';
    }
  }

  private openBlob(blob: Blob): void {
    const url = URL.createObjectURL(blob); window.open(url, '_blank', 'noopener,noreferrer'); window.setTimeout(() => URL.revokeObjectURL(url), 60000);
  }
}
