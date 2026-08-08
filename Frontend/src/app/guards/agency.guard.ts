import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const agencyGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.getUserRole() === 'TravelAgency') {
    return true;
  }

  return router.createUrlTree([authService.hasAdminRole() ? '/admin' : '/']);
};
