import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { NotificationCenterComponent } from '../../../shared/notifications/notification-center.component';

export interface NavItem {
  path: string;
  label: string;
  icon: string;
}

export interface NavGroup {
  sectionTitle: string;
  items: NavItem[];
}

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, NotificationCenterComponent],
  templateUrl: './admin-layout.component.html',
})
export class AdminLayoutComponent {
  readonly groupedNavItems: NavGroup[] = [
    {
      sectionTitle: 'Core Console',
      items: [
        { path: '/admin', label: 'Dashboard', icon: 'grid_view' },
        { path: '/admin/analytics', label: 'Analytics', icon: 'insights' },
      ],
    },
    {
      sectionTitle: 'User & Governance',
      items: [
        { path: '/admin/users', label: 'Users', icon: 'group' },
        { path: '/admin/agencies', label: 'Agency Verification', icon: 'domain_verification' },
        { path: '/admin/roles', label: 'Roles', icon: 'admin_panel_settings' },
        { path: '/admin/audit-logs', label: 'Audit Logs', icon: 'history_edu' },
      ],
    },
    {
      sectionTitle: 'AI & Infrastructure',
      items: [
        { path: '/admin/knowledge', label: 'Knowledge Base', icon: 'auto_stories' },
        { path: '/admin/ai-activity', label: 'AI Activity', icon: 'psychology' },
        { path: '/admin/integrations', label: 'Integrations', icon: 'hub' },
      ],
    },
  ];

  readonly isMobileMenuOpen = signal(false);
  readonly isSidebarCollapsed = signal(false);

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  toggleSidebar(): void {
    this.isSidebarCollapsed.set(!this.isSidebarCollapsed());
  }

  get flatNavItems(): NavItem[] {
    return this.groupedNavItems.flatMap((g) => g.items);
  }

  get currentUser(): { userId: string; fullName: string; email: string } | null {
    return this.authService.getCurrentUser();
  }

  get initials(): string {
    const name = this.currentUser?.fullName || 'Admin';
    const parts = name.split(' ').filter(Boolean);
    return ((parts[0]?.charAt(0) ?? 'A') + (parts[1]?.charAt(0) ?? '')).toUpperCase();
  }

  get currentPageTitle(): string {
    const url = this.router.url.split('?')[0];
    const match = [...this.flatNavItems]
      .reverse()
      .find((item) => (item.path === '/admin' ? url === '/admin' : url.startsWith(item.path)));
    return match ? match.label : 'Dashboard';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
