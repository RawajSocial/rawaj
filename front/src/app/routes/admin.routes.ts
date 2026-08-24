import { Routes } from '@angular/router';

/** Children of `/admin` — platform-wide administration area. */
export const adminRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('../features/admin/admin-overview-page/admin-overview-page').then((m) => m.AdminOverviewPage),
  },
  {
    path: 'users',
    loadComponent: () =>
      import('../features/admin/admin-users-page/admin-users-page').then((m) => m.AdminUsersPage),
  },
  {
    path: 'tenants',
    loadComponent: () =>
      import('../features/admin/admin-tenants-page/admin-tenants-page').then((m) => m.AdminTenantsPage),
  },
  {
    path: 'plans',
    loadComponent: () =>
      import('../features/admin/admin-plans-page/admin-plans-page').then((m) => m.AdminPlansPage),
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('../features/admin/admin-settings-page/admin-settings-page').then((m) => m.AdminSettingsPage),
  },
];
