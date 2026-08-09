import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const noAdminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.hasAdminRole()) {
    return router.createUrlTree(['/admin']);
  }
  if (authService.isTravelAgency()) {
    return router.createUrlTree(['/agency']);
  }
  return true;
};
