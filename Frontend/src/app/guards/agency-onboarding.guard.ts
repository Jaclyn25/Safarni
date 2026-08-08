import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AgencyService } from '../services/agency.service';

export const agencyOnboardingGuard: CanActivateFn = () => {
  const agencies = inject(AgencyService);
  const router = inject(Router);

  return agencies.getOnboardingStatus().pipe(
    map((status) =>
      status.isCompanyProfileComplete
        ? true
        : router.createUrlTree(['/agency/setup'])
    ),
    catchError(() => of(router.createUrlTree(['/agency/setup'])))
  );
};
