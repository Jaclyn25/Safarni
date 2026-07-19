import { Component, OnInit } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

interface PasswordChecks {
  length: boolean;
  symbol: boolean;
  upper: boolean;
  number: boolean;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './register.component.html'
})
export class RegisterComponent implements OnInit {
  isLogin = false;
  showPassword = false;
  showConfirmPassword = false;

  fullName = '';
  email = '';
  password = '';
  confirmPassword = '';
  agreeToTerms = false;

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.data.subscribe(data => {
      this.isLogin = data['isLogin'] ?? false;
      this.clearMessages();
    });
  }

  private clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }

  toggleMode(): void {
    this.isLogin = !this.isLogin;
    this.clearMessages();
    if (this.isLogin) {
      this.router.navigate(['/login']);
    } else {
      this.router.navigate(['/register']);
    }
  }

  onSubmit(): void {
    this.clearMessages();

    if (!this.isLogin) {
      // Registration flow
      if (!this.fullName || !this.email || !this.password || !this.confirmPassword) {
        this.errorMessage = 'Please fill in all fields.';
        return;
      }

      if (this.password !== this.confirmPassword) {
        this.errorMessage = 'Passwords do not match.';
        return;
      }

      if (this.strengthScore < 4) {
        this.errorMessage = 'Please make sure your password meets all criteria.';
        return;
      }

      if (!this.agreeToTerms) {
        this.errorMessage = 'You must agree to the Terms & Conditions.';
        return;
      }

      this.isLoading = true;
      this.authService.register({
        fullName: this.fullName,
        email: this.email,
        password: this.password,
        confirmPassword: this.confirmPassword
      }).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.successMessage = 'Account created successfully! Redirecting to login...';
            setTimeout(() => {
              this.router.navigate(['/login']);
            }, 2000);
          } else {
            this.errorMessage = res.message || 'Registration failed.';
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = this.extractErrorMessage(err);
        }
      });

    } else {
      // Login flow
      if (!this.email || !this.password) {
        this.errorMessage = 'Please enter your email and password.';
        return;
      }

      this.isLoading = true;
      this.authService.login({
        email: this.email,
        password: this.password
      }).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.successMessage = 'Signed in successfully! Redirecting...';
            setTimeout(() => {
              this.router.navigate(['/']);
            }, 1500);
          } else {
            this.errorMessage = res.message || 'Login failed.';
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = this.extractErrorMessage(err);
        }
      });
    }
  }

  private extractErrorMessage(err: any): string {
    if (err.error) {
      if (err.error.errors && err.error.errors.length > 0) {
        return err.error.errors[0].message;
      }
      if (err.error.message) {
        return err.error.message;
      }
    }
    return err.message || 'An unexpected error occurred. Please try again.';
  }

  checks: PasswordChecks = { length: false, symbol: false, upper: false, number: false };

  matchState: 'match' | 'mismatch' | null = null;

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPassword(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  onPasswordInput(value: string): void {
    this.password = value;
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
    this.validatePasswordMatch();
  }

  private validatePasswordMatch(): void {
    if (!this.confirmPassword) {
      this.matchState = null;
      return;
    }
    this.matchState = this.password === this.confirmPassword ? 'match' : 'mismatch';
  }

  get strengthScore(): number {
    return Object.values(this.checks).filter(Boolean).length;
  }

  get strengthBarClass(): string {
    const base = 'h-full transition-all duration-500';
    if (this.strengthScore <= 1) return `${base} bg-error`;
    if (this.strengthScore <= 3) return `${base} bg-secondary-fixed-dim`;
    return `${base} bg-tertiary`;
  }

  checkClass(active: boolean): string {
    return active
      ? 'flex items-center gap-2 opacity-100 text-tertiary transition-opacity'
      : 'flex items-center gap-2 opacity-60 text-on-surface-variant transition-opacity';
  }
}
