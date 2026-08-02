import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserRole } from '../models/admin.models';

const ADMIN_ROLES: UserRole[] = ['Admin', 'Support', 'ContentManager', 'Analyst'];

function decodeJwtRole(token: string): UserRole | null {
  try {
    const payloadPart = token.split('.')[1];
    if (!payloadPart) {
      return null;
    }
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
      return (roleClaim.find((value) => ADMIN_ROLES.includes(value as UserRole)) as UserRole) ?? null;
    }
    if (typeof roleClaim === 'string' && ADMIN_ROLES.includes(roleClaim as UserRole)) {
      return roleClaim as UserRole;
    }
    return null;
  } catch {
    return null;
  }
}

export const adminGuard: CanActivateFn = () => {
  const router = inject(Router);
  const token = localStorage.getItem('access_token');
  const role = token ? decodeJwtRole(token) : null;
  if (!role) {
    return router.createUrlTree(['/']);
  }
  return true;
};
