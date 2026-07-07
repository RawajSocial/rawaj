import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/landing/landing').then((m) => m.Landing),
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'sign-up',
    loadComponent: () => import('./features/auth/sign-up/sign-up').then((m) => m.SignUp),
  },
  {
    path: 'account-setup',
    loadComponent: () =>
      import('./features/account-setup/account-setup/account-setup').then((m) => m.AccountSetup),
  },
  {
    path: 'on-boarding',
    loadComponent: () =>
      import('./features/on-boarding/rawaj-onboarding/rawaj-onboarding').then((m) => m.RawajOnboarding),
  },
  {
    path: 'dashboard',
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
        path: 'ads',
        loadComponent: () =>
          import('./features/ads/ads-page/ads-page').then((m) => m.AdsPage),
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
    ],
  },
];
