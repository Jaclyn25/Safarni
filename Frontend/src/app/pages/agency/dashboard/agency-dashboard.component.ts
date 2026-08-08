import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AgencyProfile, AgencyVerificationOverview, AgencyVerificationStatus } from '../../../models/agency.models';
import { AgencyService } from '../../../services/agency.service';
import { getApiErrorMessage } from '../../../services/auth.service';

@Component({
  selector: 'app-agency-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './agency-dashboard.component.html',
  styleUrl: '../agency-shared.css'
})
export class AgencyDashboardComponent implements OnInit {
  readonly isLoading = signal(true);
  readonly error = signal('');
  verification: AgencyVerificationOverview | null = null;
  profile: AgencyProfile | null = null;

  constructor(private readonly agencies: AgencyService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true); this.error.set('');
    forkJoin({ verification: this.agencies.getVerification(), profile: this.agencies.getProfile() }).subscribe({
      next: ({ verification, profile }) => { this.verification = verification; this.profile = profile; this.isLoading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load your agency workspace.')); this.isLoading.set(false); }
    });
  }

  statusLabel(status: AgencyVerificationStatus): string {
    return status === 'ChangesRequested' ? 'Changes requested' : status;
  }
}
