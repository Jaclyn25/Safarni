import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-agency-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './agency-layout.component.html',
  styleUrl: './agency-layout.component.css'
})
export class AgencyLayoutComponent {
  readonly mobileMenuOpen = signal(false);
  readonly navItems = [
    { path: '/agency', label: 'Overview', icon: 'space_dashboard', exact: true },
    { path: '/agency/verification', label: 'Verification', icon: 'verified_user', exact: false },
    { path: '/agency/profile', label: 'Company Profile', icon: 'storefront', exact: false }
  ];

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  get currentUser() {
    return this.authService.getCurrentUser();
  }

  get initials(): string {
    const parts = (this.currentUser?.fullName || 'Agency').split(' ').filter(Boolean);
    return `${parts[0]?.[0] ?? 'A'}${parts[1]?.[0] ?? ''}`.toUpperCase();
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
