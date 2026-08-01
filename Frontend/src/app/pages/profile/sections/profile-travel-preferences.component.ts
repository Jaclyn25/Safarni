import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  PreferencesService,
  TravelProfile,
  UpsertTravelProfileCommand,
  getPreferencesErrorMessage
} from '../../../services/preferences.service';

@Component({
  selector: 'app-profile-travel-preferences',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile-travel-preferences.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileTravelPreferencesComponent implements OnInit {
  readonly travelStyles = ['Luxury', 'Adventure', 'Backpacking', 'Business', 'Culinary'];
  selectedTravelStyles = new Set<string>(['Luxury', 'Adventure', 'Culinary']);

  readonly dietaryOptions = ['None', 'Vegetarian', 'Vegan', 'Gluten-Free'];
  readonly seatOptions = ['Window', 'Aisle', 'No Preference'];
  dietaryRequirement = 'None';
  seatPreference = 'Window';

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

  constructor(private readonly preferencesService: PreferencesService) {}

  ngOnInit(): void {
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
}
