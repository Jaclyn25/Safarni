import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  SettingsService,
  MySettings,
  UpdateMySettingsCommand,
  getSettingsErrorMessage
} from '../../../services/settings.service';
import { BrowserNotificationService } from '../../../services/browser-notification.service';

@Component({
  selector: 'app-profile-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile-notifications.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileNotificationsComponent implements OnInit {
  readonly settings = signal<MySettings | null>(null);
  readonly isSavingSettings = signal(false);
  readonly settingsError = signal<string | null>(null);
  readonly settingsSuccess = signal(false);
  readonly permissionMessage = signal<string | null>(null);

  constructor(
    private readonly settingsService: SettingsService,
    private readonly browserNotifications: BrowserNotificationService
  ) {}

  ngOnInit(): void {
    this.settingsService.getMySettings().subscribe({
      next: (settings) => {
        this.settings.set(settings);
      },
      error: (error: unknown) => {
        this.settingsError.set(
          getSettingsErrorMessage(error, 'Could not load your notification settings.')
        );
      },
    });
  }

  async saveSettings(): Promise<void> {
    const settings = this.settings();
    if (!settings) return;

    this.isSavingSettings.set(true);
    this.settingsError.set(null);
    this.settingsSuccess.set(false);
    this.permissionMessage.set(null);

    if (settings.pushNotificationsEnabled) {
      const permission = await this.browserNotifications.requestPermission();

      if (permission !== 'granted') {
        settings.pushNotificationsEnabled = false;
        this.permissionMessage.set(
          'Browser notifications are blocked, so push notifications were turned off. ' +
            'Allow notifications for this site in your browser to enable them.'
        );
      }
    }

    const command: UpdateMySettingsCommand = {
      emailNotificationsEnabled: settings.emailNotificationsEnabled,
      pushNotificationsEnabled: settings.pushNotificationsEnabled,
      profileIsPrivate: settings.profileIsPrivate,
      showLastSeen: settings.showLastSeen,
      showEmail: settings.showEmail,
      showPhoneNumber: settings.showPhoneNumber,
    };

    this.settingsService.updateMySettings(command).subscribe({
      next: (updated) => {
        this.settings.set(updated);
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
