import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Route guard that blocks access to protected pages
 * (planner, bookings, concierge, profile) for anonymous users.
 *
 * Authenticated users pass through. Anonymous users are sent to
 * /login with a `returnUrl` query parameter so the login flow can
 * send them back to the page they originally tried to open.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};
