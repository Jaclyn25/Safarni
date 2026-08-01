import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ProfileService, getProfileErrorMessage } from '../../../services/profile.service';
import { MyProfile } from '../../../shared/models/update-my-profile.model';
import { applyLanguage, applyThemeClass, readStoredLanguage, readStoredTheme } from '../profile-utils';

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
  readonly navItems: NavItem[] = [
    { path: 'trips', label: 'My Trips', icon: 'luggage' },
    { path: 'account', label: 'Account', icon: 'person' },
    { path: 'password', label: 'Password', icon: 'lock' },
    { path: 'notifications', label: 'Notifications', icon: 'notifications' },
    { path: 'travel-preferences', label: 'Travel Preferences', icon: 'travel_explore' },
    { path: 'theme', label: 'Theme', icon: 'dark_mode' },
    { path: 'privacy', label: 'Privacy', icon: 'shield' },
    { path: 'security', label: 'Security', icon: 'security' },
    { path: 'connected-accounts', label: 'Connected Accounts', icon: 'link' },
    { path: 'language', label: 'Language', icon: 'language' },
    { path: 'currency', label: 'Currency', icon: 'payments' },
  ];

  profile: MyProfile | null = null;
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);

  constructor(private readonly profileService: ProfileService) {}

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
    const name = this.profile?.fullName || 'Traveler';
    const parts = name.split(' ').filter(Boolean);
    return ((parts[0]?.charAt(0) ?? 'T') + (parts[1]?.charAt(0) ?? '')).toUpperCase();
  }
}
