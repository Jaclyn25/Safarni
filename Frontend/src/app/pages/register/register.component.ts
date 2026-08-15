import {
  AfterViewInit,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  computed,
  signal
} from '@angular/core';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import {
  GOOGLE_AUTH_CONFIG
} from '../../config/google-auth.config';

import {
  GoogleCredentialResponse,
  GoogleIdentityService
} from '../../services/google-identity.service';

import {
  AuthService,
  RegistrationAccountType,
  getApiErrorMessage,
  getAuthFormErrorMessage
} from '../../services/auth.service';

interface PasswordChecks {
  length: boolean;
  symbol: boolean;
  upper: boolean;
  number: boolean;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    RouterLink,
    FormsModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent
  implements OnInit, AfterViewInit {

  @ViewChild('googleButton')
  private googleButton?: ElementRef<HTMLDivElement>;

  isLogin = false;

  /**
   * Page the user was trying to reach before being
   * redirected to the login screen (set by authGuard).
   */
  private returnUrl: string | null = null;

  showPassword = false;
  showConfirmPassword = false;

  fullName = '';
  email = '';
  password = '';
  confirmPassword = '';
  agreeToTerms = false;

  accountType: RegistrationAccountType = 'Traveler';
  phoneNumber = '';

  readonly isLoading = signal(false);
  readonly isGoogleLoading = signal(false);

  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  /**
   * Prevents Google login and email/password login
   * from running at the same time.
   */
  readonly isBusy = computed(
    () =>
      this.isLoading() ||
      this.isGoogleLoading()
  );

  checks: PasswordChecks = {
    length: false,
    symbol: false,
    upper: false,
    number: false
  };

  matchState: 'match' | 'mismatch' | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly googleIdentityService:
      GoogleIdentityService
  ) {}

  ngOnInit(): void {
    this.route.data.subscribe((data) => {
      this.isLogin = data['isLogin'] ?? false;
      this.clearMessages();
    });

    // Remember where the user wanted to go (set by authGuard).
    this.route.queryParams.subscribe((params) => {
      const candidate = params['returnUrl'];
      this.returnUrl =
        typeof candidate === 'string' &&
        candidate.startsWith('/') &&
        !candidate.startsWith('//')
          ? candidate
          : null;
    });

    // Already signed in? Send the user where they wanted to go.
    if (this.authService.isAuthenticated()) {
      void this.router.navigate([this.defaultRedirectUrl]);
    }
  }

  ngAfterViewInit(): void {
    void this.renderGoogleButton();
  }

  onSubmit(): void {
    if (this.isBusy()) {
      return;
    }

    this.clearMessages();

    if (this.isLogin) {
      this.submitLogin();
      return;
    }

    this.submitRegistration();
  }

  private submitLogin(): void {
    const normalizedEmail =
      this.email.trim().toLowerCase();

    if (!normalizedEmail || !this.password) {
      this.setError(
        'Please enter your email address and password.'
      );
      return;
    }

    if (!this.isValidEmail(normalizedEmail)) {
      this.setError(
        'Please enter a valid email address.'
      );
      return;
    }

    this.email = normalizedEmail;
    this.isLoading.set(true);

    this.authService
      .login({
        email: normalizedEmail,
        password: this.password
      })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        })
      )
      .subscribe({
        next: (response) => {
          if (
            !response.success ||
            !response.data
          ) {
            this.setError(
              response.errors?.[0]?.message ||
                response.message ||
                'We could not sign you in.'
            );
            return;
          }

          this.errorMessage.set('');

          this.successMessage.set(
            'Signed in successfully. Redirecting...'
          );

          window.setTimeout(() => {
            void this.router.navigate([this.defaultRedirectUrl]);
          }, 700);
        },

        error: (error: unknown) => {
          this.setError(
            getAuthFormErrorMessage(
              error,
              'login'
            )
          );
        }
      });
  }

  private submitRegistration(): void {
    const normalizedFullName =
      this.fullName.trim();

    const normalizedEmail =
      this.email.trim().toLowerCase();

    if (
      !normalizedFullName ||
      !normalizedEmail ||
      !this.password ||
      !this.confirmPassword
    ) {
      this.setError(
        'Please complete all required fields.'
      );
      return;
    }

    if (!this.isValidEmail(normalizedEmail)) {
      this.setError(
        'Please enter a valid email address.'
      );
      return;
    }

    if (
      this.password !==
      this.confirmPassword
    ) {
      this.setError(
        'The password confirmation does not match.'
      );
      return;
    }

    if (this.strengthScore < 4) {
      this.setError(
        'Your password must contain at least 8 characters, one uppercase letter, one number, and one special character.'
      );
      return;
    }

    if (!this.agreeToTerms) {
      this.setError(
        'You must agree to the Terms & Conditions and Privacy Policy.'
      );
      return;
    }

    if (this.isAgencyRegistration && !this.phoneNumber.trim()) {
      this.setError('Please enter the representative phone number.');
      return;
    }

    this.fullName = normalizedFullName;
    this.email = normalizedEmail;
    this.isLoading.set(true);

    this.authService
      .register({
        fullName: normalizedFullName,
        email: normalizedEmail,
        password: this.password,
        confirmPassword:
          this.confirmPassword,
        accountType: this.accountType,
        phoneNumber: this.isAgencyRegistration
          ? this.phoneNumber.trim()
          : null
      })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        })
      )
      .subscribe({
        next: (response) => {
          if (!response.success) {
            this.setError(
              response.errors?.[0]?.message ||
                response.message ||
                'We could not create your account.'
            );
            return;
          }

          const registeredEmail = (
            response.data?.email ||
            normalizedEmail
          )
            .trim()
            .toLowerCase();

          sessionStorage.setItem(
            'email_verification_email',
            registeredEmail
          );

          void this.router.navigate(
            ['/verification-email-sent'],
            {
              queryParams: {
                email: registeredEmail
              }
            }
          );
        },

        error: (error: unknown) => {
          this.setError(
            getAuthFormErrorMessage(
              error,
              'register'
            )
          );
        }
      });
  }

  onAccountTypeChanged(
    accountType: RegistrationAccountType
  ): void {
    if (this.isBusy()) {
      return;
    }

    this.accountType = accountType;
    this.clearMessages();

    if (accountType === 'Traveler') {
      window.setTimeout(() => {
        void this.renderGoogleButton();
      });
    }
  }

  /**
   * Loads and displays the official Google
   * Identity Services button.
   */
  private async renderGoogleButton():
    Promise<void> {
    const container =
      this.googleButton?.nativeElement;

    if (!container) {
      return;
    }

    try {
      await this.googleIdentityService
        .renderButton(
          container,
          GOOGLE_AUTH_CONFIG.clientId,
          this.handleGoogleCredential
        );
    } catch (error: unknown) {
      console.error(
        'Google Identity Services initialization failed.',
        error
      );

      this.setError(
        'Google sign-in is temporarily unavailable. ' +
          'You can still continue with email and password.'
      );
    }
  }

  /**
   * Receives the Google ID token, then sends it
   * to the ASP.NET backend for validation.
   */
  private readonly handleGoogleCredential = (
    response: GoogleCredentialResponse
  ): void => {
    const idToken =
      response.credential?.trim();

    if (!idToken) {
      this.setError(
        'Google did not return a valid sign-in credential. Please try again.'
      );
      return;
    }

    if (this.isBusy()) {
      return;
    }

    this.clearMessages();
    this.isGoogleLoading.set(true);

    this.authService
      .googleLogin({ idToken })
      .pipe(
        finalize(() => {
          this.isGoogleLoading.set(false);
        })
      )
      .subscribe({
        next: (response) => {
          if (
            !response.success ||
            !response.data
          ) {
            this.setError(
              response.errors?.[0]?.message ||
                response.message ||
                'Google sign-in could not be completed. Please close the Google window and try again.'
            );
            return;
          }

          this.errorMessage.set('');

          this.successMessage.set(
            'Google authentication successful. Redirecting...'
          );

          /*
           * AuthService should already have stored:
           * - Access token
           * - Refresh token
           * - Current user
           */
          void this.router.navigate([this.defaultRedirectUrl]);
        },

        error: (error: unknown) => {
          this.setError(
            getApiErrorMessage(
              error,
              'Google sign-in could not be completed. Please close the Google window and try again.'
            )
          );
        }
      });
  };

  toggleMode(): void {
    if (this.isBusy()) {
      return;
    }

    this.isLogin = !this.isLogin;
    this.clearMessages();

    void this.router.navigate([
      this.isLogin
        ? '/login'
        : '/register'
    ]);
  }

  dismissError(): void {
    this.errorMessage.set('');
  }

  togglePassword(): void {
    this.showPassword =
      !this.showPassword;
  }

  toggleConfirmPassword(): void {
    this.showConfirmPassword =
      !this.showConfirmPassword;
  }

  onPasswordInput(value: string): void {
    this.password = value;
    this.dismissError();

    this.checks = {
      length: value.length >= 8,
      symbol: /[^A-Za-z0-9]/.test(value),
      upper: /[A-Z]/.test(value),
      number: /[0-9]/.test(value)
    };

    this.validatePasswordMatch();
  }

  onConfirmInput(value: string): void {
    this.confirmPassword = value;
    this.dismissError();
    this.validatePasswordMatch();
  }

  private validatePasswordMatch(): void {
    if (!this.confirmPassword) {
      this.matchState = null;
      return;
    }

    this.matchState =
      this.password ===
      this.confirmPassword
        ? 'match'
        : 'mismatch';
  }

  private clearMessages(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  private setError(message: string): void {
    this.successMessage.set('');
    this.errorMessage.set(message);
  }

  private isValidEmail(
    email: string
  ): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
      email
    );
  }

  get isAgencyRegistration(): boolean {
    return !this.isLogin && this.accountType === 'TravelAgency';
  }

  get strengthScore(): number {
    return Object
      .values(this.checks)
      .filter(Boolean)
      .length;
  }

  get strengthBarClass(): string {
    const base =
      'h-full transition-all duration-500';

    if (this.strengthScore <= 1) {
      return `${base} bg-error`;
    }

    if (this.strengthScore <= 3) {
      return `${base} bg-secondary-fixed-dim`;
    }

    return `${base} bg-tertiary`;
  }

  checkClass(active: boolean): string {
    return active
      ? 'flex items-center gap-2 opacity-100 text-tertiary transition-opacity'
      : 'flex items-center gap-2 opacity-60 text-on-surface-variant transition-opacity';
  }

  private get defaultRedirectUrl(): string {
    const isAdmin = this.authService.hasAdminRole();
    const isAgency = this.authService.isTravelAgency();
    if (this.returnUrl) {
      if (isAdmin && this.returnUrl.startsWith('/admin')) {
        return this.returnUrl;
      }
      if (!isAdmin && !this.returnUrl.startsWith('/admin')) {
        return this.returnUrl;
      }
    }
    return isAdmin ? '/admin' : isAgency ? '/agency/setup' : '/';
  }
}
