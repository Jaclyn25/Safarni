import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';

interface ConnectedAccount {
  provider: 'google' | 'facebook';
  label: string;
  status: string;
  connected: boolean;
  available: boolean;
}

@Component({
  selector: 'app-profile-connected-accounts',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-connected-accounts.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileConnectedAccountsComponent {
  readonly trackByIndex = (index: number): number => index;

  constructor(readonly authService: AuthService) {}

  get connectedAccounts(): ConnectedAccount[] {
    const user = this.authService.getCurrentUser();
    return [
      {
        provider: 'google',
        label: 'Google',
        status: user
          ? `Primary sign-in · ${user.email}`
          : 'Primary sign-in',
        connected: true,
        available: true
      },
      {
        provider: 'facebook',
        label: 'Facebook',
        status: 'Not available yet',
        connected: false,
        available: false
      }
    ];
  }
}