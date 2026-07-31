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
      import('./pages/profile/profile.component').then(
        (m) => m.ProfileComponent
      ),
    canActivate: [authGuard],
    title: 'Safarni | My Profile'
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
