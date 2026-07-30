import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthService, getApiErrorMessage } from '../../services/auth.service';
import { PasswordRecoveryShellComponent } from '../../shared/password-recovery-shell/password-recovery-shell.component';

type VerificationState = 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink, PasswordRecoveryShellComponent],
  templateUrl: './verify-email.component.html'
})
export class VerifyEmailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  readonly visualSrc = 'assets/password-recovery/password-updated-visual.jpg';
  readonly state = signal<VerificationState>('verifying');
  readonly message = signal('We are securely verifying your email address.');

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email')?.trim().toLowerCase() ?? '';
    const token = this.route.snapshot.queryParamMap.get('token')?.trim() ?? '';

    if (!email || !token) {
      this.state.set('error');
      this.message.set('This verification link is incomplete. Please use the complete link from your email.');
      return;
    }

    this.authService.verifyEmail({ email, token }).subscribe({
      next: (response) => {
        sessionStorage.removeItem('email_verification_email');
        this.state.set('success');
        this.message.set(response.data?.message || 'Your email address has been verified successfully.');
      },
      error: (error: unknown) => {
        this.state.set('error');
        this.message.set(
          getApiErrorMessage(
            error,
            'This verification link is invalid or has expired. Please request a new verification email.'
          )
        );
      }
    });
  }
}
