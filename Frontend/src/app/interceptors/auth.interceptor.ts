import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, map, of, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Endpoints that legitimately return 401/400 during the auth flow
 * (failed login, expired verification link, etc.) and must NOT
 * trigger the refresh or the session-expiry redirect.
 */
const PUBLIC_AUTH_PATHS = [
  '/api/Auth/login',
  '/api/Auth/google-login',
  '/api/Auth/refresh-token',
  '/api/Auth/register',
  '/api/Auth/forgot-password',
  '/api/Auth/reset-password',
  '/api/Auth/verify-email'
];

const SESSION_KEYS = ['access_token', 'refresh_token', 'user'] as const;

/**
 * Single-flight refresh: concurrent 401s share one refresh call instead
 * of each triggering their own.
 */
let refreshInFlight: Promise<boolean> | null = null;

function tryRefreshSession(authService: AuthService): Promise<boolean> {
  if (!refreshInFlight) {
    refreshInFlight = firstValueFromRefresh(authService).finally(() => {
      refreshInFlight = null;
    });
  }

  return refreshInFlight;
}

function firstValueFromRefresh(authService: AuthService): Promise<boolean> {
  return new Promise((resolve) => {
    authService.refreshSession().subscribe({
      next: () => resolve(true),
      error: () => resolve(false)
    });
  });
}

function clearSessionAndRedirect(router: Router): void {
  for (const key of SESSION_KEYS) {
    localStorage.removeItem(key);
  }

  const currentUrl = router.url;
  if (!currentUrl.startsWith('/login')) {
    void router.navigate(['/login'], {
      queryParams: { returnUrl: currentUrl }
    });
  }
}

/**
 * Attaches the bearer token to API calls and transparently refreshes an
 * expired access token once before giving up. If the refresh token is
 * also rejected, the session is cleared and the user is sent to login.
 */
export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  const isAuthFlow = PUBLIC_AUTH_PATHS.some((path) =>
    req.url.includes(path)
  );
  const accessToken = localStorage.getItem('access_token');

  let authReq = req;
  if (!isAuthFlow && accessToken) {
    authReq = req.clone({
      setHeaders: { Authorization: `Bearer ${accessToken}` }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isAuthFlow && accessToken) {
        return from(tryRefreshSession(authService)).pipe(
          switchMap((refreshed) => {
            if (refreshed) {
              const newToken = localStorage.getItem('access_token');
              return next(
                req.clone({
                  setHeaders: { Authorization: `Bearer ${newToken}` }
                })
              );
            }

            clearSessionAndRedirect(router);
            return throwError(() => error);
          })
        );
      }

      return throwError(() => error);
    })
  );
};
