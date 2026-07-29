import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService } from '../../services/profile.service';
import { MyProfile, UpdateMyProfileCommand } from '../../shared/models/update-my-profile.model';

type NavKey =
  | 'account'
  | 'password'
  | 'notifications'
  | 'travel-preferences'
  | 'theme'
  | 'privacy'
  | 'security'
  | 'connected-accounts'
  | 'language'
  | 'currency';

interface NavItem {
  key: NavKey;
  label: string;
  icon: string;
}

interface ConnectedAccount {
  provider: 'google' | 'facebook';
  label: string;
  status: string;
  connected: boolean;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
})
export class ProfileComponent implements OnInit {
  readonly navItems: NavItem[] = [
    { key: 'account', label: 'Account', icon: 'person' },
    { key: 'password', label: 'Password', icon: 'lock' },
    { key: 'notifications', label: 'Notifications', icon: 'notifications' },
    { key: 'travel-preferences', label: 'Travel Preferences', icon: 'travel_explore' },
    { key: 'theme', label: 'Theme', icon: 'dark_mode' },
    { key: 'privacy', label: 'Privacy', icon: 'shield' },
    { key: 'security', label: 'Security', icon: 'security' },
    { key: 'connected-accounts', label: 'Connected Accounts', icon: 'link' },
    { key: 'language', label: 'Language', icon: 'language' },
    { key: 'currency', label: 'Currency', icon: 'payments' },
  ];

  activeSection = signal<NavKey>('account');
  isSaving = signal(false);
  isLoading = signal(true);
  saveError = signal<string | null>(null);
  saveSuccess = signal(false);

  profile: MyProfile | null = null;

  readonly travelStyles = ['Luxury', 'Adventure', 'Backpacking', 'Business', 'Culinary'];
  selectedTravelStyles = new Set<string>(['Luxury', 'Adventure', 'Culinary']);

  readonly dietaryOptions = ['None', 'Vegetarian', 'Vegan', 'Gluten-Free'];
  readonly seatOptions = ['Window', 'Aisle', 'No Preference'];
  dietaryRequirement = 'None';
  seatPreference = 'Window';

  themeChoice: 'light' | 'dark' | 'system' = 'light';

  connectedAccounts: ConnectedAccount[] = [
    { provider: 'google', label: 'Google', status: 'Connected as julian.w@gmail.com', connected: true },
    { provider: 'facebook', label: 'Facebook', status: 'Not connected', connected: false },
  ];

  profileForm: FormGroup;

  constructor(private fb: FormBuilder, private profileService: ProfileService) {
    // Mirrors UpdateMyProfileCommandValidator on the backend exactly,
    // so client-side errors match server-side ones field for field.
    this.profileForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.maxLength(20), Validators.pattern(/^[0-9+\-() ]+$/)]],
      dateOfBirth: ['', [this.notInFutureValidator]],
      country: ['', [Validators.maxLength(100)]],
      city: ['', [Validators.maxLength(100)]],
      bio: ['', [Validators.maxLength(500)]],
    });
  }

  ngOnInit(): void {
    this.profileService.getMyProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.profileForm.patchValue({
          fullName: profile.fullName,
          email: profile.email,
          phoneNumber: profile.phoneNumber ?? '',
          dateOfBirth: profile.dateOfBirth ?? '',
          country: profile.country ?? '',
          city: profile.city ?? '',
          bio: profile.bio ?? '',
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.saveError.set('Could not load your profile. Please refresh the page.');
      },
    });
  }

  private notInFutureValidator(control: { value: string }) {
    if (!control.value) return null;
    const date = new Date(control.value);
    return date.getTime() > Date.now() ? { dateInFuture: true } : null;
  }

  setActiveSection(key: NavKey): void {
    this.activeSection.set(key);
    document.getElementById(key)?.scrollIntoView({ behavior: 'smooth' });
  }

  toggleTravelStyle(style: string): void {
    if (this.selectedTravelStyles.has(style)) {
      this.selectedTravelStyles.delete(style);
    } else {
      this.selectedTravelStyles.add(style);
    }
  }

  isTravelStyleSelected(style: string): boolean {
    return this.selectedTravelStyles.has(style);
  }

  selectTheme(theme: 'light' | 'dark' | 'system'): void {
    this.themeChoice = theme;
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.profileService.uploadAvatar(file).subscribe({
      next: (res) => {
        if (this.profile) this.profile.avatarUrl = res.avatarUrl;
      },
      error: () => this.saveError.set('Could not upload your photo. Please try again.'),
    });
  }

  discardChanges(): void {
    if (!this.profile) return;
    this.profileForm.patchValue({
      fullName: this.profile.fullName,
      email: this.profile.email,
      phoneNumber: this.profile.phoneNumber ?? '',
      dateOfBirth: this.profile.dateOfBirth ?? '',
      country: this.profile.country ?? '',
      city: this.profile.city ?? '',
      bio: this.profile.bio ?? '',
    });
    this.saveError.set(null);
    this.saveSuccess.set(false);
  }

  saveChanges(): void {
    if (this.profileForm.invalid || !this.profile) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.saveError.set(null);
    this.saveSuccess.set(false);

    const command: UpdateMyProfileCommand = {
      userId: this.profile.userId,
      fullName: this.profileForm.value.fullName,
      phoneNumber: this.profileForm.value.phoneNumber || null,
      dateOfBirth: this.profileForm.value.dateOfBirth || null,
      country: this.profileForm.value.country || null,
      city: this.profileForm.value.city || null,
      bio: this.profileForm.value.bio || null,
    };

    this.profileService.updateMyProfile(command).subscribe({
      next: (updated) => {
        this.profile = updated;
        this.isSaving.set(false);
        this.saveSuccess.set(true);
      },
      error: () => {
        this.isSaving.set(false);
        this.saveError.set('Could not save your changes. Please check the form and try again.');
      },
    });
  }

  get f() {
    return this.profileForm.controls;
  }
}
