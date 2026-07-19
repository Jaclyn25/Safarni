import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/explore/explore.component').then((m) => m.ExploreComponent),
    title: 'Safarni - Discover Your Next Journey'
  },
  {
    path: 'planner',
    loadComponent: () =>
      import('./pages/planner/planner.component').then((m) => m.PlannerComponent),
    title: 'Safarni - 7-Day Tokyo Adventure'
  },
  {
    path: 'bookings',
    loadComponent: () =>
      import('./pages/bookings/bookings.component').then((m) => m.BookingsComponent),
    title: 'Safarni | Your Bookings'
  },
  {
    path: 'concierge',
    loadComponent: () =>
      import('./pages/concierge/concierge.component').then((m) => m.ConciergeComponent),
    title: 'Safarni | AI Concierge'
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
  { path: '**', redirectTo: '' }
];
