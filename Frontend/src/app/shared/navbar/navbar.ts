import { Component, HostListener, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { filter } from 'rxjs/operators';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class NavbarComponent implements OnInit {
  isScrolled = false;
  isExplorePage = false;
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
      const url = event.urlAfterRedirects.split('?')[0];
      this.isExplorePage = url === '/' || url === '';
      this.updateBgClass();
    });
    
    const currentUrl = window.location.pathname;
    this.isExplorePage = currentUrl === '/' || currentUrl === '';
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
