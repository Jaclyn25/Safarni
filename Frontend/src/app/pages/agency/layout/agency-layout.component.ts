import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { NotificationCenterComponent } from '../../../shared/notifications/notification-center.component';

export interface AgencyNavItem {
  path: string;
  label: string;
  icon: string;
  exact: boolean;
}

export interface AgencyNavGroup {
  sectionTitle: string;
  items: AgencyNavItem[];
}

@Component({
  selector: 'app-agency-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, NotificationCenterComponent],
  templateUrl: './agency-layout.component.html',
  styleUrl: './agency-layout.component.css'
})
export class AgencyLayoutComponent {
  readonly isMobileMenuOpen = signal(false);
  readonly isSidebarCollapsed = signal(false);

  readonly groupedNavItems: AgencyNavGroup[] = [
    {
      sectionTitle: 'Main Console',
      items: [
        { path: '/agency', label: 'Overview', icon: 'grid_view', exact: true },
        { path: '/agency/packages', label: 'Packages', icon: 'luggage', exact: false },
        { path: '/agency/booking-requests', label: 'Booking Requests', icon: 'inbox', exact: false },
        { path: '/agency/pricing', label: 'Subscription & Finance', icon: 'payments', exact: false }
      ]
    },
    {
      sectionTitle: 'Trust & Governance',
      items: [
        { path: '/agency/verification', label: 'Verification', icon: 'verified_user', exact: false }
      ]
    }
  ];

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  toggleSidebar(): void {
    this.isSidebarCollapsed.set(!this.isSidebarCollapsed());
  }

  get flatNavItems(): AgencyNavItem[] {
    return this.groupedNavItems.flatMap((g) => g.items);
  }

  get currentUser() {
    return this.authService.getCurrentUser();
  }

  get initials(): string {
    const parts = (this.currentUser?.fullName || 'Agency').split(' ').filter(Boolean);
    return `${parts[0]?.[0] ?? 'A'}${parts[1]?.[0] ?? ''}`.toUpperCase();
  }

  get currentPageTitle(): string {
    const url = this.router.url.split('?')[0];
    const match = [...this.flatNavItems]
      .reverse()
      .find((item) => (item.exact ? url === item.path : url.startsWith(item.path)));
    return match ? match.label : 'Overview';
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
