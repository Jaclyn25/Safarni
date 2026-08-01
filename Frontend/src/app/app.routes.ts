import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/explore/explore.component').then((m) => m.ExploreComponent),
    title: 'Safarni - Discover Your Next Journey'
  },
  {
    path: 'planner/:tripId',
    loadComponent: () =>
      import('./pages/planner/planner.component').then((m) => m.PlannerComponent),
    canActivate: [authGuard],
    title: 'Safarni | Trip Planner'
  },
  {
    path: 'planner',
    loadComponent: () =>
      import('./pages/planner/planner.component').then((m) => m.PlannerComponent),
    canActivate: [authGuard],
    title: 'Safarni | Trip Planner'
  },
  {
    path: 'bookings',
    loadComponent: () =>
      import('./pages/bookings/bookings.component').then((m) => m.BookingsComponent),
    canActivate: [authGuard],
    title: 'Safarni | Your Bookings'
  },
  {
    path: 'concierge',
    loadComponent: () =>
      import('./pages/concierge/concierge.component').then((m) => m.ConciergeComponent),
    canActivate: [authGuard],
    title: 'Safarni | Travel Concierge'
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register.component').then((m) => m.RegisterComponent),
    data: { isLogin: false },
    title: 'Safarni | Start Your Journey'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/register/register.component').then((m) => m.RegisterComponent),
    data: { isLogin: true },
    title: 'Safarni | Sign In'
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/forgot-password/forgot-password.component').then(
        (m) => m.ForgotPasswordComponent
      ),
    title: 'Safarni | Reset Your Password'
  },
  {
    path: 'check-email',
    loadComponent: () =>
      import('./pages/check-email/check-email.component').then((m) => m.CheckEmailComponent),
    title: 'Safarni | Check Your Email'
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./pages/reset-password/reset-password.component').then(
        (m) => m.ResetPasswordComponent
      ),
    title: 'Safarni | Create New Password'
  },
  {
    path: 'password-updated',
    loadComponent: () =>
      import('./pages/password-updated/password-updated.component').then(
        (m) => m.PasswordUpdatedComponent
      ),
    title: 'Safarni | Password Updated'
  },
  {
    path: 'verification-email-sent',
    loadComponent: () =>
      import('./pages/verification-email-sent/verification-email-sent.component').then(
        (m) => m.VerificationEmailSentComponent
      ),
    title: 'Safarni | Verify Your Email'
  },
  {
    path: 'verify-email',
    loadComponent: () =>
      import('./pages/verify-email/verify-email.component').then(
        (m) => m.VerifyEmailComponent
      ),
    title: 'Safarni | Email Verification'
  },
  {
    path: 'profile',
    loadComponent: () =>
      import('./pages/profile/layout/profile-layout.component').then(
        (m) => m.ProfileLayoutComponent
      ),
    canActivate: [authGuard],
    title: 'Safarni | My Profile',
    children: [
      { path: '', redirectTo: 'account', pathMatch: 'full' },
      {
        path: 'account',
        loadComponent: () =>
          import('./pages/profile/sections/profile-account.component').then(
            (m) => m.ProfileAccountComponent
          ),
        title: 'Safarni | Account'
      },
      {
        path: 'trips',
        loadComponent: () =>
          import('./pages/profile/sections/profile-trips.component').then(
            (m) => m.ProfileTripsComponent
          ),
        title: 'Safarni | My Trips'
      },
      {
        path: 'password',
        loadComponent: () =>
          import('./pages/profile/sections/profile-password.component').then(
            (m) => m.ProfilePasswordComponent
          ),
        title: 'Safarni | Password'
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./pages/profile/sections/profile-notifications.component').then(
            (m) => m.ProfileNotificationsComponent
          ),
        title: 'Safarni | Notifications'
      },
      {
        path: 'privacy',
        loadComponent: () =>
          import('./pages/profile/sections/profile-notifications.component').then(
            (m) => m.ProfileNotificationsComponent
          ),
        title: 'Safarni | Privacy'
      },
      {
        path: 'travel-preferences',
        loadComponent: () =>
          import('./pages/profile/sections/profile-travel-preferences.component').then(
            (m) => m.ProfileTravelPreferencesComponent
          ),
        title: 'Safarni | Travel Preferences'
      },
      {
        path: 'theme',
        loadComponent: () =>
          import('./pages/profile/sections/profile-theme.component').then(
            (m) => m.ProfileThemeComponent
          ),
        title: 'Safarni | Appearance'
      },
      {
        path: 'language',
        loadComponent: () =>
          import('./pages/profile/sections/profile-language.component').then(
            (m) => m.ProfileLanguageComponent
          ),
        title: 'Safarni | Language'
      },
      {
        path: 'currency',
        loadComponent: () =>
          import('./pages/profile/sections/profile-currency.component').then(
            (m) => m.ProfileCurrencyComponent
          ),
        title: 'Safarni | Currency'
      },
      {
        path: 'security',
        loadComponent: () =>
          import('./pages/profile/sections/profile-security.component').then(
            (m) => m.ProfileSecurityComponent
          ),
        title: 'Safarni | Security'
      },
      {
        path: 'connected-accounts',
        loadComponent: () =>
          import('./pages/profile/sections/profile-connected-accounts.component').then(
            (m) => m.ProfileConnectedAccountsComponent
          ),
        title: 'Safarni | Connected Accounts'
      }
    ]
  },
  {
    path: 'destination/:id',
    loadComponent: () =>
      import('./pages/destination/destination.component').then(
        (m) => m.DestinationComponent
      ),
    title: 'Safarni | Destination Guide'
  },
  { path: '**', redirectTo: '' }
];
