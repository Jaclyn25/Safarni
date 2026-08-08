import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminAgencySummary, AgencyVerificationStatus, PagedResult } from '../../../models/agency.models';
import { AgencyService } from '../../../services/agency.service';
import { getApiErrorMessage } from '../../../services/auth.service';

@Component({ selector: 'app-admin-agencies', standalone: true, imports: [CommonModule, FormsModule, RouterLink], templateUrl: './agencies.component.html', styleUrl: './agencies.component.css' })
export class AdminAgenciesComponent implements OnInit {
  readonly statuses: Array<AgencyVerificationStatus | ''> = ['', 'Pending', 'ChangesRequested', 'Verified', 'Rejected', 'Suspended'];
  readonly isLoading = signal(true); readonly error = signal('');
  result: PagedResult<AdminAgencySummary> | null = null;
  search = ''; status: AgencyVerificationStatus | '' = ''; page = 1;
  constructor(private readonly agencies: AgencyService) {}
  ngOnInit(): void { this.load(); }
  load(): void { this.isLoading.set(true); this.error.set(''); this.agencies.listAdminAgencies({ status: this.status, search: this.search, page: this.page, pageSize: 20 }).subscribe({ next: (result) => { this.result = result; this.isLoading.set(false); }, error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'Could not load agency applications.')); this.isLoading.set(false); } }); }
  applyFilters(): void { this.page = 1; this.load(); }
  clearFilters(): void { this.search = ''; this.status = ''; this.page = 1; this.load(); }
  goToPage(page: number): void { if (!this.result || page < 1 || page > this.result.totalPages || page === this.page) return; this.page = page; this.load(); }
  label(status: AgencyVerificationStatus | ''): string { return status === '' ? 'All statuses' : status === 'ChangesRequested' ? 'Changes requested' : status; }
}
