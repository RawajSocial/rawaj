import { Routes } from '@angular/router';
import { userRoutes } from './routes/user.routes';
import { adminRoutes } from './routes/admin.routes';
import { guestGuard } from './core/guards/guest.guard';
import { authGuard, authCanMatch } from './core/guards/auth.guard';
import { adminGuard, adminCanMatch } from './core/guards/admin.guard';

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
    path: 'invite',
    loadComponent: () =>
      import('./features/auth/invite-login/invite-login').then((m) => m.InviteLogin),
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
    canMatch: [authCanMatch],
    loadComponent: () => import('./layout/user/user-layout').then((m) => m.UserLayout),
    children: userRoutes,
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    canMatch: [adminCanMatch],
    loadComponent: () => import('./layout/admin/admin-layout').then((m) => m.AdminLayout),
    children: adminRoutes,
  },
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
  },
];
