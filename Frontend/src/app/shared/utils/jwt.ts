export const ADMIN_ROLES = ['Admin', 'Support', 'ContentManager', 'Analyst'] as const;

export type AdminRole = (typeof ADMIN_ROLES)[number];

export function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const payloadPart = token.split('.')[1];
    if (!payloadPart) {
      return null;
    }
    const normalized = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(
      normalized.length + ((4 - (normalized.length % 4)) % 4),
      '='
    );
    return JSON.parse(
      decodeURIComponent(
        atob(padded)
          .split('')
          .map((char) => '%' + ('00' + char.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      )
    );
  } catch {
    return null;
  }
}

/**
 * Returns the role claims from a JWT payload, handling both the
 * "role" claim and the legacy Microsoft schema claim, in either
 * string or array form.
 */
export function extractJwtRoles(payload: Record<string, unknown> | null): string[] {
  if (!payload) {
    return [];
  }
  const roleClaim =
    payload['role'] ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
  if (Array.isArray(roleClaim)) {
    return roleClaim.filter((value): value is string => typeof value === 'string');
  }
  return typeof roleClaim === 'string' ? [roleClaim] : [];
}

/**
 * Decodes the JWT and returns the first admin role it contains,
 * or null when the token is invalid or carries no admin role.
 */
export function extractAdminRole(token: string): AdminRole | null {
  const roles = extractJwtRoles(decodeJwtPayload(token));
  return (roles.find((value) => ADMIN_ROLES.includes(value as AdminRole)) as AdminRole) ?? null;
}