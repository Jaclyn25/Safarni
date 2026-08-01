import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../services/auth.service';

interface PasswordChecks {
  length: boolean;
  symbol: boolean;
  upper: boolean;
  number: boolean;
}

@Component({
  selector: 'app-profile-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile-password.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfilePasswordComponent {
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

  constructor(private readonly authService: AuthService) {}

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
}
