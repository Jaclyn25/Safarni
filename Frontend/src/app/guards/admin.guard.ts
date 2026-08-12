import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { extractAdminRole } from '../shared/utils/jwt';

export const adminGuard: CanActivateFn = () => {
  const router = inject(Router);
  const token = localStorage.getItem('access_token');
  const role = token ? extractAdminRole(token) : null;
  if (!role) {
    return router.createUrlTree(['/']);
  }
  return true;
};