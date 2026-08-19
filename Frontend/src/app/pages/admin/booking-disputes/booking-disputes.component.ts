import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../../services/auth.service';
import { AdminService } from '../../../services/admin.service';
import { BookingDisputeItem } from '../../../models/admin.models';

@Component({
  selector: 'app-admin-booking-disputes',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './booking-disputes.component.html'
})
export class AdminBookingDisputesComponent implements OnInit {
  readonly items = signal<BookingDisputeItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal('');
  readonly actionId = signal<string | null>(null);
  readonly decisions: Record<string, 'ResolvedForTraveler' | 'ResolvedForAgency' | 'Rejected'> = {};
  readonly amounts: Record<string, number | null> = {};
  readonly notes: Record<string, string> = {};

  constructor(private readonly admin: AdminService) {}
  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true); this.error.set('');
    this.admin.getBookingDisputes().subscribe({
      next: (items) => { this.items.set(items); for (const item of items) { this.decisions[item.id] ??= 'ResolvedForTraveler'; this.amounts[item.id] ??= item.requestedRefundAmount; } this.loading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'Could not load booking disputes.')); this.loading.set(false); }
    });
  }

  resolve(item: BookingDisputeItem): void {
    this.actionId.set(item.id); this.error.set(''); this.notice.set('');
    this.admin.resolveBookingDispute(item.id, this.decisions[item.id] ?? 'ResolvedForTraveler', this.amounts[item.id] ?? null, this.notes[item.id]?.trim() || null).subscribe({
      next: () => { this.items.update((items) => items.filter((current) => current.id !== item.id)); this.actionId.set(null); this.notice.set('Dispute resolved and the booking settlement was updated.'); },
      error: (error: unknown) => { this.actionId.set(null); this.error.set(getApiErrorMessage(error, 'Could not resolve the dispute.')); }
    });
  }
}
