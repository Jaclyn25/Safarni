import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService, getApiErrorMessage } from '../../services/auth.service';
import { PasswordRecoveryShellComponent } from '../../shared/password-recovery-shell/password-recovery-shell.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PasswordRecoveryShellComponent],
  templateUrl: './forgot-password.component.html'
})
export class ForgotPasswordComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly visualSrc = 'assets/password-recovery/forgot-password-visual.jpg';
  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]]
  });

  isSubmitting = false;
  errorMessage = '';

  submit(): void {
    this.errorMessage = '';
    this.form.markAllAsTouched();

    if (this.form.invalid || this.isSubmitting) {
      return;
    }

    const email = this.form.controls.email.value.trim().toLowerCase();
    this.isSubmitting = true;

    this.authService
      .forgotPassword({ email })
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          sessionStorage.setItem('password_recovery_email', email);
          void this.router.navigate(['/check-email'], { queryParams: { email } });
        },
        error: (error: unknown) => {
          this.errorMessage = getApiErrorMessage(
            error,
            'We could not send the reset email. Please try again.'
          );
        }
      });
  }
}
