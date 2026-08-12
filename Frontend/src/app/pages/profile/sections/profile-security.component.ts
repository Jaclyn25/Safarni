import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-profile-security',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-security.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileSecurityComponent implements OnInit {
  readonly confirmRequest = signal(false);

  constructor(private readonly authService: AuthService) {}

  email = '';

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.email = user?.email ?? '';
  }

  toggleConfirm(): void {
    this.confirmRequest.set(!this.confirmRequest());
  }

  get closureMailto(): string {
    const subject = encodeURIComponent('Account closure request');
    const body = encodeURIComponent(
      `Hi Safarni support,\n\nI would like to close my account (${this.email}) and have my personal data removed.\n\nThank you.`
    );
    return `mailto:support@safarni.example?subject=${subject}&body=${body}`;
  }
}