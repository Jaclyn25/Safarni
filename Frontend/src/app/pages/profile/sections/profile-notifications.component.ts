import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  SettingsService,
  MySettings,
  UpdateMySettingsCommand,
  getSettingsErrorMessage
} from '../../../services/settings.service';

@Component({
  selector: 'app-profile-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile-notifications.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileNotificationsComponent implements OnInit {
  settings: MySettings | null = null;
  readonly isSavingSettings = signal(false);
  readonly settingsError = signal<string | null>(null);
  readonly settingsSuccess = signal(false);

  constructor(private readonly settingsService: SettingsService) {}

  ngOnInit(): void {
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
}
