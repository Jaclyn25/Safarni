import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface ApiError {
  code: string;
  message: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: ApiError[];
  statusCode: number;
  timestamp: string;
}

export interface RegisterUserResponse {
  id: string;
  fullName: string;
  email: string;
  createdAt: string;
  isActive: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  userId: string;
  fullName: string;
  email: string;
}

export interface PasswordRecoveryResponse {
  message: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface VerifyEmailRequest {
  email: string;
  token: string;
}

export interface VerifyEmailResponse {
  message: string;
}
  export type AuthOperation = 'login' | 'register';
export interface GoogleLoginRequest {
  idToken: string;
}
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  /**
   * Relative URL keeps the frontend environment-agnostic.
   * In development, proxy.conf.json forwards /api to http://localhost:5110.
   * In production, route /api to the ASP.NET API through your web server.
   */
  private readonly apiUrl = '/api/Auth';

  constructor(private readonly http: HttpClient) {}

  register(command: {
    fullName: string;
    email: string;
    password: string;
    confirmPassword: string;
  }): Observable<ApiResponse<RegisterUserResponse>> {
    return this.http.post<ApiResponse<RegisterUserResponse>>(`${this.apiUrl}/register`, command);
  }

  login(command: { email: string; password: string }): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.apiUrl}/login`, command).pipe(
      tap((response) => {
        if (response.success && response.data) {
          this.saveSession(response.data);
        }
      })
    );
  }
  googleLogin(
  command: GoogleLoginRequest
): Observable<ApiResponse<LoginResponse>> {
  return this.http
    .post<ApiResponse<LoginResponse>>(
      `${this.apiUrl}/google-login`,
      command
    )
    .pipe(
      tap((response) => {
        if (
          response.success &&
          response.data
        ) {
          this.saveSession(response.data);
        }
      })
    );
}

  /**
   * Exchanges the stored refresh token for a new access/refresh pair.
   * The backend rotates tokens, so both values are replaced.
   * The `user` blob is preserved because the refresh response does not
   * include profile data.
   */
  refreshSession(): Observable<ApiResponse<LoginResponse>> {
    const refreshToken = localStorage.getItem('refresh_token');

    return this.http.post<ApiResponse<LoginResponse>>(
      `${this.apiUrl}/refresh-token`,
      { refreshToken }
    ).pipe(
      tap((response) => {
        if (response.success && response.data) {
          this.saveSession(response.data);
        }
      })
    );
  }

  forgotPassword(command: ForgotPasswordRequest): Observable<ApiResponse<PasswordRecoveryResponse>> {
    return this.http.post<ApiResponse<PasswordRecoveryResponse>>(
      `${this.apiUrl}/forgot-password`,
      command
    );
  }

  resetPassword(command: ResetPasswordRequest): Observable<ApiResponse<PasswordRecoveryResponse>> {
    return this.http.post<ApiResponse<PasswordRecoveryResponse>>(
      `${this.apiUrl}/reset-password`,
      command
    );
  }

  verifyEmail(command: VerifyEmailRequest): Observable<ApiResponse<VerifyEmailResponse>> {
    return this.http.post<ApiResponse<VerifyEmailResponse>>(
      `${this.apiUrl}/verify-email`,
      command
    );
  }

  /**
   * Changes the signed-in user's password.
   * NOTE: unlike the other auth endpoints, the backend returns the
   * raw ChangePasswordResponse here (no ApiResponse<T> envelope).
   */
  changePassword(command: {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
  }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/change-password`,
      command
    );
  }

  logout(): void {
    const accessToken = localStorage.getItem('access_token');
    const refreshToken = localStorage.getItem('refresh_token');

    // Clear session IMMEDIATELY so UI updates instantly
    this.clearSession();

    if (accessToken && refreshToken) {
      const headers = new HttpHeaders({ Authorization: `Bearer ${accessToken}` });
      // Fire-and-forget: notify backend but don't block UI
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }, { headers }).subscribe();
    }
  }

  isAuthenticated(): boolean {
    return Boolean(localStorage.getItem('access_token'));
  }

  getCurrentUser(): { userId: string; fullName: string; email: string } | null {
    const user = localStorage.getItem('user');
    return user ? (JSON.parse(user) as { userId: string; fullName: string; email: string }) : null;
  }

  getUserRole(): string | null {
    const token = localStorage.getItem('access_token');
    if (!token) return null;
    try {
      const payloadPart = token.split('.')[1];
      if (!payloadPart) return null;
      const normalized = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
      const payload = JSON.parse(
        decodeURIComponent(
          atob(padded)
            .split('')
            .map((char) => '%' + ('00' + char.charCodeAt(0).toString(16)).slice(-2))
            .join('')
        )
      );
      const roleClaim = payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (Array.isArray(roleClaim)) {
        return roleClaim.find((r: string) => ['Admin', 'Support', 'ContentManager', 'Analyst'].includes(r)) ?? roleClaim[0] ?? null;
      }
      return typeof roleClaim === 'string' ? roleClaim : null;
    } catch {
      return null;
    }
  }

  hasAdminRole(): boolean {
    const role = this.getUserRole();
    return Boolean(role && ['Admin', 'Support', 'ContentManager', 'Analyst'].includes(role));
  }

  private saveSession(data: LoginResponse): void {
    localStorage.setItem('access_token', data.accessToken);
    localStorage.setItem('refresh_token', data.refreshToken);
    localStorage.setItem(
      'user',
      JSON.stringify({
        userId: data.userId,
        fullName: data.fullName,
        email: data.email
      })
    );
  }

  private clearSession(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
  }
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const apiResponse = error.error as Partial<ApiResponse<unknown>> | null;
    const firstError = apiResponse?.errors?.[0]?.message;

    if (firstError) {
      return firstError;
    }

    if (apiResponse?.message) {
      return apiResponse.message;
    }

    if (error.status === 0) {
      return 'The API is unavailable. Make sure the backend is running on http://localhost:5110.';
    }
  }

  return fallback;
}

export function getAuthFormErrorMessage(
  error: unknown,
  operation: AuthOperation
): string {
  const fallback =
    operation === 'login'
      ? 'We could not sign you in. Please try again.'
      : 'We could not create your account. Please try again.';

  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  const response =
    error.error &&
    typeof error.error === 'object'
      ? (error.error as Partial<ApiResponse<unknown>>)
      : null;

  const firstApiError =
    response?.errors && response.errors.length > 0
      ? response.errors[0]
      : null;

  /*
   * Map backend error codes to clear, user-friendly messages.
   */
  switch (firstApiError?.code) {
    case 'Auth.InvalidCredentials':
      /*
       * Do not reveal whether the email exists.
       * This avoids account-enumeration security problems.
       */
      return 'The email address or password you entered is incorrect.';

    case 'Auth.EmailNotVerified':
      return 'Please verify your email address before signing in. Check your inbox for the verification email.';

    case 'Auth.InactiveAccount':
      return 'This account is currently inactive. Please contact support for assistance.';

    case 'User.EmailAlreadyExists':
      return 'An account already exists with this email address. Try signing in instead.';

    default:
      break;
  }

  /*
   * Return backend validation messages when available.
   */
  const backendMessage = firstApiError?.message?.trim();

  if (backendMessage) {
    return backendMessage;
  }

  /*
   * Handle HTTP failures when an error code was not returned.
   */
  switch (error.status) {
    case 0:
      return 'We could not connect to the server. Make sure the backend is running and try again.';

    case 400:
      return operation === 'login'
        ? 'Please check your email and password and try again.'
        : 'Please check your account information and try again.';

    case 401:
      return 'The email address or password you entered is incorrect.';

    case 403:
      return 'You cannot sign in to this account yet. Make sure your email has been verified.';

    case 404:
      return 'The authentication service could not be reached. Please try again later.';

    case 409:
      return 'An account already exists with this email address. Try signing in instead.';

    default:
      if (error.status >= 500) {
        return 'The server encountered a problem. Please try again in a moment.';
      }

      return fallback;
  }
}
