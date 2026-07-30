import { Component, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService, getApiErrorMessage } from '../../services/auth.service';
import { PasswordRecoveryShellComponent } from '../../shared/password-recovery-shell/password-recovery-shell.component';

const passwordsMatchValidator: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const password = control.get('newPassword')?.value as string | undefined;
  const confirmation = control.get('confirmPassword')?.value as string | undefined;

  return password && confirmation && password !== confirmation
    ? { passwordsMismatch: true }
    : null;
};

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PasswordRecoveryShellComponent],
  templateUrl: './reset-password.component.html'
})
export class ResetPasswordComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  readonly visualSrc = 'assets/password-recovery/create-password-visual.jpg';
  readonly email = this.route.snapshot.queryParamMap.get('email')?.trim().toLowerCase() ?? '';
  readonly token = this.route.snapshot.queryParamMap.get('token')?.trim() ?? '';
  readonly hasValidLink = Boolean(this.email && this.token);

  readonly form = this.formBuilder.nonNullable.group(
    {
      newPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(/[A-Z]/),
          Validators.pattern(/[0-9]/),
          Validators.pattern(/[^A-Za-z0-9]/)
        ]
      ],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordsMatchValidator }
  );

  showPassword = false;
  showConfirmPassword = false;
  isSubmitting = false;
  errorMessage = '';

  get passwordValue(): string {
    return this.form.controls.newPassword.value;
  }

  get hasMinimumLength(): boolean {
    return this.passwordValue.length >= 8;
  }

  get hasUppercase(): boolean {
    return /[A-Z]/.test(this.passwordValue);
  }

  get hasNumber(): boolean {
    return /[0-9]/.test(this.passwordValue);
  }

  get hasSpecialCharacter(): boolean {
    return /[^A-Za-z0-9]/.test(this.passwordValue);
  }

  submit(): void {
    this.errorMessage = '';
    this.form.markAllAsTouched();

    if (!this.hasValidLink) {
      this.errorMessage = 'This password reset link is incomplete. Request a new reset link.';
      return;
    }

    if (this.form.invalid || this.isSubmitting) {
      return;
    }

    const { newPassword, confirmPassword } = this.form.getRawValue();
    this.isSubmitting = true;

    this.authService
      .resetPassword({
        email: this.email,
        token: this.token,
        newPassword,
        confirmPassword
      })
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          sessionStorage.removeItem('password_recovery_email');
          void this.router.navigate(['/password-updated'], { replaceUrl: true });
        },
        error: (error: unknown) => {
          this.errorMessage = getApiErrorMessage(
            error,
            'The reset link is invalid or expired. Request a new link and try again.'
          );
        }
      });
  }
}
