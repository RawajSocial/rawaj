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
    path: 'invite',
    loadComponent: () =>
      import('./features/auth/invite-login/invite-login').then((m) => m.InviteLogin),
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
        path: 'campaigns/:id',
        loadComponent: () =>
          import('./features/campaigns/campaign-detail-page/campaign-detail-page').then((m) => m.CampaignDetailPage),
      },
      {
        path: 'campaigns/:id/calendar',
        loadComponent: () =>
          import('./features/campaigns/campaign-calendar-page/campaign-calendar-page').then((m) => m.CampaignCalendarPage),
      },
      {
        path: 'campaigns/:id/posts/:postId',
        loadComponent: () =>
          import('./features/campaigns/campaign-post-detail-page/campaign-post-detail-page').then((m) => m.CampaignPostDetailPage),
      },
      {
        path: 'ads',
        loadComponent: () =>
          import('./features/ads/ads-page/ads-page').then((m) => m.AdsPage),
      },
      {
        path: 'ads/:id',
        loadComponent: () =>
          import('./features/ads/ad-detail-page/ad-detail-page').then((m) => m.AdDetailPage),
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
        path: 'my-projects',
        loadComponent: () =>
          import('./features/dashboard/my-projects/my-projects-page/my-projects-page').then((m) => m.MyProjectsPage),
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
];
