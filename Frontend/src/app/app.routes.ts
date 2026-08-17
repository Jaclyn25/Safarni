import { Routes } from '@angular/router';

import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';
import { noAdminGuard } from './guards/no-admin.guard';
import { agencyGuard } from './guards/agency.guard';
import { adminAgencyGuard } from './guards/admin-agency.guard';
import { agencyOnboardingGuard } from './guards/agency-onboarding.guard';

export const routes: Routes = [
  // =========================
  // Explore
  // =========================

  {
    path: '',
    loadComponent: () =>
      import('./pages/explore/explore.component')
        .then((m) => m.ExploreComponent),
    title: 'Safarni - Discover Your Next Journey'
  },

  // =========================
  // Public Trips
  // =========================

  {
    path: 'community',
    loadComponent: () =>
      import('./pages/community/community.component')
        .then((m) => m.CommunityComponent),
    title: 'Safarni | Community'
  },

  {
    path: 'community/:postId',
    loadComponent: () =>
      import('./pages/community-post-details/community-post-details.component')
        .then((m) => m.CommunityPostDetailsComponent),
    title: 'Safarni | Community Post'
  },

  {
    path: 'public-trips',
    loadComponent: () =>
      import('./pages/public-trips/public-trips.component')
        .then((m) => m.PublicTripsComponent),
    title: 'Safarni | Search Public Trips'
  },

  {
    path: 'public-trips/:versionId',
    loadComponent: () =>
      import('./pages/public-trip-details/public-trip-details.component')
        .then((m) => m.PublicTripDetailsComponent),
    title: 'Safarni | Public Trip Details'
  },

  // =========================
  // Planner
  // =========================

  {
    path: 'planner/:tripId',
    loadComponent: () =>
      import('./pages/planner/planner.component')
        .then((m) => m.PlannerComponent),
    canActivate: [authGuard],
    title: 'Safarni | Trip Planner'
  },

  {
    path: 'planner',
    loadComponent: () =>
      import('./pages/planner/planner.component')
        .then((m) => m.PlannerComponent),
    canActivate: [authGuard],
    title: 'Safarni | Trip Planner'
  },

  // =========================
  // Agency Trips
  // =========================

  {
    path: 'agency-trips',
    redirectTo: 'profile/agency-trips',
    pathMatch: 'full',
    title: 'Safarni | My Agency Trips'
  },

  // =========================
  // Marketplace
  // =========================

  {
    path: 'marketplace',
    pathMatch: 'full',
    loadComponent: () =>
      import('./pages/marketplace/marketplace.component')
        .then((m) => m.MarketplaceComponent),
    title: 'Safarni | Agency Marketplace'
  },

  {
    path: 'marketplace/packages/:packageId',
    loadComponent: () =>
      import('./pages/marketplace/package-details.component')
        .then((m) => m.PackageDetailsComponent),
    title: 'Safarni | Package Details'
  },

  {
    path: 'marketplace/agencies/:agencyId',
    loadComponent: () =>
      import('./pages/marketplace/agency-public-profile.component')
        .then((m) => m.AgencyPublicProfileComponent),
    title: 'Safarni | Travel Agency'
  },

  // =========================
  // Pricing
  // =========================

  {
    path: 'pricing/checkout',
    loadComponent: () =>
      import('./pages/pricing/checkout.component')
        .then((m) => m.CheckoutComponent),
    canActivate: [authGuard],
    title: 'Safarni | Checkout'
  },

  {
    path: 'pricing',
    loadComponent: () =>
      import('./pages/pricing/pricing.component')
        .then((m) => m.PricingComponent),
    title: 'Safarni | Pricing'
  },

  // =========================
  // Concierge
  // =========================

  {
    path: 'concierge',
    loadComponent: () =>
      import('./pages/concierge/concierge.component')
        .then((m) => m.ConciergeComponent),
    canActivate: [authGuard],
    title: 'Safarni | Travel Concierge'
  },

  // =========================
  // Authentication
  // =========================

  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register.component')
        .then((m) => m.RegisterComponent),
    data: { isLogin: false },
    title: 'Safarni | Start Your Journey'
  },

  {
    path: 'login',
    loadComponent: () =>
      import('./pages/register/register.component')
        .then((m) => m.RegisterComponent),
    data: { isLogin: true },
    title: 'Safarni | Sign In'
  },

  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/forgot-password/forgot-password.component')
        .then((m) => m.ForgotPasswordComponent),
    title: 'Safarni | Reset Your Password'
  },

  {
    path: 'check-email',
    loadComponent: () =>
      import('./pages/check-email/check-email.component')
        .then((m) => m.CheckEmailComponent),
    title: 'Safarni | Check Your Email'
  },

  {
    path: 'reset-password',
    loadComponent: () =>
      import('./pages/reset-password/reset-password.component')
        .then((m) => m.ResetPasswordComponent),
    title: 'Safarni | Create New Password'
  },

  {
    path: 'password-updated',
    loadComponent: () =>
      import('./pages/password-updated/password-updated.component')
        .then((m) => m.PasswordUpdatedComponent),
    title: 'Safarni | Password Updated'
  },

  {
    path: 'verification-email-sent',
    loadComponent: () =>
      import('./pages/verification-email-sent/verification-email-sent.component')
        .then((m) => m.VerificationEmailSentComponent),
    title: 'Safarni | Verify Your Email'
  },

  {
    path: 'verify-email',
    loadComponent: () =>
      import('./pages/verify-email/verify-email.component')
        .then((m) => m.VerifyEmailComponent),
    title: 'Safarni | Email Verification'
  },

  // =========================
  // Agency
  // =========================

  {
    path: 'agency/setup',
    loadComponent: () =>
      import('./pages/agency/onboarding/agency-onboarding.component')
        .then((m) => m.AgencyOnboardingComponent),
    canActivate: [
      authGuard,
      agencyGuard
    ],
    title: 'Safarni | Complete Agency Setup'
  },

  {
    path: 'agency',
    loadComponent: () =>
      import('./pages/agency/layout/agency-layout.component')
        .then((m) => m.AgencyLayoutComponent),
    canActivate: [
      authGuard,
      agencyGuard,
      agencyOnboardingGuard
    ],
    title: 'Safarni | Agency Portal',

    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./pages/agency/dashboard/agency-dashboard.component')
            .then((m) => m.AgencyDashboardComponent),
        title: 'Safarni | Agency Dashboard'
      },

      {
        path: 'verification',
        loadComponent: () =>
          import('./pages/agency/verification/agency-verification.component')
            .then((m) => m.AgencyVerificationComponent),
        title: 'Safarni | Agency Verification'
      },

      {
        path: 'profile',
        loadComponent: () =>
          import('./pages/agency/profile/agency-profile.component')
            .then((m) => m.AgencyProfileComponent),
        title: 'Safarni | Company Profile'
      },

      {
        path: 'packages',
        loadComponent: () =>
          import('./pages/agency/packages/agency-packages.component')
            .then((m) => m.AgencyPackagesComponent),
        title: 'Safarni | Agency Packages'
      },

      {
        path: 'booking-requests',
        loadComponent: () =>
          import('./pages/agency/booking-requests/agency-booking-requests.component')
            .then((m) => m.AgencyBookingRequestsComponent),
        title: 'Safarni | Booking Requests'
      },

      {
        path: 'pricing',
        loadComponent: () =>
          import('./pages/pricing/pricing.component')
            .then((m) => m.PricingComponent),
        data: { customerType: 'Agency' },
        title: 'Safarni | Agency Pricing'
      }
    ]
  },

  // =========================
  // Profile
  // =========================

  {
    path: 'profile',
    loadComponent: () =>
      import('./pages/profile/layout/profile-layout.component')
        .then((m) => m.ProfileLayoutComponent),
    canActivate: [
      authGuard,
      noAdminGuard
    ],
    title: 'Safarni | My Profile',

    children: [
      {
        path: '',
        redirectTo: 'account',
        pathMatch: 'full'
      },

      {
        path: 'account',
        loadComponent: () =>
          import('./pages/profile/sections/profile-account.component')
            .then((m) => m.ProfileAccountComponent),
        title: 'Safarni | Account'
      },

      {
        path: 'trips',
        loadComponent: () =>
          import('./pages/profile/sections/profile-trips.component')
            .then((m) => m.ProfileTripsComponent),
        title: 'Safarni | My Trips'
      },

      {
        path: 'published-trips',
        loadComponent: () =>
          import('./pages/profile/sections/profile-published-trips.component')
            .then((m) => m.ProfilePublishedTripsComponent),
        title: 'Safarni | Published Trips'
      },

      {
        path: 'agency-trips',
        loadComponent: () =>
          import('./pages/agency-trips/agency-trips.component')
            .then((m) => m.AgencyTripsComponent),
        title: 'Safarni | Agency Trips'
      },

      {
        path: 'password',
        loadComponent: () =>
          import('./pages/profile/sections/profile-password.component')
            .then((m) => m.ProfilePasswordComponent),
        title: 'Safarni | Password'
      },

      {
        path: 'notifications',
        loadComponent: () =>
          import('./pages/profile/sections/profile-notifications.component')
            .then((m) => m.ProfileNotificationsComponent),
        title: 'Safarni | Notifications'
      },

      {
        path: 'privacy',
        loadComponent: () =>
          import('./pages/profile/sections/profile-privacy.component')
            .then((m) => m.ProfilePrivacyComponent),
        title: 'Safarni | Privacy Policy'
      },

      {
        path: 'travel-preferences',
        loadComponent: () =>
          import('./pages/profile/sections/profile-travel-preferences.component')
            .then((m) => m.ProfileTravelPreferencesComponent),
        title: 'Safarni | Travel Preferences'
      },

      {
        path: 'theme',
        loadComponent: () =>
          import('./pages/profile/sections/profile-theme.component')
            .then((m) => m.ProfileThemeComponent),
        title: 'Safarni | Appearance'
      },

      {
        path: 'language',
        loadComponent: () =>
          import('./pages/profile/sections/profile-language.component')
            .then((m) => m.ProfileLanguageComponent),
        title: 'Safarni | Language'
      },

      {
        path: 'currency',
        loadComponent: () =>
          import('./pages/profile/sections/profile-currency.component')
            .then((m) => m.ProfileCurrencyComponent),
        title: 'Safarni | Currency'
      },

      {
        path: 'security',
        loadComponent: () =>
          import('./pages/profile/sections/profile-security.component')
            .then((m) => m.ProfileSecurityComponent),
        title: 'Safarni | Security'
      },

      {
        path: 'connected-accounts',
        loadComponent: () =>
          import('./pages/profile/sections/profile-connected-accounts.component')
            .then((m) => m.ProfileConnectedAccountsComponent),
        title: 'Safarni | Connected Accounts'
      }
    ]
  },

  // =========================
  // Destination
  // =========================

  {
    path: 'destination/:id',
    loadComponent: () =>
      import('./pages/destination/destination.component')
        .then((m) => m.DestinationComponent),
    title: 'Safarni | Destination Guide'
  },

  // =========================
  // Admin
  // =========================

  {
    path: 'admin',
    loadComponent: () =>
      import('./pages/admin/layout/admin-layout.component')
        .then((m) => m.AdminLayoutComponent),
    canActivate: [
      authGuard,
      adminGuard
    ],
    title: 'Safarni | Admin',

    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./pages/admin/dashboard/dashboard.component')
            .then((m) => m.AdminDashboardComponent),
        title: 'Safarni | Admin Dashboard'
      },

      {
        path: 'users',
        loadComponent: () =>
          import('./pages/admin/users/users.component')
            .then((m) => m.AdminUsersComponent),
        title: 'Safarni | Users'
      },

      {
        path: 'agencies',
        canActivate: [adminAgencyGuard],
        loadComponent: () =>
          import('./pages/admin/agencies/agencies.component')
            .then((m) => m.AdminAgenciesComponent),
        title: 'Safarni | Agency Applications'
      },

      {
        path: 'agencies/:agencyId',
        canActivate: [adminAgencyGuard],
        loadComponent: () =>
          import('./pages/admin/agencies/agency-review.component')
            .then((m) => m.AdminAgencyReviewComponent),
        title: 'Safarni | Agency Review'
      },

      {
        path: 'knowledge',
        loadComponent: () =>
          import('./pages/admin/knowledge/knowledge.component')
            .then((m) => m.AdminKnowledgeComponent),
        title: 'Safarni | Knowledge Base'
      },

      {
        path: 'ai-activity',
        loadComponent: () =>
          import('./pages/admin/ai-activity/ai-activity.component')
            .then((m) => m.AdminAiActivityComponent),
        title: 'Safarni | AI Activity'
      },

      {
        path: 'integrations',
        loadComponent: () =>
          import('./pages/admin/integrations/integrations.component')
            .then((m) => m.AdminIntegrationsComponent),
        title: 'Safarni | Integrations'
      },

      {
        path: 'analytics',
        loadComponent: () =>
          import('./pages/admin/analytics/analytics.component')
            .then((m) => m.AdminAnalyticsComponent),
        title: 'Safarni | Analytics'
      },

      {
        path: 'billing',
        loadComponent: () =>
          import('./pages/admin/billing/admin-billing.component')
            .then((m) => m.AdminBillingComponent),
        title: 'Safarni | Billing'
      },

      {
        path: 'roles',
        loadComponent: () =>
          import('./pages/admin/roles/roles.component')
            .then((m) => m.AdminRolesComponent),
        title: 'Safarni | Roles & Permissions'
      },

      {
        path: 'audit-logs',
        loadComponent: () =>
          import('./pages/admin/audit-logs/audit-logs.component')
            .then((m) => m.AdminAuditLogsComponent),
        title: 'Safarni | Audit Logs'
      }
    ]
  },

  // =========================
  // Static Pages
  // =========================

  {
    path: 'about',
    loadComponent: () =>
      import('./pages/static/about/about.component')
        .then((m) => m.AboutComponent),
    title: 'Safarni | About Us',
    data: {
      description:
        'Safarni is an AI-powered travel companion that turns scattered ideas into personalized itineraries, backed by a marketplace of verified travel agencies.'
    }
  },

  {
    path: 'journal',
    loadComponent: () =>
      import('./pages/static/journal/journal.component')
        .then((m) => m.JournalComponent),
    title: 'Safarni | Journal',
    data: {
      description:
        'Travel stories, practical guides, and thinking from the Safarni team on smarter trip planning.'
    }
  },

  {
    path: 'contact',
    loadComponent: () =>
      import('./pages/static/contact/contact.component')
        .then((m) => m.ContactComponent),
    title: 'Safarni | Contact Us',
    data: {
      description:
        'Questions, feedback, or partnership ideas? Reach the Safarni team and we will respond within one business day.'
    }
  },

  {
    path: 'privacy',
    loadComponent: () =>
      import('./pages/static/privacy/privacy.component')
        .then((m) => m.PrivacyComponent),
    title: 'Safarni | Privacy Policy',
    data: {
      description:
        'How Safarni collects, uses, and protects your personal information when you use our travel planning platform.'
    }
  },

  {
    path: 'terms',
    loadComponent: () =>
      import('./pages/static/terms/terms.component')
        .then((m) => m.TermsComponent),
    title: 'Safarni | Terms of Service',
    data: {
      description:
        'The terms and conditions that govern your use of Safarni\'s travel planning platform and marketplace.'
    }
  },

  {
    path: 'cookies',
    loadComponent: () =>
      import('./pages/static/cookies/cookies.component')
        .then((m) => m.CookiesComponent),
    title: 'Safarni | Cookie Policy',
    data: {
      description:
        'How Safarni uses cookies and similar technologies, and how you can manage your preferences.'
    }
  },

  // =========================
  // Wildcard
  // =========================

  {
    path: '**',
    redirectTo: ''
  }
];
