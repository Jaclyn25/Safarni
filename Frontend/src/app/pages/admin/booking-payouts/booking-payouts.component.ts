import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../../services/auth.service';
import { AdminService } from '../../../services/admin.service';
import { AgencyPayoutProfileItem, BookingPayoutItem, SupplierEvidenceItem } from '../../../models/admin.models';

@Component({
  selector: 'app-admin-booking-payouts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './booking-payouts.component.html'
})
export class AdminBookingPayoutsComponent implements OnInit {
  readonly items = signal<SupplierEvidenceItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal('');
  readonly actionId = signal<string | null>(null);
  readonly payoutProfiles = signal<AgencyPayoutProfileItem[]>([]);
  readonly payouts = signal<BookingPayoutItem[]>([]);
  readonly notes: Record<string, string> = {};
  readonly transferIds: Record<string, string> = {};

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true); this.error.set('');
    this.admin.getPendingSupplierEvidence().subscribe({
      next: (items) => { this.items.set(items); this.admin.getAgencyPayoutProfiles().subscribe({ next: (profiles) => this.payoutProfiles.set(profiles), error: () => undefined }); this.admin.getActionableBookingPayouts().subscribe({ next: (payouts) => this.payouts.set(payouts), error: () => undefined }); this.loading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'Could not load settlement reviews.')); this.loading.set(false); }
    });
  }

  review(item: SupplierEvidenceItem, approve: boolean): void {
    this.actionId.set(item.id); this.error.set(''); this.notice.set('');
    this.admin.reviewSupplierEvidence(item.id, approve, this.notes[item.id]?.trim() || null).subscribe({
      next: (updated) => {
        this.items.update((items) => items.map((current) => current.id === updated.id ? updated : current));
        this.actionId.set(null); this.notice.set(approve ? 'Evidence approved and payout queued.' : 'Evidence rejected.');
      },
      error: (error: unknown) => { this.actionId.set(null); this.error.set(getApiErrorMessage(error, 'Could not save the review.')); }
    });
  }

  markPaid(item: SupplierEvidenceItem): void {
    const transferId = this.transferIds[item.id]?.trim();
    if (!transferId) { this.error.set('Enter the external bank or wallet transfer reference first.'); return; }
    this.actionId.set(item.id); this.error.set(''); this.notice.set('');
    this.admin.markSupplierEvidencePaid(item.id, transferId).subscribe({
      next: (updated) => { this.items.update((items) => items.filter((current) => current.id !== updated.id)); this.actionId.set(null); this.notice.set('Supplier advance marked as paid.'); },
      error: (error: unknown) => { this.actionId.set(null); this.error.set(getApiErrorMessage(error, 'Could not mark the payout as paid.')); }
    });
  }

  openEvidence(item: SupplierEvidenceItem): void {
    window.open(`/api/admin/booking-payouts/${item.id}/file`, '_blank', 'noopener');
  }

  reviewProfile(item: AgencyPayoutProfileItem, approve: boolean): void {
    this.actionId.set(item.agencyId); this.error.set(''); this.notice.set('');
    this.admin.reviewAgencyPayoutProfile(item.agencyId, approve, null).subscribe({
      next: () => { this.payoutProfiles.update((items) => items.filter((current) => current.agencyId !== item.agencyId)); this.actionId.set(null); this.notice.set(approve ? 'Agency payout details verified.' : 'Agency payout details rejected.'); },
      error: (error: unknown) => { this.actionId.set(null); this.error.set(getApiErrorMessage(error, 'Could not review payout details.')); }
    });
  }

  markPayoutPaid(payout: BookingPayoutItem): void {
    const transferId = window.prompt('External bank/wallet transfer reference:')?.trim();
    if (!transferId) return;
    this.actionId.set(payout.id); this.error.set(''); this.notice.set('');
    this.admin.markBookingPayoutPaid(payout.id, transferId).subscribe({
      next: () => { this.payouts.update((items) => items.filter((current) => current.id !== payout.id)); this.actionId.set(null); this.notice.set('Agency payout marked as paid.'); },
      error: (error: unknown) => { this.actionId.set(null); this.error.set(getApiErrorMessage(error, 'Could not mark the agency payout as paid.')); }
    });
  }
}
