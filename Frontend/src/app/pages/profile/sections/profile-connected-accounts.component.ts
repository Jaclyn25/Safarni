import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface ConnectedAccount {
  provider: 'google' | 'facebook';
  label: string;
  status: string;
  connected: boolean;
}

@Component({
  selector: 'app-profile-connected-accounts',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-connected-accounts.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileConnectedAccountsComponent {
  connectedAccounts: ConnectedAccount[] = [
    { provider: 'google', label: 'Google', status: 'Connected as julian.w@gmail.com', connected: true },
    { provider: 'facebook', label: 'Facebook', status: 'Not connected', connected: false },
  ];
}
