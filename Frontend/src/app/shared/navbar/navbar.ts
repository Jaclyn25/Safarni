import { Component, HostListener, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { filter } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { NotificationCenterComponent } from '../notifications/notification-center.component';

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
  navbarBgClass = 'bg-surface/80 dark:bg-surface/80';
  isLogoutModalOpen = false;

  constructor(
    public authService: AuthService, 
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
      this.updateBgClass();
    });
    
    const currentUrl = window.location.pathname;
    this.isExplorePage = currentUrl === '/' || currentUrl === '';
    this.isAdminPage = currentUrl.startsWith('/admin');
    this.isAgencyPage = currentUrl === '/agency' || currentUrl.startsWith('/agency/');
    this.updateBgClass();
  }

  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.isScrolled = window.scrollY > 50;
    this.updateBgClass();
  }

  private updateBgClass() {
    if (this.isExplorePage && !this.isScrolled) {
      this.navbarBgClass = 'bg-transparent';
    } else {
      this.navbarBgClass = 'bg-surface/80 dark:bg-surface/80';
    }
    this.cdr.detectChanges();
  }

  get user() {
    return this.authService.getCurrentUser();
  }

  get userInitial(): string {
    const user = this.user;
    const source = user?.fullName?.trim() || user?.email?.trim() || 'S';
    return source.charAt(0).toUpperCase();
  }

  get isAdmin(): boolean {
    return this.authService.hasAdminRole();
  }

  get isAgency(): boolean {
    return this.authService.isTravelAgency();
  }

  logout(): void {
    this.isLogoutModalOpen = true;
  }

  cancelLogout(): void {
    this.isLogoutModalOpen = false;
  }

  confirmLogout(): void {
    this.isLogoutModalOpen = false;
    this.authService.logout();
    this.router.navigate(['/']);
  }

  @HostListener('document:keydown.escape')
  handleEscape(): void {
    if (this.isLogoutModalOpen) {
      this.isLogoutModalOpen = false;
    }
  }
}
