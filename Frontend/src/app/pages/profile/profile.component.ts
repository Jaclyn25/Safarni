import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ProfileService, getProfileErrorMessage } from '../../services/profile.service';
import {
  PreferencesService,
  TravelProfile,
  UpsertTravelProfileCommand,
  getPreferencesErrorMessage
} from '../../services/preferences.service';
import {
  SettingsService,
  MySettings,
  UpdateMySettingsCommand,
  getSettingsErrorMessage
} from '../../services/settings.service';
import { MyProfile, UpdateMyProfileCommand } from '../../shared/models/update-my-profile.model';
import { TripResponse, TripService } from '../../services/trip.service';

type NavKey =
  | 'trips'
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

interface PasswordChecks {
  length: boolean;
  symbol: boolean;
  upper: boolean;
  number: boolean;
}

type ThemeChoice = 'light' | 'dark' | 'system';

type LanguageChoice = 'en' | 'ar';

const THEME_STORAGE_KEY = 'safarni_theme';

const LANGUAGE_STORAGE_KEY = 'safarni_lang';

/** Applies (or clears) the `dark` class Tailwind listens to. */
function applyThemeClass(theme: ThemeChoice): void {
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

  if (theme === 'dark' || (theme === 'system' && prefersDark)) {
    document.documentElement.classList.add('dark');
  } else {
    document.documentElement.classList.remove('dark');
  }
}

function readStoredTheme(): ThemeChoice {
  const stored = localStorage.getItem(THEME_STORAGE_KEY);
  return stored === 'dark' || stored === 'light' || stored === 'system'
    ? stored
    : 'system';
}

/**
 * Applies the app-wide language: sets <html lang> and flips
 * the document direction so Tailwind `rtl:`/`ltr:` variants work.
 */
function applyLanguage(lang: LanguageChoice): void {
  document.documentElement.lang = lang;
  document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
}

function readStoredLanguage(): LanguageChoice {
  return localStorage.getItem(LANGUAGE_STORAGE_KEY) === 'ar' ? 'ar' : 'en';
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
})
export class ProfileComponent implements OnInit, OnDestroy {
  readonly navItems: NavItem[] = [
    { key: 'trips', label: 'My Trips', icon: 'luggage' },
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

  // ----- My Trips (GET /api/trips) -----
  readonly trips = signal<TripResponse[]>([]);
  readonly isTripsLoading = signal(true);
  readonly tripsError = signal<string | null>(null);

  readonly travelStyles = ['Luxury', 'Adventure', 'Backpacking', 'Business', 'Culinary'];
  selectedTravelStyles = new Set<string>(['Luxury', 'Adventure', 'Culinary']);

  readonly dietaryOptions = ['None', 'Vegetarian', 'Vegan', 'Gluten-Free'];
  readonly seatOptions = ['Window', 'Aisle', 'No Preference'];
  dietaryRequirement = 'None';
  seatPreference = 'Window';

  themeChoice: ThemeChoice = 'system';

  languageChoice: LanguageChoice = 'en';

  private readonly systemThemeQuery =
    window.matchMedia('(prefers-color-scheme: dark)');

  private readonly onSystemThemeChange = (
    event: MediaQueryListEvent
  ): void => {
    if (this.themeChoice === 'system') {
      applyThemeClass('system');
      void event;
    }
  };

  connectedAccounts: ConnectedAccount[] = [
    { provider: 'google', label: 'Google', status: 'Connected as julian.w@gmail.com', connected: true },
    { provider: 'facebook', label: 'Facebook', status: 'Not connected', connected: false },
  ];

  profileForm: FormGroup;

  // ----- Travel preferences (GET/PUT /api/preferences/travel) -----
  travelProfile: TravelProfile | null = null;
  preferredDestinationCountry = '';
  travelPurpose = '';
  passportNumber = '';
  nationality = '';
  passportExpiryDate = '';
  minBudget: number | null = null;
  maxBudget: number | null = null;
  readonly isSavingPreferences = signal(false);
  readonly preferencesError = signal<string | null>(null);
  readonly preferencesSuccess = signal(false);

  // ----- Change password (POST /api/auth/change-password) -----
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  readonly isChangingPassword = signal(false);
  readonly passwordError = signal<string | null>(null);
  readonly passwordSuccess = signal(false);

  passwordChecks: PasswordChecks = {
    length: false,
    symbol: false,
    upper: false,
    number: false
  };

  passwordMatchState: 'match' | 'mismatch' | null = null;

  // ----- Notifications & privacy (GET/PUT /api/settings/me) -----
  settings: MySettings | null = null;
  readonly isSavingSettings = signal(false);
  readonly settingsError = signal<string | null>(null);
  readonly settingsSuccess = signal(false);

  constructor(
    private fb: FormBuilder,
    private profileService: ProfileService,
    private preferencesService: PreferencesService,
    private settingsService: SettingsService,
    private authService: AuthService,
    private tripService: TripService
  ) {
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
    this.themeChoice = readStoredTheme();
    applyThemeClass(this.themeChoice);
    this.systemThemeQuery.addEventListener(
      'change',
      this.onSystemThemeChange
    );

    this.languageChoice = readStoredLanguage();
    applyLanguage(this.languageChoice);

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
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.saveError.set(
          getProfileErrorMessage(error, 'Could not load your profile. Please refresh the page.')
        );
      },
    });

    this.loadTravelPreferences();
    this.loadSettings();
    this.loadTrips();
  }

  ngOnDestroy(): void {
    this.systemThemeQuery.removeEventListener(
      'change',
      this.onSystemThemeChange
    );
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

  // ================= Account profile =================

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.profileService.uploadAvatar(file).subscribe({
      next: (avatarUrl) => {
        if (this.profile) {
          this.profile.avatarUrl = avatarUrl;
          this.saveSuccess.set(true);
        }
      },
      error: (error: unknown) => {
        this.saveError.set(
          getProfileErrorMessage(error, 'Could not upload your photo. Please try again.')
        );
      },
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
      error: (error: unknown) => {
        this.isSaving.set(false);
        this.saveError.set(
          getProfileErrorMessage(error, 'Could not save your changes. Please check the form and try again.')
        );
      },
    });
  }

  // ================= Travel preferences =================

  private loadTravelPreferences(): void {
    this.preferencesService.getTravelProfile().subscribe({
      next: (profile) => {
        this.travelProfile = profile;
        this.preferredDestinationCountry = profile.preferredDestinationCountry;
        this.travelPurpose = profile.travelPurpose;
        this.passportNumber = profile.passportNumber;
        this.nationality = profile.nationality;
        this.passportExpiryDate = profile.passportExpiryDate;
        this.minBudget = profile.minBudget;
        this.maxBudget = profile.maxBudget;
      },
      error: (error: unknown) => {
        this.preferencesError.set(
          getPreferencesErrorMessage(error, 'Could not load your travel preferences.')
        );
      },
    });
  }

  saveTravelPreferences(): void {
    if (!this.travelProfile) return;

    this.isSavingPreferences.set(true);
    this.preferencesError.set(null);
    this.preferencesSuccess.set(false);

    const command: UpsertTravelProfileCommand = {
      preferredDestinationCountry: this.preferredDestinationCountry.trim(),
      travelPurpose: this.travelPurpose.trim() || 'leisure',
      travelStyle:
        this.selectedTravelStyles.size > 0
          ? Array.from(this.selectedTravelStyles).join(', ')
          : 'Luxury',
      minBudget: this.minBudget,
      maxBudget: this.maxBudget,
      passportNumber: this.passportNumber.trim(),
      nationality: this.nationality.trim(),
      passportExpiryDate: this.passportExpiryDate,
    };

    this.preferencesService.updateTravelProfile(command).subscribe({
      next: (profile) => {
        this.travelProfile = profile;
        this.isSavingPreferences.set(false);
        this.preferencesSuccess.set(true);
      },
      error: (error: unknown) => {
        this.isSavingPreferences.set(false);
        this.preferencesError.set(
          getPreferencesErrorMessage(error, 'Could not save your travel preferences.')
        );
      },
    });
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

  // ================= Theme & language =================

  selectTheme(theme: ThemeChoice): void {
    this.themeChoice = theme;
    localStorage.setItem(THEME_STORAGE_KEY, theme);
    applyThemeClass(theme);
  }

  selectLanguage(lang: LanguageChoice): void {
    this.languageChoice = lang;
    localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
    applyLanguage(lang);
  }

  // ================= Change password =================

  onNewPasswordInput(value: string): void {
    this.newPassword = value;
    this.passwordError.set(null);

    this.passwordChecks = {
      length: value.length >= 8,
      symbol: /[^A-Za-z0-9]/.test(value),
      upper: /[A-Z]/.test(value),
      number: /[0-9]/.test(value)
    };

    this.validatePasswordMatch();
  }

  onConfirmPasswordInput(value: string): void {
    this.confirmPassword = value;
    this.passwordError.set(null);
    this.validatePasswordMatch();
  }

  private validatePasswordMatch(): void {
    if (!this.confirmPassword) {
      this.passwordMatchState = null;
      return;
    }

    this.passwordMatchState =
      this.newPassword === this.confirmPassword
        ? 'match'
        : 'mismatch';
  }

  get passwordStrengthScore(): number {
    return Object.values(this.passwordChecks).filter(Boolean).length;
  }

  changePassword(): void {
    this.passwordError.set(null);
    this.passwordSuccess.set(false);

    if (!this.currentPassword) {
      this.passwordError.set('Please enter your current password.');
      return;
    }

    if (this.passwordStrengthScore < 4) {
      this.passwordError.set(
        'Your new password must contain at least 8 characters, one uppercase letter, one number, and one special character.'
      );
      return;
    }

    if (this.passwordMatchState !== 'match') {
      this.passwordError.set('The new password confirmation does not match.');
      return;
    }

    this.isChangingPassword.set(true);

    this.authService
      .changePassword({
        currentPassword: this.currentPassword,
        newPassword: this.newPassword,
        confirmPassword: this.confirmPassword,
      })
      .subscribe({
        next: () => {
          this.isChangingPassword.set(false);
          this.currentPassword = '';
          this.newPassword = '';
          this.confirmPassword = '';
          this.passwordChecks = { length: false, symbol: false, upper: false, number: false };
          this.passwordMatchState = null;
          this.passwordSuccess.set(true);
        },
        error: (error: unknown) => {
          this.isChangingPassword.set(false);
          // Backend change-password returns a raw { code, message } on failure.
          const raw = error as { error?: { message?: string } } | null;
          const message =
            raw?.error?.message ||
            (error instanceof Error ? error.message : null);
          this.passwordError.set(
            message || 'Could not change your password. Please check the current password and try again.'
          );
        },
      });
  }

  // ================= Notifications & privacy =================

  private loadSettings(): void {
    this.settingsService.getMySettings().subscribe({
      next: (settings) => {
        this.settings = settings;
      },
      error: (error: unknown) => {
        this.settingsError.set(
          getSettingsErrorMessage(error, 'Could not load your notification settings.')
        );
      },
    });
  }

  saveSettings(): void {
    if (!this.settings) return;

    this.isSavingSettings.set(true);
    this.settingsError.set(null);
    this.settingsSuccess.set(false);

    const command: UpdateMySettingsCommand = {
      emailNotificationsEnabled: this.settings.emailNotificationsEnabled,
      pushNotificationsEnabled: this.settings.pushNotificationsEnabled,
      profileIsPrivate: this.settings.profileIsPrivate,
      showLastSeen: this.settings.showLastSeen,
      showEmail: this.settings.showEmail,
      showPhoneNumber: this.settings.showPhoneNumber,
    };

    this.settingsService.updateMySettings(command).subscribe({
      next: (updated) => {
        this.settings = updated;
        this.isSavingSettings.set(false);
        this.settingsSuccess.set(true);
      },
      error: (error: unknown) => {
        this.isSavingSettings.set(false);
        this.settingsError.set(
          getSettingsErrorMessage(error, 'Could not save your notification settings.')
        );
      },
    });
  }

  // ================= My Trips =================

  loadTrips(): void {
    this.isTripsLoading.set(true);
    this.tripsError.set(null);

    this.tripService.getTrips().subscribe({
      next: (response) => {
        this.trips.set(response.data ?? []);
        this.isTripsLoading.set(false);
      },
      error: (error: unknown) => {
        this.isTripsLoading.set(false);
        this.tripsError.set(
          this.tripService.getErrorMessage(
            error,
            'Could not load your trips. Please try again.'
          )
        );
      },
    });
  }

  tripDateRange(trip: TripResponse): string {
    const start = new Date(trip.startDate);
    const end = new Date(trip.endDate);

    const format = (date: Date): string =>
      date.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      });

    return `${format(start)} – ${format(end)}`;
  }

  tripCityNames(trip: TripResponse): string {
    return (trip.cities ?? [])
      .map((city) => city.name)
      .join(', ');
  }

  tripBadge(trip: TripResponse): { label: string; classes: string } {
    switch (trip.itineraryStatus) {
      case 'completed':
        return {
          label: 'Itinerary ready',
          classes: 'badge--success'
        };
      case 'generating':
        return {
          label: 'Generating…',
          classes: 'badge--progress'
        };
      case 'failed':
        return {
          label: 'Generation failed',
          classes: 'badge--danger'
        };
      default:
        return {
          label: 'No itinerary yet',
          classes: 'badge--muted'
        };
    }
  }

  tripCost(trip: TripResponse): string | null {
    if (trip.estimatedTotalCost == null) {
      return null;
    }

    const currency = trip.itineraryCurrency ?? 'USD';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency
    }).format(trip.estimatedTotalCost);
  }

  get f() {
    return this.profileForm.controls;
  }
}
