import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/landing/landing').then((m) => m.Landing),
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'sign-up',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/sign-up/sign-up').then((m) => m.SignUp),
  },
  {
    path: 'account-setup',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/account-setup/account-setup/account-setup').then((m) => m.AccountSetup),
  },
  {
    path: 'on-boarding',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/on-boarding/rawaj-onboarding/rawaj-onboarding').then((m) => m.RawajOnboarding),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/dashboard/crm-page/crm-page').then((m) => m.CrmPage),
      },
      {
        path: 'campaigns',
        loadComponent: () =>
          import('./features/campaigns/campaigns-page/campaigns-page').then((m) => m.CampaignsPage),
      },
      {
        path: 'competitors',
        loadComponent: () =>
          import('./features/competitors/competitors-page/competitors-page').then((m) => m.CompetitorsPage),
      },
      {
        path: 'calendar',
        loadComponent: () =>
          import('./features/calendar/calendar-page/calendar-page').then((m) => m.CalendarPage),
      },
      {
        path: 'content-gen',
        loadComponent: () =>
          import('./features/content-gen/content-gen-page/content-gen-page').then((m) => m.ContentGenPage),
      },
      {
        path: 'my-media',
        loadComponent: () =>
          import('./features/my-media/my-media-page/my-media-page').then((m) => m.MyMediaPage),
      },
      {
        path: 'marketing-plan',
        loadComponent: () =>
          import('./features/marketing-plan/marketing-plan-page/marketing-plan-page').then((m) => m.MarketingPlanPage),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/dashboard/users/users-page/users-page').then((m) => m.UsersPage),
      },
      {
        path: 'users/:id',
        loadComponent: () =>
          import('./features/dashboard/users/user-profile-page/user-profile-page').then((m) => m.UserProfilePage),
      },
      {
        path: 'social-accounts',
        loadComponent: () =>
          import('./features/dashboard/social-accounts/social-accounts-page/social-accounts-page').then((m) => m.SocialAccountsPage),
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/dashboard/notifications/notifications-page/notifications-page').then((m) => m.NotificationsPage),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/dashboard/settings/settings-page/settings-page').then((m) => m.SettingsPage),
      },
      {
        path: 'billing',
        loadComponent: () =>
          import('./features/dashboard/billing/billing-page/billing-page').then((m) => m.BillingPage),
      },
      {
        path: 'help',
        loadComponent: () =>
          import('./features/dashboard/help-support/help-support-page/help-support-page').then((m) => m.HelpSupportPage),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
  },
];
