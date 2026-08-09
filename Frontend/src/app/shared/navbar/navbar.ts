import { Component, HostListener, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { filter } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { NotificationCenterComponent } from '../notifications/notification-center.component';
import { ProfileService } from '../../services/profile.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule, NotificationCenterComponent],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class NavbarComponent implements OnInit {
  isScrolled = false;
  isExplorePage = false;
  isAdminPage = false;
  isAgencyPage = false;
  navbarBgClass = 'bg-transparent';
  isLogoutModalOpen = false;
  isUserMenuOpen = false;
  isMobileMenuOpen = false;

  constructor(
    public authService: AuthService,
    public readonly profileService: ProfileService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      const url = (event.urlAfterRedirects || event.url).split('?')[0];
      this.isExplorePage = url === '/' || url === '';
      this.isAdminPage = url.startsWith('/admin');
      this.isAgencyPage = url === '/agency' || url.startsWith('/agency/');
      this.isMobileMenuOpen = false;
      this.isUserMenuOpen = false;
      this.updateBgClass();
      this.ensureTravelerProfileLoaded();
    });

    const currentUrl = window.location.pathname;
    this.isExplorePage = currentUrl === '/' || currentUrl === '';
    this.isAdminPage = currentUrl.startsWith('/admin');
    this.isAgencyPage = currentUrl === '/agency' || currentUrl.startsWith('/agency/');
    this.updateBgClass();

    this.ensureTravelerProfileLoaded();
  }

  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.isScrolled = window.scrollY > 20;
    this.updateBgClass();
  }

  private updateBgClass() {
    if (this.isExplorePage && !this.isScrolled) {
      this.navbarBgClass = 'bg-transparent';
    } else {
      this.navbarBgClass = 'bg-surface/85 backdrop-blur-xl';
    }
    this.cdr.detectChanges();
  }

  get user() {
    return this.authService.getCurrentUser();
  }

  get userInitial(): string {
    const source = this.displayName.trim() || this.displayEmail.trim() || 'S';
    return source.charAt(0).toUpperCase();
  }

  get displayName(): string {
    return this.profileService.currentProfile()?.fullName || this.user?.fullName || 'Traveler';
  }

  get displayEmail(): string {
    return this.profileService.currentProfile()?.email || this.user?.email || '';
  }

  get avatarUrl(): string | null {
    return this.profileService.currentProfile()?.avatarUrl ?? null;
  }

  get isAdmin(): boolean {
    return this.authService.hasAdminRole();
  }

  get isAgency(): boolean {
    return this.authService.isTravelAgency();
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }

  toggleUserMenu(event: Event): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  closeUserMenu(): void {
    this.isUserMenuOpen = false;
  }

  logout(): void {
    this.isLogoutModalOpen = true;
    this.isUserMenuOpen = false;
    this.isMobileMenuOpen = false;
  }

  cancelLogout(): void {
    this.isLogoutModalOpen = false;
  }

  confirmLogout(): void {
    this.isLogoutModalOpen = false;
    this.profileService.clearCurrentProfile();
    this.authService.logout();
    this.router.navigate(['/']);
  }

  private ensureTravelerProfileLoaded(): void {
    const user = this.user;
    const loadedProfile = this.profileService.currentProfile();

    if (user && !this.isAdmin && !this.isAgency && loadedProfile?.userId !== user.userId) {
      this.profileService.getMyProfile().subscribe({ error: () => undefined });
    }
  }

  @HostListener('document:keydown.escape')
  handleEscape(): void {
    if (this.isLogoutModalOpen) {
      this.isLogoutModalOpen = false;
    }
    if (this.isMobileMenuOpen) {
      this.isMobileMenuOpen = false;
    }
    if (this.isUserMenuOpen) {
      this.isUserMenuOpen = false;
    }
  }

  @HostListener('document:click')
  handleDocumentClick(): void {
    this.isUserMenuOpen = false;
  }
}
