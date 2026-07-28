import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import {
  AuthService,
  getApiErrorMessage
} from '../../services/auth.service';

import {
  PasswordRecoveryShellComponent
} from '../../shared/password-recovery-shell/password-recovery-shell.component';

@Component({
  selector: 'app-check-email',
  standalone: true,
  imports: [
    RouterLink,
    PasswordRecoveryShellComponent
  ],
  templateUrl: './check-email.component.html'
})
export class CheckEmailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  readonly visualSrc =
    'assets/password-recovery/check-email-visual.jpg';

  readonly emailArtSrc =
    'assets/password-recovery/email-sent-icon.png';

  readonly email =
    this.route.snapshot.queryParamMap
      .get('email')
      ?.trim()
      .toLowerCase() ??
    sessionStorage.getItem('password_recovery_email') ??
    '';

  /*
   * Signals automatically notify Angular when their values change.
   */
  readonly isResending = signal(false);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  resend(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (!this.email) {
      this.errorMessage.set(
        'Return to the reset page and enter your email address first.'
      );
      return;
    }

    if (this.isResending()) {
      return;
    }

    this.isResending.set(true);

    this.authService
      .forgotPassword({ email: this.email })
      .pipe(
        finalize(() => {
          this.isResending.set(false);
        })
      )
      .subscribe({
        next: () => {
          this.successMessage.set(
            'A new password reset link has been sent.'
          );
        },

        error: (error: unknown) => {
          this.errorMessage.set(
            getApiErrorMessage(
              error,
              'We could not resend the email. Please try again.'
            )
          );
        }
      });
  }
}