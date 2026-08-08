import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const adminAgencyGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return ['Admin', 'Support'].includes(auth.getUserRole() ?? '')
    ? true
    : router.createUrlTree(['/admin']);
};
