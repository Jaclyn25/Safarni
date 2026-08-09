import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../services/auth.service';
import { MarketplaceService } from '../../services/marketplace.service';
import { TravelPackage } from '../../models/marketplace.models';

@Component({
  selector: 'app-marketplace',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './marketplace.component.html',
  styleUrl: './marketplace.component.css'
})
export class MarketplaceComponent implements OnInit {
  readonly packages = signal<TravelPackage[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly destination = signal('');
  readonly startDate = signal('');
  readonly durationDays = signal<number | null>(null);
  readonly minPrice = signal<number | null>(null);
  readonly maxPrice = signal<number | null>(null);

  totalCount = 0;
  page = 1;
  totalPages = 1;

  constructor(private readonly marketplace: MarketplaceService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.marketplace.list({
      destination: this.destination(), startDate: this.startDate(), durationDays: this.durationDays(),
      minPrice: this.minPrice(), maxPrice: this.maxPrice(), page: this.page, pageSize: 12
    }).subscribe({
      next: (result) => { this.packages.set(result.items); this.totalCount = result.totalCount; this.totalPages = Math.max(1, result.totalPages); this.loading.set(false); },
      error: (error: unknown) => { this.error.set(getApiErrorMessage(error, 'We could not load agency packages.')); this.loading.set(false); }
    });
  }

  reset(): void {
    this.destination.set(''); this.startDate.set(''); this.durationDays.set(null); this.minPrice.set(null); this.maxPrice.set(null); this.page = 1; this.load();
  }

  nextPage(): void { if (this.page < this.totalPages) { this.page++; this.load(); } }
  previousPage(): void { if (this.page > 1) { this.page--; this.load(); } }

  price(packageItem: TravelPackage): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: packageItem.currencyCode }).format(packageItem.price);
  }

  date(value: string): string { return new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }); }
}
