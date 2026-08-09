import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../services/auth.service';
import { MarketplaceService } from '../../services/marketplace.service';
import { PackageBookingRequest } from '../../models/marketplace.models';

@Component({
  selector: 'app-agency-trips',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './agency-trips.component.html',
  styleUrl: './agency-trips.component.css'
})
export class AgencyTripsComponent implements OnInit {
  readonly requests = signal<PackageBookingRequest[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  constructor(private readonly marketplace: MarketplaceService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true); this.error.set('');
    this.marketplace.listTravelerBookings().subscribe({
      next: (requests) => { this.requests.set(requests); this.loading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load your agency trip requests.')); this.loading.set(false); }
    });
  }

  statusCopy(status: PackageBookingRequest['status']): string {
    return status === 'Pending' ? 'Waiting for agency review' : status === 'Accepted' ? 'Your place is confirmed' : status === 'Rejected' ? 'The agency could not accept this request' : 'Request cancelled';
  }
}
