import { Routes } from '@angular/router';
import { activationGuard } from '../core/guards/activation.guard';

/** Children of `/dashboard` — the regular tenant (agency/business owner) area.
 *  Routes gated by `activationGuard` require the user's OWN tenant to have completed its business
 *  info — content-gen/ads/my-media/my-projects/notifications/settings/help stay reachable
 *  regardless, so an invited member whose own tenant isn't activated can still work inside the
 *  tenant that invited them (see `activation.guard.ts`). */
export const userRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('../features/dashboard/crm-page/crm-page').then((m) => m.CrmPage),
  },
  {
    path: 'locked',
    loadComponent: () =>
      import('../features/dashboard/locked-page/locked-page').then((m) => m.LockedPage),
  },
  {
    path: 'brand-profiles',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/brand-profiles/brand-profiles-page/brand-profiles-page').then((m) => m.BrandProfilesPage),
  },
  {
    path: 'brand-profiles/new',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/brand-profiles/brand-profile-create-page/brand-profile-create-page').then((m) => m.BrandProfileCreatePage),
  },
  {
    path: 'campaigns',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/campaigns/campaigns-page/campaigns-page').then((m) => m.CampaignsPage),
  },
  {
    path: 'campaigns/:id',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/campaigns/campaign-detail-page/campaign-detail-page').then((m) => m.CampaignDetailPage),
  },
  {
    path: 'campaigns/:id/calendar',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/campaigns/campaign-calendar-page/campaign-calendar-page').then((m) => m.CampaignCalendarPage),
  },
  {
    path: 'campaigns/:id/content',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/campaigns/campaign-content-page/campaign-content-page').then((m) => m.CampaignContentPage),
  },
  {
    path: 'campaigns/:id/posts/:postId',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/campaigns/campaign-post-detail-page/campaign-post-detail-page').then((m) => m.CampaignPostDetailPage),
  },
  {
    path: 'ads',
    loadComponent: () =>
      import('../features/ads/ads-page/ads-page').then((m) => m.AdsPage),
  },
  {
    path: 'ads/:id',
    loadComponent: () =>
      import('../features/ads/ad-detail-page/ad-detail-page').then((m) => m.AdDetailPage),
  },
  {
    path: 'calendar',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/calendar/calendar-page/calendar-page').then((m) => m.CalendarPage),
  },
  {
    path: 'content-gen',
    loadComponent: () =>
      import('../features/content-gen/content-gen-page/content-gen-page').then((m) => m.ContentGenPage),
  },
  {
    path: 'my-media',
    loadComponent: () =>
      import('../features/my-media/my-media-page/my-media-page').then((m) => m.MyMediaPage),
  },
  {
    path: 'marketing-plan',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/marketing-plan/marketing-plan-page/marketing-plan-page').then((m) => m.MarketingPlanPage),
  },
  {
    path: 'my-projects',
    loadComponent: () =>
      import('../features/dashboard/my-projects/my-projects-page/my-projects-page').then((m) => m.MyProjectsPage),
  },
  {
    path: 'users',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/dashboard/users/users-page/users-page').then((m) => m.UsersPage),
  },
  {
    path: 'users/:id',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/dashboard/users/user-profile-page/user-profile-page').then((m) => m.UserProfilePage),
  },
  {
    path: 'notifications',
    loadComponent: () =>
      import('../features/dashboard/notifications/notifications-page/notifications-page').then((m) => m.NotificationsPage),
  },
  {
    path: 'social-accounts',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/dashboard/social-accounts-page/social-accounts-page').then((m) => m.SocialAccountsPage),
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('../features/dashboard/settings/settings-page/settings-page').then((m) => m.SettingsPage),
  },
  {
    path: 'billing',
    canActivate: [activationGuard],
    loadComponent: () =>
      import('../features/dashboard/billing/billing-page/billing-page').then((m) => m.BillingPage),
  },
  {
    path: 'help',
    loadComponent: () =>
      import('../features/dashboard/help-support/help-support-page/help-support-page').then((m) => m.HelpSupportPage),
  },
  {
    path: 'loading-test',
    loadComponent: () =>
      import('../features/dashboard/loading-test-page/loading-test-page').then((m) => m.LoadingTestPage),
  },
];
