import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ProfileService, getProfileErrorMessage } from '../../../services/profile.service';
import { MyProfile } from '../../../shared/models/update-my-profile.model';
import { applyLanguage, applyThemeClass, readStoredLanguage, readStoredTheme } from '../profile-utils';
import { AuthService } from '../../../services/auth.service';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-profile-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './profile-layout.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileLayoutComponent implements OnInit {
  private readonly standardNavItems: NavItem[] = [
    { path: 'account', label: 'Account', icon: 'person' },
    { path: 'password', label: 'Password', icon: 'lock' },
    { path: 'trips', label: 'My Trips', icon: 'luggage' },
    { path: 'agency-trips', label: 'Agency Trips', icon: 'travel_explore' },
    { path: 'notifications', label: 'Notifications', icon: 'notifications' },
    { path: 'travel-preferences', label: 'Travel Preferences', icon: 'travel_explore' },
    { path: 'theme', label: 'Theme', icon: 'dark_mode' },
    { path: 'language', label: 'Language', icon: 'language' },
  ];

  profile: MyProfile | null = null;
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);

  constructor(
    public readonly profileService: ProfileService,
    private readonly authService: AuthService
  ) {}

  get navItems(): NavItem[] {
    if (!this.authService.isTravelAgency()) {
      return this.standardNavItems;
    }

    return [
      ...this.standardNavItems.slice(0, 4),
      { path: '/agency', label: 'Agency Dashboard', icon: 'storefront' },
      ...this.standardNavItems.slice(4),
    ];
  }

  ngOnInit(): void {
    applyThemeClass(readStoredTheme());
    applyLanguage(readStoredLanguage());

    this.profileService.getMyProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.loadError.set(
          getProfileErrorMessage(error, 'Could not load your profile. Please refresh the page.')
        );
      },
    });
  }

  get initials(): string {
    const name = this.displayProfile?.fullName || 'Traveler';
    const parts = name.split(' ').filter(Boolean);
    return ((parts[0]?.charAt(0) ?? 'T') + (parts[1]?.charAt(0) ?? '')).toUpperCase();
  }

  get displayProfile(): MyProfile | null {
    return this.profileService.currentProfile() ?? this.profile;
  }
}
